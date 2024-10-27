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
public struct Harvestable
{
    /// <summary>
    ///   Required tool to harvest
    /// </summary>
    public EquipmentCategory RequiredTool;

    public Harvestable(EquipmentCategory requiredTool)
    {
        RequiredTool = requiredTool;
    }
}

public static class HarvestableHelpers
{
    public static bool CanHarvest(this ref Harvestable harvestable, EquipmentCategory equipmentCategory)
    {
        return equipmentCategory == harvestable.RequiredTool;
    }

    public static bool Harvest(this ref Harvestable harvestable)
    {
        return true;
    }
}
