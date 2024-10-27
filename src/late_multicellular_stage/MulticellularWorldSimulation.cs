using Components;
using DefaultEcs;
using DefaultEcs.Threading;
using Godot;
using Newtonsoft.Json;
using Systems;
using World = DefaultEcs.World;

/// <summary>
///   For use in the prototypes not yet converted to using world simulations
/// </summary>
public partial class MulticellularWorldSimulation : WorldSimulationWithPhysics
{
    private readonly IParallelRunner nonParallelRunner = new DefaultParallelRunner(1);

    // Base systems
    private PathBasedSceneLoader pathBasedSceneLoader = null!;
    private PhysicsBodyControlSystem physicsBodyControlSystem = null!;
    private PhysicsBodyCreationSystem physicsBodyCreationSystem = null!;
    private PhysicsBodyDisablingSystem physicsBodyDisablingSystem = null!;
    private PhysicsCollisionManagementSystem physicsCollisionManagementSystem = null!;
    private PhysicsSensorSystem physicsSensorSystem = null!;
    private CollisionShapeLoaderSystem collisionShapeLoaderSystem = null!;
    private PhysicsUpdateAndPositionSystem physicsUpdateAndPositionSystem = null!;
    private PredefinedVisualLoaderSystem predefinedVisualLoaderSystem = null!;

    private SimpleShapeCreatorSystem simpleShapeCreatorSystem = null!;
    private SoundEffectSystem soundEffectSystem = null!;
    private SoundListenerSystem soundListenerSystem = null!;
    private SpatialAttachSystem spatialAttachSystem = null!;
    private SpatialPositionSystem spatialPositionSystem = null!;

    // Late multicellular systems
    private MulticellularVisualsSystem multicellularVisualsSystem = null!;

    private EntitySet multicellularCountingEntitySet = null!;

#pragma warning disable CA2213
    private Node visualsParent = null!;
#pragma warning restore CA2213

    [JsonIgnore]
    public TimedLifeSystem TimedLifeSystem { get; private set; } = null!;

    [JsonIgnore]
    public CameraFollowSystem CameraFollowSystem { get; private set; } = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public SpawnSystem SpawnSystem { get; private set; } = null!;

    /// <summary>
    ///   First initialization step which creates all the system objects. When loading from a save objects of this
    ///   type should have <see cref="AssignOnlyChildItemsOnDeserializeAttribute"/> and this method should be called
    ///   before those child properties are loaded.
    /// </summary>
    /// <param name="visualDisplayRoot">Godot Node to place all simulation graphics underneath</param>
    /// <param name="cloudSystem">
    ///   Compound cloud simulation system. This method will call <see cref="CompoundCloudSystem.Init"/>
    /// </param>
    /// <param name="spawnEnvironment">Spawn environment data to give to microbes spawned by systems</param>
    public void Init(Node visualDisplayRoot, CompoundCloudSystem cloudSystem, IMicrobeSpawnEnvironment spawnEnvironment)
    {
        InitGenerated();
        ResolveNodeReferences();

        visualsParent = visualDisplayRoot;

        // Threading using our task system
        IParallelRunner parallelRunner = TaskExecutor.Instance;

        if (GenerateThreadedSystems.UseCheckedComponentAccess)
        {
            GD.Print("Disallowing threaded execution to allow strict component thread checks to work");
            parallelRunner = new DefaultParallelRunner(1);
        }

        // Set on systems that can be run in parallel but aren't currently as there's no real performance improvement
        // / the system entity count per thread needs tweaking before there's any benefit
        var couldParallelize = new DefaultParallelRunner(1);

        // Systems stored in fields
        pathBasedSceneLoader = new PathBasedSceneLoader(EntitySystem, nonParallelRunner);
        physicsBodyControlSystem = new PhysicsBodyControlSystem(physics, EntitySystem, couldParallelize);
        physicsBodyDisablingSystem = new PhysicsBodyDisablingSystem(physics, EntitySystem);
        physicsBodyCreationSystem =
            new PhysicsBodyCreationSystem(this, physicsBodyDisablingSystem, EntitySystem);
        physicsCollisionManagementSystem =
            new PhysicsCollisionManagementSystem(physics, EntitySystem, couldParallelize);
        physicsSensorSystem = new PhysicsSensorSystem(this, EntitySystem);
        physicsUpdateAndPositionSystem = new PhysicsUpdateAndPositionSystem(physics, EntitySystem, couldParallelize);
        collisionShapeLoaderSystem = new CollisionShapeLoaderSystem(EntitySystem);
        predefinedVisualLoaderSystem = new PredefinedVisualLoaderSystem(EntitySystem);

        simpleShapeCreatorSystem = new SimpleShapeCreatorSystem(EntitySystem, couldParallelize);

        // TODO: different root for sounds?
        soundEffectSystem = new SoundEffectSystem(visualsParent, EntitySystem);
        soundListenerSystem = new SoundListenerSystem(visualsParent, EntitySystem);
        spatialAttachSystem = new SpatialAttachSystem(visualsParent, EntitySystem);
        spatialPositionSystem = new SpatialPositionSystem(EntitySystem);

        // Systems stored in properties
        CameraFollowSystem = new CameraFollowSystem(EntitySystem);

        // physics.RemoveGravity();

        OnInitialized();

        // In case this is loaded from a save ensure the next save has correct ignore entities
        entitiesToNotSave.SetExtraIgnoreSource(queuedForDelete);
    }

