using UnityEditor;      // Editor, CustomEditor, EditorGUILayout, Undo
using UnityEngine;

// This file MUST be inside a folder named "Editor" (e.g. Assets/Editor/) or builds will fail.
[CustomEditor(typeof(ForestGenerator))]
public class ForestGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var gen = (ForestGenerator)target;

        // Preset first: this is the "start here" button.
        EditorGUILayout.HelpBox(
            "New here? Click 'Setup Forest Preset' first. It finds your PP_ prefabs and fills in every layer with sensible numbers.",
            MessageType.Info);

        if (GUILayout.Button("Setup Forest Preset (assign prefabs + layer values)", GUILayout.Height(26)))
            gen.SetupPreset();

        EditorGUILayout.Space();

        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate", GUILayout.Height(30)))
            gen.Generate();

        if (GUILayout.Button("Randomize Seed + Generate"))
        {
            Undo.RecordObject(gen, "Randomize Seed");
            gen.seed = Random.Range(0, 100000);
            gen.Generate();
        }

        if (GUILayout.Button("Clear"))
            gen.Clear();
    }
}