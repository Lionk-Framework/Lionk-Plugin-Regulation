// Copyright © 2024 Lionk Project

namespace Regulation.Components;

/// <summary>
/// This enum represents the state of a valve.
/// </summary>
public enum VavleState
{
    /// <summary>
    /// The valve is closed.
    /// </summary>
    Closed,

    /// <summary>
    /// The valve is open.
    /// </summary>
    Open,

    /// <summary>
    /// The valve is opening.
    /// </summary>
    Opening,

    /// <summary>
    /// The valve is closing.
    /// </summary>
    Closing,

    /// <summary>
    /// The state of the valve is undefined.
    /// </summary>
    Undefined,
}
