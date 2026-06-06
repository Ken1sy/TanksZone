using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using GameScripts.Camera;

namespace GameScripts.AIM
{
    [RequireComponent(typeof(SmartAim))]
    public abstract class WeaponController : MonoBehaviour
    {
        [Header("Weapon Base State")]
        public bool isLocalPlayer = false;
        public bool useAutoAim = true;
        [Header("References")]
        public LayerMask hitMask = ~0;
        protected Transform muzzlePoint;
        protected SmartAim smartAim;
        protected Rigidbody tankRigidbody;
        protected UnityEngine.Camera mainCamera;
        [HideInInspector]
        public PlayerTankBrain tankBrain;
        [Header("Base Shooting Stats")]
        public float damage = 50f;
        public float range = 1000f;
        public float impactForce = 7000f;
        public float recoilForce = 10500f;

        public virtual void Initialize(PlayerTankBrain brain)
        {
            tankBrain = brain;
            smartAim = GetComponent<SmartAim>();
            tankRigidbody = GetComponentInParent<Rigidbody>();
            if (tankBrain != null && tankBrain.camController != null)
            {
                mainCamera = tankBrain.camController.GetComponentInChildren<UnityEngine.Camera>();
            }
        }
        public void SetMuzzlePoint(Transform muzzle) { muzzlePoint = muzzle; }
        protected void ApplyRecoil()
        {
            if (tankRigidbody == null || muzzlePoint == null) return;
            Vector3 recoilDirection = -muzzlePoint.forward;
            Vector3 flatRecoil = new Vector3(recoilDirection.x, recoilDirection.y * 0.4f, recoilDirection.z).normalized;
            Vector3 pushPoint = Vector3.Lerp(tankRigidbody.worldCenterOfMass, muzzlePoint.position, 0.3f);
            tankRigidbody.AddForceAtPosition(flatRecoil * recoilForce, pushPoint, ForceMode.Impulse);
            if (mainCamera != null)
            {
                CameraController camCtrl = mainCamera.GetComponentInParent<CameraController>();
                if (camCtrl != null) camCtrl.ApplyCameraRecoil(1f);
            }
        }
        public abstract void ProcessInput(bool isShootingHeld);
        public abstract void PerformRemoteVisualShot(Vector3 aimDirection, bool isBlocked);
        public virtual void PerformServerPhysics(Vector3 aimDirection, bool isBlocked, NetworkObject hitNetObj, Vector3 hitPoint)
        {
            if (isBlocked || hitNetObj == null) return;
            PlayerTankBrain targetBrain = hitNetObj.GetComponent<PlayerTankBrain>();
            if (targetBrain != null)
            {
                targetBrain.TargetApplyImpact(targetBrain.Owner, aimDirection, impactForce, hitPoint);
                targetBrain.TakeDamage(damage, tankBrain);
            }
            else
            {
                Rigidbody targetRb = hitNetObj.GetComponent<Rigidbody>();
                if (targetRb != null && targetRb != tankRigidbody)
                {
                    targetRb.AddForceAtPosition(aimDirection * impactForce, hitPoint, ForceMode.Impulse);
                }
            }
        }

        protected virtual void LateUpdate()
        {
            if (!isLocalPlayer || muzzlePoint == null || mainCamera == null) return;
            Vector3 aimDirection = muzzlePoint.forward;
            bool isBlocked = false;
            if (smartAim != null)
            {
                Vector3 smartDir = smartAim.GetAimDirection(transform, muzzlePoint, out isBlocked);
                if (useAutoAim) aimDirection = smartDir;
            }
            Vector3 worldHitPoint = muzzlePoint.position + aimDirection * range;
            RaycastHit[] hits = Physics.RaycastAll(muzzlePoint.position, aimDirection, range, hitMask);
            float closestDist = float.MaxValue;
            foreach (var hit in hits)
            {
                if (hit.collider.transform.root != transform.root)
                {
                    if (hit.distance < closestDist)
                    {
                        closestDist = hit.distance;
                        worldHitPoint = hit.point;
                    }
                }
            }
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldHitPoint);
            bool isCursorFree = (Cursor.lockState == CursorLockMode.None);
            if (CrosshairUI.Instance != null)
            {
                CrosshairUI.Instance.UpdateCrosshair(screenPos, isBlocked, isCursorFree);
            }
        }
        public virtual float GetReloadProgress() { return 1f; }
    }
}