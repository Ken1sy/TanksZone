using UnityEngine;

namespace GameScripts.AIM
{
    public class SmartAim : MonoBehaviour
    {
        [Header("Aim Settings")]
        public float maxDistance = 100f;
        public float verticalAngleUp = 15f;
        public float verticalAngleDown = 15f;
        [Tooltip("Плотность веера сканирования. 50 дает идеально гладкие рикошеты.")]
        public int raysPerAngle = 50;
        public LayerMask targetLayer;
        public LayerMask obstacleLayer;

        [Header("Ricochet Auto-Aim")]
        [Tooltip("Сколько отскоков просчитывать при поиске цели (0 для обычных пушек, >0 для Рикошета)")]
        public int maxBounces = 0;

        [Header("Wall Protection")]
        public float muzzleClearanceRadius = 0.15f;

        // Кэш лучей, чтобы не нагружать память (сборщик мусора) постоянными аллокациями
        private RaycastHit[] _hitsCache = new RaycastHit[30];

        public Vector3 GetAimDirection(Transform turretBase, Transform muzzle, out bool isBlocked)
        {
            isBlocked = CheckIfBlocked(turretBase, muzzle);
            if (isBlocked) return muzzle.forward;

            return ScanForTarget(muzzle);
        }

        // Вспомогательный метод, который пускает луч, но ИГНОРИРУЕТ наш собственный танк
        private bool GetClosestValidHit(PhysicsScene roomPhysics, Vector3 origin, Vector3 dir, float maxDist, int layerMask, out RaycastHit closestHit)
        {
            closestHit = default;
            int hitCount = roomPhysics.Raycast(origin, dir, _hitsCache, maxDist, layerMask);
            float minDist = float.MaxValue;
            bool found = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _hitsCache[i];

                // Самое важное: Игнорируем попадания по себе!
                if (hit.collider.transform.root == transform.root) continue;

                if (hit.distance < minDist)
                {
                    minDist = hit.distance;
                    closestHit = hit;
                    found = true;
                }
            }
            return found;
        }

        private bool CheckIfBlocked(Transform turretBase, Transform muzzle)
        {
            if (muzzle == null) return false;

            PhysicsScene roomPhysics = gameObject.scene.GetPhysicsScene();
            Vector3 dir = muzzle.position - turretBase.position;

            // Проверка от основания башни до дула
            int lineHits = roomPhysics.Raycast(turretBase.position, dir.normalized, _hitsCache, dir.magnitude, obstacleLayer);
            for (int i = 0; i < lineHits; i++)
            {
                if (_hitsCache[i].collider.transform.root != transform.root) return true;
            }

            // Сферическая проверка перед дулом (Оставляем muzzle.forward как ты и просил)
            int sphereHits = roomPhysics.SphereCast(muzzle.position, muzzleClearanceRadius, muzzle.forward, _hitsCache, 0.01f, obstacleLayer);
            for (int i = 0; i < sphereHits; i++)
            {
                if (_hitsCache[i].collider.transform.root != transform.root) return true;
            }

            return false;
        }

