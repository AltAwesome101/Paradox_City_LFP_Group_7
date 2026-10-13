// CameraRig.cs (renamed from CameraModeSwitcher — it no longer computes any
// state of its own; it's purely a consumer of PlayerMovement.IsLocked)
using UnityEngine;
using BetterEventBus;
using Unity.Cinemachine;

namespace Forestlevel
{
    public class CameraRig : MonoBehaviour
    ,IGamePlayEventListener<CameraChangeEvent>
    ,IGamePlayEventListener<CameraTrackTargetEvent>
    {
        [SerializeField] CinemachineCamera ExploreCamera;
        [SerializeField] CinemachineCamera InGameCamera;

        CameraType currentCameraType = CameraType.FreeLook;
        
        public void OnGamePlayEvent(CameraChangeEvent evt){
            currentCameraType = evt.CameraType;
            ChangeCamera(currentCameraType);
            Debug.Log($"Current Camera Type:{currentCameraType}");
        }

        void ChangeCamera(CameraType cameraType){
            ExploreCamera.Priority = cameraType == CameraType.FreeLook? 10 : 0;
            InGameCamera.Priority = cameraType == CameraType.InGame? 10 : 0;
        }
        void OnEnable()
        {
            GameEventBus.Register<CameraChangeEvent>(this);
            GameEventBus.Register<CameraTrackTargetEvent>(this);
        }
        void OnDisable()
        {
            GameEventBus.Unregister<CameraChangeEvent>(this);
            GameEventBus.Unregister<CameraTrackTargetEvent>(this);
        }

        public void OnGamePlayEvent(CameraTrackTargetEvent evt)
        {
            InGameCamera.Target.TrackingTarget = evt.target;
            InGameCamera.PreviousStateIsValid = false;   // snap to the new anchor instead of blending from the old zone
        }
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


public struct CameraTrackTargetEvent: IGameplayEvent{
    public readonly Transform target;
    public CameraTrackTargetEvent(Transform target)
    {
        this.target = target;
    }
}