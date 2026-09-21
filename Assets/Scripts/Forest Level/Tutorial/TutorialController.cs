using BetterSingletons;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class TutorialController : Singleton<TutorialController>
{
    [SerializeField] List<TutorialDataSO> startingdata;
    [SerializeField] UIDocument document;
    TutorialView view;
    Queue<TutorialDataSO> dataqueue;

    protected override void Awake()
    {
        base.Awake();
        var root = document.rootVisualElement;
        dataqueue = new Queue<TutorialDataSO>(startingdata);
        view = new TutorialView(root.Q<VisualElement>("TutorialPanel-container"));
    }

    void Start(){
        view.ChangeTutorialInfo(data);
    }
}
