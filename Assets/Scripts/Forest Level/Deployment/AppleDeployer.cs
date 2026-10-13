using UnityEngine;

public class AppleDeployer : MonoBehaviour
{
    public bool IsBusy { get; private set; }
    public DeployZone Zone { get; private set; }

    void Awake() => Zone = GetComponentInParent<DeployZone>();

    public void MarkBusy() => IsBusy = true;
    public void MarkFree() => IsBusy = false;
}