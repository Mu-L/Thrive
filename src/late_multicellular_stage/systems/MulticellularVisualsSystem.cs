namespace Systems;

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using Godot;
using World = DefaultEcs.World;

/// <summary>
///   Generates the visuals needed for microbes. Handles the membrane and organelle graphics. Attaching to the
///   Godot scene tree is handled by <see cref="SpatialAttachSystem"/>
/// </summary>
[With(typeof(OrganelleContainer))]
[With(typeof(CellProperties))]
[With(typeof(SpatialInstance))]
[With(typeof(EntityMaterial))]
[With(typeof(RenderPriorityOverride))]
[RunsBefore(typeof(SpatialAttachSystem))]
[RunsBefore(typeof(EntityMaterialFetchSystem))]
[RunsBefore(typeof(SpatialPositionSystem))]
[RuntimeCost(6)]
[RunsOnMainThread]
public sealed class MulticellularVisualsSystem : AEntitySetSystem<float>
{
    // private readonly Lazy<PackedScene> membraneScene =
    //     new(() => GD.Load<PackedScene>("res://src/microbe_stage/Membrane.tscn"));

    private readonly StringName tintParameterName = new("tint");

    private readonly ConcurrentQueue<MembraneGenerationParameters> membranesToGenerate = new();

    /// <summary>
    ///   Keeps track of generated tasks, just to allow disposing this object safely by waiting for them all
    /// </summary>
    private readonly List<Task> activeGenerationTasks = new();

    private bool pendingConvolutionGenerations;

    public MulticellularVisualsSystem(World world) : base(world, null)
    {
    }

    public bool HasPendingOperations()
    {
        return pendingConvolutionGenerations;
    }

    public override void Dispose()
    {
        Dispose(true);
        base.Dispose();
    }

    protected override void PreUpdate(float delta)
    {
        base.PreUpdate(delta);

        pendingConvolutionGenerations = false;

        activeGenerationTasks.RemoveAll(t => t.IsCompleted);
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var organelleContainer = ref entity.Get<OrganelleContainer>();

        if (organelleContainer.OrganelleVisualsCreated)
            return;

        // Skip if no organelle data
        if (organelleContainer.Organelles == null)
        {
            GD.PrintErr("Missing organelles list for MicrobeVisualsSystem");
            return;
        }

        ref var creatureProperties = ref entity.Get<CellProperties>();

        ref var spatialInstance = ref entity.Get<SpatialInstance>();

        // Create graphics top level node if missing for entity
        spatialInstance.GraphicalInstance ??= new Node3D();

        creatureProperties.ShapeCreated = false;
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            tintParameterName.Dispose();
        }

        var maxWait = TimeSpan.FromSeconds(10);
        foreach (var task in activeGenerationTasks)
        {
            if (!task.Wait(maxWait))
            {
                GD.PrintErr("Failed to wait for a background membrane generation task to finish on " +
                    "dispose");
            }
        }

        activeGenerationTasks.Clear();
    }
}
