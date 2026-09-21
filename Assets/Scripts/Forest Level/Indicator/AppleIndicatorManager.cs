using UnityEngine;
using UnityEngine.UIElements;
using BetterSingletons;
public class AppleIndicatorManager : Singleton<AppleIndicatorManager>
{
#region fields
    [SerializeField] VisualTreeAsset arrowTemplate;
    [SerializeField] UIDocument uiDocument;
    [SerializeField] Camera cam;
    [SerializeField] Transform player;
    [SerializeField] float maxAngle = 60f;         // degrees either side of straight up — never dips into the bottom half of the clock
    [SerializeField] float maxLaneOffset = 10f;    // world units mapping to maxAngle — set to your outer-tree spread
    [SerializeField] float heightAboveHead = 120f; // pixels above player's screen position
#endregion

    VisualElement anchor;
    TemplateContainer arrow;

    protected override void Awake()
    {
        base.Awake();
        anchor = uiDocument.rootVisualElement.Q<VisualElement>("applenotifier-element-archor");

        arrow = arrowTemplate.CloneTree();
        anchor.Add(arrow);

        arrow.style.position = Position.Absolute;
        arrow.style.display = DisplayStyle.None;
        arrow.style.transformOrigin = new StyleTransformOrigin(
            new TransformOrigin(Length.Percent(50), Length.Percent(100))); // pivot at base so the tip swings, not the center
    }

    public void PointAt(AppleDeployer deployer)
    {
        if (deployer == null) { Hide(); return; }

        arrow.style.display = DisplayStyle.Flex;
        PositionAboveHead();

        float dx = deployer.transform.position.x - player.position.x; // ASSUMPTION: lane runs along world X — swap to .z if yours runs along Z
        float t = Mathf.Clamp(dx / maxLaneOffset, -1f, 1f);
        float angle = t * maxAngle;

        arrow.style.rotate = new StyleRotate(new Rotate(new Angle(angle, AngleUnit.Degree)));
    }

    public void Hide() => arrow.style.display = DisplayStyle.None;

    void PositionAboveHead()
    {
        Vector2 screenPos = cam.WorldToScreenPoint(player.position);
        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(uiDocument.rootVisualElement.panel, screenPos);
        arrow.style.left = panelPos.x;
        arrow.style.top = panelPos.y - heightAboveHead;
    }
}