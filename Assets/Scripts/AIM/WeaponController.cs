using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

namespace GameScripts.AIM
{
    public class WeaponController : MonoBehaviour
    {
        [Header("References")]
        public Transform muzzlePoint;
        public GameObject projectilePrefab;

        [Header("Weapon Stats")]
        public float fireRate = 1.5f;
        public float recoilForce = 2000f;

        private Rigidbody tankRigidbody;
        private SmartAim smartAim;
        private Vector3 lastShotStart;
        private Vector3 lastShotEnd;
        private bool hasLastShot = false;
        private float nextFireTime = 0f;

        private void Start()
        {
            tankRigidbody = GetComponentInParent<Rigidbody>();
            smartAim = GetComponent<SmartAim>();
        }

        public void OnShoot(InputAction.CallbackContext context) { if (context.started) TryShoot(); }

        void TryShoot()
        {
            if (Time.time < nextFireTime) return;

            if (muzzlePoint == null) Debug.Log("muzzlePoint is null from WeaponController");
            Vector3 aimDirection = smartAim.GetAimDirection(transform, muzzlePoint, out bool isBlocked);

            // 3. Устанавливаем кулдаун
            nextFireTime = Time.time + (1f / fireRate);

            // 1. ФИЗИЧЕСКАЯ ОТДАЧА (Всегда срабатывает)
            if (tankRigidbody != null)
            {
                tankRigidbody.AddForceAtPosition(-muzzlePoint.forward * recoilForce, muzzlePoint.position, ForceMode.Impulse);
            }

            // 2. Проверяем блокировку ствола
            if (isBlocked)
            {
                Debug.Log("Выстрел заблокирован: ствол находится в стене!");
                return;
            }

            // 2. ФИКСАЦИЯ ДЛЯ GIZMOS
            lastShotStart = muzzlePoint.position;
            if (Physics.Raycast(muzzlePoint.position, aimDirection, out RaycastHit hit, 200f, smartAim.targetLayer | smartAim.obstacleLayer))
            {
                lastShotEnd = hit.point;
            }
            else
            {
                lastShotEnd = muzzlePoint.position + aimDirection * 100f;
            }
            hasLastShot = true;

            // 3. СОЗДАНИЕ СНАРЯДА
            // Если ствол заблокирован, мы можем создать эффект взрыва прямо в дуле
            if (isBlocked)
            {
                Debug.Log("Выстрел в упор или внутри объекта!");
                // Можно заспавнить эффект взрыва в muzzlePoint.position и не создавать снаряд
                // Или создать снаряд, который мгновенно столкнется
            }

            Instantiate(projectilePrefab, muzzlePoint.position, Quaternion.LookRotation(aimDirection));
        }
        private void OnDrawGizmos()
        {
            if (muzzlePoint == null || smartAim == null || !smartAim.showGizmos) return;

            if (hasLastShot)
            {
                // Рисуем линию последнего выстрела
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(lastShotStart, lastShotEnd);

                // Рисуем точку попадания
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(lastShotEnd, 0.15f);
            }
        }
    }
}