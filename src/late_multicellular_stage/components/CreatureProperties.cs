namespace Components;

using System;
using System.Collections.Generic;
using System.Linq;
using DefaultEcs;
using DefaultEcs.Command;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base properties of a microbe (separate from the species info as early multicellular species object couldn't
///   work there)
/// </summary>
[JSONDynamicTypeAllowed]
public struct CreatureProperties
{
    /// <summary>
    ///   The membrane created for this cell. This is here so that some other systems apart from the visuals system
    ///   can have access to the membrane data.
    /// </summary>
    [JsonIgnore]
    public Membrane? CreatedSurface;

    [JsonIgnore]
    public bool SurfaceCreated;

    public CreatureProperties()
    {
        CreatedSurface = null;

        SurfaceCreated = false;
    }
}

public static class CreaturePropertiesHelpers
{
    /// <summary>
    ///   Checks if creature can eat
    /// </summary>
    public static bool CanEat(this ref CreatureProperties creatureProperties, in Entity entity)
    {
        return true;
    }
}
