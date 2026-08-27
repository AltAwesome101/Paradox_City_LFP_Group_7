using System.Collections.Generic;
using UnityEngine;
using StarterAssets; // needed to reference your ThirdPersonController

// Attach this to the PLAYER GameObject - the same one that already has
// ThirdPersonController, CharacterController and HealthController on it.
public class PlayerCarInteraction : MonoBehaviour
{
    [Header("Cars in the scene")]
    [Tooltip("Drag every drivable car here (for now, just the DeLorean)")]
    public List<CarController> nearbyCars = new List<CarController>();
    [Tooltip("How close the player needs to be to a car to press F and get in")]
    public float enterRange = 3f;
    [Tooltip("How far to the side of the car the player appears when getting out")]
    public float exitOffsetDistance = 2.5f;

    [Header("Camera swap")]
    [Tooltip("Drag the scene's cameraswap1 object here")]
    public cameraswap1 cameraSwitcher;
    [Tooltip("Index (in cameraswap1's camera list) of the player's normal follow camera")]
    public int playerCameraIndex = 0;
    [Tooltip("Index (in cameraswap1's camera list) of the car's follow camera")]
    public int carCameraIndex = 1;

    [Header("Debug")]
    [Tooltip("Prints what this script is doing to the Console. Turn off once everything works.")]
    public bool debugLogging = true;

    private ThirdPersonController thirdPersonController;
    private CharacterController characterController;
    private HealthController healthController;
    private Renderer[] playerRenderers;

    private CarController currentCar;
    public bool IsInCar { get; private set; }

    private void Awake()
    {
        thirdPersonController = GetComponent<ThirdPersonController>();
        characterController = GetComponent<CharacterController>();
        healthController = GetComponent<HealthController>();
        playerRenderers = GetComponentsInChildren<Renderer>();

        // These four catch the #1 cause of "F does nothing": the script ended up
        // on the wrong GameObject, or is missing something it expects to sit next to.
        if (thirdPersonController == null)
            Debug.LogError("[PlayerCarInteraction] No ThirdPersonController on this GameObject. This script must be on the Player, next to ThirdPersonController.");
        if (characterController == null)
            Debug.LogError("[PlayerCarInteraction] No CharacterController on this GameObject. This script must be on the Player.");
        if (healthController == null)
            Debug.LogError("[PlayerCarInteraction] No HealthController on this GameObject. This script must be on the Player.");
        if (cameraSwitcher == null)
            Debug.LogWarning("[PlayerCarInteraction] 'Camera Switcher' isn't assigned - camera won't swap when you enter/exit the car.");
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F)) return;

        if (debugLogging) Debug.Log($"[PlayerCarInteraction] F pressed. Currently in car: {IsInCar}");

        if (IsInCar)
        {
            ExitCar();
        }
        else
        {
            CarController closest = FindClosestCarInRange();
            if (closest != null)
            {
                EnterCar(closest);
            }
            else if (debugLogging)
            {
                Debug.Log("[PlayerCarInteraction] No car in range - see the distances logged above. " +
                          "Either 'Nearby Cars' is empty, or you're further from the car's pivot than 'Enter Range' allows.");
            }
        }
    }

    private CarController FindClosestCarInRange()
    {
        if (nearbyCars.Count == 0)
        {
            if (debugLogging) Debug.LogWarning("[PlayerCarInteraction] 'Nearby Cars' list is empty - drag the DeLorean's root object (the one with CarController on it) into it.");
            return null;
        }

        CarController closest = null;
        float closestDist = enterRange;

        foreach (CarController car in nearbyCars)
        {
            if (car == null) continue;
            float dist = Vector3.Distance(transform.position, car.transform.position);
            if (debugLogging) Debug.Log($"[PlayerCarInteraction] Distance to {car.name}: {dist:F1}m (enter range: {enterRange}m)");

            if (dist <= closestDist)
            {
                closest = car;
                closestDist = dist;
            }
        }
        return closest;
    }

    private void EnterCar(CarController car)
    {
        currentCar = car;
        IsInCar = true;

        // "disappear": stop the character's own movement/physics and hide it
        thirdPersonController.enabled = false;
        characterController.enabled = false;
        SetRenderersVisible(false);

        // hand control to the car, and tell it who to damage if it hits a wall
        car.isBeingDriven = true;
        car.driverHealth = healthController;

        if (cameraSwitcher != null) cameraSwitcher.SwitchTo(carCameraIndex);
    }

    private void ExitCar()
    {
        if (currentCar == null) return;

        currentCar.isBeingDriven = false;
        currentCar.driverHealth = null;

        // place the player beside the car, not inside its mesh
        Transform carT = currentCar.transform;
        transform.position = carT.position + carT.right * exitOffsetDistance;

        // characterController must be re-enabled AFTER moving the transform,
        // otherwise Unity can fight you on the teleport
        SetRenderersVisible(true);
        characterController.enabled = true;
        thirdPersonController.enabled = true;

        if (cameraSwitcher != null) cameraSwitcher.SwitchTo(playerCameraIndex);

        IsInCar = false;
        currentCar = null;
    }

    private void SetRenderersVisible(bool visible)
    {
        foreach (Renderer r in playerRenderers)
        {
            r.enabled = visible;
        }
    }
}