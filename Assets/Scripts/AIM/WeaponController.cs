using FishNet.Object;
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

        public void SetMuzzlePoint(Transform muzzle) { muzzlePoint = muzzle; }

        private void Update() { if (isLocalPlayer) UpdateCrosshairPosition(); }

        private void UpdateCrosshairPosition()
        {
            if (muzzlePoint == null || CrosshairUI.Instance == null || mainCamera == null) return;
            bool isCursorFree = Cursor.visible || Cursor.lockState == CursorLockMode.None;
            if (isCursorFree) { CrosshairUI.Instance.UpdateCrosshair(Vector3.zero, false, true); return; }
            Vector3 aimDirection = smartAim.GetAimDirection(transform, muzzlePoint, out bool isBlocked).normalized;
            Vector3 targetPoint;
            if (Physics.Raycast(muzzlePoint.position, aimDirection, out RaycastHit hit, range, hitMask)) targetPoint = hit.point;
            else targetPoint = muzzlePoint.position + aimDirection * range;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(targetPoint);
            CrosshairUI.Instance.UpdateCrosshair(screenPos, isBlocked, false);
        }

        public void TryShootLocal(out Vector3 aimDirection, out bool isBlocked, out NetworkObject hitNetObj, out Vector3 hitPoint)
        {
            aimDirection = Vector3.forward;
            isBlocked = false;
            hitNetObj = null;
            hitPoint = Vector3.zero;
            if (muzzlePoint == null) return;
            aimDirection = smartAim.GetAimDirection(transform, muzzlePoint, out isBlocked).normalized;
            ExecuteVisualShot(aimDirection, isBlocked, out hitNetObj, out hitPoint);
            if (mainCamera != null)
            {
                GameScripts.Camera.CameraController camCtrl = mainCamera.GetComponentInParent<GameScripts.Camera.CameraController>();
                if (camCtrl != null) camCtrl.ApplyCameraRecoil(1f);
            }
        }

        public void PerformRemoteShoot(Vector3 aimDirection, bool isBlocked)
        {
            NetworkObject dummyObj; Vector3 dummyPos;
            ExecuteVisualShot(aimDirection, isBlocked, out dummyObj, out dummyPos);
        }

        private void ExecuteVisualShot(Vector3 aimDirection, bool isBlocked, out NetworkObject hitNetObj, out Vector3 hitPoint)
        {
            hitNetObj = null; hitPoint = Vector3.zero;
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
                SpawnHitVisuals(impactPos, impactNormal, targetTransform);
                return;
            }
            if (Physics.Raycast(muzzlePoint.position, aimDirection, out RaycastHit hit, range, hitMask))
            {
                hitPoint = hit.point;
                hitNetObj = hit.collider.GetComponentInParent<NetworkObject>();
                SpawnHitVisuals(hit.point, hit.normal, hit.collider.transform);
            }
        }

        public void PerformServerPhysics(Vector3 aimDirection, bool isBlocked, NetworkObject hitNetObj, Vector3 hitPoint)
        {
            ApplyRecoil();

            if (isBlocked) return;
            if (hitNetObj != null)
            {
                Rigidbody targetRb = hitNetObj.GetComponent<Rigidbody>();
                if (targetRb != null && targetRb != tankRigidbody)
                {
                    targetRb.AddForceAtPosition(aimDirection * impactForce, hitPoint, ForceMode.Impulse);
                }
            }
        }

        private void SpawnHitVisuals(Vector3 pos, Vector3 normal, Transform parent)
        {
            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, pos, Quaternion.LookRotation(normal));

            if (decalPrefab != null && parent != null)
            {
                Vector3 safePosition = pos + normal * 0.02f;
                GameObject decal = Instantiate(decalPrefab, safePosition, Quaternion.LookRotation(-normal));
                decal.transform.SetParent(parent);
                decal.transform.Rotate(0, 0, Random.Range(0f, 360f), Space.Self);
                Destroy(decal, 10f);
            }
        }

        private void ShowMuzzleFlash()
        {
            if (muzzleFlashPrefab != null && muzzlePoint != null)
                Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation, muzzlePoint);
        }

        private void ApplyRecoil()
        {
            if (tankRigidbody == null || muzzlePoint == null) return;
            Vector3 recoilDirection = -muzzlePoint.forward;
            tankRigidbody.AddForceAtPosition(recoilDirection * recoilForce, muzzlePoint.position, ForceMode.Impulse);
        }
    }
}