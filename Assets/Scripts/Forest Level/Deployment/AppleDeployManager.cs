using System.Collections.Generic;
using BetterEventBus;
using BetterPooling;
using BetterSingletons;
using Forestlevel;
using UnityEngine;

public class AppleDeployManager : Singleton<AppleDeployManager>,
    IGamePlayEventListener<DeployZoneRangeEvent>,
    IGamePlayEventListener<LevelWonEvent>,
    IGamePlayEventListener<LevelLostEvent>
{
    enum Phase { Idle, InRange, Playing }

    [SerializeField] Apple ApplePrefab;
    [SerializeField] int defaultCapacity = 10;
    [SerializeField] int maxSize = 10;
    [SerializeField] InputReaderSO input;

    [Header("Pacing")]
    [SerializeField] int maxConcurrent = 2;
    [SerializeField] float minSpawnInterval = 1.5f;
    [SerializeField] float warningDuration = 3f;

    ISingleObjectPool<Apple> applePool;
    ApplePacer pacer;
    Phase phase = Phase.Idle;
    DeployZone inRangeZone, activeZone;
    readonly Dictionary<AppleDeployer, CountdownTimer> activeCountdowns = new();

    protected override void Awake()
    {
        base.Awake();
        applePool = new SingleObjectPool<Apple>(prefab: ApplePrefab,parent:null,defaultCapacity: defaultCapacity, maxSize);
    }

    void OnEnable()
    {
        GameEventBus.Register<DeployZoneRangeEvent>(this);
        GameEventBus.Register<LevelWonEvent>(this);
        GameEventBus.Register<LevelLostEvent>(this);
        input.Interact += OnInteract;   // use whatever your InputReaderSO exposes
    }
    void OnDisable()
    {
        GameEventBus.Unregister<DeployZoneRangeEvent>(this);
        GameEventBus.Unregister<LevelWonEvent>(this);
        GameEventBus.Unregister<LevelLostEvent>(this);
        input.Interact -= OnInteract;
    }

    void Update()
    {
        if (phase != Phase.Playing) return;
        TickCountdowns(Time.deltaTime);
        UpdateWarningDisplay();
        if (pacer.TryGetNextDeployer(Time.deltaTime, out var d)) BeginWarning(d);
    }

    // ---- zone / session flow ----
    public void OnGamePlayEvent(DeployZoneRangeEvent e)
    {
        if (e.InRange) inRangeZone = e.Zone;
        else if (inRangeZone == e.Zone) inRangeZone = null;
        RefreshPrompt();
    }

    void RefreshPrompt()
    {
        if (phase == Phase.Playing) return;
        bool show = inRangeZone != null && !inRangeZone.IsCompleted;
        phase = show ? Phase.InRange : Phase.Idle;
        GameEventBus.Raise(new InteractPromptEvent(show, show ? inRangeZone.PromptText : ""));
    }

    void OnInteract()
    {
        if (phase != Phase.InRange) return;
        activeZone = inRangeZone;
        pacer = new ApplePacer(activeZone, maxConcurrent, minSpawnInterval);
        phase = Phase.Playing;
        GameEventBus.Raise(new InteractPromptEvent(false, ""));
        GameEventBus.Raise(new DeploySessionStartedEvent(activeZone));
    }

    void EndSession(bool won)
    {
        if (phase != Phase.Playing) return;

        foreach (var kvp in activeCountdowns)
        {
            kvp.Key.MarkFree();
            GameEventBus.Raise(new AppleWarningEndedEvent(kvp.Key.transform));
        }
        activeCountdowns.Clear();
        AppleIndicatorManager.Instance.Hide();

        var zone = activeZone;
        if (won) zone.IsCompleted = true;
        activeZone = null;
        pacer = null;
        phase = Phase.Idle;

        GameEventBus.Raise(new DeploySessionEndedEvent(zone, won));
        RefreshPrompt();   // lost + still standing in range -> prompt returns for a retry
    }

    public void OnGamePlayEvent(LevelWonEvent e)  => EndSession(true);
    public void OnGamePlayEvent(LevelLostEvent e) => EndSession(false);

    // ---- warning / deploy (unchanged apart from the UI call) ----
    void BeginWarning(AppleDeployer deployer)
    {
        deployer.MarkBusy();
        GameEventBus.Raise(new AppleWarningStartedEvent(deployer.transform));

        var countdown = new CountdownTimer(warningDuration);
        countdown.OnTimerStop += () =>
        {
            activeCountdowns.Remove(deployer);
            GameEventBus.Raise(new AppleWarningEndedEvent(deployer.transform));
            DeployAt(deployer);
        };
        countdown.Start();
        activeCountdowns[deployer] = countdown;
    }

    void TickCountdowns(float dt)
    {
        foreach (var c in new List<CountdownTimer>(activeCountdowns.Values)) c.Tick(dt);
    }

    void DeployAt(AppleDeployer deployer)
    {
        var apple = applePool.Get();
        apple.transform.position = deployer.transform.position;
        apple.StartFalling(deployer);
        deployer.MarkFree();
    }

    public void ReturnToPool(Apple apple) => applePool.Release(apple);

    void UpdateWarningDisplay()
    {
        AppleDeployer soonest = null; CountdownTimer soonestTimer = null;
        foreach (var kvp in activeCountdowns)
            if (soonestTimer == null || kvp.Value.CurrentTime < soonestTimer.CurrentTime)
            { soonest = kvp.Key; soonestTimer = kvp.Value; }

        var counter = UIManager.Instance.WarningCounter;
        if (soonest == null) { counter.Clear(); AppleIndicatorManager.Instance.Hide(); return; }

        counter.UpdateDisplay(soonestTimer.CurrentTime);
        AppleIndicatorManager.Instance.PointAt(soonest);
    }
}

public readonly struct DeployZoneRangeEvent : IGameplayEvent   // zone -> manager
{
    public readonly DeployZone Zone; 
    public readonly bool InRange;
    public DeployZoneRangeEvent(DeployZone z, bool inRange) { Zone = z; InRange = inRange; }
}

public readonly struct InteractPromptEvent : IGameplayEvent    // manager -> UIManager
{
    public readonly bool Visible; 
    public readonly string Text;
    public InteractPromptEvent(bool v, string t) { 
        Visible = v; 
        Text = t; 
    }
}

public readonly struct DeploySessionStartedEvent : IGameplayEvent
{
    public readonly DeployZone Zone;
    public DeploySessionStartedEvent(DeployZone z) { 
        Zone = z; 

        //Raising Camera type change = Objective
        GameEventBus.Raise<CameraChangeEvent>(new CameraChangeEvent(Forestlevel.CameraType.InGame));
        GameEventBus.Raise<PlayerLocationEvent>(new PlayerLocationEvent(z.PlayerSpawn.position)); 
        GameEventBus.Raise<CameraTrackTargetEvent>(new CameraTrackTargetEvent(z.CameraAnchor));

        // Raise Game State event
        GameEventBus.Raise<InGameGameStateEvent>(new InGameGameStateEvent());

    }
}
public readonly struct DeploySessionEndedEvent : IGameplayEvent
{
    public readonly DeployZone Zone; 
    public readonly bool Won;
    public DeploySessionEndedEvent(DeployZone z, bool won) { 
        Zone = z; 
        Won = won;

        //Raising Camera type change = Explorative
        GameEventBus.Raise<CameraChangeEvent>(new CameraChangeEvent(Forestlevel.CameraType.FreeLook)); 
        // Raise Game State event
        GameEventBus.Raise<ExplorationGameStateEvent>(new ExplorationGameStateEvent());
    }
}