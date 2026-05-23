using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class GarageCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    [Tooltip("Базовая точка фокуса (обычно центр танка)")]
    public Vector3 baseOffset = new Vector3(0, 1.5f, 0);

    [Header("UI States (Open / Closed)")]
    [Tooltip("Смещение камеры, когда интерфейс ЗАКРЫТ (Танк по центру)")]
    public Vector3 cameraShiftClosed = Vector3.zero;
    public float distanceClosed = 7.0f;

    [Tooltip("Смещение камеры, когда интерфейс ОТКРЫТ")]
    // X > 0 сдвигает камеру вправо (танк кажется левее)
    // Y < 0 сдвигает камеру вниз (танк кажется выше)
    public Vector3 cameraShiftOpen = new Vector3(1.5f, -0.5f, 0);
    public float distanceOpen = 6.0f;

    [Tooltip("Скорость плавного перемещения камеры")]
    public float transitionSpeed = 5.0f;

    [Header("Rotation Settings")]
    public float sensitivity = 0.05f;
    public float damping = 5.0f;
    public float fixedVerticalAngle = 20.0f;

    [Header("Auto Rotation")]
    public float autoRotationSpeed = 8.0f;
    public float idleWaitTime = 3.0f;

    // Внутренние переменные состояния
    private float targetX = 0.0f;
    private float currentX = 0.0f;
    private float idleTimer = 0.0f;
    private bool isManualRotating = false;

    private bool isUIOpen = false; // При старте считаем, что гараж открыт
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

    // Этот метод вызывается из GarageUIManager при нажатии кнопки интерфейса
    public void SetUIState(bool isOpen)
    {
        isUIOpen = isOpen;
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleInput();

        // 1. Логика авто-вращения
        if (!isManualRotating)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleWaitTime)
            {
                targetX += autoRotationSpeed * Time.deltaTime;
            }
        }
        else
        {
            idleTimer = 0;
        }

        // 2. Плавное вращение (Damping)
        currentX = Mathf.Lerp(currentX, targetX, Time.deltaTime * damping);

        // 3. Плавное изменение смещения и дистанции в зависимости от UI (Lerp)
        Vector3 targetShift = isUIOpen ? cameraShiftOpen : cameraShiftClosed;
        float targetDist = isUIOpen ? distanceOpen : distanceClosed;

        currentShift = Vector3.Lerp(currentShift, targetShift, Time.deltaTime * transitionSpeed);
        currentDistance = Mathf.Lerp(currentDistance, targetDist, Time.deltaTime * transitionSpeed);

        // 4. Вычисление финальной позиции
        Quaternion rotation = Quaternion.Euler(fixedVerticalAngle, currentX, 0);
        Vector3 pivot = target.position + baseOffset;

        // Магия здесь: мы применяем сдвиг в локальных координатах камеры!
        Vector3 position = rotation * new Vector3(currentShift.x, currentShift.y, -currentDistance) + pivot;

        transform.rotation = rotation;
        transform.position = position;
    }

    private void HandleInput()
    {
        bool mousePressed = Mouse.current.leftButton.isPressed;
        // Блокируем вращение, если мышка над интерфейсом
        bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (mousePressed && !isOverUI)
        {
            isManualRotating = true;
            float mouseDeltaX = Mouse.current.delta.ReadValue().x * sensitivity;
            targetX += mouseDeltaX;
        }
        else
        {
            isManualRotating = false;
        }
    }
}