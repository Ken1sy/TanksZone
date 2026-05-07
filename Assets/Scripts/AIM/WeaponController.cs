using UnityEngine;

namespace GameScripts.AIM
{
    public class WeaponController : MonoBehaviour
    {
        [Header("State")]
        public bool isLocalPlayer = false;

        [Header("References")]
        public LayerMask hitMask = ~0;
        private Transform muzzlePoint;
        private SmartAim smartAim;
        private Rigidbody tankRigidbody;
        private UnityEngine.Camera mainCamera;

        [Header("Shooting Stats")]
        public float damage = 50f;
        public float range = 1000f;
        public float impactForce = 7000f;
        public float recoilForce = 3000f;

        [Header("Visual Effects")]
        public GameObject hitEffectPrefab;
        public GameObject muzzleFlashPrefab;
        public GameObject decalPrefab;

        private void Start()
        {
            smartAim = GetComponent<SmartAim>();
            tankRigidbody = GetComponentInParent<Rigidbody>();
            mainCamera = UnityEngine.Camera.main;
        }

        public void SetMuzzlePoint(Transform muzzle)
        {
            muzzlePoint = muzzle;
        }

        private void Update()
        {
            if (isLocalPlayer)
            {
                UpdateCrosshairPosition();
            }
        }

        private void UpdateCrosshairPosition()
        {
            if (muzzlePoint == null || CrosshairUI.Instance == null || mainCamera == null) return;

            bool isCursorFree = Cursor.visible || Cursor.lockState == CursorLockMode.None;
            if (isCursorFree)
            {
                CrosshairUI.Instance.UpdateCrosshair(Vector3.zero, false, true);
                return;
            }

            Vector3 aimDirection = smartAim.GetAimDirection(transform, muzzlePoint, out bool isBlocked).normalized;
            Vector3 targetPoint;

            if (Physics.Raycast(muzzlePoint.position, aimDirection, out RaycastHit hit, range, hitMask))
            {
                targetPoint = hit.point;
            }
            else
            {
                targetPoint = muzzlePoint.position + aimDirection * range;
            }

            Vector3 screenPos = mainCamera.WorldToScreenPoint(targetPoint);
            CrosshairUI.Instance.UpdateCrosshair(screenPos, isBlocked, false);
        }

        public void TryShoot()
        {
            if (muzzlePoint == null) return;

            Vector3 aimDirection = smartAim.GetAimDirection(transform, muzzlePoint, out bool isBlocked).normalized;

            ShowMuzzleFlash();

            if (isBlocked)
            {
                Vector3 impactPos = muzzlePoint.position;
                Vector3 impactNormal = -muzzlePoint.forward;
                Transform targetTransform = null;

                if (Physics.Linecast(transform.position, muzzlePoint.position, out RaycastHit blockHit, hitMask))
                {
                    impactPos = blockHit.point + blockHit.normal * 0.02f;
                    impactNormal = blockHit.normal;
                    targetTransform = blockHit.collider.transform;
                }

                if (hitEffectPrefab != null)
                {
                    Instantiate(hitEffectPrefab, impactPos, Quaternion.LookRotation(impactNormal));
                }

                if (decalPrefab != null && targetTransform != null)
                {
                    GameObject decal = Instantiate(decalPrefab, impactPos, Quaternion.LookRotation(-impactNormal));
                    decal.transform.SetParent(targetTransform);
                    decal.transform.Rotate(0, 0, Random.Range(0f, 360f), Space.Self);
                    Destroy(decal, 10f);
                }

                // Вызываем отдачу без передачи aimDirection
                ApplyRecoil();
                return;
            }

            PerformRaycastShot(aimDirection);

            // Вызываем отдачу без передачи aimDirection
            ApplyRecoil();
        }

        private void PerformRaycastShot(Vector3 direction)
        {
            Vector3 hitPosition;

            if (Physics.Raycast(muzzlePoint.position, direction, out RaycastHit hit, range, hitMask))
            {
                hitPosition = hit.point;

                if (hitEffectPrefab != null)
                {
                    Instantiate(hitEffectPrefab, hitPosition, Quaternion.LookRotation(hit.normal));
                }

                if (decalPrefab != null)
                {
                    Vector3 safePosition = hitPosition + hit.normal * 0.02f;
                    GameObject decal = Instantiate(decalPrefab, safePosition, Quaternion.LookRotation(-hit.normal));
                    decal.transform.SetParent(hit.collider.transform);
                    decal.transform.Rotate(0, 0, Random.Range(0f, 360f), Space.Self);
                    Destroy(decal, 10f);
                }

                Rigidbody targetRb = hit.collider.attachedRigidbody;

                if (targetRb != null && targetRb != tankRigidbody)
                {
                    targetRb.AddForceAtPosition(direction * impactForce, hitPosition, ForceMode.Impulse);
                }
            }
            else
            {
                hitPosition = muzzlePoint.position + direction * range;
            }
        }

        private void ShowMuzzleFlash()
        {
            if (muzzleFlashPrefab != null && muzzlePoint != null)
            {
                Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation, muzzlePoint);
            }
        }

        private void ApplyRecoil()
        {
            if (tankRigidbody == null || muzzlePoint == null) return;

            // 1. Направление отдачи ВСЕГДА строго назад относительно самого ствола
            Vector3 recoilDirection = -muzzlePoint.forward;

            // 2. Возвращаем AddForceAtPosition. 
            // Теперь сила снова бьет высоко в дуло, создавая рычаг и реалистичный крен,
            // но так как вектор правильный, танк больше не будет делать сальто.
            tankRigidbody.AddForceAtPosition(recoilDirection * recoilForce, muzzlePoint.position, ForceMode.Impulse);
        }
    }
}