using UnityEngine;
using System;
using BetterEventBus;
using Forestlevel;

public class Tester : MonoBehaviour
{
    [SerializeField] BaseTutorialInstructionSO walktutorial;
    [SerializeField] BaseTutorialInstructionSO objtutorial;


    void Start()
    {
        Invoke(nameof(TestWalkTutorial), 3f);
        Invoke(nameof(TestObjTutorial), 8f);
    
    }

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

}
