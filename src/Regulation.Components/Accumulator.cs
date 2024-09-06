// Copyright © 2024 Lionk Project

using Lionk.Core;
using Lionk.Core.Component;
using Lionk.Core.DataModel;
using Lionk.TemperatureSensor;
using Newtonsoft.Json;

namespace Regulation.Components;

/// <summary>
/// This class is used to represent an accumulator that store hot water.
/// </summary>
[NamedElement("Accumulator", "This component is used to represent an accumulator that store hot water.")]
public class Accumulator : BaseComponent
{
    #region Public Events

    /// <summary>
    /// Event raised when the temperature changed.
    /// </summary>
    public event EventHandler? TemperatureChanged;

    #endregion Public Events

    #region Private Fields

    private Guid _bottomSensorId;
    private double _maxTemp = 85.0;
    private Guid _middleSensorId;
    private double _minTemp = 20.0;
    private Guid _topSensorId;

    #endregion Private Fields

    #region Public Properties

    private BaseTemperatureSensor? _bottomSensor;
    private BaseTemperatureSensor? _middleSensor;
    private BaseTemperatureSensor? _topSensor;

    /// <summary>
    /// Gets or sets the bottom sensor.
    /// </summary>
    [JsonIgnore]
    public BaseTemperatureSensor? BottomSensor
    {
        get => _bottomSensor;
        set
        {
            _bottomSensor = value;
            if (_bottomSensor is not null)
            {
                _bottomSensor.NewValueAvailable += OnNewTemperatureAvailable;
            }
        }
    }

    /// <summary>
    /// Gets or sets the bottom part sensor id.
    /// </summary>
    public Guid BottomSensorId
    {
        get => _bottomSensorId;
        set => SetField(ref _bottomSensorId, value);
    }

    /// <summary>
    /// Gets or sets the maximum temperature.
    /// </summary>
    public double MaxTemp
    {
        get => _maxTemp;
        set
        {
            if (value <= MinTemp)
            {
                value = MinTemp + 1;
            }

            SetField(ref _maxTemp, value);
        }
    }

    /// <summary>
    /// Gets or sets the middle sensor.
    /// </summary>
    [JsonIgnore]
    public BaseTemperatureSensor? MiddleSensor
    {
        get => _middleSensor;
        set
        {
            _middleSensor = value;
            if (_middleSensor is not null)
            {
                _middleSensor.NewValueAvailable += OnNewTemperatureAvailable;
            }
        }
    }

    /// <summary>
    /// Gets or sets the middle part sensor id.
    /// </summary>
    public Guid MiddleSensorId
    {
        get => _middleSensorId;
        set => SetField(ref _middleSensorId, value);
    }

    /// <summary>
    /// Gets or sets the minimum temperature.
    /// </summary>
    public double MinTemp
    {
        get => _minTemp;
        set
        {
            if (value >= MaxTemp)
            {
                value = MaxTemp - 1;
            }

            SetField(ref _minTemp, value);
        }
    }

    /// <summary>
    /// Gets or sets the top sensor.
    /// </summary>
    [JsonIgnore]
    public BaseTemperatureSensor? TopSensor
    {
        get => _topSensor;
        set
        {
            _topSensor = value;
            if (_topSensor is not null)
            {
                _topSensor.NewValueAvailable += OnNewTemperatureAvailable;
            }
        }
    }

    /// <summary>
    /// Gets or sets the top part sensor id.
    /// </summary>
    public Guid TopSensorId
    {
        get => _topSensorId;
        set => SetField(ref _topSensorId, value);
    }

    #endregion Public Properties

    #region Private Methods

    private void OnNewTemperatureAvailable(object? sender, MeasureEventArgs<double> e) => TemperatureChanged?.Invoke(this, EventArgs.Empty);

    #endregion Private Methods

    /// <summary>
    /// Gets the temperature of the high part of the Accumulator.
    /// </summary>
    /// <returns> The temperature of the high part of the Accumulator. </returns>
    public double GetTopTemperature() => TopSensor?.GetTemperature() ?? double.NaN;
}
