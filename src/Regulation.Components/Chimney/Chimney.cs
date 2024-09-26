// Copyright © 2024 Lionk Project

using Lionk.Core;
using Lionk.Core.Component;
using Lionk.Core.DataModel;
using Lionk.Log;
using Lionk.Rpi.Gpio;
using Lionk.TemperatureSensor;
using Newtonsoft.Json;

namespace Regulation.Components;

/// <summary>
/// This class represents a chimney.
/// </summary>
[NamedElement("Chimney", "This component is used to represent a chimney.")]
public class Chimney : BaseComponent
{
    #region Public Events

    /// <summary>
    /// Initializes a new instance of the <see cref="Chimney"/> class.
    /// </summary>
    public Chimney() => StateChanged += OnStateChanged;

    /// <summary>
    /// Event raised when the state of the chimney changes.
    /// </summary>
    public event EventHandler? StateChanged;
    #endregion Public Events

    #region Private Fields
    private const int SpecificHeatCapacity = 4180;
    private readonly Queue<double> _temperatureHistory = new();
    private readonly IStandardLogger? _logger = LogService.CreateLogger("ChimneyLogs");

    private BaseTemperatureSensor? _chimneySensor;
    private Guid _chimneySensorId;

    private OutputGpio? _chimneySensorPower;
    private Guid _chimneySensorPowerId;

    private FlowMeter? _flowMeter;
    private Guid _flowMeterId;

    private BaseTemperatureSensor? _inputSensor;
    private Guid _inputSensorId;

    private BaseTemperatureSensor? _outputSensor;
    private Guid _outputSensorId;

    private Pump? _pump;
    private Guid _pumpId;

    private int _maxResetCount = 10;
    private int _resetCount = 0;
    private int _totalResetCount = 0;
    private double _consideredFireThreshold = 30;
    private double _minTemperaturePumpThresold = 45;
    private double _maxTemperaturePumpThresold = 85;
    private int _maxHistorySize = 10;

    #endregion Private Fields

    #region Public Properties

    /// <summary>
    /// Gets or sets the chimney sensor.
    /// </summary>
    [JsonIgnore]
    public BaseTemperatureSensor? ChimneySensor
    {
        get => _chimneySensor;
        set
        {
            _chimneySensor = value;
            if (_chimneySensor is null) return;
            _chimneySensorId = _chimneySensor.Id;
            _chimneySensor.NewValueAvailable += OnNewTemperatureAvailable;
        }
    }

    /// <summary>
    /// Gets or sets the input temperature sensor id.
    /// </summary>
    public Guid ChimneySensorId
    {
        get => _chimneySensorId;
        set => SetField(ref _chimneySensorId, value);
    }

    /// <summary>
    /// Gets or sets the chimney sensor power.
    /// </summary>
    [JsonIgnore]
    public OutputGpio? ChimneySensorPower
    {
        get => _chimneySensorPower;
        set
        {
            SetField(ref _chimneySensorPower, value);
            if (_chimneySensorPower is null) return;
            ChimneySensorPowerId = _chimneySensorPower.Id;
            _chimneySensorPower.PinValue = 1;
            _chimneySensorPower.Execute();
            _chimneySensorPower.NewValueAvailable += OnSensorPowerChanged;
        }
    }

    /// <summary>
    /// Gets or sets the chimney sensor power id.
    /// </summary>
    public Guid ChimneySensorPowerId
    {
        get => _chimneySensorPowerId;
        set => SetField(ref _chimneySensorPowerId, value);
    }

    /// <summary>
    /// Gets or sets the considered fire threshold.
    /// </summary>
    public double ConsideredFireThreshold
    {
        get => _consideredFireThreshold;
        set => SetField(ref _consideredFireThreshold, value);
    }

    /// <summary>
    /// Gets or sets the minimum temperature to start the pump.
    /// </summary>
    public double MinTemperaturePumpThresold
    {
        get => _minTemperaturePumpThresold;
        set => SetField(ref _minTemperaturePumpThresold, value);
    }

    /// <summary>
    /// Gets or sets the maximum temperature to start the pump.
    /// </summary>
    public double MaxTemperaturePumpThresold
    {
        get => _maxTemperaturePumpThresold;
        set => SetField(ref _maxTemperaturePumpThresold, value);
    }

    /// <summary>
    /// Gets the current power of the chimney.
    /// </summary>
    [JsonIgnore]
    public double CurrentPower { get; private set; }

    /// <summary>
    /// Gets or sets the flow meter.
    /// </summary>
    [JsonIgnore]
    public FlowMeter? FlowMeter
    {
        get => _flowMeter;
        set
        {
            _flowMeter = value;
            if (_flowMeter is null) return;
            FlowMeterId = _flowMeter.Id;
            _flowMeter.NewValueAvailable += OnNewMeterValueAvailable;
        }
    }

    /// <summary>
    /// Gets or sets the chimney sensor.
    /// </summary>
    public Guid FlowMeterId
    {
        get => _flowMeterId;
        set => SetField(ref _flowMeterId, value);
    }

