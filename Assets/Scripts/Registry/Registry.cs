using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;

namespace GameRegistry{
    
    public abstract class Registry<T> : ScriptableObject, IRegistry<T>
    {
        readonly protected List<T> _items = new();
        public IReadOnlyList<T> Items => _items;

        // Events for when an item is registered and deregistered
        public event Action<T> OnRegister;
        public event Action<T> OnDeregister;

        protected virtual void OnEnable() => _items.Clear();
        public void Register(T item){
            if(_items.Contains(item)) return;

            // notify any listeners when an item is registered
            _items.Add(item);
            OnRegister?.Invoke(item);
        }
        public void Deregister(T item){
            // Notify any listeners when an item is deregistered
            if(_items.Remove(item))
                OnDeregister?.Invoke(item);
        }


    }
}

