using UnityEngine;

public class FanRotator : MonoBehaviour
{
    [Header("Настройки вращения")]
    [Tooltip("Скорость вращения (градусы в секунду)")]
    public float rotationSpeed = 720f;

    [Tooltip("Ось вращения. Измени на (0, 1, 0), если лопасти крутятся не в ту сторону")]
    public Vector3 rotationAxis = new Vector3(0, 0, 1);

    void Update()
    {
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }
}