    /// <summary>
    /// Gets or sets the input temperature.
    /// </summary>
    [JsonIgnore]
    public BaseTemperatureSensor? InputSensor
    {
        get => _inputSensor;
        set
        {
            _inputSensor = value;
            if (_inputSensor is null) return;
            _inputSensorId = _inputSensor.Id;
            _inputSensor.NewValueAvailable += OnNewTemperatureAvailable;
        }
    }

    /// <summary>
    /// Gets or sets the output sensor id.
    /// </summary>
    public Guid InputSensorId
    {
        get => _inputSensorId;
        set => SetField(ref _inputSensorId, value);
    }

    /// <summary>
    /// Gets or sets the maximum reset count.
    /// </summary>
    public int MaxResetCount
    {
        get => _maxResetCount;
        set => SetField(ref _maxResetCount, value);
    }

    /// <summary>
    /// Gets or sets the maximum temperature.
    /// </summary>
    public double MaxTemperature { get; set; } = 85.0;

    /// <summary>
    /// Gets or sets the maximum history size.
    /// </summary>
    public int MaxHistorySize
    {
        get => _maxHistorySize;
        set => SetField(ref _maxHistorySize, value);
    }

    /// <summary>
    /// Gets or sets the ouput temperature.
    /// </summary>
    [JsonIgnore]
    public BaseTemperatureSensor? OutputSensor
    {
        get => _outputSensor;
        set
        {
            _outputSensor = value;
            if (_outputSensor is null) return;
            _outputSensorId = _outputSensor.Id;
            _outputSensor.NewValueAvailable += OnNewTemperatureAvailable;
        }
    }

    /// <summary>
    /// Gets or sets the input sensor id.
    /// </summary>
    public Guid OutputSensorId
    {
        get => _outputSensorId;
        set => SetField(ref _outputSensorId, value);
    }

    /// <summary>
    /// Gets or sets the pump.
    /// </summary>
    [JsonIgnore]
    public Pump? Pump
    {
        get => _pump;
        set
        {
            _pump = value;
            if (_pump is null) return;
            _pumpId = _pump.Id;
        }
    }

    /// <summary>
    /// Gets or sets the pump id.
    /// </summary>
    public Guid PumpId
    {
        get => _pumpId;
        set => SetField(ref _pumpId, value);
    }

    /// <summary>
    /// Gets the reset counter.
    /// </summary>
    public int ResetCount => _resetCount;

    /// <summary>
    /// Gets the minimum temperature.
    /// </summary>
    [JsonIgnore]
    public ChimneyState State { get; private set; } = ChimneyState.Off;

    #endregion Public Properties

    #region Private Methods

    private void DefineState()
    {
        double currentTemperature = GetTemperature();
        double inputTemperature = GetInputTemp();
        double outputTemperature = GetOutputTemp();
        ChimneyState oldState = State;

        if (currentTemperature is double.NaN)
        {
            State = ChimneyState.Error;
            _logger?.Log(LogSeverity.Error, "Chimney sensor is in error");
        }
        else if (currentTemperature > MaxTemperature)
        {
            _temperatureHistory.Enqueue(currentTemperature);
            State = ChimneyState.AtFullPower;
        }
        else if (currentTemperature < ConsideredFireThreshold)
        {
            _temperatureHistory.Enqueue(currentTemperature);
            State = ChimneyState.Off;
        }
        else
        {
            _temperatureHistory.Enqueue(currentTemperature);
            if (_temperatureHistory.Count > MaxHistorySize)
            {
                _temperatureHistory.Dequeue();
            }

            if (_temperatureHistory.Count >= 2)
            {
                double historyAverage = _temperatureHistory.Average();
                double averageDelta = currentTemperature - historyAverage;
                if (outputTemperature is double.NaN || inputTemperature is double.NaN)
                {
                    State = ChimneyState.Undefined;
                }
                else
                {
                    if (averageDelta > 0.5)
                    {
                        State = ChimneyState.HeatingUp;
                    }
                    else if (averageDelta < 0)
                    {
                        State = ChimneyState.HeatingDown;
                    }
                    else if (averageDelta >= 0 && averageDelta <= 0.5 && oldState is not ChimneyState.HeatingDown)
                    {
                        State = ChimneyState.Stabilized;
                    }
                }
            }
        }

        if (oldState != State)
        {
            StateChanged?.Invoke(this, new EventArgs());
        }
    }

    private void OnNewMeterValueAvailable(object? sender, MeasureEventArgs<int> e) => CurrentPower = CalculatePower();

