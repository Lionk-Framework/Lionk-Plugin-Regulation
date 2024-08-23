// Copyright © 2024 Lionk Project

using Lionk.Core.Component;
using Lionk.Rpi.Gpio;

namespace Regulation.Components;

/// <summary>
/// This class represents a pump.
/// </summary>
public class Pump : BaseExecutableComponent
{
    /// <summary>
    /// Gets or sets the speed of the pump.
    /// </summary>
    public double Speed { get; set; }

    private Guid _pwmPinId;
    private IComponentService _componentService;
    private StandardPwmGpio? _pwmGpio;

    /// <summary>
    /// Initializes a new instance of the <see cref="Pump"/> class.
    /// </summary>
    /// <param name="componentService"> The component service. </param>
    public Pump(IComponentService componentService) => _componentService = componentService;

    /// <summary>
    /// Gets or sets the PWM pin id.
    /// </summary>
    public Guid PwmPinId
    {
        get => _pwmPinId;
        set
        {
            _pwmGpio = (StandardPwmGpio?)_componentService.GetInstanceByID(value);
            SetField(ref _pwmPinId, value);
        }
    }

    /// <inheritdoc/>
    public override bool CanExecute => _pwmGpio != null;

    /// <inheritdoc/>
    protected override void OnExecute(CancellationToken cancellationToken)
    {
        base.OnExecute(cancellationToken);
        if (_pwmGpio is null) return;
        _pwmGpio.DutyCycle = Speed;
    }
}
