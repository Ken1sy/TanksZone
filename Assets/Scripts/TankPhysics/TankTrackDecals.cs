using UnityEngine;

public class TankTrackDecals : MonoBehaviour
{
    [Header("Точки спавна")]
    public Transform leftTrackPoint;
    public Transform rightTrackPoint;

    [Header("Настройки декали")]
    public GameObject trackDecalPrefab;
    public float spawnDistance = 0.5f; // Как часто оставлять след (в метрах)
    public float raycastDistance = 2f; // Длина луча до земли
    public LayerMask groundLayer;      // Слой земли (чтобы следы не рисовались на других танках)

    [Header("Оптимизация")]
    public int maxDecals = 500; // Лимит следов на один танк (чтобы не лагало)

    private Vector3 lastSpawnPosition;
    private GameObject[] decalPool;
    private int poolIndex = 0;
    private GameObject poolContainer;

    void Start()
    {
        lastSpawnPosition = transform.position;

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
        // Проверяем, проехал ли танк нужное расстояние
        if (Vector3.Distance(transform.position, lastSpawnPosition) >= spawnDistance)
        {
            SpawnDecal(leftTrackPoint);
            SpawnDecal(rightTrackPoint);

            lastSpawnPosition = transform.position;
        }
    }

    private void SpawnDecal(Transform point)
    {
        if (point == null) return;

        // Пускаем луч строго вниз от точки гусеницы
        if (Physics.Raycast(point.position, -point.up, out RaycastHit hit, raycastDistance, groundLayer))
        {
            // Достаем декаль из пула
            GameObject decal = decalPool[poolIndex];
            decal.SetActive(true);

            // Ставим декаль в точку попадания луча
            decal.transform.position = hit.point + hit.normal * 0.02f;

            // ВАЖНО: Правильно поворачиваем декаль.
            // URP Decal Projector светит по оси Z. Значит, Z должен смотреть в землю (-hit.normal).
            // А верх (Y) декали должен смотреть туда, куда едет танк (transform.forward).
            decal.transform.rotation = Quaternion.LookRotation(-hit.normal, transform.forward);

            // Сдвигаем индекс пула. Если дошли до конца - начинаем с нуля (заменяя самые старые следы)
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