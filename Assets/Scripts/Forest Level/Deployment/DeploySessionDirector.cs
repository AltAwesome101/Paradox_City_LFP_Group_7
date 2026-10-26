using BetterEventBus;
using Forestlevel;

public class DeploySessionDirector : IDeployModule,
    IGamePlayEventListener<DeploySessionStartedEvent>,
    IGamePlayEventListener<DeploySessionEndedEvent>
{
    public void Enable()
    {
        GameEventBus.Register<DeploySessionStartedEvent>(this);
        GameEventBus.Register<DeploySessionEndedEvent>(this);
    }

    public void Disable()
    {
        GameEventBus.Unregister<DeploySessionStartedEvent>(this);
        GameEventBus.Unregister<DeploySessionEndedEvent>(this);
    }
    public void OnGamePlayEvent(DeploySessionStartedEvent e)
    {
        GameEventBus.Raise(new CameraTrackTargetEvent(e.Zone.CameraAnchor));
        GameEventBus.Raise(new CameraChangeEvent(Forestlevel.CameraType.InGame));
        GameEventBus.Raise(new PlayerLocationEvent(e.Zone.PlayerSpawn.position));
        GameEventBus.Raise(new InGameGameStateEvent());
    }

    public void OnGamePlayEvent(DeploySessionEndedEvent e)
    {
        GameEventBus.Raise(new CameraChangeEvent(Forestlevel.CameraType.FreeLook));
        GameEventBus.Raise(new ExplorationGameStateEvent());
    }
}
