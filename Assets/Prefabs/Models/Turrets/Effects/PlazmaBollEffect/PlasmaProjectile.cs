using System.Collections;
using FishNet.Object;
using UnityEngine;

namespace GameScripts.AIM
{
    public class PlasmaProjectile : MonoBehaviour
    {
        [Header("Projectile Stats")]
        public float speed = 50f;
        public float maxLifetime = 3f;

        [Header("Bounce Settings (Рикошет)")]
        [Tooltip("0 = Взрывается сразу (Твинс). >0 = Отскакивает (Рикошет)")]
        public int maxBounces = 0;

        [Header("Visuals & Physics")]
        public GameObject hitEffectPrefab; // Взрыв
        public GameObject bounceEffectPrefab; // Искры при отскоке
        public LayerMask collisionMask = ~0;

        [Header("Targeting")]
        [Tooltip("Слой, на котором находятся танки. Если снаряд попадет в него, он взорвется без отскоков.")]
        public LayerMask tankLayer;

        private ProjectileGun _gun;
        private Vector3 _direction;
        private bool _isLocalShooter;
        private int _currentBounces = 0;

        public void Initialize(ProjectileGun gun, Vector3 dir, bool isLocalOwner)
        {
            _gun = gun;
            _direction = dir.normalized;
            _isLocalShooter = isLocalOwner;

            // Запускаем таймер жизни снаряда
            StartCoroutine(LifetimeRoutine());
        }

        private IEnumerator LifetimeRoutine()
        {
            yield return new WaitForSeconds(maxLifetime);
            // Если время жизни вышло, взрываемся прямо в воздухе.
            // Передаем нормаль -_direction, чтобы эффект взрыва смотрел в сторону стрелявшего
            Explode(null, transform.position, -_direction);
        }

        private void Update()
        {
            float moveDistance = speed * Time.deltaTime;

            // Используем Raycast для движения
            if (Physics.Raycast(transform.position, _direction, out RaycastHit hit, moveDistance, collisionMask))
            {
                HandleCollision(hit);
            }
            else
            {
                transform.position += _direction * moveDistance;
            }
        }

        private void HandleCollision(RaycastHit hit)
        {
            // Проверяем, принадлежит ли объект, в который мы попали, слою танков
            bool isTankHit = ((1 << hit.collider.gameObject.layer) & tankLayer) != 0;

            // Если попали в танк ИЛИ закончились лимиты отскоков — взрываемся
            if (isTankHit || _currentBounces >= maxBounces)
            {
                Explode(hit.collider, hit.point, hit.normal);
            }
            else
            {
                // Логика РИКОШЕТА (отскок от стены)
                _direction = Vector3.Reflect(_direction, hit.normal);
                transform.position = hit.point + _direction * 0.05f; // Чуть-чуть отодвигаем, чтобы не застрял
                transform.rotation = Quaternion.LookRotation(_direction);

                _currentBounces++;

                if (bounceEffectPrefab != null)
                {
                    Instantiate(bounceEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                }
            }
        }

        private void Explode(Collider hitCollider, Vector3 point, Vector3 normal)
        {
            // Только владелец пушки сообщает серверу о попадании (защита от двойного урона)
            if (_isLocalShooter && _gun != null && _gun.tankBrain != null)
            {
                NetworkObject netObj = null;
                if (hitCollider != null)
                {
                    netObj = hitCollider.GetComponentInParent<NetworkObject>();
                }

                _gun.tankBrain.CmdSubmitProjectileHit(_direction, netObj, point);
            }

            // Спавним красивый эффект взрыва
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, point, Quaternion.LookRotation(normal));
            }

            // Уничтожаем сам плазменный шар
            Destroy(gameObject);
        }
    }
}