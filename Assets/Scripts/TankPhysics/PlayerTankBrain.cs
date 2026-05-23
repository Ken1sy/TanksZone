using FishNet.Object;
using FishNet.Object.Synchronizing;
using GameScripts.AIM;
using GameScripts.Camera;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTankBrain : NetworkBehaviour
{
    [Header("Подключенные модули")]
    public TankChassisController chassis;
    public TurretController turret;
    public WeaponController weapon;
    public CameraController camController;

    public readonly SyncVar<Vector2> networkInput = new SyncVar<Vector2>();
    public readonly SyncVar<float> networkTurretAngle = new SyncVar<float>();

    private float _lastSentAngle = -999f;
    private float _remoteTurretVelocity = 0f;

    private void Awake() { networkInput.OnChange += OnNetworkInputChanged; }

    private void OnNetworkInputChanged(Vector2 prevValue, Vector2 newValue, bool asServer)
    {
        if (!base.IsOwner && chassis != null) { chassis.SetMoveInput(newValue); }
    }

    private void Update()
    {
        if (base.IsOwner)
        {
            if (turret == null) return;
            float currentAngle = turret.transform.localEulerAngles.y;
            if (Mathf.Abs(Mathf.DeltaAngle(currentAngle, _lastSentAngle)) > 0.5f)
            {
                _lastSentAngle = currentAngle;
                SubmitTurretAngleServer(currentAngle);
            }
        }
        else
        {
            if (turret != null)
            {
                float currentAngle = turret.transform.localEulerAngles.y;
                float targetAngle = networkTurretAngle.Value;
                float smoothAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref _remoteTurretVelocity, 0.08f);
                turret.transform.localEulerAngles = new Vector3(0f, smoothAngle, 0f);
            }
        }
    }

    [ServerRpc]
    private void SubmitTurretAngleServer(float angle)
    {
        networkTurretAngle.Value = angle;
    }

    public void InitializeBrain(TankChassisController chassisCtrl, TurretController turretCtrl, WeaponController weaponCtrl, CameraController camCtrl)
    {
        chassis = chassisCtrl;
        turret = turretCtrl;
        weapon = weaponCtrl;
        camController = camCtrl;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!base.IsOwner) return;
        Vector2 input = context.ReadValue<Vector2>();
        if (chassis != null) chassis.SetMoveInput(input);
        SubmitMoveInputServer(input);
    }

    [ServerRpc]
    private void SubmitMoveInputServer(Vector2 input)
    {
        networkInput.Value = input;
        if (chassis != null) chassis.SetMoveInput(input);
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (!IsOwner || weapon == null) return;
        if (Cursor.visible || Cursor.lockState == CursorLockMode.None) return;

        if (context.performed)
        {
            // Получаем от оружия не только вектор, но и в кого мы попали
            weapon.TryShootLocal(out Vector3 aimDirection, out bool isBlocked, out NetworkObject hitNetObj, out Vector3 hitPoint);

            // Передаем это на Сервер
            SubmitShootServer(aimDirection, isBlocked, hitNetObj, hitPoint);
        }
    }

    [ServerRpc]
    private void SubmitShootServer(Vector3 aimDirection, bool isBlocked, NetworkObject hitNetObj, Vector3 hitPoint)
    {
        if (weapon != null)
        {
            // Сервер жестко толкает цель
            weapon.PerformServerPhysics(aimDirection, isBlocked, hitNetObj, hitPoint);
        }
        UpdateShootObservers(aimDirection, isBlocked);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void UpdateShootObservers(Vector3 aimDirection, bool isBlocked)
    {
        if (weapon != null)
        {
            weapon.PerformRemoteShoot(aimDirection, isBlocked);
        }
    }

    public void OnLockTurret(InputAction.CallbackContext context)
    {
        if (!IsOwner || turret == null) return;
        if (context.started || context.performed) turret.SetTurretLock(true);
        else if (context.canceled) turret.SetTurretLock(false);
    }

    public void OnCameraLook(InputAction.CallbackContext context)
    {
        if (!IsOwner || camController == null) return;
        camController.SetLookInput(context.ReadValue<Vector2>());
    }

    public void OnCameraZoom(InputAction.CallbackContext context)
    {
        if (!IsOwner || camController == null) return;
        camController.SetZoomInput(context.ReadValue<Vector2>().y);
    }

    public void OnFreeCursor(InputAction.CallbackContext context)
    {
        if (!IsOwner || camController == null) return;
        if (context.started) camController.SetFreeCursor(true);
        else if (context.canceled) camController.SetFreeCursor(false);
    }
}