namespace Systems;

using System;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Newtonsoft.Json.Linq;

/// <summary>
///   Handles awakening stage interaction
/// </summary>
/// <remarks>
///   <para>
///     I don't know what to do when AI comes aboard
///   </para>
/// </remarks>
[With(typeof(PlayerMarker))]
[With(typeof(Inventory))]
[With(typeof(LateMulticellularSpeciesMember))]
[ReadsComponent(typeof(OrganelleContainer))]
[ReadsComponent(typeof(CellProperties))]
[ReadsComponent(typeof(MicrobeStatus))]
[ReadsComponent(typeof(Engulfable))]
[ReadsComponent(typeof(MicrobeColony))]
[ReadsComponent(typeof(MicrobeColonyMember))]
[RuntimeCost(0.75f)]
public sealed class InteractionSystem : AEntitySetSystem<float>
{
    private GameWorld? gameWorld;

    public InteractionSystem(World world, IParallelRunner parallelRunner) :
        base(world, parallelRunner)
    {
    }

    public void SetWorld(GameWorld world)
    {
        gameWorld = world;
    }

    protected override void PreUpdate(float state)
    {
        base.PreUpdate(state);

        if (gameWorld == null)
            throw new InvalidOperationException("GameWorld not set");
    }

    protected override void Update(float delta, in Entity entity)
    {
        var potentialInteractables = World.GetEntities().With<Interactable>().AsEnumerable();

        Entity? closestInteractable = null;
        float closestDistance = Constants.INTERACTION_MAX_DISTANCE;

        foreach (var potentialInteractable in potentialInteractables)
        {
            if (!potentialInteractable.Has<WorldPosition>())
                continue;

            var position = potentialInteractable.Get<WorldPosition>().Position;
            var distance = position.DistanceTo(entity.Get<WorldPosition>().Position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestInteractable = potentialInteractable;
            }
        }

        entity.Get<Inventory>().InteractionTarget = closestInteractable;
    }
}
