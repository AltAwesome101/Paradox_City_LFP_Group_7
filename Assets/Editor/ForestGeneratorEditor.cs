using UnityEditor;      // Editor, CustomEditor, EditorGUILayout, Undo
using UnityEngine;

// Tells Unity: "when you show a ForestGenerator in the Inspector, use this class to draw it".
// This file MUST be inside a folder named "Editor" (e.g. Assets/Editor/) or builds will fail.
[CustomEditor(typeof(ForestGenerator))]
public class ForestGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();                     // draw all the normal fields exactly as before

        var gen = (ForestGenerator)target;          // 'target' = the component being inspected

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate", GUILayout.Height(30)))
            gen.Generate();

        // Handy for exploring: new seed, then generate straight away.
        if (GUILayout.Button("Randomize Seed + Generate"))
        {
            Undo.RecordObject(gen, "Randomize Seed");   // makes the seed change undoable and marks the scene dirty
            gen.seed = Random.Range(0, 100000);
            gen.Generate();
        }

        if (GUILayout.Button("Clear"))
            gen.Clear();
    }
}