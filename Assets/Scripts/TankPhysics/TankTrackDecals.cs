using UnityEngine;

public class TankTrackDecals : MonoBehaviour
{
    [Header("Точки спавна")]
    public Transform leftTrackPoint;
    public Transform rightTrackPoint;

    [Header("Настройки декали")]
    public GameObject trackDecalPrefab;
    public float spawnDistance = 0.22f;
    public float raycastDistance = 0.5f;
    public LayerMask groundLayer;

    [Header("Оптимизация")]
    public int maxDecals = 500;

    private Vector3 lastSpawnPosition;
    private Vector3 lastLeftTrackPos;
    private Vector3 lastRightTrackPos;

    private GameObject[] decalPool;
    private int poolIndex = 0;
    private GameObject poolContainer;

    void Start()
    {
        lastSpawnPosition = transform.position;
        lastLeftTrackPos = leftTrackPoint.position;
        lastRightTrackPos = rightTrackPoint.position;

        poolContainer = new GameObject($"TrackDecalsPool_{gameObject.name}");

        // Создаем пул объектов заранее
        decalPool = new GameObject[maxDecals];
        for (int i = 0; i < maxDecals; i++)
        {
            decalPool[i] = Instantiate(trackDecalPrefab);
            decalPool[i].transform.SetParent(poolContainer.transform);
            decalPool[i].SetActive(false);
        }
    }

    void Update()
    {
        if (leftTrackPoint == null || rightTrackPoint == null) return;

        float dist = Vector3.Distance(transform.position, lastSpawnPosition);

        if (dist >= spawnDistance)
        {
            // Защита от телепортации (респавн или сильная сетевая коррекция)
            if (dist > 5f)
            {
                lastSpawnPosition = transform.position;
                lastLeftTrackPos = leftTrackPoint.position;
                lastRightTrackPos = rightTrackPoint.position;
                return;
            }

            // ========================================================
            // НОВОЕ: Находим, сколько следов пропущено за этот кадр
            // ========================================================
            int steps = Mathf.FloorToInt(dist / spawnDistance);

            for (int i = 1; i <= steps; i++)
            {
                // Вычисляем точный процент пути для идеального шага
                float lerpFactor = (spawnDistance * i) / dist;

                Vector3 lerpLeft = Vector3.Lerp(lastLeftTrackPos, leftTrackPoint.position, lerpFactor);
                Vector3 lerpRight = Vector3.Lerp(lastRightTrackPos, rightTrackPoint.position, lerpFactor);

                SpawnDecal(lerpLeft, -leftTrackPoint.up, transform.forward);
                SpawnDecal(lerpRight, -rightTrackPoint.up, transform.forward);
            }

            // Запоминаем остаток пути (чтобы следы рисовались без сбоев ритма)
            float totalSpawnedDist = spawnDistance * steps;
            float finalLerp = totalSpawnedDist / dist;

            lastSpawnPosition = Vector3.Lerp(lastSpawnPosition, transform.position, finalLerp);
            lastLeftTrackPos = Vector3.Lerp(lastLeftTrackPos, leftTrackPoint.position, finalLerp);
            lastRightTrackPos = Vector3.Lerp(lastRightTrackPos, rightTrackPoint.position, finalLerp);
        }
    }

    private void SpawnDecal(Vector3 rayOrigin, Vector3 rayDir, Vector3 forwardDir)
    {
        if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, raycastDistance, groundLayer))
        {
            GameObject decal = decalPool[poolIndex];
            decal.SetActive(true);
            decal.transform.position = hit.point + hit.normal * 0.02f;
            decal.transform.rotation = Quaternion.LookRotation(-hit.normal, forwardDir);

            poolIndex++;
            if (poolIndex >= maxDecals)
            {
                poolIndex = 0;
            }
        }
    }

    private void OnDestroy()
    {
        if (poolContainer != null)
        {
            Destroy(poolContainer);
        }
    }
}