        private Vector3 ScanForTarget(Transform muzzle)
        {
            PhysicsScene roomPhysics = gameObject.scene.GetPhysicsScene();

            // Глобальная горизонтальная ось для стабильного сканирования
            Vector3 flatForward = new Vector3(muzzle.forward.x, 0, muzzle.forward.z);
            Vector3 globalRight = flatForward.sqrMagnitude > 0.001f
                ? Vector3.Cross(Vector3.up, flatForward.normalized)
                : muzzle.right;

            int firstDirect = -1;
            int lastDirect = -1;
            Collider bestDirectTarget = null;

            int bestRicochetIndex = -1;
            float minRicochetDist = float.MaxValue;

            // ==========================================
            // ФАЗА 1: СКАНИРОВАНИЕ ВЕЕРОМ
            // ==========================================
            for (int i = 0; i < raysPerAngle; i++)
            {
                float lerpPct = (raysPerAngle > 1) ? (float)i / (raysPerAngle - 1) : 0.5f;
                float currentAngle = Mathf.Lerp(verticalAngleUp, -verticalAngleDown, lerpPct);

                Vector3 initialRayDir = Quaternion.AngleAxis(currentAngle, -globalRight) * muzzle.forward;

                if (SimulateTrajectory(roomPhysics, muzzle.position, initialRayDir, out RaycastHit finalHit, out int bounceCount))
                {
                    if (bounceCount == 0)
                    {
                        // Прямое попадание (без отскоков)
                        if (firstDirect == -1) firstDirect = i;
                        lastDirect = i;
                        bestDirectTarget = finalHit.collider;
                    }
                    else
                    {
                        // Рикошет: Ищем луч, который попадает ближе всего к ЦЕНТРУ ПО ВЕРТИКАЛИ (ось Y)
                        float verticalDist = Mathf.Abs(finalHit.point.y - finalHit.collider.bounds.center.y);
                        if (verticalDist < minRicochetDist)
                        {
                            minRicochetDist = verticalDist;
                            bestRicochetIndex = i;
                        }
                    }
                }
            }

            // ==========================================
            // ФАЗА 2: НАВЕДЕНИЕ (ПРЯМАЯ ВИДИМОСТЬ)
            // ==========================================
            if (bestDirectTarget != null)
            {
                Vector3 targetCenter = bestDirectTarget.bounds.center;

                // Получаем строго горизонтальное направление дула
                Vector3 flatMuzzleForward = new Vector3(muzzle.forward.x, 0, muzzle.forward.z);
                if (flatMuzzleForward.sqrMagnitude > 0.001f) flatMuzzleForward.Normalize();
                else flatMuzzleForward = muzzle.forward;

                // Считаем горизонтальную дистанцию от дула до центра танка
                Vector2 muzzleXZ = new Vector2(muzzle.position.x, muzzle.position.z);
                Vector2 targetXZ = new Vector2(targetCenter.x, targetCenter.z);
                float flatDistance = Vector2.Distance(muzzleXZ, targetXZ);

                // ИДЕАЛЬНАЯ ТОЧКА: Оставляем горизонтальное направление как хочет игрок, но высоту ставим в центр танка
                Vector3 idealTargetPoint = new Vector3(muzzle.position.x, 0, muzzle.position.z) + flatMuzzleForward * flatDistance;
                idealTargetPoint.y = targetCenter.y; // Выравниваем только по вертикали!

                Vector3 dirToVerticalCenter = (idealTargetPoint - muzzle.position).normalized;

                // Проверяем, находится ли эта точка в пределах углов наклона пушки
                Vector3 localDir = muzzle.InverseTransformDirection(dirToVerticalCenter);
                float pitchAngle = Mathf.Asin(Mathf.Clamp(localDir.y, -1f, 1f)) * Mathf.Rad2Deg;

                if (pitchAngle >= -verticalAngleDown - 1f && pitchAngle <= verticalAngleUp + 1f)
                {
                    // Проверяем, свободен ли путь до этой идеальной вертикальной точки
                    if (GetClosestValidHit(roomPhysics, muzzle.position, dirToVerticalCenter, maxDistance, targetLayer | obstacleLayer, out RaycastHit centerHit))
                    {
                        if (centerHit.collider == bestDirectTarget)
                        {
                            return dirToVerticalCenter; // Успех! Плавно целимся в вертикальный центр
                        }
                    }
                }

                // Запасной план: Центр по вертикали закрыт стеной, усредняем видимую область
                float middleIndex = (firstDirect + lastDirect) / 2f;
                float middlePct = (raysPerAngle > 1) ? middleIndex / (raysPerAngle - 1) : 0.5f;
                float middleAngle = Mathf.Lerp(verticalAngleUp, -verticalAngleDown, middlePct);
                return Quaternion.AngleAxis(middleAngle, -globalRight) * muzzle.forward;
            }

            // ==========================================
            // ФАЗА 3: НАВЕДЕНИЕ РИКОШЕТОМ
            // ==========================================
            if (bestRicochetIndex != -1)
            {
                float pct = (raysPerAngle > 1) ? (float)bestRicochetIndex / (raysPerAngle - 1) : 0.5f;
                float ricochetAngle = Mathf.Lerp(verticalAngleUp, -verticalAngleDown, pct);
                return Quaternion.AngleAxis(ricochetAngle, -globalRight) * muzzle.forward;
            }

            // Цель не найдена, стреляем прямо по прицелу
            return muzzle.forward;
        }

        private bool SimulateTrajectory(PhysicsScene roomPhysics, Vector3 startPos, Vector3 startDir, out RaycastHit finalHit, out int bounceCount)
        {
            finalHit = default;
            bounceCount = 0;
            Vector3 currentPos = startPos;
            Vector3 currentDir = startDir;
            float remainingDistance = maxDistance;

            for (int bounce = 0; bounce <= maxBounces; bounce++)
            {
                if (GetClosestValidHit(roomPhysics, currentPos, currentDir, remainingDistance, targetLayer | obstacleLayer, out RaycastHit hit))
                {
                    if (((1 << hit.collider.gameObject.layer) & targetLayer) != 0)
                    {
                        finalHit = hit;
                        bounceCount = bounce;
                        return true;
                    }
                    else
                    {
                        if (bounce < maxBounces)
                        {
                            currentPos = hit.point + hit.normal * 0.02f;
                            currentDir = Vector3.Reflect(currentDir, hit.normal);
                            remainingDistance -= hit.distance;

                            if (remainingDistance <= 0) return false;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }

            return false;
        }
    }
}