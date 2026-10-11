using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ResetParadoxProgressOnPlay : MonoBehaviour
{
#if UNITY_EDITOR

    [InitializeOnLoadMethod]
    private static void RegisterPlayModeCallback()
    {
        EditorApplication.playModeStateChanged
            -= OnPlayModeStateChanged;

        EditorApplication.playModeStateChanged
            += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(
        PlayModeStateChange state)
    {
        // Reset saved test progress immediately before
        // Unity enters Play mode.
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            PlayerPrefs.DeleteKey(
                "ParadoxCity_WrongBeerLevel");

            PlayerPrefs.DeleteKey(
                "ParadoxCity_Artist_Hitler_Level");

            PlayerPrefs.DeleteKey(
                "ParadoxCity_Forest_Scene");

            PlayerPrefs.Save();

            Debug.Log(
                "[Paradox City] Playtest progress cleared. " +
                "All levels start unplayed."
            );
        }
    }

#endif
}