using System.IO;
using UnityEngine;
using GameScripts.Camera;
using GameScripts.AIM;

public class TankAssembler : MonoBehaviour
{
    [Header("Настройки для Болванок (Ручной спавн)")]
    public bool assembleOnStart = false;
    public GameObject manualHullPrefab;
    public GameObject manualTurretPrefab;
    public string manualHullId = "Hornet_Standart";
    public string manualSkinId = "blue";

    private void Start()
    {
        if (assembleOnStart && manualHullPrefab != null && manualTurretPrefab != null)
        {
            Assemble(manualHullPrefab, manualTurretPrefab, manualHullId);
        }
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

        TankChassisController tankController = GetComponent<TankChassisController>();

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

        CameraController cam = GetComponentInChildren<CameraController>();
        if (cam != null && turretMount != null) cam.SetTarget(turretMount);

        TurretController turretMountController = turretMount.GetComponent<TurretController>();
        turretMountController?.SetCamTransform(cam != null ? cam.transform : null);

        Transform muzzlePoint = turretInstance.transform.Find("muzzle");
        WeaponController weaponCtrl = GetComponentInChildren<WeaponController>();
        if (muzzlePoint != null && weaponCtrl != null)
        {
            weaponCtrl.SetMuzzlePoint(muzzlePoint);
        }

        PlayerTankBrain brain = GetComponent<PlayerTankBrain>();
        Transform followingCamera = transform.Find("FollowingCamera");

        if (brain != null)
        {
            if (weaponCtrl != null) weaponCtrl.isLocalPlayer = true;
            if (followingCamera != null) followingCamera.gameObject.SetActive(true);

            brain.InitializeBrain(tankController, turretMountController, weaponCtrl, cam);
        }
        else
        {
            if (weaponCtrl != null) weaponCtrl.isLocalPlayer = false;
            if (followingCamera != null) followingCamera.gameObject.SetActive(false);
        }
    }
}