using System.Collections.Generic;
using UnityEngine;

namespace BetterStateMachine
{
    public class StateNode
    {
        public IState State{get;}

        public HashSet<ITransition> Transitions{get;}

        public StateNode(IState state)
        {
            State = state;
            Transitions = new HashSet<ITransition>();
        }

        public void AddTransitions(IState to, IPredicate condition)
        {
            Transitions.Add(new Transition(to,condition));
        }
    }
    
}
