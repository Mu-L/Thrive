using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main script on each multicellular creature in the game
/// </summary>
[JsonObject(IsReference = true)]
[JSONAlwaysDynamicType]
[SceneLoadedClass("res://src/late_multicellular_stage/MulticellularCreature.tscn", UsesEarlyResolve = false)]
[DeserializedCallbackTarget]
public partial class MulticellularCreature : RigidBody3D//, ISaveLoadedTracked, IEntity,
    //IStructureSelectionReceiver<StructureDefinition>, IActionProgressSource
{
    /*
    private static readonly Vector3 SwimUpForce = new(0, 10, 0);

    [JsonProperty]
    private readonly CompoundBag compounds = new(0.0f);

    [JsonProperty]
    private readonly List<IInteractableEntity> carriedObjects = new();

    private StructureDefinition? buildingTypeToPlace;

    [JsonProperty]
    private CreatureAI? ai;

    [JsonProperty]
    private ISpawnSystem? spawnSystem;

#pragma warning disable CA2213
    private MulticellularConvolutionDispayer metaballDisplayer = null!;

    private Node3D? buildingToPlaceGhost;
#pragma warning restore CA2213

    // TODO: a real system for determining the hand and equipment slots
    // TODO: hand count based on body plan
    [JsonProperty]
    private InventorySlotData handSlot = new(1, EquipmentSlotType.Hand, new Vector2(0.82f, 0.43f));

    // TODO: increase inventory slots based on equipment
    [JsonProperty]
    private InventorySlotData[] inventorySlots =
    {
        new(2),
        new(3),
    };

    [JsonProperty]
    private EntityReference<IInteractableEntity>? actionTarget;

    [JsonProperty]
    private float performedActionTime;

    [JsonProperty]
    private float totalActionRequiredTime;

    /// <summary>
    ///   Where an action was started, used to detect if the creature moves too much and the action should be canceled
    /// </summary>
    [JsonProperty]
    private Vector3 startedActionPosition;

    [JsonProperty]
    private float targetSwimLevel;

    [JsonProperty]
    private float upDownSwimSpeed = 3;

    private bool actionHasSucceeded;

    // TODO: implement
    [JsonIgnore]
    public List<TweakedProcess> ActiveProcesses => new();

    // TODO: implement
    [JsonIgnore]
    public CompoundBag ProcessCompoundStorage => compounds;

    // TODO: implement multicellular process statistics
    [JsonIgnore]
    public ProcessStatistics? ProcessStatistics => null;

    [JsonProperty]
    public bool Dead { get; private set; }

    [JsonProperty]
    public Action<MulticellularCreature>? OnDeath { get; set; }

    [JsonProperty]
    public Action<MulticellularCreature, bool>? OnReproductionStatus { get; set; }

    [JsonProperty]
    public Action<MulticellularCreature, IInteractableEntity>? RequestCraftingInterfaceFor { get; set; }

    /// <summary>
    ///   The species of this creature. It's mandatory to initialize this with <see cref="ApplySpecies"/> otherwise
    ///   random stuff in this instance won't work
    /// </summary>
    [JsonProperty]
    public LateMulticellularSpecies Species { get; private set; } = null!;

    /// <summary>
    ///   True when this is the player's creature
    /// </summary>
    [JsonProperty]
    public bool IsPlayerCreature { get; private set; }

    /// <summary>
    ///   For checking if the player is in freebuild mode or not
    /// </summary>
    [JsonProperty]
    public GameProperties CurrentGame { get; private set; } = null!;

    /// <summary>
    ///   Needs access to the world for population changes
    /// </summary>
    [JsonIgnore]
    public GameWorld GameWorld => CurrentGame.GameWorld;

    /// <summary>
    ///   The direction the creature wants to move. Doesn't need to be normalized
    /// </summary>
    public Vector3 MovementDirection { get; set; } = Vector3.Zero;

    [JsonProperty]
    public MovementMode MovementMode { get; set; }

    [JsonProperty]
    public float TimeUntilNextAIUpdate { get; set; }

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    [JsonIgnore]
    public Node3D EntityNode => this;

    [JsonIgnore]
    public bool IsLoadedFromSave { get; set; }

    [JsonProperty]
    public bool ActionInProgress { get; private set; }

    [JsonIgnore]
    public float ActionProgress => totalActionRequiredTime != 0 ? performedActionTime / totalActionRequiredTime : 0;

    // TODO: make this creature height dependent
    [JsonIgnore]
    public Vector3? ExtraProgressBarWorldOffset => null;

    [JsonIgnore]
    public bool IsPlacingStructure => buildingTypeToPlace != null;

    public override void _Ready()
    {
        base._Ready();

        metaballDisplayer = GetNode<MulticellularConvolutionDispayer>("MetaballDisplayer");
    }

    /// <summary>
    ///   Must be called when spawned to provide access to the needed systems
    /// </summary>
    public void Init(ISpawnSystem spawnSystem, GameProperties currentGame, bool isPlayer)
    {
        this.spawnSystem = spawnSystem;
        CurrentGame = currentGame;
        IsPlayerCreature = isPlayer;

        if (!isPlayer)
            ai = new CreatureAI(this);

        // Needed for immediately applying the species
        _Ready();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // TODO: implement growth
        OnReproductionStatus?.Invoke(this, true);

        UpdateActionStatus((float)delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (MovementMode == MovementMode.Swimming)
        {
            // TODO: apply buoyancy (if this is underwater)

            if (Position.Y < targetSwimLevel)
                ApplyCentralImpulse(Mass * SwimUpForce * (float)delta);

            if (MovementDirection != Vector3.Zero)
            {
                // TODO: allow the species bodies to tilt with enough force, for now to make a simple fix tipping axis
                // is locked
                // TODO: movement force calculation
                ApplyCentralImpulse(Mass * MovementDirection * (float)delta * 2 *
                    (Math.Clamp(Species.MuscularPower, 0, 1 * Mass) + 1));
            }
        }
        else
        {
            if (MovementDirection != Vector3.Zero)
            {
                // TODO: movement force calculation
                ApplyCentralImpulse(Mass * MovementDirection * (float)delta * 15 *
                    (Math.Clamp(Species.MuscularPower, 0, 1 * Mass) + 1));
            }
        }

        // This is in physics process as this follows the player physics entity
        if (IsPlacingStructure && buildingToPlaceGhost != null)
        {
            // Position the preview
            buildingToPlaceGhost.GlobalTransform = GetStructurePlacementLocation();
        }
    }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }

    public void ApplySpecies(Species species)
    {
        if (species is not LateMulticellularSpecies lateSpecies)
            throw new ArgumentException("Only late multicellular species can be used on creatures");

        Species = lateSpecies;

        // TODO: set from species
        compounds.NominalCapacity = 100;

        // TODO: better mass calculation
        // TotalMass is no longer available due to microbe stage physics refactor
        // Mass = lateSpecies.BodyLayout.Sum(m => m.Size * m.CellType.TotalMass);
        Mass = lateSpecies.BodyLayout.Sum(m => m.Size * 30);

        // Setup graphics
        // TODO: handle lateSpecies.Scale
        metaballDisplayer.DisplayFromLayout(lateSpecies.BodyLayout);
    }

    /// <summary>
    ///   Applies the default movement mode this species has when spawned.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: we probably need to allow spawning in different modes for example amphibian creatures
    ///   </para>
    /// </remarks>
    public void ApplyMovementModeFromSpecies()
    {
        if (Species.ReproductionLocation != ReproductionLocation.Water)
        {
            MovementMode = MovementMode.Walking;
        }
    }

    public void SetInitialCompounds()
    {
        compounds.AddCompound(Compound.ATP, 50);
        compounds.AddCompound(Compound.Glucose, 50);
    }

    public MulticellularCreature SpawnOffspring()
    {
        var currentPosition = GlobalTransform.Origin;

        // TODO: calculate size somehow
        var separation = new Vector3(10, 0, 0);

        // Create the offspring
        var copyEntity = SpawnHelpers.SpawnCreature(Species, currentPosition + separation,
            GetParent(), SpawnHelpers.LoadMulticellularScene(), true, spawnSystem!, CurrentGame);

        // Make it despawn like normal
        // TODO: reimplement spawn system for the multicellular stage
        // spawnSystem!.NotifyExternalEntitySpawned(copyEntity);

        // TODO: some kind of resource splitting for the offspring?

        PlaySoundEffect("res://assets/sounds/soundeffects/reproduction.ogg");

        return copyEntity;
    }

    public void BecomeFullyGrown()
    {
        // TODO: implement growth
        // Once growth is added check spawnSystem.IsUnderEntityLimitForReproducing before calling SpawnOffspring
    }

    public void ResetGrowth()
    {
        // TODO: implement growth
    }

    public void Damage(float amount, string source)
    {
        if (IsPlayerCreature && CheatManager.GodMode)
            return;

        if (amount == 0 || Dead)
            return;

        if (string.IsNullOrEmpty(source))
            throw new ArgumentException("damage type is empty");

        // if (amount < 0)
        // {
        //     GD.PrintErr("Trying to deal negative damage");
        //     return;
        // }

        // TODO: sound

        // TODO: show damage visually
        // Flash(1.0f, new Color(1, 0, 0, 0.5f), 1);

        // TODO: hitpoints and death
        // if (Hitpoints <= 0.0f)
        // {
        //     Hitpoints = 0.0f;
        //     Kill();
        // TODO: kill method needs to call DropAll() and CancelStructurePlacing
        // }
    }

    public void PlaySoundEffect(string effect, float volume = 1.0f)
    {
        // TODO: make these sound objects only be loaded once
        // var sound = GD.Load<AudioStream>(effect);

        // TODO: implement sound playing, should probably create a helper method to share with Microbe

        // Find a player not in use or create a new one if none are available.
        var player = otherAudioPlayers.Find(p => !p.Playing);

        if (player == null)
        {
            // If we hit the player limit just return and ignore the sound.
            if (otherAudioPlayers.Count >= Constants.MAX_CONCURRENT_SOUNDS_PER_ENTITY)
                return;

            player = new AudioStreamPlayer3D();
            player.MaxDistance = 100.0f;
            player.Bus = "SFX";

            AddChild(player);
            otherAudioPlayers.Add(player);
        }

        player.VolumeDb = MathF.LinearToDb(volume);
        player.Stream = sound;
        player.Play();
    }

    public void SwimUpOrJump(double delta)
    {
        if (MovementMode == MovementMode.Swimming)
        {
            targetSwimLevel += upDownSwimSpeed * (float)delta;
        }
        else
        {
            // TODO: only allow jumping when touching the ground
            // TODO: suppress jump when the user just interacted with a dialog to confirm something, maybe jump should
            // use the on press key thing to only trigger jumping once?
            ApplyCentralImpulse(new Vector3(0, 1, 0) * (float)delta * 12000);
        }
    }

    public void SwimDownOrCrouch(double delta)
    {
        // TODO: crouching
        targetSwimLevel -= upDownSwimSpeed * (float)delta;
    }

    public bool DeleteWorldEntity(IInteractableEntity entity)
    {
        // TODO: could verify the interact distance etc. here
        // If the above TODO is done then probably the crafting action should have test methods to verify that it can
        // consume all of the items first, before attempting the craft to not consume partial resources
        entity.DestroyDetachAndQueueFree();
        return true;
    }

    public void DirectlyDropEntity(IInteractableEntity entity)
    {
        HandleEntityDrop(entity, entity.EntityNode);
    }

    public IEnumerable<InventorySlotData> ListInventoryContents()
    {
        return inventorySlots;
    }

    public IEnumerable<InventorySlotData> ListEquipmentContents()
    {
        yield return handSlot;
    }

    public bool IsItemSlotMoveAllowed(int fromSlotId, int toSlotId)
    {
        // TODO: implement slot type restrictions

        // TODO: non-hand equipment slots should only take equipment of the right type

        return true;
    }

    public void CancelCurrentAction()
    {
        if (!ActionInProgress)
            return;

        totalActionRequiredTime = 0;

        // Reset the shown progress
        var target = actionTarget?.Value;
        if (target != null)
        {
            performedActionTime = 0;
            UpdateActionTargetProgress(target);
        }

        ActionInProgress = false;
        actionTarget = null;
    }

    public void OnStructureTypeSelected(StructureDefinition structureDefinition)
    {
        // Just to be safe, cancel existing placing
        CancelStructurePlacing();

        buildingTypeToPlace = structureDefinition;

        // Show the ghost where it is about to be placed
        buildingToPlaceGhost = buildingTypeToPlace.GhostScene.Instantiate<Node3D>();

        // TODO: should we add the ghost to our child or keep it in the world?
        GetParent().AddChild(buildingToPlaceGhost);

        buildingToPlaceGhost.GlobalTransform = GetStructurePlacementLocation();

        // TODO: disallow placing when overlaps with physics objects (and show ghost with red tint)
    }

    
    public void AttemptStructurePlace()
    {
        if (buildingTypeToPlace == null)
            return;

        // TODO: check placement location being valid
        var location = GetStructurePlacementLocation();

        // Take the resources the construction takes
        var usedResources = this.FindRequiredResources(buildingTypeToPlace.ScaffoldingCost);

        if (usedResources == null)
        {
            GD.Print("Not enough resources to start structure after all");

            // TODO: play invalid placement sound
            return;
        }

        foreach (var usedResource in usedResources)
        {
            if (!DeleteItem(usedResource))
            {
                GD.PrintErr("Resource for placing structure consuming failed");
                return;
            }
        }

        // Create the structure entity
        var structureScene = SpawnHelpers.LoadStructureScene();

        SpawnHelpers.SpawnStructure(buildingTypeToPlace, location, GetParent(), structureScene);

        // Stop showing the ghost
        CancelStructurePlacing();
    }

    public void CancelStructurePlacing()
    {
        if (!IsPlacingStructure)
            return;

        buildingToPlaceGhost?.QueueFree();
        buildingToPlaceGhost = null;

        buildingTypeToPlace = null;
    }

    public bool GetAndConsumeActionSuccess()
    {
        if (actionHasSucceeded)
        {
            actionHasSucceeded = false;
            return true;
        }

        return false;
    }

    public Dictionary<WorldResource, int> CalculateWholeAvailableResources()
    {
        return this.CalculateAvailableResources();
    }

    private bool PickupToSlot(IInteractableEntity item, InventorySlotData slot)
    {
        if (slot.ContainedItem != null)
            return false;

        slot.ContainedItem = item;

        var targetNode = item.EntityNode;

        if (targetNode.GetParent() != null)
        {
            // Remove the object from the world
            targetNode.ReParent(this);
        }
        else
        {
            // We are picking up a crafting result or another entity that is not in the world
            AddChild(targetNode);
        }

        SetItemPositionInSlot(slot, targetNode);

        // Add the object to be carried
        carriedObjects.Add(item);

        // Would be very annoying to keep getting the prompt to interact with the object
        item.InteractionDisabled = true;

        // Surprise surprise, the physics detach bug can also hit here
        if (targetNode is RigidBody3D entityPhysics)
        {
            entityPhysics.Freeze = true;
            entityPhysics.FreezeMode = FreezeModeEnum.Kinematic;
        }

        return true;
    }

    private void HandleEntityDrop(IInteractableEntity item, Node3D entityNode)
    {
        // TODO: drop position based on creature size, and also confirm the drop point is free from other physics
        // objects

        var offset = new Vector3(0, 1.5f, 3.6f);

        // Assume our parent is the world
        var world = GetParent() ?? throw new Exception("Creature has no parent to place dropped entity in");

        var ourTransform = GlobalTransform;

        // Handle directly dropped entities that haven't been anywhere yet
        if (entityNode.GetParent() == null)
        {
            world.AddChild(entityNode);
        }
        else
        {
            entityNode.ReParent(world);
        }

        entityNode.GlobalPosition = ourTransform.Origin + ourTransform.Basis.GetRotationQuaternion() * offset;

        // Allow others to interact with the object again
        item.InteractionDisabled = false;

        if (entityNode is RigidBody3D entityPhysics)
            entityPhysics.Freeze = false;
    }

    private void SetItemPositionInSlot(InventorySlotData slot, Node3D node)
    {
        // TODO: inventory carried items should not be shown in the world

        // TODO: better positioning and actually attaching it to the place the object is carried in
        var offset = new Vector3(-0.5f, 2.7f, 1.5f + 2.5f * slot.Id);

        node.Position = offset;
    }

    private void StartAction(IInteractableEntity target, float totalDuration)
    {
        if (ActionInProgress)
            CancelCurrentAction();

        ActionInProgress = true;
        actionTarget = new EntityReference<IInteractableEntity>(target);
        performedActionTime = 0;
        totalActionRequiredTime = totalDuration;
        startedActionPosition = GlobalPosition;
    }

    private void UpdateActionStatus(float delta)
    {
        if (!ActionInProgress)
            return;

        // If moved too much, cancel
        if (GlobalPosition.DistanceSquaredTo(startedActionPosition) > Constants.ACTION_CANCEL_DISTANCE)
        {
            // TODO: play an action cancel sound
            CancelCurrentAction();
            return;
        }

        // If target is gone, cancel the action
        var target = actionTarget?.Value;
        if (target == null)
        {
            // TODO: play an action cancel sound
            CancelCurrentAction();
            return;
        }

        // Update the time to update the progress value
        performedActionTime += delta;

        if (performedActionTime >= totalActionRequiredTime)
        {
            // Action is now complete
            SetActionTargetAsCompleted(target);
            ActionInProgress = false;
            actionTarget = null;
        }
        else
        {
            UpdateActionTargetProgress(target);
        }
    }

    private void UpdateActionTargetProgress(IInteractableEntity target)
    {
        if (target is IProgressReportableActionSource progressReportable)
        {
            progressReportable.ReportActionProgress(ActionProgress);
        }
    }

    private void SetActionTargetAsCompleted(IInteractableEntity target)
    {
        if (target is ITimedActionSource actionSource)
        {
            actionSource.OnFinishTimeTakingAction();
        }
        else
        {
            GD.PrintErr("Cannot report finished action to unknown entity type");
        }
    }

    private Transform3D GetStructurePlacementLocation()
    {
        if (buildingTypeToPlace == null)
            throw new InvalidOperationException("No structure type selected");

        var relative = new Vector3(0, 0, 1) * buildingTypeToPlace.WorldSize.Z * 1.3f;

        // TODO: a raycast to get the structure on the ground
        // Also for player creature, taking the camera direction into account instead of the creature rotation would
        // be better
        var transform = GlobalTransform;
        var rotation = transform.Basis.GetRotationQuaternion();

        var worldTransform = new Transform3D(new Basis(rotation), transform.Origin + rotation * relative);
        return worldTransform;
    }
    */
}
