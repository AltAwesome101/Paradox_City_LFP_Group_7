using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HubAISpawner : MonoBehaviour
{
    [Header("Spawning (same as the original AISpawner)")]
    public GameObject[] AiPrefab;
    public int AiToSpawn = 10;
    public float spawnInterval = 1f;

    [Header("Outfits")]
    [Tooltip("When this level is completed, every NPC switches to the 'After' outfit.")]
    [SerializeField] private LevelId triggerLevel = LevelId.ArtistHitler;
    [SerializeField] private bool dressNpcs = true;
    [Tooltip("Optional. A lit material to base the clothes on. Assigning one is the safest option for builds.")]
    [SerializeField] private Material outfitMaterial;
    [SerializeField] private bool playChangeEffect = true;
    [Tooltip("Logs the measured body sizes for each NPC.")]
    [SerializeField] private bool logOutfitInfo = false;

    [Header("Looks (edit colours, shapes and chances here)")]
    [SerializeField] private OutfitLook beforeLook = OutfitLook.Overcast();
    [SerializeField] private OutfitLook afterLook = OutfitLook.Vibrant();

    private readonly List<NpcOutfit> _npcs = new List<NpcOutfit>();
    private bool _completed;

    private void Start()
    {
        _completed = IsTriggered();
        StartCoroutine(Spawn());
    }

    private void Update()
    {
        bool now = IsTriggered();
        if (now == _completed) return;
        _completed = now;

        for (int i = _npcs.Count - 1; i >= 0; i--)
        {
            if (_npcs[i] == null) { _npcs.RemoveAt(i); continue; }
            _npcs[i].SetCompleted(_completed, playChangeEffect);
        }
    }

    private bool IsTriggered()
    {
        var wsm = WorldStateManager.Instance;
        return wsm != null && wsm.IsLevelCompleted(triggerLevel);
    }

    private IEnumerator Spawn()
    {
        if (AiPrefab == null || AiPrefab.Length == 0 || transform.childCount == 0)
        {
            Debug.LogWarning("[HubAISpawner] Needs at least one AI prefab and one waypoint child.");
            yield break;
        }

        for (int count = 0; count < AiToSpawn; count++)
        {
            GameObject prefab = AiPrefab[Random.Range(0, AiPrefab.Length)];
            Transform child = transform.GetChild(Random.Range(0, transform.childCount));

            GameObject obj = Instantiate(prefab);

            if (obj.TryGetComponent(out WaypointNavigator nav) &&
                child.TryGetComponent(out Waypoint waypoint))
            {
                nav.currentWaypoint = waypoint;
            }

            obj.transform.position = child.position;

            
            if (dressNpcs)
            {
                var outfit = obj.AddComponent<NpcOutfit>();
                outfit.verbose = logOutfitInfo;
                outfit.Build(beforeLook, afterLook, outfitMaterial, Random.Range(0, int.MaxValue - 10000));
                outfit.SetCompleted(_completed, false);
                _npcs.Add(outfit);
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }
}