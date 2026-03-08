using UnityEngine;
using UnityEngine.InputSystem;

public class TurretController : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float maxSpeed = 80f;
    public float acceleration = 120f;
    public float deceleration = 150f;

    [Header("State")]
    public bool canMove = true;

    private float currentSpeed = 0f;
    private float inputDirection = 0f;

    public void OnTurretRotate(InputAction.CallbackContext context) => inputDirection = context.ReadValue<float>();

    private void Update()
    {
        if (!canMove) return;

        HandleRotation();
    }
    private void HandleRotation()
    {
        if (Mathf.Abs(inputDirection) > 0.1f)
        {
            float targetVel = inputDirection * maxSpeed;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetVel, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
        }
        if (Mathf.Abs(currentSpeed) > 0.01f)
        {
            transform.Rotate(0f, currentSpeed * Time.deltaTime, 0f, Space.Self);
        }
    }
}