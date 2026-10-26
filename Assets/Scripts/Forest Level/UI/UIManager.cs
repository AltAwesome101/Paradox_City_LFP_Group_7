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
    IGamePlayEventListener<TutorialClosedEvent>,
    IGamePlayEventListener<InteractPromptEvent>,
    IGamePlayEventListener<DeploySessionStartedEvent>,
    IGamePlayEventListener<DeploySessionEndedEvent>
{
    [Header("UI Document")]
    [SerializeField] UIDocument document;
    [SerializeField] InputReaderSO input;
    [SerializeField] AppleWarningCounterDataSO warningData;

    [Header("Pause Menu info")]   // moved over from the old TutorialController
    [SerializeField] SceneReference currentScene;
    [SerializeField] SceneReference futureScene;

    [Header("Apple Indicator")]
    [SerializeField] AppleIndicatorSettings indicatorSettings;
    [SerializeField] Camera cam;
    [SerializeField] Transform player;

    [Header("Minimap")]
    [SerializeField] Camera minimapCamera;
    [SerializeField] RenderTexture minimapTexture;
    [SerializeField] MinimapMarkerRegistry markerRegistry;

    // public UI view Declarations
    public AppleCollectionView Collection{get; private set;}
    public PauseMenuView PauseMenu{get; private set;}
    public WakeMeterUIView WakeMeter{get; private set;}
    public TutorialView Tutorial{get; private set;}
    public AppleWarningCounterView WarningCounter { get; private set; }
    public InteractPromptView InteractPrompt { get; private set; }
    public AppleIndicatorView Indicator { get; private set; }
    public ObjectiveView Objective {get; private set;}
    public MiniMapView MiniMap { get; private set; }
    public KeyChainView KeyChain{get; private set;}


    readonly List<IHideableView> _allViews = new();
    readonly HashSet<IHideableView> _overlays = new(); 
    readonly Dictionary<UIState, HashSet<IHideableView>> _layouts = new();
    UIState _currentState = UIState.Exploration;
    protected override void Awake()
    {
        base.Awake();

        DeclareUIViewsAndLayout();
        SetState(UIState.Exploration);
    }

    void OnEnable()
    {
        GameEventBus.Register<TutorialStartedEvent>(this);
        GameEventBus.Register<TutorialClosedEvent>(this);
        GameEventBus.Register<InteractPromptEvent>(this);
        GameEventBus.Register<DeploySessionStartedEvent>(this);
        GameEventBus.Register<DeploySessionEndedEvent>(this);

        input.Escape += OnEscape;
    }

    void OnDisable()
    {
        GameEventBus.Unregister<TutorialStartedEvent>(this);
        GameEventBus.Unregister<TutorialClosedEvent>(this);
        GameEventBus.Unregister<InteractPromptEvent>(this);          // these three say Register
        GameEventBus.Unregister<DeploySessionStartedEvent>(this);    // in your pasted version
        GameEventBus.Unregister<DeploySessionEndedEvent>(this);

        input.Escape -= OnEscape;
        MiniMap?.Dispose();
        KeyChain?.Dispose();
        GamePause.Release(PauseMenu);
    }

    void DeclareUIViewsAndLayout()
    {
        var root = document.rootVisualElement;
        Collection = new AppleCollectionView(root.Q<VisualElement>("appleCollection-container"));
        PauseMenu  = new PauseMenuView(root.Q<VisualElement>("pauseMenu-container"), currentScene, futureScene);
        Tutorial   = new TutorialView(root.Q<VisualElement>("TutorialPanel-container"));
        WakeMeter  = new WakeMeterUIView(root, disturb: 0, maxLimit: 0); // TODO real values
        WarningCounter = new AppleWarningCounterView(root.Q<VisualElement>("approachingApple-counter-element"), warningData);
        InteractPrompt = new InteractPromptView(root.Q<VisualElement>("interactPrompt-container"));
        Indicator = new AppleIndicatorView(root.Q<VisualElement>("applenotifier-element-archor"), indicatorSettings, cam, player);
        Objective = new ObjectiveView(root.Q<VisualElement>("objective-container"));
        MiniMap = new MiniMapView(root.Q<VisualElement>("minimap-container"), minimapCamera, minimapTexture, markerRegistry);
        KeyChain = new KeyChainView(root.Q<VisualElement>("keychain-container"),LevelProgressManager.Instance);

        _allViews.AddRange(new IHideableView[] { Collection, PauseMenu, Tutorial, WakeMeter, WarningCounter, InteractPrompt, Objective, MiniMap,KeyChain });

        _layouts[UIState.Objective]   = new HashSet<IHideableView> { Collection, WakeMeter, WarningCounter, Indicator};
        _layouts[UIState.Exploration] = new HashSet<IHideableView> { Objective, MiniMap,KeyChain };

        PauseMenu.ResumeRequested += () => RemoveOverlay(PauseMenu);
    }

    void OnEscape() => TogglePause();

    public void TogglePause()
    {
        if (_overlays.Contains(PauseMenu)) RemoveOverlay(PauseMenu);
        else AddOverlay(PauseMenu);
    }


    void SetState(UIState state)
    {
        _currentState = state;
        Debug.Log($"UI State: {state}");
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

    public void OnGamePlayEvent(InteractPromptEvent e)
    {
        if (e.Visible) 
        {   InteractPrompt.SetText(e.Text); 
            AddOverlay(InteractPrompt);
        }
        else RemoveOverlay(InteractPrompt);
    }
    public void OnGamePlayEvent(DeploySessionStartedEvent e) => SetState(UIState.Objective);
    public void OnGamePlayEvent(DeploySessionEndedEvent e)   => SetState(UIState.Exploration);
}