    private void OnNewTemperatureAvailable(object? sender, MeasureEventArgs<double> e)
    {
        DefineState();
        if (State is ChimneyState.Off && (Pump is null || !Pump.CanExecute))
        {
            // TODO Notification info
            _logger?.Log(LogSeverity.Information, "Pump is not available");
        }
        else if (Pump is null || !Pump.CanExecute)
        {
            // TODO Notification High severitiy
            _logger?.Log(LogSeverity.Warning, "Pump is not available");
            Console.WriteLine("Pump is not available - High Severity");
        }
        else if (State is ChimneyState.Error or ChimneyState.AtFullPower)
        {
            Pump.Speed = 1;
            Pump.Execute();
        }

        CurrentPower = CalculatePower();
    }

    private void OnSensorPowerChanged(object? sender, MeasureEventArgs<int> e)
    {
        // do nothing
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        // Do nothing
    }
    #endregion Private Methods

    #region Public Methods

    /// <summary>
    /// Methode to calculate the power of the chimney.
    /// </summary>
    /// <returns> The power of the chimney. </returns>
    public double CalculatePower()
    {
        if (FlowMeter is null || InputSensor is null || OutputSensor is null)
            return 0;

        double flowRateInLiterPerSecond = FlowMeter.GetAverageFlowRateLps();

        double tempDifference = GetOutputTemp() - GetInputTemp();

        double power = flowRateInLiterPerSecond * SpecificHeatCapacity * tempDifference;

        return Math.Round(power, 1);
    }

    /// <summary>
    /// Gets the power of the chimney as a string.
    /// </summary>
    /// <returns> The power of the chimney as a string. </returns>
    /// <remarks> If the pump is off or the input temperature is higher than the output temperature, the method returns "-". </remarks>
    public string GetCurrentPowerString()
    {
        if (Pump is null
            || Pump.Speed == 0
            || GetInputTemp() > GetOutputTemp()) return "-";

        return CurrentPower.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ') + " W";
    }

    /// <summary>
    /// Methode to get the output temperature.
    /// </summary>
    /// <returns> The output temperature. </returns>
    public double GetInputTemp()
    {
        if (InputSensor is null || InputSensor.IsInError)
        {
            InputSensor?.Reset();
            return double.NaN;
        }

        return InputSensor.GetTemperature();
    }

    /// <summary>
    /// Methode to get the output temperature.
    /// </summary>
    /// <returns> The output temperature. </returns>
    public double GetOutputTemp()
    {
        if (OutputSensor is null || OutputSensor.IsInError)
        {
            {
                OutputSensor?.Reset();
                return double.NaN;
            }
        }

        return OutputSensor.GetTemperature();
    }

    /// <summary>
    /// Gets the temperature of the chimney.
    /// </summary>
    /// <returns> The temperature of the chimney. </returns>
    public double GetTemperature()
    {
        if (ChimneySensor is null) return double.NaN;
        if (ChimneySensor.IsInError)
        {
            if (_resetCount < MaxResetCount)
            {
                ResetChimneySensor();
            }
            else
            {
                return double.NaN;
            }
        }

        _resetCount = 0;
        return ChimneySensor.GetTemperature();
    }

    /// <summary>
    /// Resets the chimney sensor by turning it off and on and reseting the sensor if is in error.
    /// </summary>
    public void ResetChimneySensor()
    {
        if (ChimneySensorPower is null) return;
        _resetCount++;
        _totalResetCount++;
        ChimneySensorPower.PinValue = 0;
        ChimneySensorPower.Execute();
        Thread.Sleep(300);
        ChimneySensorPower.PinValue = 1;
        ChimneySensorPower.Execute();
        if (ChimneySensor is not null && ChimneySensor.IsInError) ChimneySensor.Reset();
        _logger?.Log(LogSeverity.Information, $"Chimney sensor has been reset current count: {_resetCount} - total count {_totalResetCount}");
    }

    /// <summary>
    /// Methode to reset the total reset count.
    /// </summary>
    public void ResetTotalResetCount() => _totalResetCount = 0;

    /// <summary>
    /// Methode to set the pump speed.
    /// </summary>
    /// <param name="speed"> The speed of the pump between 0 and 1. </param>
    public void SetPumpSpeed(double speed)
    {
        if (Pump is null || !Pump.CanExecute)
        {
            // TODO Notification
            Console.WriteLine("Pump is not available or can't be executed");
            _logger?.Log(LogSeverity.Warning, "Pump is not available or can't be executed");
            return;
        }

        if (State is ChimneyState.AtFullPower or ChimneyState.Error or ChimneyState.Undefined)
        {
            speed = 1;
            if (State is ChimneyState.Error)
            {
                Console.WriteLine("Chimney is in Error");
                _logger?.Log(LogSeverity.Error, "Chimney is in Error");
            }

            if (State is ChimneyState.Undefined)
            {
                _logger?.Log(LogSeverity.Warning, "Chimney is in Undefined state");
                Console.WriteLine("Chimney is in Undefined state");
            }
        }

        if (speed > 1)
        {
            speed = 1;
        }
        else if (speed < 0)
        {
            speed = 0;
        }

        Pump.Speed = speed;
        Pump.Execute();

        if (FlowMeter is not null) FlowMeter.Enable = speed > 0;
    }
    #endregion Public Methods
}
