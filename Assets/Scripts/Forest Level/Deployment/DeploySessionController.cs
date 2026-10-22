using System.Collections.Generic;
using BetterEventBus;
using BetterPooling;
using Forestlevel;
using UnityEngine;

public class DeploySessionController : IDeployModule,
    IGamePlayEventListener<DeploySessionRequestedEvent>,
    IGamePlayEventListener<AppleCollectedEvent>,
    IGamePlayEventListener<WakeMeterFullEvent>
{
    readonly AppleSpawner spawner;
    readonly DeploySettings settings;
    AppleDeployScheduler scheduler;
    DeployZone zone;
    int progress;

    public DeploySessionController(AppleSpawner spawner, DeploySettings settings)
    { this.spawner = spawner; this.settings = settings; }

    public void Enable()
    {
        GameEventBus.Register<DeploySessionRequestedEvent>(this);
        GameEventBus.Register<AppleCollectedEvent>(this);
        GameEventBus.Register<WakeMeterFullEvent>(this);
    }

    public void Disable()
    {
        GameEventBus.Unregister<DeploySessionRequestedEvent>(this);
        GameEventBus.Unregister<AppleCollectedEvent>(this);
        GameEventBus.Unregister<WakeMeterFullEvent>(this);
        Abort();
    }

    public void Tick(float dt) => scheduler?.Tick(dt);

    public void OnGamePlayEvent(DeploySessionRequestedEvent e)
    {
        if (zone != null) return;
        zone = e.Zone;
        progress = 0;

        var pacer = new ApplePacer(zone, settings.MaxConcurrent, settings.MinSpawnInterval);
        scheduler = new AppleDeployScheduler(pacer, spawner, zone.SpawnTable, settings.WarningDuration);

        zone.SetSessionActive(true);
        GameEventBus.Raise(new DeploySessionStartedEvent(zone));
        GameEventBus.Raise(new DeployProgressEvent(0, zone.CompletionRequirement));
    }

    public void OnGamePlayEvent(AppleCollectedEvent e)
    {
        if (zone == null) return;
        progress += e.Apple.Definition.ProgressValue;
        GameEventBus.Raise(new DeployProgressEvent(progress, zone.CompletionRequirement));
        if (progress >= zone.CompletionRequirement) End(won: true);
    }

    public void OnGamePlayEvent(WakeMeterFullEvent e) => End(won: false);

    void End(bool won)
    {
        if (zone == null) return;

        scheduler.Stop();
        scheduler = null;

        var finished = zone;
        zone = null;
        if (won) finished.IsCompleted = true;
        finished.SetSessionActive(false);
        GameEventBus.Raise(new DeploySessionEndedEvent(finished, won));
    }

    // Used when the module is switched off mid-session: tidy up without announcing a result.
    void Abort()
    {
        if (zone == null) return;
        scheduler.Stop();
        scheduler = null;
        zone.SetSessionActive(false);
        zone = null;
    }
}

public class AppleDeployScheduler
{
    struct Pending { public CountdownTimer Timer; public AppleDefinitionSO Apple; }

    readonly ApplePacer pacer;
    readonly AppleSpawner spawner;
    readonly AppleSpawnTable table;
    readonly float warningDuration;
    readonly Dictionary<AppleDeployer, Pending> pending = new();
    readonly List<AppleDeployer> finished = new();   // reused every frame
    float elapsed;
    bool showingWarning;

    public AppleDeployScheduler(ApplePacer pacer, AppleSpawner spawner, AppleSpawnTable table, float warningDuration)
    { this.pacer = pacer; this.spawner = spawner; this.table = table; this.warningDuration = warningDuration; }

    public void Tick(float dt)
    {
        elapsed += dt;

        foreach (var p in pending.Values) p.Timer.Tick(dt);   // OnTimerStop only queues into 'finished'
        foreach (var d in finished) Deploy(d);
        finished.Clear();

        RaiseNextWarning();
        if (pacer.TryGetNextDeployer(dt, out var next)) BeginWarning(next);
    }

