using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Equipment as in inventory
/// </summary>
public partial class InventoryEquipment : IInventoryItem
{

    [JsonProperty]
    public EquipmentDefinition? Definition { get; private set; }

    [JsonIgnore]
    public string ReadableName => Definition?.Name ?? throw new NotSupportedException("Not initialized yet");

    [JsonIgnore]
    public Texture2D Icon => Definition?.Icon ?? throw new NotSupportedException("Not initialized yet");

    [JsonIgnore]
    public WeakReference<InventorySlot>? ShownAsGhostIn { get; set; }
}
