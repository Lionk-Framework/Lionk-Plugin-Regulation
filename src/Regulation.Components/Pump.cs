// Copyright © 2024 Lionk Project

using Lionk.Core;
using Lionk.Core.Component;
using Lionk.Rpi.Gpio;
using Newtonsoft.Json;

namespace Regulation.Components;

/// <summary>
/// This class represents a pump.
/// </summary>
[NamedElement("Pump", "Pump with inversed PWM control")]
public class Pump : BaseExecutableComponent
{
    #region Public Events

    /// <summary>
    /// Event that is raised when the speed of the pump is changed.
    /// </summary>
    public event EventHandler? SpeedChanged;

    #endregion Public Events

    #region Private Fields

    private Guid _pwmPinId;

    private double _speed;

    private StandardPwmGpio? _pwmGpio;

    #endregion Private Fields

    #region Private Properties

    private double DutyCycle => 1 - Speed;

    #endregion Private Properties

    #region Public Properties

    /// <inheritdoc/>
    public override bool CanExecute
        => PwmGpio is not null
        && PwmGpio.CanExecute;

    /// <summary>
    /// Gets or sets the PWM GPIO.
    /// </summary>
    [JsonIgnore]
    public StandardPwmGpio? PwmGpio
    {
        get => _pwmGpio;
        set
        {
            if (_pwmGpio == value) return;
            PwmGpioId = value?.Id ?? Guid.Empty;
            _pwmGpio = value;
        }
    }

    /// <summary>
    /// Gets or sets the PWM pin id.
    /// </summary>
    public Guid PwmGpioId
    {
        get => _pwmPinId;
        set => SetField(ref _pwmPinId, value);
    }

    /// <summary>
    /// Gets or sets the speed of the pump.
    /// </summary>
    [JsonIgnore]
    public double Speed
    {
        get => Math.Round(_speed, 2);
        set
        {
            _speed = value;
            SpeedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    #endregion Public Properties

    #region Protected Methods

    /// <inheritdoc/>
    protected override void OnExecute(CancellationToken cancellationToken)
    {
        if (PwmGpio is null) return;
        base.OnExecute(cancellationToken);
        PwmGpio.DutyCycle = DutyCycle;
        PwmGpio.Execute();
    }

    #endregion Protected Methods

    #region Public Methods

    /// <inheritdoc/>
    public override void Abort()
    {
        if (PwmGpio is null) return;
        base.Abort();
        Speed = 0;
        PwmGpio.DutyCycle = DutyCycle;
        PwmGpio.Execute();
    }

    /// <summary>
    /// Initializes the component.
    /// </summary>
    public override void Dispose()
    {
        base.Dispose();
        PwmGpio?.Dispose();
    }

    #endregion Public Methods
}
