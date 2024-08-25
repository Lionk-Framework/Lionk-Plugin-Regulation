// Copyright © 2024 Lionk Project

using System.Device.Gpio;
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
    #region Private Fields

    private readonly object _locker = new();
    private DateTime _startActionTime = DateTime.MinValue;
    private bool _isBusy = false;

    #endregion Private Fields

    #region Public Properties

    /// <summary>
    /// Gets or sets the pin that is used to close the valve.
    /// </summary>
    public OutputGpio? ClosingPin { get; set; }

    /// <summary>
    /// Gets or sets the pin that is used to open the valve.
    /// </summary>
    public OutputGpio? OpeningPin { get; set; }

    /// <summary>
    /// Gets or sets the time in secondes that the valve will be open.
    /// </summary>
    public int OpeningDuration { get; set; } = 0;

    /// <summary>
    /// Gets or sets the order to execute.
    /// </summary>
    public ValveOrder ValveOrder { get; set; } = ValveOrder.Stop;

    /// <summary>
    /// Gets the state of the valve.
    /// </summary>
    public VavleState State { get; private set; } = VavleState.Undefined;

    /// <summary>
    /// Gets a value indicating whether the component is initialized.
    /// </summary>
    public bool IsInitialized { get; private set; } = false;

    /// <summary>
    /// Gets the remaining time before the valve is fully open.
    /// </summary>
    public TimeSpan RemainingTime { get; private set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets a value indicating whether the component can be executed.
    /// </summary>
    public override bool CanExecute
        => OpeningPin is not null
        && ClosingPin is not null
        && OpeningPin.CanExecute
        && ClosingPin.CanExecute;

    #endregion Public Properties

    /// <inheritdoc/>
    protected override void OnExecute(CancellationToken cancellationToken)
    {
        base.OnExecute(cancellationToken);
        Process(ValveOrder);
    }

    /// <inheritdoc/>
    public override void Abort()
    {
        base.Abort();
        Process(ValveOrder.Stop);
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

    private void OpenProcess()
    {
        if (OpeningPin is null || ClosingPin is null) return;
        if (!IsInitialized) InitializeProcess();
        _startActionTime = DateTime.UtcNow;
        while (State is not VavleState.Open)
        {
            switch (State)
            {
                case VavleState.Open:
                    RemainingTime = TimeSpan.Zero;
                    break;

                case VavleState.Opening:
                    OpeningPin.PinValue = PinValue.High;
                    ClosingPin.PinValue = PinValue.Low;
                    if (DateTime.UtcNow - _startActionTime >= TimeSpan.FromSeconds(OpeningDuration))
                    {
                        State = VavleState.Open;
                        RemainingTime = TimeSpan.Zero;
                        OpeningPin.PinValue = PinValue.Low;
                    }
                    else
                    {
                        RemainingTime = TimeSpan.FromSeconds(OpeningDuration) - (DateTime.UtcNow - _startActionTime);
                    }

                    break;

                default:
                    State = VavleState.Opening;
                    break;
            }

            OpeningPin.Execute();
            ClosingPin.Execute();
        }
    }

    private void CloseProcess()
    {
        if (OpeningPin is null || ClosingPin is null) return;
        if (!IsInitialized) InitializeProcess();
        _startActionTime = DateTime.UtcNow;
        while (State is not VavleState.Closed)
        {
            switch (State)
            {
                case VavleState.Closed:
                    RemainingTime = TimeSpan.Zero;
                    break;

                case VavleState.Closing:
                    OpeningPin.PinValue = PinValue.Low;
                    ClosingPin.PinValue = PinValue.High;
                    if (DateTime.UtcNow - _startActionTime >= TimeSpan.FromSeconds(OpeningDuration))
                    {
                        State = VavleState.Closed;
                        RemainingTime = TimeSpan.Zero;
                        ClosingPin.PinValue = PinValue.Low;
                    }
                    else
                    {
                        RemainingTime = TimeSpan.FromSeconds(OpeningDuration) - (DateTime.UtcNow - _startActionTime);
                    }

                    break;

                default:
                    State = VavleState.Opening;
                    break;
            }

            OpeningPin.Execute();
            ClosingPin.Execute();
        }
    }

    private void StopProcess()
    {
        if (OpeningPin is null || ClosingPin is null) return;
        OpeningPin.PinValue = PinValue.Low;
        ClosingPin.PinValue = PinValue.Low;
        State = VavleState.Undefined;
        RemainingTime = TimeSpan.Zero;
        OpeningPin.Execute();
        ClosingPin.Execute();
    }

    private void InitializeProcess()
    {
        if (OpeningPin is null || ClosingPin is null)
        {
            IsInitialized = false;
            return;
        }

        int openingDurationSave = OpeningDuration;
        OpeningDuration += 5;
        while (State is not VavleState.Initialised)
        {
            switch (State)
            {
                case VavleState.Initialised:
                    RemainingTime = TimeSpan.Zero;
                    break;

                case VavleState.Initialising:
                    OpeningPin.PinValue = PinValue.High;
                    ClosingPin.PinValue = PinValue.Low;
                    if (DateTime.UtcNow - _startActionTime >= TimeSpan.FromSeconds(OpeningDuration))
                    {
                        State = VavleState.Initialised;
                        RemainingTime = TimeSpan.Zero;
                        OpeningPin.PinValue = PinValue.Low;
                    }
                    else
                    {
                        RemainingTime = TimeSpan.FromSeconds(OpeningDuration) - (DateTime.UtcNow - _startActionTime);
                    }

                    break;

                default:
                    State = VavleState.Opening;
                    break;
            }

            OpeningPin.Execute();
            ClosingPin.Execute();
        }

        OpeningDuration = openingDurationSave;
        IsInitialized = true;
    }
}
