// Copyright © 2024 Lionk Project

using System.Device.Gpio;
using System.Text.Json.Serialization;
using Lionk.Core;
using Lionk.Core.Component;
using Lionk.Rpi.Gpio;

namespace Regulation.Components;

/// <summary>
/// This class represents a three way valve.
/// </summary>
[NamedElement("Three Way Valve", "This component represents a three way valve.")]
public class ThreeWayValve : BaseExecutableComponent
{
    #region Public Events

    /// <summary>
    /// Event raised when the state of the valve changed.
    /// </summary>
    public event EventHandler? StateChanged;
    #endregion Public Events

    #region Private Fields

    private readonly object _locker = new();
    private OutputGpio? _closingGpio;
    private Guid _closingGpioId;
    private bool _isBusy = false;
    private OutputGpio? _openingGpio;
    private Guid _openingGpioId;
    private DateTime _startActionTime = DateTime.MinValue;
    #endregion Private Fields

    #region Public Properties

    /// <summary>
    /// Gets a value indicating whether the component can be executed.
    /// </summary>
    public override bool CanExecute
        => OpeningGpio is not null
        && ClosingGpio is not null
        && OpeningGpio.CanExecute
        && ClosingGpio.CanExecute;

    /// <summary>
    /// Gets or sets the pin that is used to close the valve.
    /// </summary>
    [JsonIgnore]
    public OutputGpio? ClosingGpio
    {
        get => _closingGpio;
        set
        {
            _closingGpio = value;
            if (_closingGpio is not null)
            {
                ClosingGpioId = _closingGpio.Id;
            }
        }
    }

    /// <summary>
    /// Gets or sets the pin id that is used to close the valve.
    /// </summary>
    public Guid ClosingGpioId
    {
        get => _closingGpioId;
        set => SetField(ref _closingGpioId, value);
    }

    /// <summary>
    /// Gets a value indicating whether the component is initialized.
    /// </summary>
    public bool IsInitialized { get; private set; } = false;

    /// <summary>
    /// Gets or sets the time in secondes that the valve will be open.
    /// </summary>
    public int OpeningDuration { get; set; } = 0;

    /// <summary>
    /// Gets or sets the pin that is used to open the valve.
    /// </summary>
    [JsonIgnore]
    public OutputGpio? OpeningGpio
    {
        get => _openingGpio;
        set
        {
            _openingGpio = value;
            if (_openingGpio is not null)
            {
                OpeningGpioId = _openingGpio.Id;
            }
        }
    }

    /// <summary>
    /// Gets or sets the pin id that is used to close the valve.
    /// </summary>
    public Guid OpeningGpioId
    {
        get => _openingGpioId;
        set => SetField(ref _openingGpioId, value);
    }

