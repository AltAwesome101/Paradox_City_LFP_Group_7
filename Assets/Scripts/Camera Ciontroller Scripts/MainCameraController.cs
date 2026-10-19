using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCameraController : MonoBehaviour
{
    [Header("Camera Controller")]
    public Transform target;
    public float gap = 3f;
    public float rotSpeed = 2f;

    [Header("Camera handling")]
    public float minVerAngle = -14f;
    public float maxVerAngle = 45f;
    public Vector2 framingBalance;
    float rotX;
    float rotY;
    public bool invertX;
    public bool invertY;
    float invertXValue;
    float invertYValue;

    [Header("Collision")]
    [Tooltip("Layers the camera can't pass through. Defaults to Default + Ground if left empty.")]
    public LayerMask collisionMask;
    [Tooltip("Radius of the camera's collision sphere. Bigger = stays further from walls and avoids near-plane clipping.")]
    public float cameraRadius = 0.25f;
    [Tooltip("Extra space kept between the camera and the surface it hit.")]
    public float collisionBuffer = 0.05f;
    [Tooltip("Closest the camera is allowed to get to the character.")]
    public float minGap = 0.3f;
    [Tooltip("How quickly the camera eases back out after an obstruction is gone.")]
    public float zoomOutSpeed = 6f;

    float currentGap;

    [Header("Interaction Lock")]
    [Tooltip("When false, mouse movement stops rotating the camera - used while interacting with something like the painting, so clicking doesn't also spin the view")]
    public bool inputEnabled = true;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        currentGap = gap;

        if (collisionMask.value == 0)
            collisionMask = LayerMask.GetMask("Default", "Ground");
    }

    private void Update()
    {
        if (inputEnabled)
        {
            invertXValue = (invertX) ? -1 : 1;
            invertYValue = (invertY) ? -1 : 1;

            rotX += Input.GetAxis("Mouse Y") * invertYValue * rotSpeed;
            rotX = Mathf.Clamp(rotX, minVerAngle, maxVerAngle);

            rotY += Input.GetAxis("Mouse X") * invertXValue * rotSpeed;
        }

        var targetRotation = Quaternion.Euler(rotX, rotY, 0);
        var focusPos = target.position + new Vector3(framingBalance.x, framingBalance.y);

        // Direction from the character back to where the camera wants to sit
        Vector3 backDir = targetRotation * Vector3.back;

        // How far back can the camera actually go?
        float desiredGap = gap;
        if (Physics.SphereCast(focusPos, cameraRadius, backDir, out RaycastHit hit,
                               gap, collisionMask, QueryTriggerInteraction.Ignore))
        {
            desiredGap = Mathf.Clamp(hit.distance - collisionBuffer, minGap, gap);
        }

        // Zoom in instantly (never clip), zoom out smoothly (no popping)
        if (desiredGap < currentGap)
            currentGap = desiredGap;
        else
            currentGap = Mathf.Lerp(currentGap, desiredGap, zoomOutSpeed * Time.deltaTime);

        transform.position = focusPos + backDir * currentGap;
        transform.rotation = targetRotation;
    }

    public Quaternion flatRotation => Quaternion.Euler(0, rotY, 0);
}