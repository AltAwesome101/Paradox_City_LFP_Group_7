using UnityEngine;
using UnityEngine.XR;


public class DroneSpawner : MonoBehaviour
{
    [Header("Prefab & Count")]
    [Tooltip("The drone model/prefab to spawn. Doesn't need a Drone component already on it - this spawner adds one if missing.")]
    public GameObject dronePrefab;

    [Range(1, 50)]
    public int droneCount = 6;

    [Header("Flight Area (spawn area AND wander area for every drone)")]
    [Tooltip("Center of the box drones fly around in. Defaults to this spawner's own position if left at (0,0,0).")]
    public Vector3 areaCenter;

    [Tooltip("Full width/height/depth of the flight box. Height (Y) is usually much smaller than width/depth for drones.")]
    public Vector3 areaSize = new Vector3(60f, 12f, 60f);

    [Header("Per-Drone Settings (applied to every spawned drone)")]
    public float moveSpeed = 4f;
    public float turnSpeed = 90f;

    [Tooltip("Layers drones must avoid flying into (buildings, terrain, props). Do NOT include the Player layer - that's handled separately below.")]
    public LayerMask obstacleLayers;

    public float obstacleDetectRange = 5f;
    public float obstacleAvoidRadius = 0.75f;

    [Tooltip("Minimum distance drones try to keep from the player.")]
    public float minPlayerDistance = 6f;

    [Header("Spawn Safety")]
    [Tooltip("Spawn points that land inside an obstacle are re-rolled up to this many times before giving up and using the area center.")]
    public int maxSpawnAttemptsPerDrone = 20;

    [Header("Debug")]
    public bool debugLogging = false;

    private void Awake()
    {
        if (areaCenter == Vector3.zero)
            areaCenter = transform.position;
    }

    private void Start()
    {
        SpawnDrones();
    }

    public void SpawnDrones()
    {
        if (dronePrefab == null)
        {
            Debug.LogError("[DroneSpawner] No dronePrefab assigned - nothing to spawn.");
            return;
        }

        for (int i = 0; i < droneCount; i++)
        {
            Vector3 spawnPos = FindClearSpawnPoint();

            GameObject droneObj = Instantiate(dronePrefab, spawnPos, Random.rotation, transform);
            droneObj.name = $"{dronePrefab.name}_{i}";

            Drone drone = droneObj.GetComponent<Drone>();
            if (drone == null)
                drone = droneObj.AddComponent<Drone>();

            drone.areaCenter = areaCenter;
            drone.areaSize = areaSize;
            drone.moveSpeed = moveSpeed;
            drone.turnSpeed = turnSpeed;
            drone.obstacleLayers = obstacleLayers;
            drone.obstacleDetectRange = obstacleDetectRange;
            drone.obstacleAvoidRadius = obstacleAvoidRadius;
            drone.minPlayerDistance = minPlayerDistance;
            drone.debugLogging = debugLogging;

            if (debugLogging)
                Debug.Log($"[DroneSpawner] Spawned {droneObj.name} at {spawnPos}");
        }
    }

    private Vector3 FindClearSpawnPoint()
    {
        for (int attempt = 0; attempt < maxSpawnAttemptsPerDrone; attempt++)
        {
            Vector3 candidate = areaCenter + new Vector3(
                Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
                Random.Range(-areaSize.y / 2f, areaSize.y / 2f),
                Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
            );

            if (!Physics.CheckSphere(candidate, obstacleAvoidRadius, obstacleLayers, QueryTriggerInteraction.Ignore))
                return candidate;
        }

        if (debugLogging)
            Debug.LogWarning("[DroneSpawner] Couldn't find a fully clear spawn point after " +
                              maxSpawnAttemptsPerDrone + " attempts - using area center instead.");

        return areaCenter;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = areaCenter == Vector3.zero ? transform.position : areaCenter;
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireCube(center, areaSize);
    }
}