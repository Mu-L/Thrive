using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Resource as in inventory
/// </summary>
public partial class InventoryResource : IInventoryItem
{

    [JsonProperty]
    public WorldResource? Definition { get; private set; }

    [JsonIgnore]
    public string ReadableName => Definition?.ReadableName ?? throw new NotSupportedException("Not initialized yet");

    [JsonIgnore]
    public Texture2D Icon => Definition?.Icon ?? throw new NotSupportedException("Not initialized yet");

    [JsonIgnore]
    public WeakReference<InventorySlot>? ShownAsGhostIn { get; set; }
}
