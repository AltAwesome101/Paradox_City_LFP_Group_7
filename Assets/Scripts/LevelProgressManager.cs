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
        // Initialise progress only once.
        // Never clear progress on every game or scene restart.
        if (PlayerPrefs.GetInt(FirstLaunchKey, 0) == 1)
            return;

        foreach (string level in playableLevels)
        {
            PlayerPrefs.DeleteKey("ParadoxCity_" + level);
        }

        PlayerPrefs.SetInt(FirstLaunchKey, 1);
        PlayerPrefs.Save();

        Debug.Log(
            "[LevelProgressManager] Initial progress setup complete."
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
                return true;
        }

        return false;
    }

    private void ActivateScene(string sceneName)
    {
        if (!IsPlayableLevel(sceneName))
        {
            Debug.Log(
                "[LevelProgressManager] Not a playable level: " +
                sceneName
            );
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
            Debug.LogWarning(
                "[LevelProgressManager] Unknown level: " + levelID
            );
            return;
        }

        LevelState currentState = GetLevelState(levelID);

        // Never downgrade a completed level.
        if (currentState == LevelState.Completed)
        {
            OnLevelProgressChanged?.Invoke(levelID);
            Debug.Log(
                "[LevelProgressManager] Completed level preserved: " +
                levelID
            );
            return;
        }

        // Mark this level active without changing any other level.
        PlayerPrefs.SetInt(
            "ParadoxCity_" + levelID,
            (int)LevelState.Active
        );

        PlayerPrefs.Save();

        OnLevelProgressChanged?.Invoke(levelID);

        Debug.Log(
            "[LevelProgressManager] Level active: " + levelID
        );
    }

    public void CompleteLevel(string levelID)
    {
        if (!IsPlayableLevel(levelID))
        {
            Debug.LogWarning(
                "[LevelProgressManager] Unknown level: " + levelID
            );
            return;
        }

        PlayerPrefs.SetInt(
            "ParadoxCity_" + levelID,
            (int)LevelState.Completed
        );

        PlayerPrefs.Save();

        OnLevelProgressChanged?.Invoke(levelID);

        Debug.Log(
            "[LevelProgressManager] Level completed: " + levelID
        );
    }

    // Use this only when you intentionally want to reset ONE level.
    public void ResetLevel(string levelID)
    {
        if (!IsPlayableLevel(levelID))
        {
            Debug.LogWarning(
                "[LevelProgressManager] Unknown level: " + levelID
            );
            return;
        }

        PlayerPrefs.DeleteKey("ParadoxCity_" + levelID);
        PlayerPrefs.Save();

        OnLevelProgressChanged?.Invoke(levelID);

        Debug.Log(
            "[LevelProgressManager] Level progress reset: " + levelID
        );
    }

    // Reserve this for an intentional full keychain reset.
    public void ResetAllProgress()
    {
        foreach (string level in playableLevels)
        {
            PlayerPrefs.DeleteKey("ParadoxCity_" + level);
            OnLevelProgressChanged?.Invoke(level);
        }

        PlayerPrefs.Save();

        Debug.Log(
            "[LevelProgressManager] All level progress reset."
        );
    }
}