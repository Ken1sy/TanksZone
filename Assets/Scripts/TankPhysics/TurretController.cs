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
    private float _currentLocalAngle = 0f;
    private float _currentSpeed = 0f;

    private void Start() { _currentLocalAngle = transform.localEulerAngles.y; }

    private void LateUpdate()
    {
        if (_cameraTransform == null) return;
        if (!canMove || _isTurretLocked) {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, deceleration * Time.deltaTime);
            ApplyRotation();
            return;
        }
        HandleRotation();
    }

    public void SetCamTransform(Transform cameraTransform)
    {
        if (cameraTransform == null) return;
        _cameraTransform = cameraTransform;
    }

    public void SetTurretLock(bool isLocked) { _isTurretLocked = isLocked; }

    private void HandleRotation()
    {
        Vector3 localTargetDir = transform.parent.InverseTransformDirection(_cameraTransform.forward);
        localTargetDir.y = 0f;
        if (localTargetDir.sqrMagnitude < 0.001f) return;
        float targetAngle = Vector3.SignedAngle(Vector3.forward, localTargetDir, Vector3.up);
        float angleDifference = Mathf.DeltaAngle(_currentLocalAngle, targetAngle);
        float absDifference = Mathf.Abs(angleDifference);
        float brakingDistance = (_currentSpeed * _currentSpeed) / (2f * deceleration);
        if (absDifference > 0.1f || Mathf.Abs(_currentSpeed) > 0.1f) {
            bool isMovingWrongWay = Mathf.Abs(_currentSpeed) > 0.1f && Mathf.Sign(angleDifference) != Mathf.Sign(_currentSpeed);
            if (absDifference <= brakingDistance || isMovingWrongWay) {
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, deceleration * Time.deltaTime);
            }
            else {
                float targetSpeed = Mathf.Sign(angleDifference) * maxSpeed;
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * Time.deltaTime);
            }
        }
        else {
            _currentSpeed = 0f; _currentLocalAngle = targetAngle;
        }
        ApplyRotation();
    }
    private void ApplyRotation()
    {
        _currentLocalAngle += _currentSpeed * Time.deltaTime;
        if (_currentLocalAngle > 180f) _currentLocalAngle -= 360f;
        if (_currentLocalAngle < -180f) _currentLocalAngle += 360f;
        transform.localEulerAngles = new Vector3(0f, _currentLocalAngle, 0f);
    }
} 