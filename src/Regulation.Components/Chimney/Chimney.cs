﻿// Copyright © 2024 Lionk Project

using Lionk.Core;
using Lionk.Core.Component;
using Lionk.Core.DataModel;
using Lionk.TemperatureSensor;
using Newtonsoft.Json;

namespace Regulation.Components;

/// <summary>
/// This class represents a chimney.
/// </summary>
[NamedElement("Chimney", "This component is used to represent a chimney.")]
public class Chimney : BaseComponent
{
    #region Private Fields

    private const int MaxHistorySize = 5;
    private const int SpecificHeatCapacity = 4180;
    private readonly Queue<double> _temperatureHistory = new();

    private BaseTemperatureSensor? _chimneySensor;
    private Guid _chimneySensorId;
    private FlowMeter? _flowMeter;
    private Guid _flowMeterId;
    private BaseTemperatureSensor? _inputSensor;
    private Guid _inputSensorId;
    private BaseTemperatureSensor? _outputSensor;
    private Guid _outputSensorId;
    private Pump? _pump;
    private Guid _pumpId;
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
    /// Gets or sets the considered fire threshold.
    /// </summary>
    public double ConsideredFireThreshold { get; set; } = 30;

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
    /// Gets the current power of the chimney.
    /// </summary>
    public double CurrentPower { get; private set; }

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
    /// Gets or sets the maximum temperature.
    /// </summary>
    public double MaxTemperature { get; set; } = 85.0;

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
    /// Gets the minimum temperature.
    /// </summary>
    [JsonIgnore]
    public ChimneyState State { get; private set; } = ChimneyState.Off;

    #endregion Public Properties

    #region Private Methods

    private void DefineState()
    {
        double currentTemperature = GetTemperature();
        if (currentTemperature is double.NaN)
        {
            State = ChimneyState.Error;
            return;
        }
        else if (currentTemperature > MaxTemperature)
        {
            _temperatureHistory.Enqueue(currentTemperature);
            State = ChimneyState.AtFullPower;
            return;
        }
        else if (currentTemperature < ConsideredFireThreshold)
        {
            _temperatureHistory.Enqueue(currentTemperature);
            State = ChimneyState.Off;
            return;
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
                if (averageDelta > 0.5)
                {
                    State = ChimneyState.HeatingUp;
                }
                else if (averageDelta < -0.5)
                {
                    State = ChimneyState.HeatingDown;
                }
                else
                {
                    State = ChimneyState.Stabilized;
                }
            }
        }
    }

    private void OnNewMeterValueAvailable(object? sender, MeasureEventArgs<int> e) => CurrentPower = CalculatePower();

    private void OnNewTemperatureAvailable(object? sender, MeasureEventArgs<double> e)
    {
        DefineState();
        if (State is ChimneyState.Off && (Pump is null || !Pump.CanExecute))
        {
            // TODO Notification info
            Console.WriteLine("Pump is not available - Info");
        }
        else if (Pump is null || !Pump.CanExecute)
        {
            // TODO Notification High severitiy
            Console.WriteLine("Pump is not available - High Severity");
        }
        else if (State is ChimneyState.Error or ChimneyState.AtFullPower)
        {
            Pump.Speed = 1;
            Pump.Execute();
        }

        CurrentPower = CalculatePower();
    }

    #endregion Private Methods

    #region Public Methods

    /// <summary>
    /// Methode to get the output temperature.
    /// </summary>
    /// <returns> The output temperature. </returns>
    public double GetInputTemp()
    {
        if (InputSensor is null) return double.NaN;
        return InputSensor.GetTemperature();
    }

    /// <summary>
    /// Methode to get the output temperature.
    /// </summary>
    /// <returns> The output temperature. </returns>
    public double GetOutputTemp()
    {
        if (OutputSensor is null) return double.NaN;
        return OutputSensor.GetTemperature();
    }

    /// <summary>
    /// Methode to set the pump speed.
    /// </summary>
    /// <param name="speed"> The speed of the pump between 0 and 1. </param>
    public void SetPumpSpeed(double speed)
    {
        if (Pump is null || !Pump.CanExecute)
        {
            // TODO Notification
            Console.WriteLine("Pump is not available");
            return;
        }

        if (State is ChimneyState.AtFullPower or ChimneyState.Error) return;

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
    }

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
    /// Gets the temperature of the chimney.
    /// </summary>
    /// <returns> The temperature of the chimney. </returns>
    public double GetTemperature()
    {
        if (ChimneySensor is null) return double.NaN;
        return ChimneySensor.GetTemperature();
    }

    /// <summary>
    /// Gets the power of the chimney as a string.
    /// </summary>
    /// <returns> The power of the chimney as a string. </returns>
    public string GetCurrentPowerString() => CurrentPower.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ') + " W";

    #endregion Public Methods
}
