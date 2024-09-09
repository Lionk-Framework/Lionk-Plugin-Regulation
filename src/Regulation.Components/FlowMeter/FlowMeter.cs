// Copyright © 2024 Lionk Project

using System.Device.Gpio;
using Lionk.Core;
using Lionk.Core.Component;
using Lionk.Core.DataModel;
using Lionk.Rpi.Gpio;
using Newtonsoft.Json;

namespace Regulation.Components;

/// <summary>
/// This class is used to represent an accumulator that store hot water.
/// </summary>
[NamedElement("FlowMeter", "This component is used to represent a flowMeter")]
public class FlowMeter : BaseComponent, IMeasurableComponent<int>
{
    #region Private Fields

    // Tolerance to avoid multiple events in ms
    private const int _raisedEventTolerance = 500;
    private const int NbMeasuresToComputeFlow = 10;
    private const string Unit = "l";
    private readonly Queue<(DateTime Time, int Value)> _lastMeasurements = new();
    private BaseGpioController? _controller;

    private InputGpio? _gpio;

    private Guid _gpioId;
    private DateTime _lastMeasureTime = DateTime.MinValue;
    private int _initialValue = 0;
    private int _totalValue = 0;

    #endregion Private Fields

    #region Public Events

    /// <summary>
    /// Event raised when a new value is available.
    /// </summary>
    public event EventHandler<MeasureEventArgs<int>>? NewValueAvailable;

    #endregion Public Events

    #region Public Properties

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    [JsonIgnore]
    public int CurrentValue { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether the component is enabled.
    /// </summary>
    [JsonIgnore]
    public bool Enable { get; set; } = false;

    /// <summary>
    /// Gets or sets the gpio.
    /// </summary>
    [JsonIgnore]
    public InputGpio? Gpio
    {
        get => _gpio;
        set
        {
            _gpio = value;
            if (_gpio is not null)
            {
                GpioId = _gpio.Id;
                _controller = _gpio.Controller;
                _controller.RegisterCallbackForPinValueChangedEvent(_gpio.Pin, PinEventTypes.Rising);
                _controller.PinValueChanged += OnPinValueChanged;
            }
        }
    }

    /// <summary>
    /// Gets or sets the gpio id.
    /// </summary>
    public Guid GpioId
    {
        get => _gpioId;
        set => SetField(ref _gpioId, value);
    }

    /// <summary>
    /// Gets or sets the initial value.
    /// </summary>
    public int InitialValue
    {
        get => _initialValue;
        set => SetField(ref _initialValue, value);
    }

    /// <summary>
    /// Gets or sets the total value.
    /// </summary>
    public int TotalValue
    {
        get => _totalValue;
        set => SetField(ref _totalValue, value);
    }

    /// <summary>
    /// Gets the measures of the GPIO component.
    /// </summary>
    public List<Measure<int>> Measures { get; } =
    [
        new Measure<int>("Valeur", DateTime.UtcNow, Unit, 0),
    ];

    #endregion Public Properties

    #region Private Methods

    private void OnPinValueChanged(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
    {
        if (!Enable || _lastMeasureTime.AddMilliseconds(_raisedEventTolerance) > DateTime.UtcNow) return;
        _lastMeasureTime = DateTime.UtcNow;
        _lastMeasureTime = DateTime.UtcNow;
        CurrentValue++;
        TotalValue++;
        Measure();
    }

    #endregion Private Methods

    #region Public Methods

    /// <summary>
    /// Method to get the average flow rate in liters per second.
    /// </summary>
    /// <param name="nbDecimal"> The number of decimal to keep. </param>
    /// <returns> The average flow rate in L/s. </returns>
    public double GetAverageFlowRateLps(int nbDecimal = 2)
    {
        if (Enable && _lastMeasurements.Count > 1)
        {
            (DateTime Time, int Value) first = _lastMeasurements.First();
            (DateTime Time, int Value) last = _lastMeasurements.Last();
            double timeDifference = (last.Time - first.Time).TotalSeconds;
            double flowRate = timeDifference > 0 ? (last.Value - first.Value) / timeDifference : 0;
            return Math.Round(flowRate, nbDecimal);
        }

        return 0;
    }

    /// <summary>
    /// Method to get the average flow rate in cubic meters per hour.
    /// </summary>
    /// <param name="nbDecimal"> The number of decimal to keep. </param>
    /// <returns> The average flow rate in m³/h. </returns>
    public double GetAverageFlowRateM3ph(int nbDecimal = 2) => Math.Round(GetAverageFlowRateLps(nbDecimal * 3) * 3.6, nbDecimal);

    /// <summary>
    /// Method to get the value.
    /// </summary>
    /// <returns> The value. </returns>
    public int GetValue() => CurrentValue;

    /// <summary>
    /// Method to get the value as a string.
    /// </summary>
    /// <returns> The value as a string. </returns>
    public string GetValueString() => GetValue() + " " + Unit;

    /// <summary>
    /// Method to get the total value as a string.
    /// </summary>
    /// <returns> The total value as a string. </returns>
    public string GetTotalValueString() => TotalValue + " " + Unit;

    /// <summary>
    /// Method to get the total value with the initial value as a string.
    /// </summary>
    /// <returns> The total value with the initial value as a string. </returns>
    public string GetTotalWithInitialValueString() => (TotalValue + InitialValue) + " " + Unit;

    /// <summary>
    /// Method to get the value.
    /// </summary>
    public void Measure()
    {
        DateTime now = DateTime.UtcNow;
        Measures[0] = new Measure<int>("Valeur", now, Unit, CurrentValue);
        NewValueAvailable?.Invoke(this, new MeasureEventArgs<int>(Measures));

        _lastMeasurements.Enqueue((now, CurrentValue));
        if (_lastMeasurements.Count > NbMeasuresToComputeFlow)
        {
            _lastMeasurements.Dequeue();
        }

        if (_lastMeasurements.Count > 1)
        {
            (DateTime Time, int Value) first = _lastMeasurements.First();
            (DateTime Time, int Value) last = _lastMeasurements.Last();
            double timeDifference = (last.Time - first.Time).TotalSeconds;
            if (timeDifference > 0)
            {
                int volumeDifference = last.Value - first.Value;
                double averageFlowRateLps = volumeDifference / timeDifference;
                double averageFlowRateM3ph = averageFlowRateLps * 3.6; // Conversion en m³/h
            }
        }
    }

    /// <summary>
    /// This method is used to reset the current value.
    /// </summary>
    public void ResetCurrentValue()
    {
        CurrentValue = 0;
        Measure();
        _lastMeasurements.Clear();
    }

    /// <summary>
    /// This method is used to reset the total value.
    /// </summary>
    public void ResetTotalValue() => TotalValue = CurrentValue;

    #endregion Public Methods
}
