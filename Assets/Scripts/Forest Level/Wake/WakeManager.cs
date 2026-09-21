using BetterSingletons;
using BetterEventBus;
using UnityEngine;
using UnityEngine.UIElements;
using Forestlevel;

public class WakeManager : Singleton<WakeManager>, IGamePlayEventListener<AppleDroppedEvent>
,IGamePlayEventListener<InGameGameStateEvent>
{
    [SerializeField] UIDocument document;
    [SerializeField] int WakeUpLimit;
    [SerializeField] int disturbAmount;
    [SerializeField] float lerpSpeed = 8;

    WakemeterUI wakemeter;
    bool hasWoken;

    protected override void Awake()
    {
        base.Awake();
        var progress = document.rootVisualElement.Q<ProgressBar>("wake-progressbar");
        wakemeter = new(progress, disturbAmount, WakeUpLimit, lerpSpeed);
        wakemeter.OnMeterFull += HandleMeterFull;
        wakemeter.HideProgress();
    }

    void OnEnable(){
        GameEventBus.Register<AppleDroppedEvent>(this);
        GameEventBus.Register<InGameGameStateEvent>(this);
    }
    void OnDisable(){
        GameEventBus.Unregister<AppleDroppedEvent>(this);
        GameEventBus.Unregister<InGameGameStateEvent>(this);
    }

    public void OnGamePlayEvent(AppleDroppedEvent gameplayEvent)
    {
        if (hasWoken) return;
        wakemeter.Disturb();
    }

    void HandleMeterFull()
    {
        if (hasWoken) return;
        hasWoken = true;
        GameEventBus.Raise(new LevelLostEvent());
    }

    public void OnGamePlayEvent(InGameGameStateEvent gameplayEvent) => wakemeter.ShowProgress();
}