using BetterSingletons;
using UnityEngine;

public class ConsequenceSystemManager : Singleton<ConsequenceSystemManager>
{
    [SerializeField] ConsequenceRegistry registry;

    // void OnValidate()=> registry = Resources.Load<ConsequenceRegistry>(""); //TODO: Precaution if the register becomnes null we find it using the Resource Api 
}
