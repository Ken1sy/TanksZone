using UnityEngine;

public class TurretController : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float maxSpeed = 80f;
    public float acceleration = 720f;
    public float deceleration = 750f;

    [Header("State")]
    public bool canMove = true;

    private Transform _cameraTransform;
    private bool _isTurretLocked = false;

    private void Update()
    {
        if (!canMove || _cameraTransform == null || _isTurretLocked) return;

        HandleRotation();
    }

    public void SetCamTransform(Transform cameraTransform)
    {
        if (cameraTransform == null) return;
        _cameraTransform = cameraTransform;
    }

    public void SetTurretLock(bool isLocked)
    {
        _isTurretLocked = isLocked;
    }

    private void HandleRotation()
    {
        Vector3 cameraForward = _cameraTransform.forward;
        Vector3 targetDirection = Vector3.ProjectOnPlane(cameraForward, transform.parent.up).normalized;
        float angleToTarget = Vector3.SignedAngle(transform.forward, targetDirection, transform.parent.up);

        float step = maxSpeed * Time.deltaTime;
        if (step > Mathf.Abs(angleToTarget))
        {
            step = Mathf.Abs(angleToTarget);
        }

        transform.Rotate(0f, step * Mathf.Sign(angleToTarget), 0f, Space.Self);
    }
}