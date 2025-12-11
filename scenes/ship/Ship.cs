using Godot;
using System;

public partial class Ship : CharacterBody2D
{
    [Signal]
    public delegate void AutopilotEngagedEventHandler(Vector2 target);
    [Signal]
    public delegate void AutopilotDisengagedEventHandler(bool reachedTarget);
    [Signal]
    public delegate void AutopilotTargetReachedEventHandler();

    private const string INPUT_FORWARD = "forward";
    private const string INPUT_BACKWARD = "backward";
    private const string INPUT_ROTATE_LEFT = "rotate_left";
    private const string INPUT_ROTATE_RIGHT = "rotate_right";

    private float _currentForwardThrust = 0.0f;
    private float _targetForwardThrust = 0.0f;
    private float _currentRotationThrust = 0.0f;
    private float _targetRotationThrust = 0.0f;
    private float _currentSpeed = 0.0f;

    private bool _autopilotEnabled = false;
    private Vector2 _autopilotTarget = Vector2.Zero;
    private PidController _rotationPID;
    private PidController _thrustPID;

    [ExportCategory("Двигатели")]
    [Export] public float MainEnginePower { get; private set; } = 800.0f;
    [Export] public float RotationEnginePower { get; private set; } = 3.0f;
    [Export] public float MaxSpeed { get; private set; } = 500.0f;
    [Export(PropertyHint.Range, "0, 1")] public float BrakingFactor { get; private set; } = 0.7f;

    [ExportCategory("Инерция")]
    [Export] public float AccelerationResponse { get; private set; } = 2.0f;
    [Export] public float RotationAccelerationResponse { get; private set; } = 1.0f;
    [Export] public float RotationBrakeResponse { get; private set; } = 2.0f;

    [ExportCategory("Автопилот - Настройки")]
    [Export] public float ArrivalThreshold = 50.0f;
    [Export] public float BrakingDistanceMultiplier = 1.5f;

    [ExportCategory("Автопилот - PID")]
    [ExportGroup("Поворот")]
    [Export] public float RotationP = 0.8f;
    [Export] public float RotationI = 0.05f;
    [Export] public float RotationD = 0.5f;

    [ExportGroup("Тяга")]
    [Export] public float ThrustP = 0.5f;
    [Export] public float ThrustI = 0.02f;
    [Export] public float ThrustD = 0.3f;

    public override void _Ready()
    {
        _rotationPID = new PidController(RotationP, RotationI, RotationD);
        _thrustPID = new PidController(ThrustP, ThrustI, ThrustD);
    }

    public override void _PhysicsProcess(double delta)
    {
        var deltaFloat = (float)delta;

        bool hasManualInput = CheckManualInput();

        if (hasManualInput && _autopilotEnabled)
        {
            DisableAutopilot(false);
        }

        if (_autopilotEnabled)
        {
            UpdateAutopilot(deltaFloat);
        }
        else
        {
            HandleInput(deltaFloat);
        }

        ApplyInertiaToControls(deltaFloat);
        CalculateMovement(deltaFloat);

        MoveAndSlide();
    }

    private bool CheckManualInput()
    {
        return Input.IsActionPressed(INPUT_FORWARD) ||
               Input.IsActionPressed(INPUT_BACKWARD) ||
               Input.IsActionPressed(INPUT_ROTATE_LEFT) ||
               Input.IsActionPressed(INPUT_ROTATE_RIGHT);
    }

    private void HandleInput(float delta)
    {
        // Управление тягой.
        if (Input.IsActionPressed(INPUT_FORWARD))
        {
            _targetForwardThrust = 1.0f;
        }
        else if (Input.IsActionPressed(INPUT_BACKWARD))
        {
            _targetForwardThrust = -1.0f;
        }
        else
        {
            _targetForwardThrust = 0.0f;
        }

        // Управление поворотом.
        if (Input.IsActionPressed(INPUT_ROTATE_LEFT))
        {
            _targetRotationThrust = -1.0f;
        }
        else if (Input.IsActionPressed(INPUT_ROTATE_RIGHT))
        {
            _targetRotationThrust = 1.0f;
        }
        else
        {
            _targetRotationThrust = 0.0f;
        }
    }

    private void UpdateAutopilot(float delta)
    {
        var shipPos = GlobalPosition;
        var toTarget = _autopilotTarget - shipPos;
        var distance = toTarget.Length();
        var direction = toTarget.Normalized();

        // Проверка достижения цели.
        if (distance <= ArrivalThreshold)
        {
            DisableAutopilot(true);
            return;
        }

        // Поворот к цели.
        var forward = GetForwardVector();
        var angleToTarget = forward.AngleTo(direction);
        float rotationOutput = _rotationPID.Update(0, angleToTarget, delta);
        _targetRotationThrust = -Mathf.Clamp(rotationOutput, -1.0f, 1.0f);

        // Управление скоростью.
        float desiredSpeed = CalculateDesiredSpeed(distance, delta);
        float currentSpeedTowardTarget = Velocity.Dot(direction);
        float thrustOutput = _thrustPID.Update(desiredSpeed, currentSpeedTowardTarget, delta);
        _targetForwardThrust = Mathf.Clamp(thrustOutput, -1.0f, 1.0f);
    }

