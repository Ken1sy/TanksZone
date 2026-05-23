using GameScripts.AIM;
using GameScripts.Camera;
using System.IO;
using FishNet.Object;
using UnityEngine;

public class TankAssembler : NetworkBehaviour
{
    [Header("Настройки для Болванок (Ручной спавн)")]
    public bool assembleOnStart = false;
    public GameObject manualHullPrefab;
    public GameObject manualTurretPrefab;
    public string manualHullId = "Hornet_Standart";
    public string manualSkinId = "blue";

    // Сохраняем ссылки на компоненты, чтобы выдать им права позже
    private TankChassisController tankController;
    private TurretController turretMountController;
    private WeaponController weaponCtrl;
    private CameraController cam;
    private Transform followingCamera;
    private Rigidbody rb;

    // 1. ЭТАП СБОРКИ: Вызывается самым первым
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (assembleOnStart && manualHullPrefab != null && manualTurretPrefab != null)
        {
            Assemble(manualHullPrefab, manualTurretPrefab, manualHullId);
        }
    }

    // 2. ЭТАП РАЗДАЧИ ПРАВ: Вызывается, когда FishNet точно знает владельца
    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyOwnershipPermissions();
    }

    public void Assemble(GameObject hullPrefab, GameObject turretPrefab, string hullId)
    {
        if (hullPrefab == null || turretPrefab == null) return;

        this.manualHullPrefab = hullPrefab;
        this.manualTurretPrefab = turretPrefab;
        this.manualHullId = hullId;

        GameObject hullInstance = Instantiate(hullPrefab, this.transform);
        hullInstance.transform.localPosition = new Vector3(0f, -0.32f, 0f);
        hullInstance.transform.localRotation = Quaternion.identity;

        tankController = GetComponent<TankChassisController>();

        TrackUVAnimator lTracksAnimator = hullInstance.transform.Find("lTrack")?.GetComponent<TrackUVAnimator>();
        TrackUVAnimator rTracksAnimator = hullInstance.transform.Find("rTrack")?.GetComponentInChildren<TrackUVAnimator>();

        if (tankController != null && lTracksAnimator != null && rTracksAnimator != null)
            tankController.SetTrackAnimators(lTracksAnimator, rTracksAnimator);

        string path = Path.Combine(Application.streamingAssetsPath, "Configs", hullId + ".cfg");
        if (File.Exists(path))
        {
            string jsonText = File.ReadAllText(path);
            TankSettings loadedSettings = JsonUtility.FromJson<TankSettings>(jsonText);
            if (tankController != null) tankController.ApplySettings(loadedSettings);
        }

        Transform turretMount = hullInstance.transform.Find("mount");
        GameObject turretInstance = Instantiate(turretPrefab, turretMount);
        turretInstance.transform.localPosition = Vector3.zero;
        turretInstance.transform.localRotation = Quaternion.identity;

        TankSkinSwitcher skinSwitcher = GetComponent<TankSkinSwitcher>();
        if (skinSwitcher != null)
        {
            Renderer hullRend = hullInstance.GetComponent<Renderer>();
            Renderer turretRend = turretInstance.GetComponent<Renderer>();

            skinSwitcher.SetRenderers(hullRend, turretRend);
            skinSwitcher.ApplySkinById(manualSkinId);
        }

        cam = GetComponentInChildren<CameraController>();
        if (cam != null && turretMount != null) cam.SetTarget(turretMount);

        turretMountController = turretMount.GetComponent<TurretController>();

        Transform muzzlePoint = turretInstance.transform.Find("muzzle");
        weaponCtrl = GetComponentInChildren<WeaponController>();
        if (muzzlePoint != null && weaponCtrl != null)
        {
            weaponCtrl.SetMuzzlePoint(muzzlePoint);
        }

        PlayerTankBrain brain = GetComponent<PlayerTankBrain>();
        followingCamera = transform.Find("FollowingCamera");
        rb = GetComponent<Rigidbody>();

        if (brain != null)
        {
            brain.InitializeBrain(tankController, turretMountController, weaponCtrl, cam);
        }

        if (base.IsServerInitialized)
        {
            if (tankController != null) tankController.isLocallyControlled = true;
            if (rb != null) rb.isKinematic = false;
        }
        else
        {
            if (tankController != null) tankController.isLocallyControlled = true;
            if (rb != null) rb.isKinematic = false;
        }
    }

    // Вспомогательный метод, который безопасно включает/выключает компоненты
    private void ApplyOwnershipPermissions()
    {
        // Теперь base.IsOwner работает абсолютно точно!
        bool isMyTank = base.IsOwner;

        if (isMyTank)
        {
            if (weaponCtrl != null) weaponCtrl.isLocalPlayer = true;
            if (followingCamera != null) followingCamera.gameObject.SetActive(true);

            // Отдаем камеру нашей башне
            if (turretMountController != null) turretMountController.SetCamTransform(cam != null ? cam.transform : null);
        }
        else
        {
            if (weaponCtrl != null) weaponCtrl.isLocalPlayer = false;
            if (followingCamera != null) followingCamera.gameObject.SetActive(false);
            if (cam != null) cam.gameObject.SetActive(false);

            // Забираем камеру у чужой башни
            if (turretMountController != null) turretMountController.SetCamTransform(null);

            AudioListener listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
    }
}