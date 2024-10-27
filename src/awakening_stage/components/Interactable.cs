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
public struct Interactable
{
    /// <summary>
    ///   Can be used to craft
    /// </summary>
    public bool IsResource;

    /// <summary>
    ///   CanBeCarried
    /// </summary>
    public bool CanBeCarried;

    public Interactable(bool isResource, bool canBeCarried)
    {
        IsResource = isResource;
        CanBeCarried = canBeCarried;
    }
}

public static class InteractableHelpers
{
    public static void GetExtraAvailableActions(this ref Interactable)
    {
    }
}
