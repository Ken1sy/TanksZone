using FishNet.Object;
using FishNet.Object.Synchronizing;
using GameScripts.AIM;
using GameScripts.Camera;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TankAssembler : NetworkBehaviour
{
    [System.Serializable]
    public class TankModelComponent
    {
        public string id;
        public GameObject prefab;
    }
    [Header("База Префабов")]
    public List<TankModelComponent> availableHulls;
    public List<TankModelComponent> availableTurrets;
    [Header("Сглаживание FishNet (Отделение визуала)")]
    public Transform visualsRoot;
    public readonly SyncVar<string> syncHullId = new SyncVar<string>();
    public readonly SyncVar<string> syncTurretId = new SyncVar<string>();
    public readonly SyncVar<string> syncSkinId = new SyncVar<string>();
    public readonly SyncVar<TankSettings> syncSettings = new SyncVar<TankSettings>();

    private TankChassisController tankController;
    private TurretController turretMountController;
    private WeaponController weaponCtrl;
    private CameraController cam;
    private Transform followingCamera;
    private Rigidbody rb;
    private bool _isAssembled = false;

    private void Awake()
    {
        syncHullId.OnChange += OnEquipmentChanged;
        syncTurretId.OnChange += OnEquipmentChanged;
        syncSkinId.OnChange += OnEquipmentChanged;
        syncSettings.OnChange += OnSettingsChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            CmdSetEquipment(TankSetupData.SelectedHullID, TankSetupData.SelectedTurretID, TankSetupData.SelectedSkinID);
        }
    }
    [ServerRpc]
    private void CmdSetEquipment(string hull, string turret, string skin)
    {
        syncHullId.Value = hull;
        syncTurretId.Value = turret;
        syncSkinId.Value = skin;
        string path = Path.Combine(Application.streamingAssetsPath, "Configs", hull + ".cfg");
        if (File.Exists(path))
        {
            string jsonText = File.ReadAllText(path);
            TankSettings loadedSettings = JsonUtility.FromJson<TankSettings>(jsonText);
            syncSettings.Value = loadedSettings;
        }
        else
        {
            Debug.LogError($"[TankAssembler] ОШИБКА НА СЕРВЕРЕ: Файл {hull}.cfg не найден! Физика может сломаться!");
        }
        TryAssemble();
    }

    private void OnEquipmentChanged(string oldVal, string newVal, bool asServer)
    {
        if (asServer) return; TryAssemble();
    }
    private void OnSettingsChanged(TankSettings oldVal, TankSettings newVal, bool asServer)
    {
        if (asServer) return; TryAssemble();
    }
    private void TryAssemble()
    {
        if (_isAssembled) return;
        if (string.IsNullOrEmpty(syncHullId.Value) || string.IsNullOrEmpty(syncTurretId.Value) || string.IsNullOrEmpty(syncSkinId.Value)) return;
        if (syncSettings.Value.weight <= 0) return;

        GameObject hullPrefab = GetPrefabById(availableHulls, syncHullId.Value);
        GameObject turretPrefab = GetPrefabById(availableTurrets, syncTurretId.Value);

        if (hullPrefab == null || turretPrefab == null)
        {
            Debug.LogError($"[TankAssembler] ОШИБКА:" +
                $" Префаб для {syncHullId.Value} или {syncTurretId.Value} не найден в базе TankAssembler!" +
                $" Добавьте их в списки Available Hulls/Turrets в префабе PlayerTank!");
            return;
        }
        Assemble(hullPrefab, turretPrefab, syncHullId.Value, syncSkinId.Value);
        _isAssembled = true;
    }

    private GameObject GetPrefabById(List<TankModelComponent> list, string id)
    {
        foreach (var comp in list) { if (comp.id == id) return comp.prefab; }
        return null;
    }

    private void Assemble(GameObject hullPrefab, GameObject turretPrefab, string hullId, string skinId)
    {
        Debug.Log($"[TankAssembler] Сборка танка. Hull: {hullId}, Turret: {turretPrefab.name}, Skin: {skinId}");
        Transform spawnParent = visualsRoot != null ? visualsRoot : this.transform;
        GameObject hullInstance = Instantiate(hullPrefab, spawnParent);
        hullInstance.transform.localPosition = new Vector3(0f, -0.32f, 0f);
        hullInstance.transform.localRotation = Quaternion.identity;
        tankController = GetComponent<TankChassisController>();
        TrackUVAnimator lTracksAnimator = hullInstance.transform.Find("lTrack")?.GetComponent<TrackUVAnimator>();
        TrackUVAnimator rTracksAnimator = hullInstance.transform.Find("rTrack")?.GetComponentInChildren<TrackUVAnimator>();
        if (tankController != null && lTracksAnimator != null && rTracksAnimator != null)
            tankController.SetTrackAnimators(lTracksAnimator, rTracksAnimator);
        if (tankController != null)
        {
            tankController.ApplySettings(syncSettings.Value);
            Debug.Log($"[TankAssembler] Сетевой конфиг {hullId} успешно применен к шасси!");
        }
        Transform turretMount = hullInstance.transform.Find("mount");
        GameObject turretInstance = Instantiate(turretPrefab, turretMount);
        turretInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        TankSkinSwitcher skinSwitcher = GetComponent<TankSkinSwitcher>();
        if (skinSwitcher != null)
        {
            Renderer hullRend = hullInstance.GetComponent<Renderer>();
            Renderer turretRend = turretInstance.GetComponent<Renderer>();
            skinSwitcher.SetRenderers(hullRend, turretRend);
            skinSwitcher.ApplySkinById(skinId);
        }
        cam = GetComponentInChildren<CameraController>();
        if (cam != null && turretMount != null) cam.SetTarget(turretMount);
        turretMountController = turretMount.GetComponent<TurretController>();
        Transform muzzlePoint = turretInstance.transform.Find("muzzle");
        weaponCtrl = GetComponentInChildren<WeaponController>();
        if (muzzlePoint != null && weaponCtrl != null) { weaponCtrl.SetMuzzlePoint(muzzlePoint); }
        PlayerTankBrain brain = GetComponent<PlayerTankBrain>();
        followingCamera = transform.Find("FollowingCamera");
        rb = GetComponent<Rigidbody>();
        if (brain != null)
        {
            brain.InitializeBrain(tankController, turretMountController, weaponCtrl, cam);
            if (base.IsServerInitialized) { brain.InitializeHealth(syncSettings.Value.maxHealth); }
        }
        ApplyOwnershipPermissions();
    }

    private void ApplyOwnershipPermissions()
    {
        bool isMyTank = base.IsOwner;
        if (isMyTank)
        {
            if (weaponCtrl != null) weaponCtrl.isLocalPlayer = true;
            if (followingCamera != null) followingCamera.gameObject.SetActive(true);
            if (turretMountController != null) turretMountController.SetCamTransform(cam != null ? cam.transform : null);
            if (tankController != null) tankController.isLocallyControlled = true;
            if (rb != null) rb.isKinematic = false;
        }
        else
        {
            if (weaponCtrl != null) weaponCtrl.isLocalPlayer = false;
            if (followingCamera != null) followingCamera.gameObject.SetActive(false);
            if (cam != null) cam.gameObject.SetActive(false);
            if (turretMountController != null) turretMountController.SetCamTransform(null);
            AudioListener listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = false;
            if (tankController != null) tankController.isLocallyControlled = false;
            if (rb != null) rb.isKinematic = true;
        }
    }
}