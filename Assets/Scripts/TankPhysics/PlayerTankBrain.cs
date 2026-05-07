using UnityEngine;
using UnityEngine.InputSystem;
using GameScripts.AIM;
using GameScripts.Camera;

public class PlayerTankBrain : MonoBehaviour
{
    [Header("Подключенные модули")]
    public TankChassisController chassis;
    public TurretController turret;
    public WeaponController weapon;
    public CameraController camController;

    public void InitializeBrain(TankChassisController chassisCtrl, TurretController turretCtrl, WeaponController weaponCtrl, CameraController camCtrl)
    {
        chassis = chassisCtrl;
        turret = turretCtrl;
        weapon = weaponCtrl;
        camController = camCtrl;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (chassis != null)
            chassis.SetMoveInput(context.ReadValue<Vector2>());
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (weapon == null) return;

        if (Cursor.visible || Cursor.lockState == CursorLockMode.None) return;

        if (context.performed)
        {
            weapon.TryShoot();
        }
    }

    public void OnLockTurret(InputAction.CallbackContext context)
    {
        if (turret == null) return;

        if (context.started || context.performed)
            turret.SetTurretLock(true);
        else if (context.canceled)
            turret.SetTurretLock(false);
    }

    public void OnCameraLook(InputAction.CallbackContext context)
    {
        if (camController != null)
            camController.SetLookInput(context.ReadValue<Vector2>());
    }

    public void OnCameraZoom(InputAction.CallbackContext context)
    {
        if (camController != null)
            camController.SetZoomInput(context.ReadValue<Vector2>().y);
    }

    public void OnFreeCursor(InputAction.CallbackContext context)
    {
        if (camController == null) return;

        if (context.started)
            camController.SetFreeCursor(true);
        else if (context.canceled)
            camController.SetFreeCursor(false);
    }
}