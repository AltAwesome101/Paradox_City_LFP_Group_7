using System;
using UnityEngine;
using UnityEngine.UIElements;

// One indicator = one arrow VisualElement + the target it's pointing at.
// Self-contained: give it a target, call Tick() every frame, done.
public class AppleDirectionIndicator
{
    readonly VisualElement element;
    readonly Camera cam;
    readonly float angleThreshold;

    public Transform Target { get; private set; }
    public VisualElement Element => element;

    public AppleDirectionIndicator(VisualElement element, Camera cam, float angleThreshold)
    {
        this.element = element;
        this.cam = cam;
        this.angleThreshold = angleThreshold;

        element.style.position = Position.Absolute;

        // Pivot at the bottom-center of the box — this is the point that
        // stays glued to the anchor while the rest of the arrow swings.
        element.style.transformOrigin = new StyleTransformOrigin(
            new TransformOrigin(Length.Percent(50), Length.Percent(100))
        );

        // Absolute positioning places the box's *top-left corner* at (left, top)
        // by default — we actually want the box's bottom-center there instead.
        // We don't know the element's real size until layout runs once, so we
        // shift it after the first GeometryChangedEvent.
        element.style.left = Length.Percent(50);
        element.style.top = Length.Percent(50);
        element.RegisterCallback<GeometryChangedEvent>(OnFirstLayout);
    }


    //TODO: Ignore the y axis change but use x-z axis
    void OnFirstLayout(GeometryChangedEvent evt)
    {
        element.style.marginLeft = -element.resolvedStyle.width / 2f;
        element.style.marginTop = -element.resolvedStyle.height;
        element.UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);
    }

    public void SetTarget(Transform target) => Target = target;

    public float? ComputeRawAngle()
    {
        if (Target == null) return null;

        Vector3 toTarget = Target.position - cam.transform.position;

        // yaw-only visibility check (your y-axis fix)
        Vector3 flatToTarget = new Vector3(toTarget.x, 0, toTarget.z).normalized;
        Vector3 flatForward = new Vector3(cam.transform.forward.x, 0, cam.transform.forward.z).normalized;
        if (Vector3.Dot(flatToTarget, flatForward) > angleThreshold) return null;

        float x = Vector3.Dot(toTarget, cam.transform.right);
        float y = Vector3.Dot(toTarget, cam.transform.up);
        float angleDeg = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
        return (angleDeg + 360f) % 360f; // normalize to [0,360)
    }

    public void Hide() => element.style.display = DisplayStyle.None;

    public void ApplyFinalAngle(float angleDeg, float radius)
    {
        element.style.display = DisplayStyle.Flex;

        float rad = angleDeg * Mathf.Deg2Rad;
        float x = Mathf.Cos(rad) * radius;
        float y = -Mathf.Sin(rad) * radius; // sign may need flipping — see note below

        element.style.translate = new StyleTranslate(new Translate(x, y));
        element.style.rotate = new StyleRotate(new Rotate(new Angle(angleDeg, AngleUnit.Degree)));
    }

    internal void ApplyFinalAngle(float finalAngle, object radius)
    {
        throw new NotImplementedException();
    }
}
