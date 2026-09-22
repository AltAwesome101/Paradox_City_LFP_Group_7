
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class SceneReference
{
    [SerializeField] string scenePath;
    [SerializeField] string sceneName;
    [SerializeField] int buildIndex = -1;
    [SerializeField] bool isAdditive;

    public string ScenePath => scenePath;
    public string Name => sceneName;
    public int BuildIndex => buildIndex;
    public bool IsAdditive => isAdditive;
    public bool IsActive{get; set;} = false;

#if UNITY_EDITOR
    public SceneAsset SceneAsset
    {
        get => AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        set 
        {    
            if(value == null)
            {
                scenePath = string.Empty;
                sceneName = string.Empty;
                buildIndex = -1;
                return;
            }

            scenePath = AssetDatabase.GetAssetPath(value);
            sceneName = value.name;

            buildIndex = -1;
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if(scene.path == scenePath){
                    buildIndex = System.Array.IndexOf(EditorBuildSettings.scenes, scene);
                    break;
                }
            }
        }


    }
#endif

}