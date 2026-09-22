using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SceneLoader))]
public class SceneLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SceneLoader _sceneLoader = (SceneLoader)target;
        _sceneLoader.SceneToLoad = (SceneAsset)EditorGUILayout.ObjectField("Scene",_sceneLoader.SceneToLoad, typeof(SceneAsset), false);
    }
}
