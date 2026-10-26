using BetterEventBus;

public class DeployZonePrompt : IDeployModule,
    IGamePlayEventListener<DeployZoneRangeEvent>,
    IGamePlayEventListener<DeploySessionStartedEvent>,
    IGamePlayEventListener<DeploySessionEndedEvent>
{
    readonly InputReaderSO input;
    DeployZone inRangeZone;
    bool sessionActive;

    bool CanStart => !sessionActive && inRangeZone != null && !inRangeZone.IsCompleted;
    public DeployZonePrompt(InputReaderSO input) { this.input = input; }

    public void Enable()
    {
        GameEventBus.Register<DeployZoneRangeEvent>(this);
        GameEventBus.Register<DeploySessionStartedEvent>(this);
        GameEventBus.Register<DeploySessionEndedEvent>(this);
        input.Interact += OnInteract;
    }

    public void Disable()
    {
        GameEventBus.Unregister<DeployZoneRangeEvent>(this);
        GameEventBus.Unregister<DeploySessionStartedEvent>(this);
        GameEventBus.Unregister<DeploySessionEndedEvent>(this);
        input.Interact -= OnInteract;
    }
    public void OnGamePlayEvent(DeployZoneRangeEvent e)
    {
        if (e.InRange) inRangeZone = e.Zone;
        else if (inRangeZone == e.Zone) inRangeZone = null;
        Refresh();
    }
    public void OnGamePlayEvent(DeploySessionStartedEvent e) { sessionActive = true;  Refresh(); }
    public void OnGamePlayEvent(DeploySessionEndedEvent e)   { sessionActive = false; Refresh(); }

    void Refresh() => GameEventBus.Raise(new InteractPromptEvent(CanStart, CanStart ? inRangeZone.PromptText : ""));

    void OnInteract()
    {
        if (CanStart) GameEventBus.Raise(new DeploySessionRequestedEvent(inRangeZone));
    }

}
