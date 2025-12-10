using Godot;
using System;

/// <summary>
/// Простой PID контроллер.
/// </summary>
public class PidController
{
    /// <summary>
    /// Пропорциональный коэффициент
    /// </summary>
    private float _kp;

    /// <summary>
    /// Интегральный коэффициент.
    /// </summary>
    private float _ki;

    /// <summary>
    /// Дифференциальный коэффициент.
    /// </summary>
    private float _kd;

    private float _integral;
    private float _previousError;
    private float _outputLimit;

    public PidController(float kp, float ki, float kd)
    {
        _kp = kp;
        _ki = ki;
        _kd = kd;

        _outputLimit = 1.0f;
    }

    public float OutputLimit
    {
        get 
        {
            return _outputLimit;
        }
        set 
        {
            _outputLimit = Mathf.Max(0.1f, value);
        } 
    }

    /// <summary>
    /// Обновить PID контроллер.
    /// </summary>
    /// <param name="setpoint">Целевое значение.</param>
    /// <param name="actual">Текущее значение.</param>
    /// <param name="dt">Время с прошлого обновления.</param>
    /// <returns>Выходное значение PID.</returns>
    public float Update(float setpoint, float actual, float dt)
    {
        float error = setpoint - actual;

        // Пропорциональная составляющая.
        float p = _kp * error;

        // Интегральная составляющая (с насыщением).
        _integral += error * dt;
        _integral = Mathf.Clamp(_integral, -_outputLimit / _ki, _outputLimit / _ki);
        float i = _ki * _integral;

        // Дифференциальная составляющая.
        float derivative = (error - _previousError) / dt;
        float d = _kd * derivative;

        _previousError = error;

        // Суммируем и ограничиваем выход.
        float output = p + i + d;
        return Mathf.Clamp(output, -_outputLimit, _outputLimit);
    }

    /// <summary>
    /// Сбросить накопленные значения.
    /// </summary>
    public void Reset()
    {
        _integral = 0;
        _previousError = 0;
    }
}
