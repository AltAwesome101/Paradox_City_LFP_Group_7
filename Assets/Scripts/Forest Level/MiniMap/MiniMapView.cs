using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MiniMapView : BaseUIView, IDisposable
{
    readonly VisualElement markerLayer;
    readonly Camera mapCam;
    readonly MinimapMarkerRegistry registry;
    readonly Dictionary<MinimapMarker, VisualElement> icons = new();

    public MiniMapView(VisualElement container, Camera mapCam, RenderTexture rt, MinimapMarkerRegistry registry)
        : base(container)
    {
        this.mapCam = mapCam;
        this.registry = registry;

        container.Q("minimap-image").style.backgroundImage = Background.FromRenderTexture(rt);
        markerLayer = container.Q("minimap-markers");

        foreach (var m in registry.Items) AddIcon(m);   // markers that registered before the UI existed
        registry.OnRegister   += AddIcon;
        registry.OnDeregister += RemoveIcon;

        markerLayer.schedule.Execute(Refresh).Every(33);   // ~30 Hz, so UIManager needs no Update
    }

    void AddIcon(MinimapMarker m)
    {
        var icon = new VisualElement();
        icon.AddToClassList("minimap-icon");
        icon.AddToClassList(m.StyleClass);
        markerLayer.Add(icon);
        icons[m] = icon;
    }

    void RemoveIcon(MinimapMarker m)
    {
        if (icons.Remove(m, out var icon)) icon.RemoveFromHierarchy();
    }

    void Refresh()
    {
        foreach (var (marker, icon) in icons)
        {
            Vector3 vp = mapCam.WorldToViewportPoint(marker.transform.position);
            bool inside = vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
            icon.style.display = inside ? DisplayStyle.Flex : DisplayStyle.None;
            if (!inside) continue;

            icon.style.left = Length.Percent(vp.x * 100f);
            icon.style.top  = Length.Percent((1f - vp.y) * 100f);   // UI Y points down
        }
    }

    public void Dispose()
    {
        registry.OnRegister  -= AddIcon;
        registry.OnDeregister -= RemoveIcon;
    }
}
