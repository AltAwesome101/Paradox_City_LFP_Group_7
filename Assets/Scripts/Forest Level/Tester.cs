using UnityEngine;
using System;
using BetterEventBus;
using Forestlevel;

public class Tester : MonoBehaviour
{
    [SerializeField] BaseTutorialInstructionSO tutorial;

    void Start()
    {
        Invoke(nameof(TestTutorial), 10f);
    
    }

    void TestTutorial(){
        Debug.Log("Tutorial run");
        GameEventBus.Raise<TutorialRequestEvent>(new TutorialRequestEvent{Instruction = tutorial});
        GameEventBus.Raise<TutorialStartedEvent>(new TutorialStartedEvent{Instruction = tutorial});
    }

}
