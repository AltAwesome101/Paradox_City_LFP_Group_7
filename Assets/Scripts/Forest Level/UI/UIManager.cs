using UnityEngine;
using BetterSingletons;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Forestlevel;

public class UIManager : Singleton<UIManager> 
{
    List<IHideableView> ExplorativeUIList = new();
    List<IHideableView> ObjectiveUIList = new();
    List<IHideableView> TutorialUIList = new();

    [Header("UI Document")]
    [SerializeField] UIDocument document;


    // UI view Declarations
    AppleCollectionView collectionView;
    PauseMenuView pauseMenuView;
    WakeMeterUIView wakeMeterUIview;
    TutorialView tutorialView;

    protected override void Awake()
    {
        base.Awake();

    }

    void DeclareUIViews(){
        var root = document.rootVisualElement;
        collectionView = new AppleCollectionView(container: root.Q<VisualElement>("appleCollection-container"));
        pauseMenuView = new PauseMenuView(container: root.Q<VisualElement>("pauseMenu-container"),CurrentScene:null,FutureScene: null);
        tutorialView = new TutorialView(container: root.Q<VisualElement>("TutorialPanel-container"));
        

    }





}
