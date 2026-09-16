using UnityEngine;
using BetterPooling;
using BetterSingletons;
using BetterEventBus;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class AppleDeployManager : Singleton<AppleDeployManager>,
    IGamePlayEventListener<LevelWonEvent>,
    IGamePlayEventListener<LevelLostEvent>
{
    [SerializeField] Apple ApplePrefab;
    [SerializeField] int defaultCapacity = 10;
    [SerializeField] int maxSize = 10;

    [Header("Pacing")]
    [SerializeField] DeployRegistry deployerRegistry;
    [SerializeField] List<DeployerPair> conflictPairs;
    [SerializeField] int maxConcurrent = 2;
    [SerializeField] float minSpawnInterval = 1.5f;
    [SerializeField] float warningDuration = 3f;

    [Header("Warning UI")]
    [SerializeField] UIDocument document;
    [SerializeField] AppleWarningCounterDataSO Data;
    [SerializeField] Transform player;
    [SerializeField] Camera cam;
    AppleWarningCounterUI warningCounterUI;

    ISingleObjectPool<Apple> applePool;
    ApplePacer pacer;
    bool deploymentHalted;

    readonly Dictionary<AppleDeployer, CountdownTimer> activeCountdowns = new();

    //TODO: Refactor responsibility so the WarningCounterUI doesnt need to be in the AppleDeployManager
    protected override void Awake()
    {
        base.Awake();
        applePool = new SingleObjectPool<Apple>(prefab: ApplePrefab, defaultCapacity: defaultCapacity, maxSize: maxSize);
        pacer = new ApplePacer(deployerRegistry, conflictPairs, maxConcurrent, minSpawnInterval);
        var warningCounterLabel = document.rootVisualElement.Q<Label>("approachingApple-counter-label");
        warningCounterUI = new AppleWarningCounterUI(warningCounterLabel,Data);
    }

    void OnEnable()
    {
        GameEventBus.Register<LevelWonEvent>(this);
        GameEventBus.Register<LevelLostEvent>(this);
    }

    void OnDisable()
    {
        GameEventBus.Unregister<LevelWonEvent>(this);
        GameEventBus.Unregister<LevelLostEvent>(this);
    }

    void Update()
    {
        if (deploymentHalted) return;

        TickCountdowns(Time.deltaTime);
        UpdateWarningDisplay();

        if (pacer.TryGetNextDeployer(Time.deltaTime, out var deployer))
            BeginWarning(deployer);
    }

    void BeginWarning(AppleDeployer deployer)
    {
        deployer.MarkBusy();

        var countdown = new CountdownTimer(warningDuration);
        // NOTE: guessing at AppleIndicatorManager's API here — swap these two calls
        // for whatever it actually exposes.
        countdown.OnTick += remaining => {/*TODO: drive the countdown visual logic in here*/};
        countdown.OnTimerStop += () =>
        {
            activeCountdowns.Remove(deployer);
            DeployAt(deployer);
        };

        countdown.Start();
        activeCountdowns[deployer] = countdown;
    }

    void TickCountdowns(float deltaTime)
    {
        // snapshot since OnComplete mutates the dictionary mid-loop
        foreach (var countdown in new List<CountdownTimer>(activeCountdowns.Values))
            countdown.Tick(deltaTime);
    }

    public void ReturnToPool(Apple apple) => applePool.Release(apple);

    void DeployAt(AppleDeployer deployer)
    {
        var apple = applePool.Get();
        Debug.Log($"Apple deployed at {deployer.transform.position.ToString().WithBold().WithColour(Color.coral)}");
        apple.transform.position = deployer.transform.position;
        apple.StartFalling(deployer);
    }

    void UpdateWarningDisplay()
    {
        AppleDeployer soonest = null;
        CountdownTimer soonestTimer = null;

        foreach (var kvp in activeCountdowns)
        {
            if (soonestTimer == null || kvp.Value.CurrentTime < soonestTimer.CurrentTime)
            {
                soonest = kvp.Key;
                soonestTimer = kvp.Value;
            }
        }

        if (soonest == null) { 
            warningCounterUI.Hide(); 
            return; 
        }


        warningCounterUI.UpdateDisplay(soonestTimer.CurrentTime);
    }


    public void OnGamePlayEvent(LevelWonEvent gameplayEvent) => deploymentHalted = true;
    public void OnGamePlayEvent(LevelLostEvent gameplayEvent) => deploymentHalted = true;
}