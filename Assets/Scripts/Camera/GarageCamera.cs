using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class GarageCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 1.5f, 0);
    public float distance = 7.0f;

    [Header("Rotation Settings")]
    [Tooltip("Чувствительность мыши (теперь значительно меньше)")]
    public float sensitivity = 0.05f;
    [Tooltip("Плавность замедления (чем выше, тем быстрее остановка)")]
    public float damping = 5.0f;
    [Tooltip("Фиксированный угол наклона пушки (вид сверху)")]
    public float fixedVerticalAngle = 20.0f;

    [Header("Auto Rotation")]
    public float autoRotationSpeed = 8.0f;
    public float idleWaitTime = 3.0f;

    private float targetX = 0.0f;
    private float currentX = 0.0f;
    private float idleTimer = 0.0f;
    private bool isManualRotating = false;

    void Start()
    {
        // Инициализируем текущие углы
        currentX = transform.eulerAngles.y;
        targetX = currentX;

        if (target == null)
        {
            GameObject tank = GameObject.FindGameObjectWithTag("Player");
            if (tank != null) target = tank.transform;
        }
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

        // 2. Плавное замедление (Damping)
        // Мы плавно приближаем текущий угол к целевому
        currentX = Mathf.Lerp(currentX, targetX, Time.deltaTime * damping);

        // 3. Вычисление финальной позиции
        // Используем fixedVerticalAngle вместо переменной Y, чтобы убрать вращение вверх/вниз
        Quaternion rotation = Quaternion.Euler(fixedVerticalAngle, currentX, 0);
        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.position + offset;

        transform.rotation = rotation;
        transform.position = position;
    }

    private void HandleInput()
    {
        bool mousePressed = Mouse.current.leftButton.isPressed;
        bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (mousePressed && !isOverUI)
        {
            isManualRotating = true;

            // Считываем только горизонтальную дельту (X)
            float mouseDeltaX = Mouse.current.delta.ReadValue().x * sensitivity;

            // Накапливаем целевой поворот
            targetX += mouseDeltaX;
        }
        else
        {
            isManualRotating = false;
        }
    }
}