using BetterSingletons;
using UnityEngine;

public class ConsequenceSystemManager : Singleton<ConsequenceSystemManager>
{
    [SerializeField] ConsequenceRegistry registry;

    void OnValidate()=> registry = Resources.Load<ConsequenceRegistry>("");
}
