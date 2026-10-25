using UnityEngine;

public class MinimapMarker : MonoBehaviour
{
    [SerializeField] MinimapMarkerRegistry registry;
    [SerializeField] string styleClass = "marker-zone";
    public string StyleClass => styleClass;

    void OnEnable()  => registry.Register(this);
    void OnDisable() => registry.Deregister(this);
}