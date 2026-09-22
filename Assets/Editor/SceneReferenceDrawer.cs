#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SceneReference))]
public class SceneReferenceDrawer : PropertyDrawer
{
    const float LINE  = 18f;
    const float INDENT = 12f;
    const float PAD   = 2f;

    // Row 0: object field
    // Row 1: info (name + build index)
    // Row 2: additive toggle
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return LINE * 3 + PAD * 2;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var pathProp      = property.FindPropertyRelative("scenePath");
        var nameProp      = property.FindPropertyRelative("sceneName");
        var buildIdxProp  = property.FindPropertyRelative("buildIndex");
        var isAdditiveProp = property.FindPropertyRelative("isAdditive");

        // ── Row 0: drag target ───────────────────────────────────────────────
        float labelW  = EditorGUIUtility.labelWidth;
        var   row0    = new Rect(position.x, position.y, position.width, LINE);
        var   assetRect = new Rect(position.x + labelW, position.y,
                                   position.width - labelW, LINE);

        EditorGUI.LabelField(row0, label);

        var currentAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(pathProp.stringValue);

        EditorGUI.BeginChangeCheck();
        var picked = (SceneAsset)EditorGUI.ObjectField(
            assetRect, currentAsset, typeof(SceneAsset), false);

        if (EditorGUI.EndChangeCheck())
        {
            // Write directly into the serialized properties — no reflection needed
            if (picked == null)
            {
                pathProp.stringValue     = string.Empty;
                nameProp.stringValue     = string.Empty;
                buildIdxProp.intValue    = -1;
            }
            else
            {
                string path = AssetDatabase.GetAssetPath(picked);
                pathProp.stringValue  = path;
                nameProp.stringValue  = picked.name;

                buildIdxProp.intValue = -1;
                var buildScenes = EditorBuildSettings.scenes;
                for (int i = 0; i < buildScenes.Length; i++)
                {
                    if (buildScenes[i].path == path)
                    {
                        buildIdxProp.intValue = i;
                        break;
                    }
                }
            }

            property.serializedObject.ApplyModifiedProperties();
        }

        // ── Row 1: read-only info ────────────────────────────────────────────
        var row1 = new Rect(position.x + INDENT,
                            position.y + (LINE + PAD),
                            position.width - INDENT, LINE);

        string sceneName = nameProp.stringValue;
        int    bi        = buildIdxProp.intValue;

        string info = string.IsNullOrEmpty(sceneName)
            ? "No scene selected"
            : $"{sceneName}   |   build index: {(bi >= 0 ? bi.ToString() : "not in build")}";

        // Tint the label red when the scene isn't in build settings
        var prevColor = GUI.contentColor;
        if (!string.IsNullOrEmpty(sceneName) && bi < 0)
            GUI.contentColor = new Color(1f, 0.4f, 0.4f);

        EditorGUI.LabelField(row1, "Info", info, EditorStyles.miniLabel);
        GUI.contentColor = prevColor;

        // ── Row 2: additive toggle ───────────────────────────────────────────
        var row2 = new Rect(position.x + INDENT,
                            position.y + (LINE + PAD) * 2,
                            position.width - INDENT, LINE);

        EditorGUI.PropertyField(row2, isAdditiveProp, new GUIContent("Load Additive"));

        EditorGUI.EndProperty();
    }
}
#endif