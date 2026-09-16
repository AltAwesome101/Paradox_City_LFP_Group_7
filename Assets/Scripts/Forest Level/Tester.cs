using UnityEngine;

public class Tester : MonoBehaviour
{

    void Start()
    {
        AppleIndicatorManager.Instance.Register(transform);        
    }

}
