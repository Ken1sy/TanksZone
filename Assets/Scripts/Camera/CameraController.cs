using UnityEngine;
using UnityEngine.InputSystem;

namespace GameScripts.Camera
{
    public class CameraController : MonoBehaviour
    {
        [Header("Targets")]
        public Transform cameraObject;      // Сама камера (дочерний объект)
        public Transform defaultPosition;   // Точка, где камера должна быть в идеале

        [Header("Movement Settings")]
        public float smoothing = 10f;
        public float rotSmoothing = 5f;
        public float moveSpeed = 3f;

        [Header("Collision")]
        public LayerMask collisionLayer;
        public float cameraRadius = 0.3f;
        public float collisionOffset = 0.1f;

        [Header("State")]
        public bool follow = true;
        public bool spectatorMode;

        private Transform target;
        private float _heightInput;

        // Метод для New Input System (вызывается через PlayerInput или напрямую)
        public void OnCameraHeightAdjust(InputAction.CallbackContext context)
        {
            // Читаем значение оси (например, кнопки PageUp/PageDown или R/F)
            _heightInput = context.ReadValue<float>();
        }
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
        private void LateUpdate()
        {
            if (target == null) return;

            if (follow)
            {
                HandleFollow();
                HandleCameraCollision();
            }
        }

        private void HandleFollow()
        {
            transform.position = Vector3.Lerp(
                transform.position,
                target.position,
                smoothing * Time.deltaTime);

            float targetYAngle = target.eulerAngles.y;
            float currentYAngle = transform.eulerAngles.y;

            float nextYAngle = Mathf.LerpAngle(
                currentYAngle,
                targetYAngle,
                rotSmoothing * Time.deltaTime);

            transform.rotation = Quaternion.Euler(0, nextYAngle, 0);

            cameraObject.LookAt(target);
        }

        private void HandleCameraCollision()
        {
            float heightChange = _heightInput * moveSpeed * Time.deltaTime;

            Vector3 defPos = defaultPosition.localPosition;
            defPos.y += heightChange;
            defaultPosition.localPosition = defPos;

            Vector3 start = target.position;
            Vector3 desiredPosition = defaultPosition.position;

            Vector3 direction = desiredPosition - start;
            float distance = direction.magnitude;

            direction.Normalize();

            RaycastHit hit;

            if (Physics.SphereCast(
                    start,
                    cameraRadius,
                    direction,
                    out hit,
                    distance,
                    collisionLayer,
                    QueryTriggerInteraction.Ignore))
            {
                float hitDistance = hit.distance - collisionOffset;

                Vector3 safePosition = start + direction * hitDistance;

                cameraObject.position = safePosition;
            }
            else
            {
                cameraObject.position = desiredPosition;
            }
        }
    }
}