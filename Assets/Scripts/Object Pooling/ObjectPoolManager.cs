using System;
using System.Collections.Generic;
using System.Linq;
using BetterSingletons;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace BetterPooling
{
    public class ObjectPoolManager: Singleton<ObjectPoolManager>
    {
        List<IPoolHandle> _registeredpools = new();
        public void RegisterPool(IPoolHandle pool) => _registeredpools.Add(pool);
        public void ClearAll()
        {
            foreach(var pool in _registeredpools){
                pool.Clear();
            }
        }
    }


    public interface ISingleObjectPool<T> : IObjectPool<T>, IPoolHandle where T : Component {
        new int CountInactive{get;}
        new void Clear();
    }
    public interface IPoolHandle{
        void Clear();
        int CountInactive{get;}
    }

    public class SingleObjectPool<T> : ISingleObjectPool<T> where T : Component
    {
        readonly T _prefab;
        readonly Transform _parent;
        readonly ObjectPool<T> _pool;


        public int CountInactive => _pool.CountInactive;

        public SingleObjectPool(T prefab,Transform parent = null, int defaultCapacity = 10, int maxSize = 100, bool collectionCheck = true ){
            _prefab = prefab;
            _parent = parent;

            _pool = new ObjectPool<T>(
                createFunc: CreateInstance,
                actionOnGet: instance => instance.gameObject.SetActive(true),
                actionOnRelease: OnRelease,
                actionOnDestroy: instance => {if(instance != null) UnityEngine.Object.Destroy(instance.gameObject);},
                collectionCheck: collectionCheck,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
        }

        T CreateInstance() => UnityEngine.Object.Instantiate(_prefab,_parent);
        void OnRelease(T instance){
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(_parent);
        }
        public void Clear()=> _pool.Clear();
        public T Get()=> _pool.Get();
        public PooledObject<T> Get(out T v) => _pool.Get(out v);
        public void Release(T element) => _pool.Release(element);
    }

    public interface IMultiObjectPool<TKey,T>: IPoolHandle where T: Component
    {
        T Get(TKey key);
        void Release(TKey key, T element);
        int CountInActive(TKey key);
    }

    public class MultiObjectPool<TKey, T> : IMultiObjectPool<TKey, T> where T : Component
    {
        readonly Dictionary<TKey, ISingleObjectPool<T>> _pools = new();
        readonly Dictionary<TKey,T> _prefabs;
        readonly Transform _parent; //?: Might have to change from dictionary to one transfrom
        readonly int _defaultCapacity, _maxSize;
        readonly bool _collectionCheck;

        public MultiObjectPool(Dictionary<TKey,T> prefabs,Transform parent, int defaultCapacity = 10, int maxSize = 100, bool collectionCheck = true){
            _prefabs = prefabs;
            _parent = parent;
            _defaultCapacity = defaultCapacity;
            _maxSize = maxSize;
            _collectionCheck = collectionCheck;
        }

        public ISingleObjectPool<T> GetPoolFor(TKey key){
            if (_pools.TryGetValue(key, out var pool))
                return pool;

            if(!_prefabs.TryGetValue(key, out var prefab))
                throw new KeyNotFoundException($"No prefab registered for key'{key}");

            pool = new SingleObjectPool<T>(prefab,_parent,_defaultCapacity,_maxSize,_collectionCheck);
            _pools[key] = pool;
            return pool; 
        }
        public void Clear(){
            foreach(var pool in _pools.Values){
                pool.Clear();
            }
            _pools.Clear();
        }
        public int CountInactive => _pools.Values.Sum(p => p.CountInactive);
        public int CountInActive(TKey key) => _pools.TryGetValue(key, out var pool)? pool.CountInactive : 0;
        public T Get(TKey key) => GetPoolFor(key).Get();
        public void Release(TKey key, T element) => GetPoolFor(key).Release(element);
    }

    //TODO: See if you can abstract UI toolkit Pooling furthur

    public class VisualElementPool<T> : IObjectPool<T> where T : VisualElement
    {
        readonly VisualElement _container;
        readonly ObjectPool<T> _pool;

        // ?: Might have to change constructor using builder
        public VisualElementPool(Func<T> createFunc,VisualElement container,Action<T> OnGetAction = null, Action<T> OnReleaseAction=null, int defaultCapacity = 5, int maxSize = 20, bool collectionCheck = true)
        {
            _container = container;

            _pool = new ObjectPool<T>(
                createFunc: createFunc,
                actionOnGet: (entry) =>
                {
                    _container.Add(entry);
                    OnGetAction?.Invoke(entry);
                },
                actionOnRelease: (entry) =>
                {
                    entry.RemoveFromHierarchy();
                    OnReleaseAction?.Invoke(entry);
                },
                actionOnDestroy: null,
                collectionCheck: collectionCheck,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
        }

        public int CountInactive => _pool.CountInactive;
        public void Clear() => _pool.Clear();
        public T Get() => _pool.Get();
        public PooledObject<T> Get(out T v) => _pool.Get(out v);
        public void Release(T element) => _pool.Release(element);
    }

}

