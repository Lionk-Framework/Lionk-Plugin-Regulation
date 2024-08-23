// Copyright © 2024 Lionk Project

using Lionk.Core;
using Lionk.Core.Component;

namespace Regulation.Components;

/// <summary>
/// This class is used to represent an accumulator that store hot water.
/// </summary>
[NamedElement("Accumulator", "This component is used to represent an accumulator that store hot water.")]
public class Accumulator : BaseComponent
{
    private Guid _bottomSensorId;
    private Guid _middleSensorId;
    private Guid _topSensorId;
    private double _maxTemp = 85.0;
    private double _minTemp = 20.0;

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
    /// Gets or sets the top part sensor id.
    /// </summary>
    public Guid TopSensorId
    {
        get => _topSensorId;
        set => SetField(ref _topSensorId, value);
    }
}
