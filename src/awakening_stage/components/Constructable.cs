namespace Components;

using System;
using System.Collections.Generic;
using System.Linq;
using DefaultEcs;
using DefaultEcs.Command;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Works together with <see cref="Deposit"/>
/// </summary>
[JSONDynamicTypeAllowed]
public struct Constructable
{
    public bool Completed;

    /// <summary>
    ///   Items demanded by constructable (and deposit) to be considered completed
    /// </summary>
    public List<IInventoryItem> RequiredItems;

    public Constructable(bool completed, List<IInventoryItem> requiredItems)
    {
        Completed = completed;
        RequiredItems = requiredItems;
    }
}

public static class ConstructableHelpers
{
    public static bool CanBeBuilt(this ref Constructable constructable, ref Entity entity)
    {
        var hasAllRequiredItems = false;

        if (entity.Has<Deposit>())
        {
            hasAllRequiredItems = true;

            foreach (var neededItem in constructable.RequiredItems)
            {
                if (!entity.Get<Deposit>().StoredItems.Contains(neededItem))
                    hasAllRequiredItems = false;
            }
        }

        return hasAllRequiredItems;
    }
}
