using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GarageCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public Vector3 baseOffset = new Vector3(0, 1.5f, 0);
    [Header("UI States (Open / Closed)")]
    public Vector3 cameraShiftClosed = Vector3.zero;
    public float distanceClosed = 7.0f;
    public Vector3 cameraShiftOpen = new Vector3(1.5f, -0.5f, 0);
    public float distanceOpen = 6.0f;
    public float transitionSpeed = 5.0f;
    [Header("Rotation Settings")]
    public float sensitivity = 0.05f;
    public float damping = 5.0f;
    public float fixedVerticalAngle = 20.0f;
    [Header("Auto Rotation")]
    public float autoRotationSpeed = 8.0f;
    public float idleWaitTime = 3.0f;

    private float targetX = 0.0f;
    private float currentX = 0.0f;
    private float idleTimer = 0.0f;
    private bool isManualRotating = false;
    private bool isUIOpen = false;
    private Vector3 currentShift;
    private float currentDistance;

    void Start()
    {
        currentX = transform.eulerAngles.y;
        targetX = currentX;
        currentShift = isUIOpen ? cameraShiftOpen : cameraShiftClosed;
        currentDistance = isUIOpen ? distanceOpen : distanceClosed;
        if (target == null)
        {
            GameObject tank = GameObject.FindGameObjectWithTag("Player");
            if (tank != null) target = tank.transform;
        }
    }
    public void SetUIState(bool isOpen) { isUIOpen = isOpen; }
    void LateUpdate()
    {
        if (target == null) return;
        HandleInput();
        if (!isManualRotating)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleWaitTime) { targetX += autoRotationSpeed * Time.deltaTime; }
        }
        else { idleTimer = 0; }
        currentX = Mathf.Lerp(currentX, targetX, Time.deltaTime * damping);
        Vector3 targetShift = isUIOpen ? cameraShiftOpen : cameraShiftClosed;
        float targetDist = isUIOpen ? distanceOpen : distanceClosed;
        currentShift = Vector3.Lerp(currentShift, targetShift, Time.deltaTime * transitionSpeed);
        currentDistance = Mathf.Lerp(currentDistance, targetDist, Time.deltaTime * transitionSpeed);
        Quaternion rotation = Quaternion.Euler(fixedVerticalAngle, currentX, 0);
        Vector3 pivot = target.position + baseOffset;
        Vector3 position = rotation * new Vector3(currentShift.x, currentShift.y, -currentDistance) + pivot;
        transform.SetPositionAndRotation(position, rotation);
    }

    private void HandleInput()
    {
        bool mousePressed = Mouse.current.leftButton.isPressed;
        bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (mousePressed && !isOverUI)
        {
            isManualRotating = true;
            float mouseDeltaX = Mouse.current.delta.ReadValue().x * sensitivity;
            targetX += mouseDeltaX;
        }
        else { isManualRotating = false; }
    }
}