using UnityEngine;
using System;
using BetterEventBus;
using Forestlevel;

public class Tester : MonoBehaviour,
    IGamePlayEventListener<DeploySessionStartedEvent>
{
    bool isFirstDeployZone = true;
    [SerializeField] BaseTutorialInstructionSO walktutorial;
    [SerializeField] BaseTutorialInstructionSO objtutorial;
    [SerializeField] BaseTutorialInstructionSO AppleCatchTutorial;



    void Start()
    {
        Invoke(nameof(TestWalkTutorial), 3f);
        Invoke(nameof(TestObjTutorial), 8f);
    
    }

    void OnEnable() => GameEventBus.Register<DeploySessionStartedEvent>(this);
    void OnDisable() => GameEventBus.Unregister<DeploySessionStartedEvent>(this);


    void TestWalkTutorial(){
        Debug.Log("Tutorial run");
        GameEventBus.Raise<TutorialRequestEvent>(new TutorialRequestEvent{Instruction = walktutorial});
        GameEventBus.Raise<TutorialStartedEvent>(new TutorialStartedEvent{Instruction = walktutorial});
    }

    void TestObjTutorial(){
        Debug.Log("Tutorial run");
        GameEventBus.Raise<TutorialRequestEvent>(new TutorialRequestEvent{Instruction = objtutorial});
        GameEventBus.Raise<TutorialStartedEvent>(new TutorialStartedEvent{Instruction = objtutorial});
    }

    public void OnGamePlayEvent(DeploySessionStartedEvent evt)
    {
        if(! isFirstDeployZone) return;
        GameEventBus.Raise<TutorialRequestEvent>(new TutorialRequestEvent{Instruction = AppleCatchTutorial});
        GameEventBus.Raise<TutorialStartedEvent>(new TutorialStartedEvent{Instruction = AppleCatchTutorial});

        isFirstDeployZone = false;
    }
}
