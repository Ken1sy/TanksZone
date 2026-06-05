using UnityEngine;

namespace GameScripts.GameMode
{
    public class SpawnPoint : MonoBehaviour
    {
        [Header("Настройки")]
        [Tooltip("Радиус проверки безопасности (нет ли в этой зоне другого танка)")]
        public float safetyRadius = 4f;

        [Tooltip("Слои, которые считаются препятствием для спавна (например, слой танков)")]
        public LayerMask obstacleMask;

        private void OnDrawGizmos()
        {
            // Рисуем зеленую сферу в редакторе Unity, чтобы было видно, где точка
            Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
            Gizmos.DrawSphere(transform.position, safetyRadius);

            // Рисуем стрелочку направления, куда будет смотреть танк при спавне
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.forward * 4f);
        }

        // Метод проверки: Свободно ли здесь место?
        public bool IsSafe()
        {
            // Проверяем, есть ли в радиусе safetyRadius объекты из слоев obstacleMask
            Collider[] colliders = Physics.OverlapSphere(transform.position, safetyRadius, obstacleMask);

            // Если коллайдеров нет (массив пустой), значит место безопасно
            return colliders.Length == 0;
        }
    }
}