    private float CalculateDesiredSpeed(float distance, float delta)
    {
        float brakingDistance = CalculateBrakingDistance(MaxSpeed);

        if (distance > brakingDistance * 2) // Не нужно тормозить.
        {
            return MaxSpeed;
        }
        else if (distance > brakingDistance) // Начинаем плавно снижать скорость.
        {
            float slowdownFactor = (distance - brakingDistance) / brakingDistance;
            return Mathf.Lerp(MaxSpeed * 0.3f, MaxSpeed, slowdownFactor);
        }
        else // В зоне торможения.
        {
            float minSpeed = 20.0f;
            return Mathf.Lerp(minSpeed, MaxSpeed * 0.3f, distance / brakingDistance);
        }
    }

    private float CalculateBrakingDistance(float currentSpeed)
    {
        return (currentSpeed * currentSpeed) / (2.0f * MainEnginePower * BrakingFactor) * BrakingDistanceMultiplier;
    }

    private void ApplyInertiaToControls(float delta)
    {
        // Асимптотическое изменение тяги.
        _currentForwardThrust = Mathf.Lerp(_currentForwardThrust, _targetForwardThrust, AccelerationResponse * delta);

        // Асимптотическое изменение поворота.
        // Определяем, нужно ли нам набирать или сбрасывать скорость поворота.
        float currentRotationAbs = Mathf.Abs(_currentRotationThrust);
        float targetRotationAbs = Mathf.Abs(_targetRotationThrust);

        // Если текущая скорость поворота больше целевой или целевая равна 0 - сбрасываем быстрее.
        if (currentRotationAbs > targetRotationAbs || Mathf.Abs(_targetRotationThrust) < 0.01f)
        {
            // Быстрая потеря скорости поворота (торможение).
            _currentRotationThrust = Mathf.Lerp(_currentRotationThrust, _targetRotationThrust, RotationBrakeResponse * delta);
        }
        else
        {
            // Медленный набор скорости поворота (ускорение).
            _currentRotationThrust = Mathf.Lerp(_currentRotationThrust, _targetRotationThrust, RotationAccelerationResponse * delta);
        }
    }

    private void CalculateMovement(float delta)
    {
        // Поворот корабля.
        float rotationAcceleration = _currentRotationThrust * RotationEnginePower;
        Rotation += rotationAcceleration * delta;

        // Расчёт ускорения вперёд.
        Vector2 forwardVector = Vector2.Up.Rotated(Rotation);
        float targetSpeed;

        if (_targetForwardThrust != 0)
        {
            targetSpeed = _currentForwardThrust * MaxSpeed;
        }
        else
        {
            targetSpeed = _currentForwardThrust * BrakingFactor;
        }

        // Асимптотическое приближение скорости к целевой.
        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, AccelerationResponse * delta);

        // Расчёт итоговой скорости.
        Vector2 newVelocity = forwardVector * _currentSpeed;
        
        // Применение скорости.
        Velocity = newVelocity;
    }

    /// <summary>
    /// Включить автопилот и установить цель.
    /// </summary>
    public void EnableAutopilot(Vector2 target)
    {
        _autopilotEnabled = true;
        _autopilotTarget = target;

        _rotationPID.Reset();
        _thrustPID.Reset();

        EmitSignal(SignalName.AutopilotEngaged, target);

        GD.Print($"Автопилот включен. Цель: {target}");
    }

    /// <summary>
    /// Отключить автопилот.
    /// </summary>
    /// <param name="targetReached">Достигнута ли цель</param>
    public void DisableAutopilot(bool targetReached = false)
    {
        if (!_autopilotEnabled) return;

        _autopilotEnabled = false;
        _autopilotTarget = Vector2.Zero;

        _targetForwardThrust = 0;
        _targetRotationThrust = 0;

        EmitSignal(SignalName.AutopilotDisengaged, targetReached);

        if (targetReached)
        {
            EmitSignal(SignalName.AutopilotTargetReached);
            GD.Print("Автопилот: цель достигнута!");
        }
        else
        {
            GD.Print("Автопилот отключен");
        }
    }

    public Vector2 GetForwardVector()
    {
        return Vector2.Up.Rotated(Rotation);
    }

    /// <summary>
    /// Получить информацию о состоянии автопилота
    /// </summary>
    public string GetAutopilotInfo()
    {
        if (!_autopilotEnabled)
        {
            return "Автопилот: ВЫКЛ";
        }

        var distance = GlobalPosition.DistanceTo(_autopilotTarget);
        var direction = (_autopilotTarget - GlobalPosition).Normalized();
        var angle = Mathf.RadToDeg(GetForwardVector().AngleTo(direction));

        return $"Автопилот: ВКЛ\n" +
               $"Цель: {_autopilotTarget}\n" +
               $"Дистанция: {distance:F1}\n" +
               $"Угол к цели: {angle:F1}°\n" +
               $"Скорость: {Velocity.Length():F1}";
    }

    public string GetDebugInfo()
    {
        string autopilotStatus = _autopilotEnabled ? "ВКЛ" : "ВЫКЛ";

        return $"Скорость: {Velocity.Length():F1}\n" +
               $"Тяга: {_currentForwardThrust:F2}\n" +
               $"Поворот: {_currentRotationThrust:F2}\n" +
               $"Направление: {Mathf.RadToDeg(Rotation):F1}°\n" +
               $"Автопилот: {autopilotStatus}";
    }
}