    /// <summary>
    /// Gets the remaining time before the valve is fully open.
    /// </summary>
    public TimeSpan RemainingTime { get; private set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets the state of the valve.
    /// </summary>
    public ValveState State { get; private set; } = ValveState.Undefined;

    /// <summary>
    /// Gets or sets the order to execute.
    /// </summary>
    [JsonIgnore]
    public ValveOrder ValveOrder { get; set; } = ValveOrder.Stop;

    #endregion Public Properties

    #region Private Methods

    private void CloseProcess()
    {
        if (OpeningGpio is null || ClosingGpio is null) return;
        if (!IsInitialized || State is ValveState.Undefined) InitializeProcess();
        _startActionTime = DateTime.UtcNow;
        while (State is not ValveState.Closed)
        {
            if (ValveOrder is ValveOrder.Stop)
            {
                StopProcess();
                break;
            }

            switch (State)
            {
                case ValveState.Closed:
                    RemainingTime = TimeSpan.Zero;
                    break;

                case ValveState.Closing:
                    OpeningGpio.PinValue = PinValue.Low;
                    ClosingGpio.PinValue = PinValue.High;
                    if (DateTime.UtcNow - _startActionTime >= TimeSpan.FromSeconds(OpeningDuration))
                    {
                        State = ValveState.Closed;
                        RemainingTime = TimeSpan.Zero;
                        ClosingGpio.PinValue = PinValue.Low;
                    }
                    else
                    {
                        RemainingTime = TimeSpan.FromSeconds(OpeningDuration) - (DateTime.UtcNow - _startActionTime);
                    }

                    break;

                default:
                    State = ValveState.Closing;
                    break;
            }

            StateChanged?.Invoke(this, EventArgs.Empty);
            OpeningGpio.Execute();
            ClosingGpio.Execute();
            Thread.Sleep(500);
        }
    }

    private void InitializeProcess()
    {
        if (OpeningGpio is null || ClosingGpio is null)
        {
            IsInitialized = false;
            return;
        }

        _startActionTime = DateTime.UtcNow;
        int initializationTime = OpeningDuration + 5;
        while (State is not ValveState.Initialised)
        {
            if (ValveOrder is ValveOrder.Stop)
            {
                StopProcess();
                break;
            }

            switch (State)
            {
                case ValveState.Initialised:
                    RemainingTime = TimeSpan.Zero;
                    break;

                case ValveState.Initialising:
                    OpeningGpio.PinValue = PinValue.Low;
                    ClosingGpio.PinValue = PinValue.High;
                    if (DateTime.UtcNow - _startActionTime >= TimeSpan.FromSeconds(initializationTime))
                    {
                        State = ValveState.Initialised;
                        RemainingTime = TimeSpan.Zero;
                        ClosingGpio.PinValue = PinValue.Low;
                    }
                    else
                    {
                        RemainingTime = TimeSpan.FromSeconds(initializationTime) - (DateTime.UtcNow - _startActionTime);
                    }

                    break;

                default:
                    State = ValveState.Initialising;
                    break;
            }

            OpeningGpio.Execute();
            ClosingGpio.Execute();
            StateChanged?.Invoke(this, EventArgs.Empty);
            Thread.Sleep(500);
        }

        IsInitialized = true;
    }

    private void OpenProcess()
    {
        if (OpeningGpio is null || ClosingGpio is null) return;
        if (!IsInitialized || State is ValveState.Undefined) InitializeProcess();
        _startActionTime = DateTime.UtcNow;
        while (State is not ValveState.Open)
        {
            if (ValveOrder is ValveOrder.Stop)
            {
                StopProcess();
                break;
            }

            switch (State)
            {
                case ValveState.Open:
                    RemainingTime = TimeSpan.Zero;
                    break;

                case ValveState.Opening:
                    OpeningGpio.PinValue = PinValue.High;
                    ClosingGpio.PinValue = PinValue.Low;
                    if (DateTime.UtcNow - _startActionTime >= TimeSpan.FromSeconds(OpeningDuration))
                    {
                        State = ValveState.Open;
                        RemainingTime = TimeSpan.Zero;
                        OpeningGpio.PinValue = PinValue.Low;
                    }
                    else
                    {
                        RemainingTime = TimeSpan.FromSeconds(OpeningDuration) - (DateTime.UtcNow - _startActionTime);
                    }

                    break;

                default:
                    State = ValveState.Opening;
                    break;
            }

            OpeningGpio.Execute();
            ClosingGpio.Execute();
            StateChanged?.Invoke(this, EventArgs.Empty);
            Thread.Sleep(500);
        }
    }

    private void Process(ValveOrder order)
    {
        if (_isBusy) return;
        lock (_locker)
        {
            _isBusy = true;
        }

        var thread = new Thread(() =>
        {
            switch (order)
            {
                case ValveOrder.Initialize:
                    InitializeProcess();
                    break;

                case ValveOrder.Open:
                    OpenProcess();
                    break;

                case ValveOrder.Close:
                    CloseProcess();
                    break;

                case ValveOrder.Stop:
                    StopProcess();
                    break;
            }

            lock (_locker)
            {
                _isBusy = false;
            }
        });
        thread.Start();
    }

    private void StopProcess()
    {
        if (OpeningGpio is null || ClosingGpio is null) return;
        OpeningGpio.PinValue = PinValue.Low;
        ClosingGpio.PinValue = PinValue.Low;
        State = ValveState.Undefined;
        RemainingTime = TimeSpan.Zero;

        OpeningGpio.Execute();
        ClosingGpio.Execute();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion Private Methods

    #region Protected Methods

    /// <inheritdoc/>
    protected override void OnExecute(CancellationToken cancellationToken)
    {
        base.OnExecute(cancellationToken);
        Process(ValveOrder);
    }

    #endregion Protected Methods

    #region Public Methods

    /// <inheritdoc/>
    public override void Abort()
    {
        base.Abort();
        Process(ValveOrder.Stop);
    }

    #endregion Public Methods
}
