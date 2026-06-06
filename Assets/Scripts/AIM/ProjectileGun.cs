using UnityEngine;

namespace GameScripts.AIM
{
    public class ProjectileGun : WeaponController
    {
        [Header("Projectile Settings")]
        public PlasmaProjectile projectilePrefab;
        public float fireRate = 0.2f;
        [Header("Muzzles")]
        public Transform[] alternateMuzzles;
        public GameObject muzzleFlashPrefab;

        private float _nextFireTime = 0f;
        private int _currentMuzzleIndex = 0;

        public override void Initialize(PlayerTankBrain brain)
        {
            base.Initialize(brain);
            if (muzzlePoint == null && alternateMuzzles != null && alternateMuzzles.Length > 0)
            {
                GameObject virtualMuzzle = new GameObject("VirtualMuzzle_Center");
                virtualMuzzle.transform.SetParent(transform);
                Vector3 avgLocalPos = Vector3.zero;
                int validCount = 0;
                foreach (var m in alternateMuzzles)
                {
                    if (m != null)
                    {
                        avgLocalPos += m.localPosition;
                        validCount++;
                    }
                }
                if (validCount > 0)
                {
                    avgLocalPos /= validCount;
                    virtualMuzzle.transform.localPosition = avgLocalPos;
                    virtualMuzzle.transform.localRotation = alternateMuzzles[0].localRotation;
                    SetMuzzlePoint(virtualMuzzle.transform);
                }
            }
        }

        public override void ProcessInput(bool isShootingHeld)
        {
            if (isShootingHeld && Time.time >= _nextFireTime)
            {
                _nextFireTime = Time.time + fireRate;
                FireLocally();
            }
        }

        private void FireLocally()
        {
            Transform activeMuzzle = (alternateMuzzles != null && alternateMuzzles.Length > 0)
                ? alternateMuzzles[_currentMuzzleIndex]
                : muzzlePoint;
            if (activeMuzzle == null) return;
            Vector3 aimDirection = smartAim.GetAimDirection(transform, activeMuzzle, out bool isBlocked).normalized;
            Vector3 spawnPos = activeMuzzle.position;
            if (isBlocked)
            {
                aimDirection = activeMuzzle.forward;
                PhysicsScene roomPhysics = gameObject.scene.GetPhysicsScene();
                Vector3 dirFromTurret = activeMuzzle.position - transform.position;
                if (roomPhysics.Raycast(
                    transform.position, dirFromTurret.normalized,
                    out RaycastHit blockHit, dirFromTurret.magnitude, hitMask))
                {
                    spawnPos = blockHit.point + blockHit.normal * 0.05f;
                }
            }
            SpawnProjectile(spawnPos, aimDirection, true);
            ApplyRecoil();
            tankBrain.CmdSubmitProjectileShoot(spawnPos, aimDirection, _currentMuzzleIndex);
            _currentMuzzleIndex = (_currentMuzzleIndex + 1) % Mathf.Max(1, alternateMuzzles?.Length ?? 1);
        }

        public void PerformRemoteProjectile(Vector3 spawnPos, Vector3 aimDirection, int muzzleIndex)
        {
            Transform activeMuzzle = (alternateMuzzles != null && alternateMuzzles.Length > muzzleIndex)
                ? alternateMuzzles[muzzleIndex]
                : muzzlePoint;
            SpawnProjectile(spawnPos, aimDirection, false);
            if (muzzleFlashPrefab != null && activeMuzzle != null)
            {
                Instantiate(muzzleFlashPrefab, activeMuzzle.position, activeMuzzle.rotation, activeMuzzle);
            }
        }

        private void SpawnProjectile(Vector3 pos, Vector3 dir, bool isLocalOwner)
        {
            if (projectilePrefab == null) return;
            PlasmaProjectile proj = Instantiate(projectilePrefab, pos, Quaternion.LookRotation(dir));
            proj.Initialize(this, dir, isLocalOwner);
            Transform activeMuzzle = (alternateMuzzles != null && alternateMuzzles.Length > _currentMuzzleIndex)
                ? alternateMuzzles[_currentMuzzleIndex]
                : muzzlePoint;
            if (isLocalOwner && muzzleFlashPrefab != null && activeMuzzle != null)
            {
                Instantiate(muzzleFlashPrefab, activeMuzzle.position, activeMuzzle.rotation, activeMuzzle);
            }
        }

        public override void PerformRemoteVisualShot(Vector3 aimDirection, bool isBlocked) { }

        public override float GetReloadProgress()
        {
            if (Time.time >= _nextFireTime) return 1f;
            float remaining = _nextFireTime - Time.time;
            return Mathf.Clamp01(1f - (remaining / fireRate));
        }
    }
}