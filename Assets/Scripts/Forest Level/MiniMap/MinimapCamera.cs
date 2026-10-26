using GameDevExtensionMethods;
using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float height = 100f;
    void LateUpdate()
    {
        var p = target.position;
        transform.SetPositionAndRotation(p.WithY(p.y+ height), Quaternion.Euler(90f, 0f, 0f));
    }
}
