using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelProgressManager : MonoBehaviour
{
    public static LevelProgressManager Instance;

    public enum LevelState
    {
        Unplayed = 0,
        Active = 1,
        Completed = 2
    }

    public static event Action<string> OnLevelProgressChanged;

    private const string FirstLaunchKey =
        "ParadoxCity_InitialProgressSetup_v1";

    private readonly string[] playableLevels =
    {
        "WrongBeerLevel",
        "Artist_Hitler_Level",
        "Forest_Scene"
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeFirstLaunch();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ActivateCurrentScene();
    }

    private void InitializeFirstLaunch()
    {
        // Clear old test progress only once.
        if (PlayerPrefs.GetInt(FirstLaunchKey, 0) == 1)
        {
            return;
        }

        foreach (string level in playableLevels)
        {
            PlayerPrefs.DeleteKey("ParadoxCity_" + level);
        }

        PlayerPrefs.SetInt(FirstLaunchKey, 1);
        PlayerPrefs.Save();

        Debug.Log(
            "[LevelProgressManager] First launch initialized. " +
            "All levels start unplayed."
        );
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ActivateScene(scene.name);
    }

    private void ActivateCurrentScene()
    {
        ActivateScene(SceneManager.GetActiveScene().name);
    }

    private bool IsPlayableLevel(string sceneName)
    {
        foreach (string level in playableLevels)
        {
            if (level == sceneName)
            {
                return true;
            }
        }

        return false;
    }

    private void ActivateScene(string sceneName)
    {
        if (!IsPlayableLevel(sceneName))
        {
            Debug.Log("Not a playable level: " + sceneName);
            return;
        }

        StartLevel(sceneName);
    }

    public LevelState GetLevelState(string levelID)
    {
        int savedState = PlayerPrefs.GetInt(
            "ParadoxCity_" + levelID,
            (int)LevelState.Unplayed
        );

        return (LevelState)savedState;
    }

    public void StartLevel(string levelID)
    {
        if (!IsPlayableLevel(levelID))
        {
            Debug.LogWarning("Unknown level: " + levelID);
            return;
        }

        if (GetLevelState(levelID) == LevelState.Completed)
        {
            OnLevelProgressChanged?.Invoke(levelID);
            return;
        }

        PlayerPrefs.SetInt(
            "ParadoxCity_" + levelID,
            (int)LevelState.Active
        );

        PlayerPrefs.Save();

        OnLevelProgressChanged?.Invoke(levelID);

        Debug.Log("Level Active: " + levelID);
    }

    public void CompleteLevel(string levelID)
    {
        if (!IsPlayableLevel(levelID))
        {
            Debug.LogWarning("Unknown level: " + levelID);
            return;
        }

        PlayerPrefs.SetInt(
            "ParadoxCity_" + levelID,
            (int)LevelState.Completed
        );

        PlayerPrefs.Save();

        OnLevelProgressChanged?.Invoke(levelID);

        Debug.Log("Level Completed: " + levelID);
    }

    public void ResetLevel(string levelID)
    {
        if (!IsPlayableLevel(levelID))
        {
            Debug.LogWarning("Unknown level: " + levelID);
            return;
        }

        PlayerPrefs.DeleteKey("ParadoxCity_" + levelID);
        PlayerPrefs.Save();

        OnLevelProgressChanged?.Invoke(levelID);

        Debug.Log("Level Reset: " + levelID);
    }

    public void ResetAllProgress()
    {
        foreach (string level in playableLevels)
        {
            PlayerPrefs.DeleteKey("ParadoxCity_" + level);
            OnLevelProgressChanged?.Invoke(level);
        }

        PlayerPrefs.Save();

        Debug.Log("All Paradox City level progress reset.");
    }
}