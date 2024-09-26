// Copyright © 2024 Lionk Project

using Lionk.Core;
using Lionk.Core.Component;
using Lionk.Log;
using Lionk.TemperatureSensor;
using Newtonsoft.Json;

namespace Regulation.Components;

/// <summary>
/// This class is used to represent a regulation.
/// </summary>
[NamedElement("Regulation Engine", "This component is used to regulate the temperature of the system.")]
public class RegulationEngine : BaseCyclicComponent
{
    #region Private Fields
    private const int ValveTemperatureHysteresis = 2;

    private readonly IStandardLogger? _logger = LogService.CreateLogger("RegulationEngineLogs");

    private Accumulator? _accumulator1;
    private Guid _accumulator1Id;
    private Accumulator? _accumulator2;
    private Guid _accumulator2Id;
    private Chimney? _chimney;
    private Guid _chimneyId;
    private BaseTemperatureSensor? _hotWaterSensor;
    private Guid _hotWaterSensorId;
    private int _minPumpSpeed = 30;
    private ThreeWayValve? _valve;
    private Guid _valveId;
    #endregion Private Fields

    #region Public Properties

    /// <summary>
    /// Gets or sets the accumulator 1.
    /// </summary>
    [JsonIgnore]
    public Accumulator? Accumulator1
    {
        get => _accumulator1;
        set
        {
            _accumulator1 = value;
            if (_accumulator1 is null) return;
            Accumulator1Id = _accumulator1.Id;
        }
    }

    /// <summary>
    /// Gets or sets the accumulator 1 id.
    /// </summary>
    public Guid Accumulator1Id
    {
        get => _accumulator1Id;
        set => SetField(ref _accumulator1Id, value);
    }

    /// <summary>
    /// Gets or sets the accumulator 2.
    /// </summary>
    [JsonIgnore]
    public Accumulator? Accumulator2
    {
        get => _accumulator2;
        set
        {
            _accumulator2 = value;
            if (_accumulator2 is null) return;
            Accumulator2Id = _accumulator2.Id;
        }
    }

    /// <summary>
    /// Gets or sets the accumulator 2 id.
    /// </summary>
    public Guid Accumulator2Id
    {
        get => _accumulator2Id;
        set => SetField(ref _accumulator2Id, value);
    }

    /// <inheritdoc/>
    public override bool CanExecute => true;

    /// <summary>
    /// Gets or sets the chimney.
    /// </summary>
    [JsonIgnore]
    public Chimney? Chimney
    {
        get => _chimney;
        set
        {
            _chimney = value;
            if (_chimney is null) return;
            ChimneyId = _chimney.Id;
        }
    }

    /// <summary>
    /// Gets or sets the chimney id.
    /// </summary>
    public Guid ChimneyId
    {
        get => _chimneyId;
        set => SetField(ref _chimneyId, value);
    }

    /// <summary>
    /// Gets or sets the Hot Water sensor.
    /// </summary>
    [JsonIgnore]
    public BaseTemperatureSensor? HotWaterSensor
    {
        get => _hotWaterSensor;
        set
        {
            _hotWaterSensor = value;
            if (_hotWaterSensor is null) return;
            HotWaterSensorId = _hotWaterSensor.Id;
        }
    }

    /// <summary>
    /// Gets or sets the hot water sensor id.
    /// </summary>
    public Guid HotWaterSensorId
    {
        get => _hotWaterSensorId;
        set => SetField(ref _hotWaterSensorId, value);
    }

    /// <summary>
    /// Gets or sets the minimum pump speed.
    /// </summary>
    public int MinPumpSpeed
    {
        get => _minPumpSpeed;
        set => SetField(ref _minPumpSpeed, value);
    }

    /// <summary>
    /// Gets or sets the three way valve.
    /// </summary>
    [JsonIgnore]
    public ThreeWayValve? Valve
    {
        get => _valve;
        set
        {
            _valve = value;
            if (_valve is null) return;
            ValveId = _valve.Id;
        }
    }

    /// <summary>
    /// Gets or sets the valve id.
    /// </summary>
    public Guid ValveId
    {
        get => _valveId;
        set => SetField(ref _valveId, value);
    }
    #endregion Public Properties

    /// <summary>
    /// Initializes a new instance of the <see cref="RegulationEngine"/> class.
    /// </summary>
    public RegulationEngine() => Period = TimeSpan.FromSeconds(3);

    /// <inheritdoc/>
    protected override void OnExecute(CancellationToken cancellationToken)
    {
        if (!CanExecute) return;
        ChimneyModulation();
        ValveManagment();
    }

    /// <summary>
    /// Method to manage the Chimney modulation.
    /// </summary>
    private void ChimneyModulation()
    {
        if (Chimney is null)
        {
            Console.WriteLine("Chimney is null");
            _logger?.Log(LogSeverity.Warning, "Chimney is null");
            return;
        }

        double chimneyTemperature = Chimney.GetTemperature();
        double outputTemperature = Chimney.GetOutputTemp();
        double accumulator2BottomSensor = Accumulator2?.BottomSensor?.GetTemperature() ?? 0;
        double minPumpSpeed = (double)MinPumpSpeed / 100.0;

        if (chimneyTemperature < Chimney.MinTemperaturePumpThresold
            || accumulator2BottomSensor + 5 > chimneyTemperature)
        {
            Chimney.SetPumpSpeed(0);
            _logger?.Log(LogSeverity.Information, $"Pump is set to 0% because chimney temperature is {chimneyTemperature} and accumulator2 bottom sensor is {accumulator2BottomSensor}");
        }
        else
        {
            if (Chimney.State is ChimneyState.HeatingDown)
            {
                Chimney.SetPumpSpeed(1);
                _logger?.Log(LogSeverity.Information, $"Pump is set to 100% because chimney temperature is heating down");
            }
            else if (Chimney.State is ChimneyState.HeatingUp || Chimney.State is ChimneyState.Stabilized)
            {
                double pumpSpeed = minPumpSpeed +
                    ((chimneyTemperature - Chimney.MinTemperaturePumpThresold) /
                    (Chimney.MaxTemperaturePumpThresold - Chimney.MinTemperaturePumpThresold)
                    * (1 - minPumpSpeed));

                Chimney.SetPumpSpeed(pumpSpeed);
                _logger?.Log(LogSeverity.Information, $"Pump is set to {pumpSpeed * 100}% because chimney temperature is heating up or stabilized");
            }
        }
    }

    private void ValveManagment()
    {
        if (Valve is null) return;
        if (Valve.State is ValveState.Undefined) Valve.ValveOrder = ValveOrder.Initialize;
        if (Chimney is null || Accumulator1 is null) return;

        ValveOrder previousOrder = Valve.ValveOrder;
        double chimneyTemperature = Chimney.GetTemperature();
        double accumulator1TopSensor = Accumulator1.GetTopTemperature();

        if (chimneyTemperature is double.NaN || accumulator1TopSensor is double.NaN) return;

        if (chimneyTemperature > accumulator1TopSensor + ValveTemperatureHysteresis)
        {
            Valve.ValveOrder = ValveOrder.Close;
        }
        else
        {
            Valve.ValveOrder = ValveOrder.Open;
        }

        if (previousOrder != Valve.ValveOrder && Valve.CanExecute)
        {
            Valve.Execute();
        }
    }
}
