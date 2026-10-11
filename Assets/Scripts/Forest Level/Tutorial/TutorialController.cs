using System.Collections.Generic;
using BetterEventBus;
using BetterSingletons;
using UnityEngine;

namespace Forestlevel
{
    public class TutorialController : Singleton<TutorialController>,
        IGamePlayEventListener<TutorialRequestEvent>
    {
        [SerializeField] InputReaderSO input;

        readonly Queue<BaseTutorialInstructionSO> _queue = new();
        readonly HashSet<BaseTutorialInstructionSO> _seen = new();

        BaseTutorialInstructionSO _current;   // null = no tutorial, player is playing
        IPredicate _condition;

        public bool IsTutorialActive => _current != null;

        void OnEnable()  => GameEventBus.Register<TutorialRequestEvent>(this);
        void OnDisable()
        {
            GameEventBus.Unregister<TutorialRequestEvent>(this);
            GamePause.Release(this);          // never leave the game frozen if we're destroyed mid-step
        }

        public void OnGamePlayEvent(TutorialRequestEvent e)
        {
            var ins = e.Instruction;
            // "seen" is checked here, when the request arrives, so repeat triggers are free
            if (ins == null || _seen.Contains(ins) || _queue.Contains(ins) || ins == _current) return;

            _queue.Enqueue(ins);
            if (_current == null) StartNext();
        }

        void StartNext()
        {
            if (!_queue.TryDequeue(out _current)) return;   // queue empty -> stays null -> gameplay

            _seen.Add(_current);
            _condition = _current.CreateCondition(input);   // fresh instance per run
            if (_current.FreezeGame) GamePause.Request(this);

            GameEventBus.Raise<TutorialStartedEvent>(new TutorialStartedEvent { Instruction = _current });
        }

        // Update still runs at timeScale 0; the condition must use unscaled time.
        void Update()
        {
            if (_current == null) return;
            if (_condition.Evaluate()) Complete();
        }

        void Complete()
        {
            GamePause.Release(this);
            _current = null;
            _condition = null;

            GameEventBus.Raise<TutorialClosedEvent>(new TutorialClosedEvent());
            StartNext();                                     // chain queued ones, if any
        }
    }


    public struct TutorialRequestEvent : IGameplayEvent { public BaseTutorialInstructionSO Instruction; } // raised by triggers
    public struct TutorialStartedEvent : IGameplayEvent { public BaseTutorialInstructionSO Instruction; } // UI listens
    public struct TutorialClosedEvent  : IGameplayEvent { }
}



