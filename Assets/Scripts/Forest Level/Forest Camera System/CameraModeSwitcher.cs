// CameraRig.cs (renamed from CameraModeSwitcher — it no longer computes any
// state of its own; it's purely a consumer of PlayerMovement.IsLocked)
using UnityEngine;
using BetterEventBus;
using Unity.Cinemachine;

namespace Forestlevel
{
    public class CameraRig : MonoBehaviour,IGamePlayEventListener<CameraChangeEvent>
    {
        CameraType currentCameraType = CameraType.FreeLook;
        public void OnGamePlayEvent(CameraChangeEvent evt){
            currentCameraType = evt.CameraType;
            Debug.Log($"Current Camera Type:{currentCameraType.ToString()}");
        }

        void OnEnable() => GameEventBus.Register<CameraChangeEvent>(this);
        void OnDisable() => GameEventBus.Unregister<CameraChangeEvent>(this);

    }

    public enum CameraType
    {
        InGame, //Used in the apple catching game mode and 
        FreeLook //Used in Traversal movement
    }

    public class CameraChangeEvent: IGameplayEvent{
        public CameraType CameraType{get;}
        public CameraChangeEvent(CameraType type){
            CameraType = type; 
        }
    }


}