using System.Collections.Generic;
using UnityEngine;

namespace GameScripts.GameManager
{
    public class TankSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class TankComponent
        {
            public string id;
            public GameObject prefab;
        }

        [Header("Base Settings")]
        public GameObject baseTankPrefab;
        public Transform spawnPoint;

        [Header("Database")]
        public List<TankComponent> availableHulls;
        public List<TankComponent> availableTurrets;

        void Start()
        {
            SpawnPlayerTank();
        }

        public void SpawnPlayerTank()
        {
            // 1. Находим префабы по ID из лобби
            GameObject hullPrefab = GetPrefabById(availableHulls, TankSetupData.SelectedHullID);
            GameObject turretPrefab = GetPrefabById(availableTurrets, TankSetupData.SelectedTurretID);

            if (hullPrefab == null || turretPrefab == null)
            {
                Debug.LogError("Ошибка: Корпус или Башня не найдены в базе Спавнера!");
                return;
            }

            // 2. Создаем пустую базу танка
            GameObject tankBase = Instantiate(baseTankPrefab, spawnPoint.position, spawnPoint.rotation);
            tankBase.name = "Player_Tank_Assembled";

            // 3. Даем команду танку собрать себя!
            TankAssembler assembler = tankBase.GetComponent<TankAssembler>();
            if (assembler != null)
            {
                assembler.Assemble(hullPrefab, turretPrefab, TankSetupData.SelectedHullID);
            }
        }

        private GameObject GetPrefabById(List<TankComponent> list, string id)
        {
            foreach (var component in list)
            {
                if (component.id == id) return component.prefab;
            }
            return null;
        }
    }
}