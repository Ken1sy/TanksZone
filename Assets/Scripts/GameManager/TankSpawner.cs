using System.Collections.Generic;
using UnityEngine;
using System.IO;

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
            AssembleAndSpawnTank();
        }

        public void AssembleAndSpawnTank()
        {
            GameObject tankBase = Instantiate(baseTankPrefab, spawnPoint.position, spawnPoint.rotation);
            tankBase.name = "Player_Tank_Assembled";

            GameObject hullPrefab = GetPrefabById(availableHulls, TankSetupData.SelectedHullID);
            GameObject turretPrefab = GetPrefabById(availableTurrets, TankSetupData.SelectedTurretID);

            if (hullPrefab == null || turretPrefab == null)
            {
                Debug.LogError("Ошибка сборки: Корпус или Пушка не найдены!");
                return;
            }

            #region HullInstance
            Transform hull = tankBase.transform.Find("Hull");
            if (hull == null)
            {
                Debug.LogError($"На {tankBase.name} нет объекта 'Hull'!");
                return;
            }

            GameObject hullInstance = Instantiate(hullPrefab, hull);
            hullInstance.transform.localPosition = new(0f, -0.3f, 0f);
            hullInstance.transform.localRotation = Quaternion.identity;

            //LinkMeshColliders
            Transform colliderModelTransform = hullInstance.transform.Find("collider");
            Mesh collisionMesh = null;

            if (colliderModelTransform != null)
            {
                // Берем меш из MeshFilter этого объекта
                MeshFilter mf = colliderModelTransform.GetComponent<MeshFilter>();
                if (mf != null)
                {
                    collisionMesh = mf.sharedMesh;
                }
            }
            MeshCollider hullMC = hull.GetComponent<MeshCollider>();
            if (hullMC != null) hullMC.sharedMesh = collisionMesh;

            Transform tankFrictionCollider = hull.Find("TankFrictionCollider");
            MeshCollider tankFrictionColliderMC = tankFrictionCollider.GetComponent<MeshCollider>();
            if (tankFrictionColliderMC != null) tankFrictionColliderMC.sharedMesh = collisionMesh;

            //LinkTracks
            TankChassisController tankController = hull.GetComponent<TankChassisController>();
            Transform lTracks = hullInstance.transform.Find("l_tracks");
            TrackBlendShapeAnimator lTracksAnimator = lTracks.GetComponent<TrackBlendShapeAnimator>();
            Transform rTracks = hullInstance.transform.Find("r_tracks");
            TrackBlendShapeAnimator rTracksAnimator = rTracks.GetComponentInChildren<TrackBlendShapeAnimator>();
            if (tankController != null && lTracksAnimator != null && rTracksAnimator != null) tankController.SetTrackAnimators(lTracksAnimator, rTracksAnimator);

            //SetHullSettings
            string fileName = TankSetupData.SelectedHullID + "CFG" + ".json";
            string path = Path.Combine(Application.streamingAssetsPath, "Configs", fileName);
            if (File.Exists(path))
            {
                string jsonText = File.ReadAllText(path);
                TankSettings loadedSettings = JsonUtility.FromJson<TankSettings>(jsonText);

                if (tankController != null) tankController.ApplySettings(loadedSettings);
            }
            else Debug.LogError("Файл конфига не найден по пути: " + path);

            #endregion

            #region TurretInstance
            Transform turretMount = hullInstance.transform.Find("turret_pos");
            if (turretMount == null)
            {
                Debug.LogError($"На корпусе {hullPrefab.name} нет объекта 'turret_pos'!");
                return;
            }
            GameObject turretInstance = Instantiate(turretPrefab, turretMount);
            turretInstance.transform.localPosition = Vector3.zero;
            turretInstance.transform.localRotation = Quaternion.identity;

            //LinkCamera
            Camera.CameraController cam = tankBase.GetComponentInChildren<Camera.CameraController>();
            if (cam != null && turretMount != null) cam.SetTarget(turretMount);

            //LinkMuzzlePoint
            Transform muzzlePoint = turretInstance.transform.Find("muzzle_point");
            AIM.SmartAim smartAim = turretInstance.GetComponent<AIM.SmartAim>();
            if (muzzlePoint != null && smartAim != null) smartAim.SetMuzzlePoint(muzzlePoint);

            #endregion

            Debug.Log("Танк успешно собран!");
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