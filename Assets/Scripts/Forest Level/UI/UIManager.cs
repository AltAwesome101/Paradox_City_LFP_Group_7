using UnityEngine;
using BetterSingletons;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Forestlevel;
using BetterEventBus;
using UnityEngine.InputSystem;

public enum UIState{
    Exploration,
    Tutorial,
    Objective,
}

public class UIManager : Singleton<UIManager>,
    IGamePlayEventListener<TutorialStartedEvent>,
    IGamePlayEventListener<TutorialClosedEvent>
{
    [Header("UI Document")]
    [SerializeField] UIDocument document;
    [SerializeField] InputReaderSO input;

    [Header("Pause Menu info")]   // moved over from the old TutorialController
    [SerializeField] SceneReference currentScene;
    [SerializeField] SceneReference futureScene;

    DefaultInputSystem _controls; // same Escape binding the old controller used
    
    // public UI view Declarations
    public AppleCollectionView Collection{get; private set;}
    public PauseMenuView PauseMenu{get; private set;}
    public WakeMeterUIView WakeMeter{get; private set;}
    public TutorialView Tutorial{get; private set;}

    readonly List<IHideableView> _allViews = new();
    readonly HashSet<IHideableView> _overlays = new(); 
    readonly Dictionary<UIState, HashSet<IHideableView>> _layouts = new();
    UIState _currentState = UIState.Exploration;

    protected override void Awake()
    {
        base.Awake();
        _controls = new DefaultInputSystem();
        DeclareUIViewsAndLayout();
        SetState(UIState.Exploration);
    }

    void OnEnable()
    {
        GameEventBus.Register<TutorialStartedEvent>(this);
        GameEventBus.Register<TutorialClosedEvent>(this);
        _controls.Enable();
        _controls.UI.Escape.performed += OnEscape;
    }

    void OnDisable()
    {
        GameEventBus.Unregister<TutorialStartedEvent>(this);
        GameEventBus.Unregister<TutorialClosedEvent>(this);
        _controls.UI.Escape.performed -= OnEscape;
        _controls.Disable();
        GamePause.Release(PauseMenu);
    }

    void DeclareUIViewsAndLayout()
    {
        var root = document.rootVisualElement;
        Collection = new AppleCollectionView(root.Q<VisualElement>("appleCollection-container"));
        PauseMenu  = new PauseMenuView(root.Q<VisualElement>("pauseMenu-container"), currentScene, futureScene);
        Tutorial   = new TutorialView(root.Q<VisualElement>("TutorialPanel-container"));
        WakeMeter  = new WakeMeterUIView(root, disturb: 0, maxLimit: 0); // TODO real values

        _allViews.AddRange(new IHideableView[] { Collection, PauseMenu, Tutorial, WakeMeter });

        _layouts[UIState.Objective]   = new HashSet<IHideableView> { Collection, WakeMeter };
        _layouts[UIState.Exploration] = new HashSet<IHideableView>();

        PauseMenu.ResumeRequested += () => RemoveOverlay(PauseMenu);
        // Overlays (Tutorial, PauseMenu) are deliberately in no layout.
    }

    void OnEscape(InputAction.CallbackContext ctx) => TogglePause();

    public void TogglePause()
    {
        if (_overlays.Contains(PauseMenu)) RemoveOverlay(PauseMenu);
        else AddOverlay(PauseMenu);
    }


    void SetState(UIState state)
    {
        _currentState = state;
        ApplyVisibility();
    }

    public void OnGamePlayEvent(TutorialStartedEvent e)
    {
        Tutorial.SetTutorialDisplay(e.Instruction);
        AddOverlay(Tutorial);
    }
    public void OnGamePlayEvent(TutorialClosedEvent e) => RemoveOverlay(Tutorial);

    public void AddOverlay(IHideableView v)    { if (_overlays.Add(v))    ApplyVisibility(); }
    public void RemoveOverlay(IHideableView v) { if (_overlays.Remove(v)) ApplyVisibility(); }

    void ApplyVisibility()
    {
        var baseLayout = _layouts[_currentState];
        foreach (var view in _allViews)
        {
            if (baseLayout.Contains(view) || _overlays.Contains(view)) view.Show();
            else view.Hide();
        }
    }
}
