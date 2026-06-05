using System.Collections.Generic;
using UnityEngine;

namespace GameScripts.GameMode
{
    public class SpawnManager : MonoBehaviour
    {
        // Делаем менеджер Singleton (одиночкой), чтобы к нему было легко обращаться отовсюду
        public static SpawnManager Instance;

        private List<SpawnPoint> _allSpawnPoints = new List<SpawnPoint>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Находим все точки спавна на сцене при загрузке карты
            SpawnPoint[] points = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            _allSpawnPoints.AddRange(points);

            if (_allSpawnPoints.Count == 0)
            {
                Debug.LogWarning("[SpawnManager] На сцене нет ни одной точки SpawnPoint!");
            }
        }

        public Transform GetSafeSpawnPoint()
        {
            if (_allSpawnPoints.Count == 0)
            {
                // Если точек нет вообще, возвращаем позицию самого менеджера как резерв
                return transform;
            }

            // Создаем список только из безопасных (свободных) точек
            List<SpawnPoint> safePoints = new List<SpawnPoint>();
            foreach (var point in _allSpawnPoints)
            {
                if (point.IsSafe())
                {
                    safePoints.Add(point);
                }
            }

            if (safePoints.Count > 0)
            {
                // Выбираем случайную точку ИЗ БЕЗОПАСНЫХ
                int randomIndex = Random.Range(0, safePoints.Count);
                return safePoints[randomIndex].transform;
            }
            else
            {
                // Если все точки заняты (танки стоят везде), выбираем просто любую случайную
                Debug.LogWarning("[SpawnManager] Все точки заняты! Спавним в случайную.");
                int randomIndex = Random.Range(0, _allSpawnPoints.Count);
                return _allSpawnPoints[randomIndex].transform;
            }
        }
    }
}