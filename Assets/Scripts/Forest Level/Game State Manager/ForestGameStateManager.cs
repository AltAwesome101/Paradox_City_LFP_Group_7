    using BetterSingletons;
    using BetterEventBus;
    using UnityEngine;
    using UnityEngine.UIElements;
    using GameDevExtensionMethods;

    namespace Forestlevel
    {
        public class ForestGameStateManager : Singleton<ForestGameStateManager>,
            IGamePlayEventListener<DeploySessionEndedEvent>
        {
            [SerializeField] int SessionToWin = 4;
            int completed;
            bool levelOver;

            void Start() => GameEventBus.Raise(new ExplorationGameStateEvent());
            void OnEnable()  => GameEventBus.Register<DeploySessionEndedEvent>(this);
            void OnDisable() => GameEventBus.Unregister<DeploySessionEndedEvent>(this);

            public void OnGamePlayEvent(DeploySessionEndedEvent e)
            {
                if (levelOver) return;

                if (!e.Won)
                {
                    levelOver = true;
                    GameEventBus.Raise(new LevelLostEvent());
                    return;
                }

                completed++;
                if (completed >= SessionToWin)
                {
                    levelOver = true;
                    GameEventBus.Raise(new LevelWonEvent());
                }
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
                // Debug.Log("Ïn Game");
                CursorUtility.LockAndHide(visible: false,lockstate: true);
            }
        }
    }