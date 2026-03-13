using UnityEngine;

[System.Serializable]
public class SuspensionRay
{
    public Vector3 localOrigin;
    public bool hasCollision;
    public RaycastHit hit;

    private float lastCompression;

    // Добавляем лимит угла наклона (чтобы не ездить по стенам)
    private const float MAX_SLOPE_ANGLE = 65f;

    public void UpdatePhysics(Rigidbody rb, Vector3 direction, float maxLen, float nominalLen, float springStiffness, float damping, LayerMask layerMask)
    {
        Vector3 worldOrigin = rb.transform.TransformPoint(localOrigin);
        Vector3 worldDir = rb.transform.TransformDirection(direction);

        // 1. Стреляем лучом
        if (Physics.Raycast(worldOrigin, worldDir, out hit, maxLen, layerMask, QueryTriggerInteraction.Ignore))
        {
            // Дополнительная проверка: если луч попал в коллайдер самого танка (на всякий случай)
            if (hit.collider.transform.root == rb.transform.root)
            {
                hasCollision = false;
                return;
            }

            // 2. ПРОВЕРКА УГЛА: Если это стена (> 50 градусов), игнорируем её
            float groundAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (groundAngle > MAX_SLOPE_ANGLE)
            {
                hasCollision = false;
                lastCompression = 0;
                return;
            }

            hasCollision = true;

            // Расчет сжатия (0 = нет сжатия, maxLen = полное сжатие)
            float compression = maxLen - hit.distance;

            // Скорость изменения сжатия для демпфера
            float compressionVelocity = (compression - lastCompression) / Time.fixedDeltaTime;
            lastCompression = compression;

            // Закон Гука: Сила = (Сжатие * Жесткость) + (СкоростьСжатия * Демпфирование)
            float springForce = (compression * springStiffness) + (compressionVelocity * damping);

            // Важно: ограничиваем силу снизу нулем (пружина только толкает)
            springForce = Mathf.Max(0, springForce);

            // Прикладываем силу строго вверх относительно луча
            rb.AddForceAtPosition(-worldDir * springForce, hit.point);
        }
        else
        {
            hasCollision = false;
            lastCompression = 0;
        }
    }
}