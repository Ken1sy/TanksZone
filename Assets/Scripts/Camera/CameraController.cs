using GameScripts.UI;
using UnityEngine;

namespace GameScripts.Camera
{
    public class CameraController : MonoBehaviour
    {
        [Header("Targets")]
        public Transform cameraObject;
        public Transform defaultPosition;
        [Header("Movement Settings")]
        public float smoothing = 10f;
        public float minPitch = -10f;
        public float maxPitch = 60f;
        [Header("Zoom Settings")]
        public float minZoom = 3f;
        public float maxZoom = 15f;
        public float zoomSpeed = 0.05f;
        public float zoomSmoothing = 10f;
        [Header("Collision")]
        public LayerMask collisionLayer;
        public float cameraRadius = 0.3f;
        public float collisionOffset = 0.1f;
        [Header("Recoil Effect")]
        public float recoilRecoverySpeed = 15f;
        private float _recoilPitch = 0f;
        [Header("State")]
        public bool follow = true;
        public bool spectatorMode;

        private Transform target;
        private float _lookInputX;
        private float _lookInputY;
        private float _zoomInput;
        private float _currentYaw;
        private float _currentPitch = 15f;
        private float _targetZoom;
        private bool isCursorFree = false;

        private void Start()
        {
            SetCursorState(false);
            if (defaultPosition != null) { _targetZoom = Mathf.Abs(defaultPosition.localPosition.z); }
        }
        public void SetLookInput(Vector2 input)
        {
            if (isCursorFree) { _lookInputX = 0f; _lookInputY = 0f; return; }
            _lookInputX = input.x;
            _lookInputY = input.y;
        }
        public void SetZoomInput(float zoomValue)
        {
            if (isCursorFree) { _zoomInput = 0f; return; }
            _zoomInput = zoomValue;
        }
        public void SetFreeCursor(bool isFree) { SetCursorState(isFree); }
        private void SetCursorState(bool free)
        {
            isCursorFree = free;
            if (SettingsMenuController.IsOpen) return;
            if (free)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null) { _currentYaw = target.eulerAngles.y; }
        }
        private void LateUpdate()
        {
            if (target == null) return;
            if (follow) { HandleFollow(); HandleZoom(); HandleCameraCollision(); }
        }
        public void ApplyCameraRecoil(float kickForce) { _recoilPitch -= kickForce; }

        private void HandleFollow()
        {
            transform.position = Vector3.Lerp(
                transform.position,
                target.position,
                smoothing * Time.deltaTime);

            if (!isCursorFree && !SettingsMenuController.IsOpen)
            {
                float currentSens = SettingsMenuController.MouseSensitivity;
                float invertMultiplier = SettingsMenuController.InvertMouseY ? -1f : 1f;
                _currentYaw += _lookInputX * currentSens;
                _currentPitch -= _lookInputY * currentSens * invertMultiplier;
                _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);
            }
            _recoilPitch = Mathf.Lerp(_recoilPitch, 0f, Time.deltaTime * recoilRecoverySpeed);
            transform.rotation = Quaternion.Euler(_currentPitch + _recoilPitch, _currentYaw, 0);
        }

        private void HandleZoom()
        {
            if (SettingsMenuController.IsOpen) return;
            if (Mathf.Abs(_zoomInput) > 0.01f)
            {
                _targetZoom -= _zoomInput * zoomSpeed;
                _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
            }
            Vector3 currentLocalPos = defaultPosition.localPosition;
            float smoothedZ = Mathf.Lerp(currentLocalPos.z, -_targetZoom, zoomSmoothing * Time.deltaTime);
            defaultPosition.localPosition = new Vector3(currentLocalPos.x, currentLocalPos.y, smoothedZ);
        }

        private void HandleCameraCollision()
        {
            Vector3 start = target.position;
            Vector3 desiredPosition = defaultPosition.position;
            Vector3 direction = desiredPosition - start;
            float distance = direction.magnitude;
            if (distance < 0.01f) return;
            direction.Normalize();
            Vector3 finalPosition = desiredPosition;
            if (Physics.SphereCast(start, cameraRadius,
                direction, out RaycastHit hit, distance, collisionLayer, QueryTriggerInteraction.Ignore))
            {
                float safeDistance = Mathf.Max(0.5f, hit.distance - collisionOffset);
                finalPosition = start + direction * safeDistance;
            }
            if (Physics.Linecast(start, finalPosition,
                out RaycastHit lineHit, collisionLayer, QueryTriggerInteraction.Ignore))
            {
                finalPosition = lineHit.point + lineHit.normal * cameraRadius;
            }
            cameraObject.position = finalPosition;
            cameraObject.LookAt(target);
        }
    }
}