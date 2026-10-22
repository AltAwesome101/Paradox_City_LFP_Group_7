using BetterEventBus;

public class DeployHudPresenter : IDeployModule,
    IGamePlayEventListener<NextWarningEvent>,
    IGamePlayEventListener<NextWarningClearedEvent>,
    IGamePlayEventListener<DeployProgressEvent>
{
    readonly UIManager ui;

    public DeployHudPresenter(UIManager ui) { this.ui = ui; }

    public void Enable()
    {
        GameEventBus.Register<NextWarningEvent>(this);
        GameEventBus.Register<NextWarningClearedEvent>(this);
        GameEventBus.Register<DeployProgressEvent>(this);
    }

    public void Disable()
    {
        GameEventBus.Unregister<NextWarningEvent>(this);
        GameEventBus.Unregister<NextWarningClearedEvent>(this);
        GameEventBus.Unregister<DeployProgressEvent>(this);
    }

    public void OnGamePlayEvent(NextWarningEvent e)
    {
        ui.WarningCounter.UpdateDisplay(e.Remaining);
        ui.Indicator.PointAt(e.Deployer);
        // e.Apple.AppleColor is available if you want to tint the warning
    }
    public void OnGamePlayEvent(NextWarningClearedEvent e) { ui.WarningCounter.Clear(); ui.Indicator.Clear(); }
    public void OnGamePlayEvent(DeployProgressEvent e) => ui.Collection.UpdateDisplay($"{e.Caught}/{e.Required} Apples Caught");
}
