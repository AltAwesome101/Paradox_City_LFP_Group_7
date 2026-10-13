using UnityEngine;
using UnityEngine.UIElements;

    [System.Serializable]
    public class AppleIndicatorSettings
    {
        public VisualTreeAsset arrowTemplate;
        public float maxAngle = 60f;          // degrees either side of straight up
        public float maxLaneOffset = 10f;     // lane distance (world units) that maps to maxAngle
        public float heightAboveHead = 120f;  // pixels above the player's screen position
    }

    public class AppleIndicatorView : BaseUIView
    {
        readonly TemplateContainer _arrow;
        readonly AppleIndicatorSettings _s;
        readonly Camera _cam;
        readonly Transform _player;

        public AppleIndicatorView(VisualElement container, AppleIndicatorSettings settings,Camera cam, Transform player) : base(container)
        {
            _s = settings;
            _cam = cam;
            _player = player;

            _arrow = settings.arrowTemplate.CloneTree();
            container.Add(_arrow);

            _arrow.style.position = Position.Absolute;
            _arrow.style.display = DisplayStyle.None;
            _arrow.style.transformOrigin = new StyleTransformOrigin(
                new TransformOrigin(Length.Percent(50), Length.Percent(100))); // pivot at the base
        }

        public void PointAt(AppleDeployer deployer)
        {
            if (deployer == null) { Clear(); return; }

            _arrow.style.display = DisplayStyle.Flex;
            PositionAboveHead();

            // lane axis = camera right, so rotated zones still work
            Vector3 offset = deployer.transform.position - _player.position;
            float lane = Vector3.Dot(offset, _cam.transform.right);
            float t = Mathf.Clamp(lane / _s.maxLaneOffset, -1f, 1f);

            _arrow.style.rotate = new StyleRotate(new Rotate(new Angle(t * _s.maxAngle, AngleUnit.Degree)));
        }

        // hides the arrow only; the container's visibility belongs to UIManager
        public void Clear() => _arrow.style.display = DisplayStyle.None;

        void PositionAboveHead()
        {
            Vector2 screenPos = _cam.WorldToScreenPoint(_player.position);
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(_container.panel, screenPos);
            _arrow.style.left = panelPos.x;
            _arrow.style.top = panelPos.y - _s.heightAboveHead;
        }
    }