using System.Collections.Generic;
using BetterEventBus;
using Forestlevel;

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
