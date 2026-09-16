using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using BetterPooling;
using BetterSingletons;
using BetterEventBus;
// Owns the shared anchor + pool. This is what the rest of your game talks to.
public class AppleIndicatorManager : Singleton<AppleIndicatorManager>,
    IGamePlayEventListener<AppleAirborneEvent>,
    IGamePlayEventListener<AppleDroppedEvent>,
    IGamePlayEventListener<AppleCollectedEvent>
{
#region fields
        [SerializeField] VisualTreeAsset arrowTemplate;
        [SerializeField] UIDocument uiDocument;
        [SerializeField] Camera cam;
        [SerializeField] float angleThreshold = 0.5f;
        [SerializeField] float clusterThreshold = 15f;   // degrees
        [SerializeField] float angularStep = 20f;        // degrees
        [SerializeField] float radius = 150f;            // pixels
#endregion

    VisualElement anchor;
    VisualElementPool<TemplateContainer> pool;
    readonly Dictionary<Transform, AppleDirectionIndicator> active = new();

    protected override void Awake()
    {
        base.Awake();
        anchor = uiDocument.rootVisualElement.Q<VisualElement>("applenotifier-element-archor");

        pool = new VisualElementPool<TemplateContainer>(
            createFunc: () => arrowTemplate.CloneTree(),
            container: anchor
        );
    }

        void OnEnable()
    {
        GameEventBus.Register<AppleAirborneEvent>(this);
        GameEventBus.Register<AppleDroppedEvent>(this);
        GameEventBus.Register<AppleCollectedEvent>(this);
    }

    void OnDisable()
    {
        GameEventBus.Unregister<AppleAirborneEvent>(this);
        GameEventBus.Unregister<AppleDroppedEvent>(this);
        GameEventBus.Unregister<AppleCollectedEvent>(this);
    }

    public void OnGamePlayEvent(AppleAirborneEvent e) => Register(e.Apple.transform);
    public void OnGamePlayEvent(AppleDroppedEvent e) => Unregister(e.Apple.transform);
    public void OnGamePlayEvent(AppleCollectedEvent e) => Unregister(e.Apple.transform);

    public void Register(Transform target)
    {
        if (active.ContainsKey(target)) return;

        var element = pool.Get();
        var indicator = new AppleDirectionIndicator(element, cam, angleThreshold);
        indicator.SetTarget(target);
        active[target] = indicator;
    }

    public void Unregister(Transform target)
    {
        if (!active.TryGetValue(target, out var indicator)) return;

        pool.Release((TemplateContainer)indicator.Element);
        active.Remove(target);
    }

    void LateUpdate()
    {
        var visible = new List<(AppleDirectionIndicator, float)>();
        foreach (var indicator in active.Values)
        {
            var angle = indicator.ComputeRawAngle();
            if (angle.HasValue) visible.Add((indicator, angle.Value));
            else indicator.Hide();
        }

        var resolved = IndicatorLayoutSolver.Resolve(visible, clusterThreshold, angularStep);
        foreach (var (indicator, finalAngle) in resolved)
            indicator.ApplyFinalAngle(finalAngle, radius);
    }
}
