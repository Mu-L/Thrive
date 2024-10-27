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
public struct Item
{
    /// <summary>
    ///   Corresponding inventory item
    /// </summary>
    public IInventoryItem CorrespondingItem;

    public Item(IInventoryItem item)
    {
        Item = item;
    }
}