    void BeginWarning(AppleDeployer deployer)
    {
        var apple = table.Pick(elapsed);   // chosen now so the HUD can show what's coming
        deployer.MarkBusy();
        GameEventBus.Raise(new AppleWarningStartedEvent(deployer.transform));

        var timer = new CountdownTimer(warningDuration);
        timer.OnTimerStop += () => finished.Add(deployer);
        timer.Start();
        pending[deployer] = new Pending { Timer = timer, Apple = apple };
    }

    void Deploy(AppleDeployer deployer)
    {
        var p = pending[deployer];
        pending.Remove(deployer);
        GameEventBus.Raise(new AppleWarningEndedEvent(deployer.transform));
        spawner.Spawn(p.Apple, deployer.transform.position);
        deployer.MarkFree();
    }

    void RaiseNextWarning()
    {
        if (pending.Count == 0) { ClearWarning(); return; }

        AppleDeployer soonest = null; Pending best = default; float bestTime = float.MaxValue;
        foreach (var kvp in pending)
            if (kvp.Value.Timer.CurrentTime < bestTime)
            { soonest = kvp.Key; best = kvp.Value; bestTime = best.Timer.CurrentTime; }

        showingWarning = true;
        GameEventBus.Raise(new NextWarningEvent(soonest, best.Apple, bestTime));
    }

    void ClearWarning()
    {
        if (!showingWarning) return;
        showingWarning = false;
        GameEventBus.Raise(new NextWarningClearedEvent());
    }

    public void Stop()   // the old EndSession cleanup
    {
        foreach (var d in pending.Keys)
        {
            d.MarkFree();
            GameEventBus.Raise(new AppleWarningEndedEvent(d.transform));
        }
        pending.Clear(); finished.Clear();
        spawner.ReleaseAll();
        ClearWarning();
    }
}

public interface IAppleOwner { void Release(Apple apple); }

public class AppleSpawner : IAppleOwner
{
    readonly Dictionary<AppleDefinitionSO, ISingleObjectPool<Apple>> pools = new();
    readonly HashSet<Apple> active = new();
    readonly int capacity;

    public AppleSpawner(int capacity) { this.capacity = capacity; }

    public void Spawn(AppleDefinitionSO def, Vector3 position)
    {
        if (!pools.TryGetValue(def, out var pool))
            pools[def] = pool = new SingleObjectPool<Apple>(prefab: def.Prefab, parent: null, defaultCapacity: capacity, maxSize: capacity);

        var apple = pool.Get();
        apple.Init(def, this);
        apple.transform.position = position;
        active.Add(apple);
        apple.Drop();
    }

    public void Release(Apple apple)
    {
        if (!active.Remove(apple)) return;   // idempotent, see the note below
        apple.ResetPhysics();
        pools[apple.Definition].Release(apple);
    }

    public void ReleaseAll()   // session ended with apples still in the air
    {
        foreach (var apple in active) { apple.ResetPhysics(); pools[apple.Definition].Release(apple); }
        active.Clear();
    }
}

public readonly struct DeploySessionRequestedEvent : IGameplayEvent { public readonly DeployZone Zone; public DeploySessionRequestedEvent(DeployZone z) { Zone = z; } }
public readonly struct DeploySessionStartedEvent   : IGameplayEvent { public readonly DeployZone Zone; public DeploySessionStartedEvent(DeployZone z) { Zone = z; } }
public readonly struct DeploySessionEndedEvent     : IGameplayEvent { public readonly DeployZone Zone; public readonly bool Won; public DeploySessionEndedEvent(DeployZone z, bool won) { Zone = z; Won = won; } }
public readonly struct DeployProgressEvent         : IGameplayEvent { public readonly int Caught, Required; public DeployProgressEvent(int c, int r) { Caught = c; Required = r; } }
public readonly struct NextWarningEvent            : IGameplayEvent { public readonly AppleDeployer Deployer; public readonly AppleDefinitionSO Apple; public readonly float Remaining; public NextWarningEvent(AppleDeployer d, AppleDefinitionSO a, float r) { Deployer = d; Apple = a; Remaining = r; } }
public readonly struct NextWarningClearedEvent     : IGameplayEvent { }

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