using UnityEngine;

public class BlimpController : MonoBehaviour
{
    public enum PathMode
    {
        Loop,
        PingPong
    }

    [Header("Path")]
    public Transform[] waypoints;
    public PathMode pathMode = PathMode.Loop;

    [Tooltip("Movement speed")]
    public float speed = 10f;

    [Tooltip("How quickly the blimp turns")]
    public float turnSpeed = 2f;

    [Tooltip("Distance from waypoint before going to the next one")]
    public float arriveDistance = 1f;

    [Header("Atmosphere")]
    public float bobAmount = 0.5f;
    public float bobSpeed = 0.3f;

    [Header("Starting Position")]
    public bool randomizeStart = false;

    private int currentIndex = 0;
    private int direction = 1;

    private float bobTimer;
    private Vector3 basePosition;

    private void Start()
    {
        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.LogWarning("[BlimpController] You need at least 2 waypoints.");
            enabled = false;
            return;
        }

        
        if (randomizeStart)
        {
            currentIndex = Random.Range(0, waypoints.Length);
        }
        else
        {
            currentIndex = 0;
        }

        
        basePosition = waypoints[currentIndex].position;
        transform.position = basePosition;

        
        bobTimer = Random.Range(0f, 100f);

        
        AdvanceWaypoint();
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Length < 2)
            return;

        Vector3 targetPosition = waypoints[currentIndex].position;

     

        basePosition = Vector3.MoveTowards(
            basePosition,
            targetPosition,
            speed * Time.deltaTime
        );

        
        if (Vector3.Distance(basePosition, targetPosition) <= arriveDistance)
        {
            basePosition = targetPosition;
            AdvanceWaypoint();
        }

        

        Vector3 movementDirection =
            targetPosition - basePosition;

        
        movementDirection.y = 0f;

        if (movementDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    movementDirection.normalized,
                    Vector3.up
                );

            
            float targetY = targetRotation.eulerAngles.y;

            Vector3 currentEuler = transform.eulerAngles;

            Quaternion yRotation = Quaternion.Euler(
                currentEuler.x,
                targetY,
                currentEuler.z
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                yRotation,
                turnSpeed * Time.deltaTime
            );
        }

       

        bobTimer += Time.deltaTime * bobSpeed;

        float bobOffset =
            Mathf.Sin(bobTimer) * bobAmount;

        
        transform.position =
            basePosition + Vector3.up * bobOffset;
    }

    private void AdvanceWaypoint()
    {
        if (pathMode == PathMode.Loop)
        {
            currentIndex++;

            if (currentIndex >= waypoints.Length)
            {
                currentIndex = 0;
            }
        }
        else
        {
            currentIndex += direction;

            
            if (currentIndex >= waypoints.Length)
            {
                direction = -1;
                currentIndex = waypoints.Length - 2;
            }

            
            else if (currentIndex < 0)
            {
                direction = 1;
                currentIndex = 1;
            }
        }
    }
}