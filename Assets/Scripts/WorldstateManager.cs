using System;
using System.Collections.Generic;
using UnityEngine;

public enum LevelId
{
    ArtistHitler,
    AppleForest,
    WrongBeer
}

/// <summary>
/// Single source of truth for which levels have been completed.
/// Survives scene loads. Creates itself on first use, so you do NOT
/// need to place it in any scene (but you can if you want to).
/// </summary>
public class WorldStateManager : MonoBehaviour
{
    private static WorldStateManager _instance;

    public static WorldStateManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("WorldStateManager");
                _instance = go.AddComponent<WorldStateManager>();
            }
            return _instance;
        }
    }

    private readonly Dictionary<LevelId, bool> _completed = new Dictionary<LevelId, bool>();

    public event Action<LevelId> OnLevelCompleted;

    // Keeps state clean if "Enter Play Mode Options" (domain reload off) is enabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetLevelCompleted(LevelId level, bool value = true)
    {
        _completed[level] = value;
        if (value) OnLevelCompleted?.Invoke(level);
    }

    public bool IsLevelCompleted(LevelId level)
    {
        return _completed.TryGetValue(level, out bool done) && done;
    }
}