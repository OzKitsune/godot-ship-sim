using Godot;
using System;

public partial class Ship : CharacterBody2D
{
    private const string INPUT_FORWARD = "forward";
    private const string INPUT_BACKWARD = "backward";
    private const string INPUT_ROTATE_LEFT = "rotate_left";
    private const string INPUT_ROTATE_RIGHT = "rotate_right";

    private float _currentForwardThrust = 0.0f;
    private float _targetForwardThrust = 0.0f;
    private float _currentRotationThrust = 0.0f;
    private float _targetRotationThrust = 0.0f;
    private float _currentSpeed = 0.0f;

    [ExportCategory("Двигатели")]
    [Export] public float MainEnginePower { get; private set; } = 800.0f;
    [Export] public float RotationEnginePower { get; private set; } = 3.0f;
    [Export] public float MaxSpeed { get; private set; } = 500.0f;

    [ExportCategory("Инерция")]
    [Export] public float AccelerationResponse { get; private set; } = 2.0f;
    [Export] public float RotationResponse { get; private set; } = 5.0f;

    public override void _PhysicsProcess(double delta)
    {
        var deltaFloat = (float)delta;

        HandleInput(deltaFloat);
        ApplyInertiaToControls(deltaFloat);
        CalculateMovement(deltaFloat);

        MoveAndSlide();

        GD.Print(GetDebugInfo());
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

    private void ApplyInertiaToControls(float delta)
    {
        // Асимптотическое изменение тяги.
        _currentForwardThrust = Mathf.Lerp(_currentForwardThrust, _targetForwardThrust, AccelerationResponse * delta);

        // Асимптотическое изменение поворота.
        _currentRotationThrust = Mathf.Lerp(_currentRotationThrust, _targetRotationThrust, RotationResponse * delta);
    }

    private void CalculateMovement(float delta)
    {
        // Поворот корабля.
        float rotationAcceleration = _currentRotationThrust * RotationEnginePower;
        Rotation += rotationAcceleration * delta;

        // Расчёт ускорения вперёд.
        Vector2 forwardVector = Vector2.Up.Rotated(Rotation);  // Нос корабля
        float targetSpeed = _currentForwardThrust * MaxSpeed;

        // Асимптотическое приближение скорости к целевой.
        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, AccelerationResponse * delta);

        // Расчёт итоговой скорости.
        Vector2 newVelocity = forwardVector * _currentSpeed;

        // Применение скорости.
        Velocity = newVelocity;
    }

    private string GetDebugInfo()
    {
        return $"Скорость: {Velocity.Length():F1}\n" +
               $"Тяга: {_currentForwardThrust:F2}\n" +
               $"Поворот: {_currentRotationThrust:F2}\n" +
               $"Направление: {Mathf.RadToDeg(Rotation):F1}°";
    }
}
