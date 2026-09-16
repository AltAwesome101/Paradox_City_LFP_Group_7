using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace BetterEventBus
{
    public interface IGameplayEvent{}
    public interface IGamePlayEventListener<T> where T: IGameplayEvent{
        void OnGamePlayEvent(T gameplayEvent);
    }

    public static class GameEventBus
    {
        static readonly Dictionary<Type,List<object>> _listeners = new();

        public static void Register<T>(object listener) where T:IGameplayEvent{
            var type = typeof(T);
            if(!_listeners.TryGetValue(type,out var list))
            {
                list = new List<object>();
                _listeners[type] = list;
            }

            if(!list.Contains(listener)){
                list.Add(listener);
            }
        }

        public static void Unregister<T>(object listener) where T: IGameplayEvent{
            var type = typeof(T);
            if(_listeners.TryGetValue(type, out var list))
            {
                list.Remove(listener);
            }
        }

        public static void Raise<T>(T gameplayEvent) where T: IGameplayEvent{
            var type = typeof(T);
            if(!_listeners.TryGetValue(type, out var list)) return;

            var snapshot = list.ToArray();
            for(int i=0; i< snapshot.Length; i++){
                
                if(snapshot[i] is IGamePlayEventListener<T> typed)
                {
                    typed.OnGamePlayEvent(gameplayEvent);
                }
            }
        }
    }
    
}

