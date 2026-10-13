using BetterSingletons;
using BetterEventBus;
using UnityEngine;
using UnityEngine.UIElements;
using Forestlevel;

public class WakeManager : Singleton<WakeManager>,
    IGamePlayEventListener<AppleDroppedEvent>,
    IGamePlayEventListener<DeploySessionStartedEvent>,
    IGamePlayEventListener<DeploySessionEndedEvent>
{
    [SerializeField] int WakeUpLimit = 50;
    [SerializeField] int disturbAmount = 10;
    [SerializeField] float lerpSpeed = 8;

    WakeMeterUIView wakemeter;
    bool sessionActive;

    void Start()   // Start, not Awake: UIManager has built its views by now
    {
        wakemeter = UIManager.Instance.WakeMeter;
        wakemeter.Configure(disturbAmount, WakeUpLimit, lerpSpeed);
        wakemeter.OnMeterFull += HandleMeterFull;
    }
    void OnDestroy() { if (wakemeter != null) wakemeter.OnMeterFull -= HandleMeterFull; }

    void OnEnable()
    {
        GameEventBus.Register<AppleDroppedEvent>(this);
        GameEventBus.Register<DeploySessionStartedEvent>(this);
        GameEventBus.Register<DeploySessionEndedEvent>(this);
    }
    void OnDisable()
    {
        GameEventBus.Unregister<AppleDroppedEvent>(this);
        GameEventBus.Unregister<DeploySessionStartedEvent>(this);
        GameEventBus.Unregister<DeploySessionEndedEvent>(this);
    }

    public void OnGamePlayEvent(DeploySessionStartedEvent e) { sessionActive = true; wakemeter.ResetMeter(); }
    public void OnGamePlayEvent(DeploySessionEndedEvent e)   => sessionActive = false;

    public void OnGamePlayEvent(AppleDroppedEvent e)
    {
        if (!sessionActive) return;   // apples still falling after a session ends must not disturb Newton
        wakemeter.Disturb();
    }

    void HandleMeterFull()
    {
        if (!sessionActive) return;
        sessionActive = false;
        GameEventBus.Raise(new WakeMeterFullEvent());
    }
}