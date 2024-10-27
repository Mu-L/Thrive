namespace Components;

/// <summary>
///   Entity is a member of a late multicellular species
/// </summary>
[ComponentIsReadByDefault]
[JSONDynamicTypeAllowed]
public struct LateMulticellularSpeciesMember
{
    public LateMulticellularSpecies Species;
}