    /// <summary>
    ///   Second phase initialization that requires access to the current game info. Must also be performed, otherwise
    ///   this class won't function correctly.
    /// </summary>
    /// <param name="currentGame">Currently started game</param>
    public void InitForCurrentGame(GameProperties currentGame)
    {
        // osmoregulationAndHealingSystem.SetWorld(currentGame.GameWorld);
    }

    public void SetSimulationBiome(BiomeConditions biomeConditions)
    {
    }

    /// <summary>
    ///   Clears system data that has been stored based on the player location. Call this when the player changes
    ///   locations a lot by respawning or by moving patches
    /// </summary>
    public void ClearPlayerLocationDependentCaches()
    {
    }

    public override bool HasSystemsWithPendingOperations()
    {
        return multicellularVisualsSystem.HasPendingOperations();
    }

    public override void FreeNodeResources()
    {
        base.FreeNodeResources();

        soundEffectSystem.FreeNodeResources();
        spatialAttachSystem.FreeNodeResources();
    }

    internal void OverrideMicrobeAIRandomSeed(int seed)
    {
        // microbeAI.OverrideAIRandomSeed(seed);
    }

    protected override void InitSystemsEarly()
    {
        IParallelRunner parallelRunner = TaskExecutor.Instance;

        // See the similar if in Init to know why this is used
        if (GenerateThreadedSystems.UseCheckedComponentAccess)
        {
            parallelRunner = new DefaultParallelRunner(1);
        }

        SpawnSystem = new SpawnSystem(this);
    }

    protected override void OnProcessFixedLogic(float delta)
    {
        int availableThreads = TaskExecutor.Instance.ParallelTasks;

        var settings = Settings.Instance;
        if (settings.RunAutoEvoDuringGamePlay)
            --availableThreads;

        if (!settings.RunGameSimulationMultithreaded || GenerateThreadedSystems.UseCheckedComponentAccess)
        {
            availableThreads = 1;
        }

        // For single-threaded testing uncomment the next line:
        // availableThreads = 1;

        // Need to have more threads than configured to run with to not deadlock on all threads just waiting for
        // tasks to be able to start. Apparently with just 1 background task the deadlock never occurs but still
        // performance is reduced a lot without enough threads
        // TODO: adjust the min threads threshold here (currently +1 for slowest systems to not get hit with the
        // threading performance penalty)
        if (availableThreads > GenerateThreadedSystems.TargetThreadCount + 1)
        {
            OnProcessFixedWithThreads(delta);
        }
        else
        {
            OnProcessFixedWithoutThreads(delta);
        }
    }

    protected override void OnProcessFrameLogic(float delta)
    {
        OnProcessFrameLogicGenerated(delta);
    }

    protected override void OnEntityDestroyed(in Entity entity)
    {
        base.OnEntityDestroyed(in entity);

        physicsCollisionManagementSystem.OnEntityDestroyed(entity);
        physicsBodyDisablingSystem.OnEntityDestroyed(entity);
        physicsBodyCreationSystem.OnEntityDestroyed(entity);
        physicsSensorSystem.OnEntityDestroyed(entity);
    }

    protected override void OnPlayerPositionSet(Vector3 playerPosition)
    {
        // Immediately report to some systems
        soundEffectSystem.ReportPlayerPosition(playerPosition);
    }

    protected override int EstimateThreadsUtilizedBySystems()
    {
        var estimateCellCount = multicellularCountingEntitySet.Count;

        return 1 + estimateCellCount / Constants.SIMULATION_CELLS_PER_THREAD_ESTIMATE;
    }

    protected override void Dispose(bool disposing)
    {
        // Must disable recording to avoid dispose exceptions from metrics reporting
        physics.DisablePhysicsTimeRecording = true;
        WaitForStartedPhysicsRun();

        if (disposing)
        {
            nonParallelRunner.Dispose();

            // If disposed before Init is called problems will happen without this check. This happens for example if
            // loading a save made in the editor and quitting the game without exiting the editor first to the microbe
            // stage.
            if (pathBasedSceneLoader != null!)
            {
                pathBasedSceneLoader.Dispose();
                physicsBodyControlSystem.Dispose();
                physicsBodyCreationSystem.Dispose();
                physicsBodyDisablingSystem.Dispose();
                physicsCollisionManagementSystem.Dispose();
                physicsSensorSystem.Dispose();
                physicsUpdateAndPositionSystem.Dispose();
                collisionShapeLoaderSystem.Dispose();
                predefinedVisualLoaderSystem.Dispose();
                simpleShapeCreatorSystem.Dispose();
                soundEffectSystem.Dispose();
                soundListenerSystem.Dispose();
                spatialAttachSystem.Dispose();
                spatialPositionSystem.Dispose();

                CameraFollowSystem.Dispose();
                TimedLifeSystem.Dispose();

                multicellularCountingEntitySet.Dispose();
            }

            if (SpawnSystem != null!)
                SpawnSystem.Dispose();

            DisposeGenerated();
        }

        base.Dispose(disposing);
    }
}
