using FishNet.Object;
using UnityEngine;

namespace GameScripts.AIM
{
    public class ProjectileGun : WeaponController
    {
        [Header("Projectile Settings")]
        public PlasmaProjectile projectilePrefab;
        public float fireRate = 0.2f; // Скорострельность

        [Header("Muzzles (Твинс = 2, Рикошет = 1)")]
        [Tooltip("Точки спавна снарядов. Если их несколько, пушка будет стрелять из них по очереди.")]
        public Transform[] alternateMuzzles;
        public GameObject muzzleFlashPrefab;

        private float _nextFireTime = 0f;
        private int _currentMuzzleIndex = 0;

        public override void Initialize(PlayerTankBrain brain)
        {
            base.Initialize(brain);

            // ИСПРАВЛЕНИЕ ДЛЯ ТВИНСА (Отдача и Прицел):
            // Если TankAssembler не нашел объект "muzzle" (так как у Твинса их два), 
            // базовая переменная muzzlePoint будет null, и отдача/прицел сломаются.
            // Мы создаем "виртуальное дуло" ровно по центру между всеми стволами.
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
                    virtualMuzzle.transform.localRotation = alternateMuzzles[0].localRotation; // Направление берем от первого дула

                    SetMuzzlePoint(virtualMuzzle.transform);
                    Debug.Log("[ProjectileGun] Создано центральное дуло для корректной работы прицела и отдачи Твинса.");
                }
            }
        }

        public override void ProcessInput(bool isShootingHeld)
        {
            // Стреляет непрерывно, пока зажата кнопка мыши
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

            // ==========================================
            // ЗАЩИТА ОТ ПРОСТРЕЛА СКВОЗЬ СТЕНЫ
            // ==========================================
            if (isBlocked)
            {
                aimDirection = activeMuzzle.forward;
                PhysicsScene roomPhysics = gameObject.scene.GetPhysicsScene();
                Vector3 dirFromTurret = activeMuzzle.position - transform.position;

                // Пускаем луч от центра башни до дула. 
                // Он найдет точку, где ствол вошел в стену.
                if (roomPhysics.Raycast(transform.position, dirFromTurret.normalized, out RaycastHit blockHit, dirFromTurret.magnitude, hitMask))
                {
                    // Спавним снаряд не в дуле, а перед самой стеной, 
                    // слегка отодвинув его назад по нормали стены, чтобы он сразу врезался в неё
                    spawnPos = blockHit.point + blockHit.normal * 0.05f;
                }
            }

            // 1. Спавним визуальный снаряд у себя в правильной точке
            SpawnProjectile(spawnPos, aimDirection, true);

            ApplyRecoil();

            // 2. Отправляем на сервер выверенные координаты
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

        public override void PerformRemoteVisualShot(Vector3 aimDirection, bool isBlocked)
        {
            // Не используется для плазмы
        }

        public override float GetReloadProgress()
        {
            if (Time.time >= _nextFireTime) return 1f; // Полностью заряжено

            float remaining = _nextFireTime - Time.time;
            return Mathf.Clamp01(1f - (remaining / fireRate)); // Возвращаем % от 0 до 1
        }
    }
}