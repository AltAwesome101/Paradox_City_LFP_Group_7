using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class BarNPCSpawner : MonoBehaviour
{
    [Tooltip("Same AI model prefabs as AISpawner. Each one needs NavMeshAgent, NPCPatrol, " +
             "and (optionally) NPCLocomotionAnimator components for this to move and animate it.")]
    public GameObject[] npcPrefab;
    public int npcToSpawn;

    [Tooltip("Where NPCs spawn in - e.g. the bar's front door. " +
             "Defaults to this object's own position/rotation if left empty.")]
    [SerializeField] Transform spawnPoint;

    void Start()
    {
        if (!spawnPoint) spawnPoint = transform;
        StartCoroutine(Spawn());
    }

   
    IEnumerator Spawn()
    {
        int count = 0;
        while (count < npcToSpawn)
        {
            if (transform.childCount == 0)
            {
                Debug.LogWarning("[BarNPCSpawner] No bar spots (child transforms) under this spawner - stopping.");
                yield break;
            }

            int randomPrefabIndex = Random.Range(0, npcPrefab.Length);
            GameObject obj = Instantiate(npcPrefab[randomPrefabIndex], spawnPoint.position, spawnPoint.rotation);

            SetupPatrol(obj);

            yield return new WaitForSeconds(1f);
            count++;
        }
    }

    void SetupPatrol(GameObject obj)
    {
        var patrol = obj.GetComponent<NPCPatrol>();
        if (patrol == null)
        {
            Debug.LogWarning($"[BarNPCSpawner] \"{obj.name}\" has no NPCPatrol component - add one to the prefab so it can wander between bar spots.");
            return;
        }

        var points = new Transform[transform.childCount];
        for (int i = 0; i < points.Length; i++)
            points[i] = transform.GetChild(i);

        patrol.points = points;
    }
}