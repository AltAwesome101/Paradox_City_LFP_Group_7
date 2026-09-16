using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameDevExtensionMethods{

    public static class GameObjectExtenstions{

        public static T GetOrAddComponent<T>(this GameObject gameobject) where T: Component{
            // Bug fix: was creating the component on a brand new empty GameObject
            // instead of the one passed in. Also uses IsAlive() instead of TryGetComponent's
            // implicit null check, since a pooled-but-destroyed component can be a "fake null".
            if (gameobject.TryGetComponent<T>(out T component) && component.IsAlive()){
                return component;
            }
            return gameobject.AddComponent<T>();
        }

        public static bool HasComponent<T>(this GameObject gameobject) where T: Component{
            return gameobject.TryGetComponent<T>(out _);
        }

        public static void SetLayerRecursively(this GameObject gameobject, int layer){
            // Common need for pooled/spawned hierarchies (e.g. tagging an enemy + all its children).
            gameobject.layer = layer;
            foreach (Transform child in gameobject.transform){
                child.gameObject.SetLayerRecursively(layer);
            }
        }
    }

    public static class TransformExtenstions{

        public static void DestroyChildren(this Transform transform){
            // Reverse loop: destroying children forward while iterating forward
            // skips every other child as indices shift.
            for (int i = transform.childCount - 1; i >= 0; i--){
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying){
                    UnityEngine.Object.Destroy(child);
                } else {
                    UnityEngine.Object.DestroyImmediate(child);
                }
            }
        }

        public static void ResetLocal(this Transform transform){
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public static Transform SetPositionX(this Transform transform, float x){
            var pos = transform.position;
            pos.x = x;
            transform.position = pos;
            return transform;
        }

        public static Transform SetPositionY(this Transform transform, float y){
            var pos = transform.position;
            pos.y = y;
            transform.position = pos;
            return transform;
        }

        public static Transform SetPositionZ(this Transform transform, float z){
            var pos = transform.position;
            pos.z = z;
            transform.position = pos;
            return transform;
        }
    }

    public static class VectorExtenstions{

        public static Vector3 WithX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
        public static Vector3 WithZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);

        public static Vector3 Flatten(this Vector3 v){
            // Drops the Y component - useful for comparing ground speed vs. full 3D velocity
            // (e.g. distinguishing swing/glide air speed from horizontal movement speed).
            return new Vector3(v.x, 0f, v.z);
        }

        public static float Remap(this float value, float inMin, float inMax, float outMin, float outMax, bool clamp = true){
            float t = (value - inMin) / (inMax - inMin);
            float result = Mathf.Lerp(outMin, outMax, t);
            return clamp ? Mathf.Clamp(result, Mathf.Min(outMin, outMax), Mathf.Max(outMin, outMax)) : result;
        }

        public static Vector3 DirectionTo(this Vector3 from, Vector3 to){
            return (to - from).normalized;
        }

        public static float DistanceTo(this Vector3 from, Vector3 to){
            return Vector3.Distance(from, to);
        }

        public static bool IsWithinDistance(this Vector3 from, Vector3 to, float distance){
            // Sqr comparison avoids a sqrt - worth it for per-frame proximity checks
            // (e.g. TargetSelector filtering before raycasts).
            return (to - from).sqrMagnitude <= distance * distance;
        }
    }

    public static class UnityObjectExtensions{

        public static bool IsAlive(this UnityEngine.Object obj){
            // Unity overloads == on UnityEngine.Object to catch "destroyed but not null"
            // objects. Naming this explicitly makes the intent obvious at call sites,
            // instead of relying on everyone remembering the == override gotcha -
            // especially relevant with your pooled/registry objects.
            return obj != null;
        }
    }

    public static class ComponentExtensions{

        public static T GetComponentInParentOrNull<T>(this Component component) where T: Component{
            var result = component.GetComponentInParent<T>();
            return result.IsAlive() ? result : null;
        }

        public static bool TryGetComponentInChildren<T>(this Component component, out T result) where T: Component{
            result = component.GetComponentInChildren<T>();
            return result.IsAlive();
        }
    }

    public static class CameraExtensions{

        public static Vector2 WorldToPanelPosition(this Camera cam, VisualElement root, Vector3 worldPos){
            // Converts a world position to UI Toolkit panel space, correcting for the fact
            // that screen space is Y-up while UI Toolkit panel space is Y-down.
            // Centralizing this here means the "which camera is currently active" fix
            // for your multi-camera rig only has to happen in one place, not per-caller.
            var screenPoint = cam.WorldToScreenPoint(worldPos);
            screenPoint.y = Screen.height - screenPoint.y;
            return RuntimePanelUtils.ScreenToPanel(root.panel, screenPoint);
        }

        public static bool IsInViewport(this Camera cam, Vector3 worldPos){
            var vp = cam.WorldToViewportPoint(worldPos);
            return vp.z > 0 && vp.x >= 0 && vp.x <= 1 && vp.y >= 0 && vp.y <= 1;
        }
    }

    public static class LayerMaskExtensions{

        public static bool Contains(this LayerMask mask, int layer){
            return (mask.value & (1 << layer)) != 0;
        }
    }

    public static class CollectionExtensions{

        public static bool IsNullOrEmpty<T>(this ICollection<T> collection){
            return collection == null || collection.Count == 0;
        }

        public static T GetRandom<T>(this IList<T> list){
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        public static void Shuffle<T>(this IList<T> list){
            // Fisher-Yates. In-place shuffle, no allocation - handy for spawn/loot tables.
            for (int i = list.Count - 1; i > 0; i--){
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    public static class UIToolKitExtenstions{

        public static VisualElement CreateChild(this VisualElement parent, params string [] classes)
        {
            // which broke chaining (e.g. container.CreateChild("foo").text = "bar").
            var child = new VisualElement();
            parent.Add(child);
            child.AddClasses(classes);
            return child;
        }

        public static T CreateChild<T>(this VisualElement parent, params string [] classes) where T : VisualElement, new ()
        {
            var child = new T();
            parent.Add(child);
            child.AddClasses(classes);
            return child;
        }

        public static T AddTo<T>(this T child, VisualElement parent) where T: VisualElement{
            parent.Add(child);
            return child;
        }

        public static VisualElement AddClasses(this VisualElement element, params string [] classes){
            foreach (var className in classes){
                if (!String.IsNullOrEmpty(className)){
                    element.AddToClassList(className);
                }
            }
            return element;
        }

        public static VisualElement WithManipulator<T>(this VisualElement element, T manipulator)where T: IManipulator{
            element.AddManipulator(manipulator);
            return element;
        }

        public static VisualElement SetDisplay(this VisualElement element, bool visible){
            // Common toggle pattern - saves writing the ternary every time
            // (relevant for your swing indicator show/hide logic).
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            return element;
        }
    }
}