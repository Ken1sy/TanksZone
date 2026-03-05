using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TankDebugControls : MonoBehaviour
{
    [Header("Teleport")]
    public float teleportHeight = 1.5f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // “елепорт вверх Ч клавиша T
        if (keyboard.tKey.wasPressedThisFrame)
        {
            TeleportUp();
        }

        // —брос поворота Ч клавиша R
        if (keyboard.rKey.wasPressedThisFrame)
        {
            ResetRotation();
        }
    }

    private void TeleportUp()
    {
        rb.position += Vector3.up * teleportHeight;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void ResetRotation()
    {
        float currentYaw = rb.rotation.eulerAngles.y;
        rb.MoveRotation(Quaternion.Euler(0f, currentYaw, 0f));
        rb.angularVelocity = Vector3.zero;
    }
}