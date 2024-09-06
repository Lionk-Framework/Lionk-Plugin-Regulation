// Copyright © 2024 Lionk Project

namespace Regulation.Components;

/// <summary>
/// Enum used to order a valve.
/// </summary>
public enum ValveOrder
{
    /// <summary>
    /// The valve is ordered to initialize.
    /// </summary>
    Initialize,

    /// <summary>
    /// The valve is ordered to close.
    /// </summary>
    Close,

    /// <summary>
    /// The valve is ordered to open.
    /// </summary>
    Open,

    /// <summary>
    /// The valve is ordered to abort.
    /// </summary>
    Stop,
}
