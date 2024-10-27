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
public struct Inventory
{
    public Dictionary<int, IInventoryItem> StoredItems;

    public int MaxInventorySize = 4;

    /// <summary>
    /// Set by InteractionSystem
    /// </summary>
    public Entity? InteractionTarget;

    public Inventory(int maxInventorySize, Dictionary<int, IInventoryItem> storedItems)
    {
        MaxInventorySize = maxInventorySize;
        StoredItems = storedItems;

        InteractionTarget = null;
    }
}

public static class InventoryAndInteractionHelpers
{
    public static IEnumerable<(InteractionType Interaction, bool Enabled, string? TextOverride)> CalculatePossibleActions(
        Entity entity, Entity target)
    {
        var inventory = entity.Get<Inventory>();

        if (!target.Has<Interactable>())
            yield return (InteractionType.Pickup, false, null);

        var interactable = target.Get<Interactable>();

        if (interactable.CanBeCarried)
        {
            bool full = !HasEmptySlot(entity);
            yield return (InteractionType.Pickup, !full,
                full ? Localization.Translate("INTERACTION_PICK_UP_CANNOT_FULL") : null);
        }

        if (interactable.IsResource)
        {
            // Assume all resources can be used in some kind of crafting
            yield return (InteractionType.Craft, true, null);
        }

        if (target.Has<Harvestable>())
        {
            foreach (var inventoryItem in inventory.StoredItems.Values)
            {
                // FIX NEEDED HERE
                if (inventoryItem is InventoryEquipment equipment)
                {
                    var message = Localization.Translate("INTERACTION_HARVEST_CANNOT_MISSING_TOOL");

                    if (equipment.Definition == null)
                        yield return (InteractionType.Harvest, false, message);

                    var canHarvest = target.Get<Harvestable>().CanHarvest(equipment.Definition!.Category);

                    if (canHarvest)
                    {
                        yield return (InteractionType.Harvest, true, null);
                    }
                    else
                    {
                        yield return (InteractionType.Harvest, false, message);
                    }
                }
            }
        }

        if (target.Has<Deposit>())
        {
            bool takesItems = target.Get<Deposit>().CharacterHasDemandedResource(ref inventory);

            yield return (InteractionType.DepositResources, takesItems,
                takesItems ?
                    null :
                    Localization.Translate("INTERACTION_DEPOSIT_RESOURCES_NO_SUITABLE_RESOURCES"));

            if (target.Has<Constructable>())
            {
                bool canBeBuilt = !target.Get<Constructable>().Completed;

                yield return (InteractionType.Construct, canBeBuilt,
                    canBeBuilt ?
                        null :
                        Localization.Translate("INTERACTION_CONSTRUCT_MISSING_DEPOSITED_MATERIALS"));
            }
        }

        // TODO: Add the extra interactions the entity provides
    }

    public static bool FitsInCarryingCapacity(IInteractableEntity interactableEntity, Entity entity)
    {
        return HasEmptySlot(entity);
    }

    public static bool HasEmptySlot(Entity entity)
    {
        if (!entity.Has<Inventory>())
            return false;

        var properties = entity.Get<Inventory>();

        return properties.StoredItems.Count < properties.MaxInventorySize;
    }

    public static bool AttemptInteraction(Entity entity, Entity target, InteractionType interactionType)
    {
        if (!entity.Has<Inventory>())
            return false;

        var inventory = entity.Get<Inventory>();

        // Make sure action is allowed first
        if (!CalculatePossibleActions(entity, target).Any(t => t.Enabled && t.Interaction == interactionType))
            return false;

        // Then perform it
        switch (interactionType)
        {
            case InteractionType.Pickup:
                return inventory.PickupItem(ref entity, ref target);
            case InteractionType.Harvest:
            {
                if (!target.Has<Harvestable>())
                    return false;

                target.Get<Harvestable>().Harvest();

                return false;
            }

            case InteractionType.Craft:
            {
                // TODO: Redo crafting
                return true;
            }

            case InteractionType.DepositResources:
            {
                // TODO: instead of closing, just update the interaction popup to allow finishing construction
                // immediately
                if (target.Has<Deposit>() && entity.Has<Inventory>())
                {
                    var deposit = target.Get<Deposit>();
                    deposit.TransferDemandedResources(ref entity.Get<Inventory>());

                    return true;
                }

                GD.PrintErr("Deposit action failed due to bad target or currently held items");
                return false;
            }

            case InteractionType.Construct:
            {
                // TODO: Bring back timed building or something like that
                if (target.Has<Constructable>())
                {
                    var constructable = target.Get<Constructable>();

                    if (constructable.CanBeBuilt(ref target))
                    {
                        constructable.Completed = true;
                        return true;
                    }
                }

                return false;
            }

            default:
            {
                // TODO: Bring back extra actions
                return false;
            }
        }
    }

    public static bool PickupItem(this ref Inventory inventory, ref Entity entity, ref Entity target)
    {
        if (!target.Has<Item>())
            return false;

        var item = target.Get<Item>();

        if (inventory.StoredItems.Count >= inventory.MaxInventorySize)
            return false;

        // Always replace with hand item. Design choice :)
        if (inventory.StoredItems.TryGetValue(0, out var alreadyHeldItem))
            inventory.DropItem(ref entity, 0);

        inventory.StoredItems.Add(0, item.CorrespondingItem);

        return true;
    }

    public static void DropItem(this ref Inventory inventory, ref Entity entity, int slot)
    {
        inventory.StoredItems.TryGetValue(slot, out var item);

        if (item == null)
            return;

        var resourceScene = SpawnHelpers.LoadResourceEntityScene();

        // ???
        //SpawnHelpers.SpawnResourceEntity(item.ResourceFromItem(), new Transform3D(Basis.Identity, entity.Get<WorldPosition>().Position), ???????, resourceScene);

        inventory.StoredItems.Remove(slot);
    }
}
