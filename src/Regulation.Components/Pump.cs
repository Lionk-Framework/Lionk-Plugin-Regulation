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
    #region Private Fields

    private Guid _pwmPinId;

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
    public StandardPwmGpio? PwmGpio { get; set; }

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
    public double Speed { get; set; }

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
