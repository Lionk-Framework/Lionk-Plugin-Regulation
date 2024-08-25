// Copyright © 2024 Lionk Project

using System.Device.Gpio;
using Lionk.Core.Component;
using Lionk.Rpi.Gpio;

namespace Regulation.Components;

/// <summary>
/// This class represents a three way valve.
/// </summary>
public class ThreeWayValve : BaseExecutableComponent
{
    #region Private Fields

    private DateTime _startActionTime = DateTime.MinValue;

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
    /// Gets or sets the state of the valve.
    /// </summary>
    public VavleState State { get; set; } = VavleState.Undefined;

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
    public override bool CanExecute => OpeningPin is not null && ClosingPin is not null && IsInitialized;

    #endregion Public Properties

    private void Process(VavleOrder order)
    {
        switch (order)
        {
            case VavleOrder.Initialize:
                InitializeProcess();
                break;

            case VavleOrder.Open:
                OpenProcess();
                break;

            case VavleOrder.Close:
                CloseProcess();
                break;

            case VavleOrder.Stop:
                StopProcess();
                break;
        }
    }

    private void OpenProcess()
    {
        if (OpeningPin is null || ClosingPin is null) return;
        _startActionTime = DateTime.UtcNow;
        while (State is not VavleState.Open)
        {
            switch (State)
            {
                case VavleState.Open:
                    RemainingTime = TimeSpan.Zero;
                    return;

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

                    return;

                case VavleState.Closing:
                case VavleState.Closed:
                case VavleState.Undefined:
                    State = VavleState.Opening;
                    return;
            }

            OpeningPin.Execute();
            ClosingPin.Execute();
        }
    }

    private void CloseProcess()
    {
        if (OpeningPin is null || ClosingPin is null) return;
        _startActionTime = DateTime.UtcNow;
        while (State is not VavleState.Closed)
        {
            switch (State)
            {
                case VavleState.Closed:
                    RemainingTime = TimeSpan.Zero;
                    return;

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

                    return;

                case VavleState.Opening:
                case VavleState.Open:
                case VavleState.Undefined:
                    State = VavleState.Closing;
                    return;
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
        OpenProcess();
        OpeningDuration = openingDurationSave;
        IsInitialized = true;
    }
}
