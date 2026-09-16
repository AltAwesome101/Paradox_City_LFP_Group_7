using UnityEngine;

public class AppleDeployer : MonoBehaviour
{
    [SerializeField] DeployRegistry registry;
    public bool IsBusy { get; private set; }

    void OnEnable() => registry.Register(this);
    void OnDisable() => registry.Deregister(this);

    public void MarkBusy() => IsBusy = true;
    public void MarkFree() => IsBusy = false;
}
