using UnityEditor;
using UnityEngine;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] string _sceneToLoadPath;

#if UNITY_EDITOR
    public SceneAsset SceneToLoad
    {
        get { return AssetDatabase.LoadAssetAtPath<SceneAsset>(_sceneToLoadPath);}
        set { _sceneToLoadPath = AssetDatabase.GetAssetPath(value);}
    }
#endif
}
