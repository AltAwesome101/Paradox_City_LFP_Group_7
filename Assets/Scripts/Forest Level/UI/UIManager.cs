using UnityEngine;
using BetterSingletons;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Forestlevel;

public enum UIState{
    Exploration,
    Tutorial,
    Objective,
}

public class UIManager : Singleton<UIManager> 
{
    [Header("UI Document")]
    [SerializeField] UIDocument document;
    [SerializeField] InputReaderSO input;


    // public UI view Declarations
    public AppleCollectionView Collection{get; private set;}
    public PauseMenuView PauseMenu{get; private set;}
    public WakeMeterUIView WakeMeter{get; private set;}
    public TutorialView Tutorial{get; private set;}

    readonly List<IHideableView> _allViews = new();
    readonly Dictionary<UIState, HashSet<IHideableView>> _layouts = new();
    UIState _currentState = UIState.Exploration;

    protected override void Awake()
    {
        base.Awake();

    }

    void DeclareUIViewsAndLayout(){
        // View Declarations
        var root = document.rootVisualElement;
        Collection = new AppleCollectionView(container: root.Q<VisualElement>("appleCollection-container"));
        PauseMenu = new PauseMenuView(container: root.Q<VisualElement>("pauseMenu-container"),CurrentScene:null,FutureScene: null);
        Tutorial = new TutorialView(container: root.Q<VisualElement>("TutorialPanel-container"));
        WakeMeter = new WakeMeterUIView(container: root,disturb:0, maxLimit: 0);// change the values 

        _allViews.AddRange(new IHideableView[]{Collection,PauseMenu,Tutorial,WakeMeter});
        //Catorisation of Views

        _layouts[UIState.Objective] = new HashSet<IHideableView>(new IHideableView[]{Collection,WakeMeter});
        _layouts[UIState.Tutorial] = new HashSet<IHideableView>(new IHideableView[]{Tutorial});
        _layouts[UIState.Exploration] = new HashSet<IHideableView>();

        // TODO: Add on for Exploration UI: MiniMap
    }


    void SetState(UIState state)
    {
        _currentState = state;
        ApplyVisibility();
    }

    void ApplyVisibility()
    {
        var visible = _layouts[_currentState];

        foreach (var view in _allViews)
        {
            if (visible.Contains(view)) 
                view.Show();
            else 
                view.Hide();
        }
    }

}
