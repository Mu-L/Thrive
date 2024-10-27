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
public struct Deposit
{
    /// <summary>
    ///   Items currently stored in a deposit
    /// </summary>
    public List<IInventoryItem> StoredItems;

    /// <summary>
    ///   Items demanded by deposit (will be transferred from character inventory to deposit upon interaction)
    /// </summary>
    public List<IInventoryItem> WantedItems;

    public Deposit(List<IInventoryItem> storedItems, List<IInventoryItem> wantedItems)
    {
        StoredItems = storedItems;
        WantedItems = wantedItems;
    }
}

public static class DepositHelpers
{
    public static bool CharacterHasDemandedResource(this ref Deposit deposit, ref Inventory characterInventory)
    {
        foreach (var characterItem in characterInventory.StoredItems)
        {
            foreach (var wantedItem in deposit.WantedItems)
            {
                if (characterItem.Value.Equals(wantedItem))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static void TransferDemandedResources(this ref Deposit deposit, ref Inventory characterInventory)
    {
        foreach (var characterItem in characterInventory.StoredItems)
        {
            foreach (var wantedItem in deposit.WantedItems)
            {
                if (characterItem.Value.Equals(wantedItem))
                {
                    // Add to deposit
                    deposit.StoredItems.Add(characterItem.Value);

                    // Remove from player inventory
                    characterInventory.StoredItems.Remove(characterItem.Key);
                }
            }
        }
    }
}
