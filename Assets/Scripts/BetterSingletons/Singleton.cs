using System;
using UnityEngine;


namespace BetterSingletons
{
    public class Singleton<T> : MonoBehaviour where T :Component{
        protected static T instance;
        public static bool HasInstance => instance != null;
        public static T TryGetInstance() => HasInstance? instance : null;
        public static T Instance
        {
            get
            {
                if(instance == null){
                    instance = FindAnyObjectByType<T>();
                    if(instance == null)
                    {
                        var go = new GameObject($"{typeof(T).Name} Auto-Generated");
                        instance = go.AddComponent<T>();
                    }
                }

                return instance;
            }
        }


        protected virtual void Awake(){
            InitializeSingleton();
        }

        protected virtual void InitializeSingleton()
        {
            if(!Application.isPlaying) return;

            instance = this as T;
        }
    }
    
    public class PersistentSingleton<T> : MonoBehaviour where T :Component{
        public bool AutoUnparentOnAwake = true;
        protected static T instance;
        public static bool HasInstance => instance != null;
        public static T TryGetInstance() => HasInstance? instance : null;

        public T Instance
        {
            get
            {
                if(instance == null){
                    instance = FindAnyObjectByType<T>();
                    if(instance == null)
                    {
                        var go = new GameObject($"{typeof(T).Name} Auto-Generated");
                        instance = go.AddComponent<T>();
                    }
                }

                return instance;
            }
        }


        protected virtual void Awake(){
            InitializeSingleton();
        }

        protected virtual void InitializeSingleton()
        {
            if(!Application.isPlaying) return;

            if(AutoUnparentOnAwake){
                transform.SetParent(null);
            }

            if(instance == null){
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }

            instance = this as T;
        }
    }

}