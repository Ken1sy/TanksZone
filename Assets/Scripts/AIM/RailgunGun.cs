using FishNet.Object;
using System.Collections;
using UnityEngine;

namespace GameScripts.AIM
{
    public class RailgunGun : WeaponController
    {
        [Header("Railgun Settings")]
        public float chargeTime = 1.1f;
        public float reloadTime = 2.0f;
        [Header("Railgun Visuals")]
        public RailgunHybridBeam beamPrefab;
        public RailgunChargeEffect chargeController;
        public GameObject impactEffectPrefab;
        public GameObject decalPrefab;

        private float _nextFireTime = 0f;
        private bool _isCharging = false;

        public override void ProcessInput(bool isShootingHeld)
        {
            if (isShootingHeld && !_isCharging && Time.time >= _nextFireTime) { StartCoroutine(FireSequence()); }
        }

        private IEnumerator FireSequence()
        {
            _isCharging = true;
            if (chargeController != null) chargeController.StartCharge(chargeTime);
            tankBrain.CmdSubmitChargeStart();
            yield return new WaitForSeconds(chargeTime);
            if (chargeController != null) chargeController.FinishCharge();
            FireHitscan();
            _isCharging = false;
            _nextFireTime = Time.time + reloadTime;
        }

        private void FireHitscan()
        {
            if (muzzlePoint == null) return;
            Vector3 aimDirection = smartAim.GetAimDirection(transform, muzzlePoint, out bool isBlocked).normalized;
            ExecuteHitscanShot(aimDirection, isBlocked, out NetworkObject hitNetObj, out Vector3 hitPoint);
            ApplyRecoil();
            tankBrain.CmdSubmitHitscanShoot(aimDirection, isBlocked, hitNetObj, hitPoint);
        }

        public void PerformRemoteCharge()
        {
            if (chargeController != null)
            {
                chargeController.StartCharge(chargeTime);
                StartCoroutine(StopRemoteCharge());
            }
        }

        private IEnumerator StopRemoteCharge()
        {
            yield return new WaitForSeconds(chargeTime);
            if (chargeController != null) chargeController.FinishCharge();
        }

        public override void PerformRemoteVisualShot(Vector3 aimDirection, bool isBlocked)
        {
            ExecuteHitscanShot(aimDirection, isBlocked, out _, out _);
        }

        private void ExecuteHitscanShot(Vector3 aimDirection, bool isBlocked, out NetworkObject hitNetObj, out Vector3 hitPoint)
        {
            hitNetObj = null; hitPoint = Vector3.zero;
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
                DrawBeam(muzzlePoint.position, impactPos);
                return;
            }

            if (roomPhysics.Raycast(muzzlePoint.position, aimDirection, out RaycastHit hit, range, hitMask))
            {
                hitPoint = hit.point;
                hitNetObj = hit.collider.GetComponentInParent<NetworkObject>();
                SpawnHitVisuals(hit.point, hit.normal, hit.collider.transform);
                DrawBeam(muzzlePoint.position, hitPoint);
            }
            else
            {
                DrawBeam(muzzlePoint.position, muzzlePoint.position + aimDirection * range);
            }
        }

        private void DrawBeam(Vector3 start, Vector3 end)
        {
            if (beamPrefab != null)
            {
                RailgunHybridBeam beam = Instantiate(beamPrefab);
                beam.FireBeam(start, end);
            }
        }

        private void SpawnHitVisuals(Vector3 pos, Vector3 normal, Transform parent)
        {
            if (impactEffectPrefab != null) Instantiate(impactEffectPrefab, pos, Quaternion.LookRotation(normal));
            if (decalPrefab != null && parent != null)
            {
                Vector3 safePosition = pos + normal * 0.02f;
                GameObject decal = Instantiate(decalPrefab, safePosition, Quaternion.LookRotation(-normal));
                decal.transform.SetParent(parent);
                decal.transform.Rotate(0, 0, Random.Range(0f, 360f), Space.Self);
                Destroy(decal, 10f);
            }
        }

        public override float GetReloadProgress()
        {
            if (_isCharging) return 0f;
            if (Time.time >= _nextFireTime) return 1f;
            float remaining = _nextFireTime - Time.time;
            return Mathf.Clamp01(1f - (remaining / reloadTime));
        }
    }
}