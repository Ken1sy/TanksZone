using FishNet.Object;
using UnityEngine;

namespace GameScripts.AIM
{
    public class StandardGun : WeaponController
    {
        [Header("Standard Gun Settings")]
        public float reloadTime = 1.5f;

        [Header("Visual Effects")]
        public GameObject hitEffectPrefab;
        public GameObject muzzleFlashPrefab;
        public GameObject decalPrefab;

        private float _nextFireTime = 0f;

        public override void ProcessInput(bool isShootingHeld)
        {
            // Смоки и Гром стреляют сразу, но имеют перезарядку
            if (isShootingHeld && Time.time >= _nextFireTime)
            {
                _nextFireTime = Time.time + reloadTime;
                FireLocally();
            }
        }

        private void FireLocally()
        {
            if (muzzlePoint == null) return;

            Vector3 aimDirection = smartAim.GetAimDirection(transform, muzzlePoint, out bool isBlocked).normalized;
            ExecuteHitscanShot(aimDirection, isBlocked, out NetworkObject hitNetObj, out Vector3 hitPoint);

            ApplyRecoil();

            // Отправляем выстрел на сервер через мозг
            tankBrain.CmdSubmitHitscanShoot(aimDirection, isBlocked, hitNetObj, hitPoint);
        }

        public override void PerformRemoteVisualShot(Vector3 aimDirection, bool isBlocked)
        {
            ExecuteHitscanShot(aimDirection, isBlocked, out _, out _);
        }

        private void ExecuteHitscanShot(Vector3 aimDirection, bool isBlocked, out NetworkObject hitNetObj, out Vector3 hitPoint)
        {
            hitNetObj = null; hitPoint = Vector3.zero;
            ShowMuzzleFlash();

            PhysicsScene roomPhysics = gameObject.scene.GetPhysicsScene();

            if (isBlocked)
            {
                Vector3 impactPos = muzzlePoint.position;
                Vector3 impactNormal = -muzzlePoint.forward;
                Transform targetTransform = null;

                Vector3 dir = muzzlePoint.position - transform.position;
                if (roomPhysics.Raycast(transform.position, dir.normalized, out RaycastHit blockHit, dir.magnitude, hitMask))
                {
                    impactPos = blockHit.point + blockHit.normal * 0.02f;
                    impactNormal = blockHit.normal;
                    targetTransform = blockHit.collider.transform;
                }
                SpawnHitVisuals(impactPos, impactNormal, targetTransform);
                return;
            }

            if (roomPhysics.Raycast(muzzlePoint.position, aimDirection, out RaycastHit hit, range, hitMask))
            {
                hitPoint = hit.point;
                hitNetObj = hit.collider.GetComponentInParent<NetworkObject>();
                SpawnHitVisuals(hit.point, hit.normal, hit.collider.transform);
            }
        }

        private void SpawnHitVisuals(Vector3 pos, Vector3 normal, Transform parent)
        {
            if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, pos, Quaternion.LookRotation(normal));

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

        public override float GetReloadProgress()
        {
            if (Time.time >= _nextFireTime) return 1f; // Полностью заряжено

            float remaining = _nextFireTime - Time.time;
            return Mathf.Clamp01(1f - (remaining / reloadTime)); // Считаем процент от 0 до 1
        }
    }
}