using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Targets")]
    public Transform target;            // За кем следим (Танк)
    public Transform cameraObject;      // Сама камера (дочерний объект)
    public Transform defaultPosition;   // Точка, где камера должна быть в идеале

    [Header("Movement Settings")]
    public float smoothing = 5f;
    public float rotSmoothing = 3f;
    public float moveSpeed = 3f;
    public float maxDistance = 9f;
    public LayerMask groundLayer;

    [Header("State")]
    public bool follow = true;
    public bool spectatorMode;

    [Header("Weather (Simplified)")]
    public MonoBehaviour snowEffect; // Ссылка на компонент снега
    public GameObject rainObject;    // Ссылка на объект дождя

    private float _heightInput;
    private bool _isSpectatorEnabled;
    private Vector3 _initialOffset;

    private void Start()
    {
        if (target != null)
        {
            _initialOffset = transform.position - target.position;
        }

        // Если нужно инициализировать погоду без ECS:
        // SetupWeather("rain"); 
    }

    // Метод для New Input System (вызывается через PlayerInput или напрямую)
    public void OnCameraHeightAdjust(InputAction.CallbackContext context)
    {
        // Читаем значение оси (например, кнопки PageUp/PageDown или R/F)
        _heightInput = context.ReadValue<float>();
    }

    private void LateUpdate()
    {
        if (spectatorMode)
        {
            HandleSpectatorMode();
            return;
        }

        if (follow && target != null)
        {
            HandleFollow();
            HandleCollisionAndHeight();
        }
    }

    private void HandleFollow()
    {
        // Плавное перемещение контейнера камеры к танку
        transform.position = Vector3.Lerp(transform.position, target.position, smoothing * Time.deltaTime);

        // Плавный поворот только по Y
        float targetYAngle = target.eulerAngles.y;
        float currentYAngle = transform.eulerAngles.y;
        float nextYAngle = Mathf.LerpAngle(currentYAngle, targetYAngle, rotSmoothing * Time.deltaTime);

        transform.rotation = Quaternion.Euler(0, nextYAngle, 0);

        // Камера всегда смотрит на танк
        cameraObject.LookAt(target);
    }

    private void HandleCollisionAndHeight()
    {
        float distanceOffset = 0.05f;

        // Вычисляем изменение высоты на основе ввода
        float heightChange = _heightInput * moveSpeed * Time.deltaTime;

        // Обновляем позицию дефолтной точки (куда камера хочет вернуться)
        Vector3 defPos = defaultPosition.localPosition;
        defPos.y += heightChange;
        defaultPosition.localPosition = defPos;

        RaycastHit hit;
        // Проверка лучом от танка к идеальной позиции камеры
        if (Physics.Linecast(target.position, defaultPosition.position, out hit, groundLayer, QueryTriggerInteraction.Ignore))
        {
            // Если есть препятствие, двигаем камеру в точку удара
            Vector3 hitPointWithOffset = hit.point + (target.position - hit.point).normalized * distanceOffset;
            cameraObject.position = Vector3.Lerp(cameraObject.position, hitPointWithOffset, smoothing * Time.deltaTime);
        }
        else
        {
            // Если пути чисто, плавно возвращаемся в defaultPosition
            cameraObject.position = Vector3.Lerp(cameraObject.position, defaultPosition.position, smoothing * Time.deltaTime);
        }
    }

    private void HandleSpectatorMode()
    {
        if (!_isSpectatorEnabled)
        {
            // Включаем компоненты свободного полета (нужно создать/прикрепить свои скрипты)
            //if (cameraObject.TryGetComponent(out CameraSpectator spectator)) spectator.enabled = true;
            //if (cameraObject.TryGetComponent(out MouseLook mouseLook)) mouseLook.enabled = true;
            _isSpectatorEnabled = true;
        }
    }
}