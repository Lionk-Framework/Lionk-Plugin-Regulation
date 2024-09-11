// Copyright © 2024 Lionk Project

namespace Regulation.Components;

/// <summary>
/// This enum represents the state of a chimney.
/// </summary>
public enum ChimneyState
{
    /// <summary>
    /// The fireplace is off, no heat production.
    /// </summary>
    Off,

    /// <summary>
    /// The fireplace is warming down.
    /// </summary>
    HeatingDown,

    /// <summary>
    /// The fireplace is warming up, but not yet at full power.
    /// </summary>
    HeatingUp,

    /// <summary>
    /// The fireplace is at full power, producing maximum heat.
    /// </summary>
    AtFullPower,

    /// <summary>
    /// The fireplace is in error state.
    /// </summary>
    Error,

    /// <summary>
    /// The fireplace is stabilized, producing a constant amount of heat.
    /// </summary>
    Stabilized,

    /// <summary>
    /// If the input or output temperature is not valid.
    /// </summary>
    Undefined,
}
