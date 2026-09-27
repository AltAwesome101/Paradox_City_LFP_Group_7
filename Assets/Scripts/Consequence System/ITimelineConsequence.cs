using System;
using System.Collections.Generic;
using BetterEventBus;
using BetterSingletons;
using Consequence;
using UnityEngine;

namespace Consequence
{
    public interface ITimelineConsequence
    {
        void OnObjectComplete();
        void Propogate(float deltaTime);
        void Activate();
        void Deactivate();
    }

    public enum Era{
        AppleForest = 0,
        Artist = 1,
        WrongBeer = 2
    }

    public enum ConsequenceType{
        Cosmetic,
        Mechanical
    }

    public interface ITimelineEvent : IGameplayEvent{
        public Era Era {get;}
        public ConsequenceType  Type {get;}
    }


}

public class TimelineState : Singleton<TimelineState>{
    // Contains event state for future levels to use if there is a time dependancy

    readonly Dictionary <Type, ITimelineEvent> _lastknown = new();
    public void Record<T>(T e) where T: ITimelineEvent => _lastknown[typeof(T)] = e;
    public bool TryGetLast<T>(out T evt) where T : ITimelineEvent{
        if(_lastknown.TryGetValue(typeof(T), out var e))
        {
            evt = (T)e;
            return true;
        }

        evt = default;
        return false;
    }
}
