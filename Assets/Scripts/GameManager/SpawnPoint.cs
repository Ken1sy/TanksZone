using UnityEngine;

namespace GameScripts.GameMode
{
    public class SpawnPoint : MonoBehaviour
    {
        [Header("Настройки")]
        public float safetyRadius = 4f;
        public LayerMask obstacleMask;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
            Gizmos.DrawSphere(transform.position, safetyRadius);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.forward * 4f);
        }
        public bool IsSafe()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, safetyRadius, obstacleMask);
            return colliders.Length == 0;
        }
    }
}