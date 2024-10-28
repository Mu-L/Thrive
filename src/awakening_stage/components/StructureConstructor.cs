namespace Components;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DefaultEcs;
using DefaultEcs.Command;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base properties of a microbe (separate from the species info as early multicellular species object couldn't
///   work there)
/// </summary>
[JSONDynamicTypeAllowed]
public struct StructureConstructor
{
    /// <summary>
    /// Structure currently placed by constructor
    /// </summary>
    public StructureDefinition? CurrentlyPlacedStructure;
}

public static class StructureConstructorHelpers
{
    public static void AttemptStructurePlace(this ref StructureConstructor constructor)
    {

    }
}
