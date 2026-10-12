    using BetterSingletons;
    using BetterEventBus;
    using UnityEngine;
    using UnityEngine.UIElements;
    using GameDevExtensionMethods;

    namespace Forestlevel
    {
        public class ForestGameStateManager : Singleton<ForestGameStateManager>,
            IGamePlayEventListener<AppleCollectedEvent>,
            IGamePlayEventListener<LevelWonEvent>,
            IGamePlayEventListener<LevelLostEvent>
        {
            [SerializeField] int applesToWin = 10;
            int collected = 0;
            void Start()
            {
                GameEventBus.Raise<ExplorationGameStateEvent>(new ExplorationGameStateEvent());
            }


            void OnEnable()
            {
                GameEventBus.Register<AppleCollectedEvent>(this);
                GameEventBus.Register<LevelWonEvent>(this);
                GameEventBus.Register<LevelLostEvent>(this);
                GameEventBus.Register<TutorialClosedEvent>(this);
            }

            void OnDisable()
            {
                GameEventBus.Unregister<AppleCollectedEvent>(this);
                GameEventBus.Unregister<LevelWonEvent>(this);
                GameEventBus.Unregister<LevelLostEvent>(this);
                GameEventBus.Unregister<TutorialClosedEvent>(this);
            }

            public void OnGamePlayEvent(LevelWonEvent gameplayEvent)
            {
                Debug.Log("Game won!");
            }

            public void OnGamePlayEvent(LevelLostEvent gameplayEvent)
            {
                collected = 0;
                // GameEventBus.Raise<TutorialGameStateEvent>(new TutorialGameStateEvent());
            }

        public void OnGamePlayEvent(AppleCollectedEvent gameplayEvent)
        {
            collected++;
            UIManager.Instance.Collection.UpdateDisplay($"{collected}/{applesToWin}");
        }
    }

        
        public class ExplorationGameStateEvent : IGameplayEvent
        {
            // static readonly Vector3 ExploreLocation = new Vector3(89f, .25f, 200f); 

            public ExplorationGameStateEvent()
            {
                // Cursor hid and fix state
                CursorUtility.LockAndHide(visible: false,lockstate: true);
            }
        }

        public class TutorialGameStateEvent : IGameplayEvent
        {
            public TutorialGameStateEvent()
            {
                // Cursor visible and free lock state
                CursorUtility.LockAndHide(visible: true,lockstate: false);
            }
        }


        public class InGameGameStateEvent : IGameplayEvent
        {
            
            public InGameGameStateEvent()
            {
                // Cursor hide and fix state
                Debug.Log("Ïn Game");
                CursorUtility.LockAndHide(visible: false,lockstate: true);
            }
        }
    }