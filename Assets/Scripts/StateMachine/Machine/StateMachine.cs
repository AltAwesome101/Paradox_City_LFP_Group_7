using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR;

namespace BetterStateMachine
{
    public class StateMachine
    {
        StateNode current;
        Dictionary<Type,StateNode> nodes = new();
        HashSet<ITransition> anyTransitions = new();

        public void Update()
        {
            var transition =GetTransition();

            if (transition != null)
            {
                ChangeState(transition.To);
            }

            current.State?.Update();
        }

        ITransition GetTransition()
        {
            foreach(var transition in anyTransitions)
            {
                if(transition.Condition.Evaluate()) return transition;
            }

            foreach(var transition in current.Transitions)
            {
                if(transition.Condition.Evaluate()) return transition;
            }

            return null;
        }

        public void AddTransitions(IState from, IState to ,IPredicate condition)
        {
            GetOrAddNode(from).AddTransitions(GetOrAddNode(to).State, condition);
        }

        public void AddAnyTransition(IState to, IPredicate condition)
        {
            GetOrAddNode(to);
            anyTransitions.Add(new Transition(to,condition));
        }

        StateNode GetOrAddNode(IState state)
        {
            var node = nodes.GetValueOrDefault(state.GetType());

            if(node == null)
            {
                node = new StateNode(state);
                nodes.Add(state.GetType(),node);
            }

            return node;
        }

        private void ChangeState(IState state)
        {
            if (state == current.State) return;

            var prevState = current.State;
            var nextState = nodes[state.GetType()].State;

            prevState?.OnExit();
            nextState?.OnEnter();
            current = nodes[state.GetType()];
        }

        public void FixedUpdate()
        {
            current.State?.FixedUpdate();
        }

        public void SetState(IState state)
        {
            current = nodes[state.GetType()];
            current.State?.OnEnter();
        }
    }
    
}
