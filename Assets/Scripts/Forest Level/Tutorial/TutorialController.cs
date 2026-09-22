using BetterSingletons;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using BetterEventBus;
using UnityEngine.InputSystem;

namespace Forestlevel
{
    public class TutorialController : Singleton<TutorialController>,
    IGamePlayEventListener<TutorialGameStateEvent>
    {
        [SerializeField] List<TutorialDataSO> startingData;
        [SerializeField] UIDocument document;
        TutorialView _view;

        //TODO: Remove the quit logic to its own manager and refactor UI management
        [Header("Pause Menu info")]
        [SerializeField] SceneReference CurrentScene;
        [SerializeField] SceneReference FutureScene;
        
        DefaultInputSystem controls;
        PauseMenuView pauseMenu;
        

        List<TutorialDataSO> _tutorials;
        int _currentIndex;

        protected override void Awake()
        {
            base.Awake();

            var root = document.rootVisualElement;

            _tutorials = new List<TutorialDataSO>(startingData);
            controls = new DefaultInputSystem();

            _view = new TutorialView(container: root.Q<VisualElement>("TutorialPanel-container"));
            pauseMenu = new PauseMenuView(container: root.Q<VisualElement>("pauseMenu-container"),CurrentScene,FutureScene);

            RegisterViewCallbacks();
        }

        void OnEnable()
        {
            GameEventBus.Register<TutorialGameStateEvent>(this);
            controls.Enable();
            controls.UI.Escape.performed += OpenPausePanel;
        }
        void OnDisable()
        {
            GameEventBus.Unregister<TutorialGameStateEvent>(this);
            controls.UI.Escape.performed -= OpenPausePanel;
            controls.Disable();
        }

        void OpenPausePanel(InputAction.CallbackContext ctx) => pauseMenu.Show();


        void RegisterViewCallbacks()
        {
            _view.CloseRequested += CloseTutorial;
            _view.PreviousRequested += ShowPreviousTutorial;
            _view.NextRequested += ShowNextTutorial;
        }

        public void OpenTutorials()
        {
            Debug.Log("Tutorial Started!");
            _currentIndex = 0;
            DisplayCurrentTutorial();
        }

        void DisplayCurrentTutorial()
        {
            TutorialDataSO data = _tutorials[_currentIndex];
            _view.EnableTutorialPanel();
            _view.ChangeTutorialInfo(data);
        }

        void ShowNextTutorial()
        {
            if (_currentIndex >= _tutorials.Count - 1)
                return;

            _currentIndex++;
            DisplayCurrentTutorial();
        }

        void ShowPreviousTutorial()
        {
            if (_currentIndex <= 0) return;

            _currentIndex--;
            DisplayCurrentTutorial();
        }

        void CloseTutorial(){
            _view.HideTutorialPanel();
            GameEventBus.Raise<TutorialClosedEvent>(new TutorialClosedEvent()); //raise tutorial closed event
        }

        public void OnGamePlayEvent(TutorialGameStateEvent gameplayEvent) => OpenTutorials();
    }
    

    public struct TutorialClosedEvent : IGameplayEvent{}
}

