using UnityEngine;

namespace GameScripts.AIM
{
    public class SmartAim : MonoBehaviour
    {
        [Header("Aim Settings")]
        public float maxDistance = 100f;
        public float verticalAngleUp = 15f;
        public float verticalAngleDown = 15f;
        public int raysPerAngle = 10;
        public LayerMask targetLayer;
        public LayerMask obstacleLayer;

        [Header("Wall Protection")]
        public float muzzleClearanceRadius = 0.15f;

        public Vector3 GetAimDirection(Transform turretBase, Transform muzzle, out bool isBlocked)
        {
            isBlocked = CheckIfBlocked(turretBase, muzzle);
            if (isBlocked) return muzzle.forward;

            return ScanForTarget(muzzle);
        }

        private bool CheckIfBlocked(Transform turretBase, Transform muzzle)
        {
            if (muzzle == null) Debug.Log("muzzlePoint is null from SmartAim");
            return Physics.Linecast(turretBase.position, muzzle.position, obstacleLayer) ||
                   Physics.CheckSphere(muzzle.position, muzzleClearanceRadius, obstacleLayer);
        }
        private Vector3 ScanForTarget(Transform muzzle)
        {
            int firstHitIndex = -1;
            int lastHitIndex = -1;

            float totalAngle = verticalAngleUp + verticalAngleDown;
            float step = totalAngle / raysPerAngle;

            for (int i = 0; i < raysPerAngle; i++)
            {
                float lerpPct = (raysPerAngle > 1) ? (float)i / (raysPerAngle - 1) : 0.5f;
                float currentAngle = Mathf.Lerp(verticalAngleUp, -verticalAngleDown, lerpPct);
                Vector3 rayDir = Quaternion.AngleAxis(currentAngle, -muzzle.right) * muzzle.forward;

                // ѕровер€ем, не преграждает ли путь преп€тствие (стена) перед тем как искать танк
                if (Physics.Raycast(muzzle.position, rayDir, out RaycastHit hit, maxDistance, targetLayer | obstacleLayer))
                {
                    // ≈сли попали именно в слой цели
                    if (((1 << hit.collider.gameObject.layer) & targetLayer) != 0)
                    {
                        if (firstHitIndex == -1) firstHitIndex = i; // «апоминаем самый верхний луч
                        lastHitIndex = i; // ѕосто€нно обновл€ем, пока попадаем (в итоге будет самый нижний)
                    }
                }
            }

            if (firstHitIndex != -1)
            {

                // Ќаходим средний индекс между верхом и низом
                float middleIndex = (firstHitIndex + lastHitIndex) / 2f;
                float middlePct = (raysPerAngle > 1) ? middleIndex / (raysPerAngle - 1) : 0.5f;
                float middleAngle = Mathf.Lerp(verticalAngleUp, -verticalAngleDown, middlePct);

                Vector3 finalDir = Quaternion.AngleAxis(middleAngle, -muzzle.right) * muzzle.forward;

                return finalDir;
            }

            return muzzle.forward;
        }
    }
}