using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using GameScripts.AIM;
using GameScripts.Camera;
using GameScripts.Visuals;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTankBrain : NetworkBehaviour
{
    public static PlayerTankBrain LocalInstance;

    [Header("Подключенные модули")]
    public TankChassisController chassis;
    public TurretController turret;
    public WeaponController weapon;
    public CameraController camController;
    [Header("UI Интерфейс")]
    public TankUIController uiPrefab;
    private TankUIController _uiInstance;
    [Header("Информация об игроке")]
    public readonly SyncVar<string> playerName = new SyncVar<string>("Танкист");
    public readonly SyncVar<int> playerRank = new SyncVar<int>(1);
    [Header("Синхронизация ввода")]
    public readonly SyncVar<Vector2> networkInput = new SyncVar<Vector2>();
    public readonly SyncVar<float> networkTurretAngle = new SyncVar<float>();
    [Header("Система Здоровья и Статуса")]
    public readonly SyncVar<float> maxHealth = new SyncVar<float>();
    public readonly SyncVar<float> currentHealth = new SyncVar<float>();
    public readonly SyncVar<bool> isDead = new SyncVar<bool>();
    public readonly SyncVar<float> networkTemperature = new SyncVar<float>();
    [Header("Настройки Температуры")]
    public float coolingRate = 5f;
    public float maxSpeedPenalty = 0.7f;
    [Header("Урон от температуры (Тики)")]
    public float maxTempDamagePerSecond = 30f;
    public float tempDamageTickRate = 1f;

    private float _nextTempDamageTime = 0f;
    private float _lastSentAngle = -999f;
    private float _remoteTurretVelocity = 0f;
    private bool _isShootingHeld = false;
    private Vector2 _rawMoveInput = Vector2.zero;

    private void Awake()
    {
        networkInput.OnChange += OnNetworkInputChanged;
        currentHealth.OnChange += OnHealthChanged;
        isDead.OnChange += OnDeadChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            LocalInstance = this;
            string myName = PlayerPrefs.GetString("MyNickname", "Танкист");
            int myRank = PlayerPrefs.GetInt("MyRank", 1);
            CmdSetPlayerInfo(myName, myRank);
        }
        if (!isDead.Value) CreateUI();
    }

    private void CreateUI()
    {
        if (uiPrefab != null)
        {
            Canvas mainCanvas = FindFirstObjectByType<Canvas>();
            if (mainCanvas != null)
            {
                _uiInstance = Instantiate(uiPrefab, mainCanvas.transform);
                _uiInstance.Initialize(this);
                _uiInstance.UpdateHealth(currentHealth.Value, maxHealth.Value);
            }
        }
    }

    private void OnDeadChanged(bool prev, bool next, bool asServer)
    {
        if (next)
        {
            if (_uiInstance != null) Destroy(_uiInstance.gameObject);
            if (turret != null) turret.enabled = false;
        }
        else { if (turret != null) turret.enabled = true; CreateUI(); }
    }

    [ServerRpc]
    private void CmdSetPlayerInfo(string nickName, int rankIndex)
    { playerName.Value = nickName; playerRank.Value = rankIndex; }

    [ServerRpc]
    public void CmdUpdateRank(int newRank) { playerRank.Value = newRank; }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (_uiInstance != null) Destroy(_uiInstance.gameObject);
        if (base.IsOwner && LocalInstance == this) LocalInstance = null;
    }

    private void OnNetworkInputChanged(Vector2 prevValue, Vector2 newValue, bool asServer)
    { if (!base.IsOwner && chassis != null) { chassis.SetMoveInput(newValue); } }

    private void OnHealthChanged(float prevHealth, float nextHealth, bool asServer)
    {
        if (_uiInstance == null) return;
        _uiInstance.UpdateHealth(nextHealth, maxHealth.Value);
        if (nextHealth < prevHealth && !isDead.Value)
        { float damageTaken = prevHealth - nextHealth; _uiInstance.ShowDamage(damageTaken); }
    }

    [Server]
    public void InitializeHealth(float hpAmount)
    { maxHealth.Value = hpAmount; currentHealth.Value = hpAmount; isDead.Value = false; }

    private void Update()
    {
        if (base.IsServerInitialized && !isDead.Value)
        {
            float currentTemp = networkTemperature.Value;
            if (currentTemp > 0)
            {
                currentTemp = Mathf.Max(0, currentTemp - coolingRate * Time.deltaTime);
                networkTemperature.Value = currentTemp;
            }
            else if (currentTemp < 0)
            {
                currentTemp = Mathf.Min(0, currentTemp + coolingRate * Time.deltaTime);
                networkTemperature.Value = currentTemp;
            }
            if (Mathf.Abs(currentTemp) > 5f)
            {
                if (Time.time >= _nextTempDamageTime)
                {
                    _nextTempDamageTime = Time.time + tempDamageTickRate;
                    float tempPercent = Mathf.Abs(currentTemp) / 100f;
                    float tickDamage = maxTempDamagePerSecond * tempPercent * tempDamageTickRate;
                    TakeDamage(tickDamage, null);
                }
            }
        }

        if (isDead.Value) { if (chassis != null) chassis.SetMoveInput(Vector2.zero); return; }

        if (base.IsOwner)
        {
            if (turret != null)
            {
                float currentAngle = turret.transform.localEulerAngles.y;
                if (Mathf.Abs(Mathf.DeltaAngle(currentAngle, _lastSentAngle)) > 0.5f)
                {
                    _lastSentAngle = currentAngle;
                    SubmitTurretAngleServer(currentAngle);
                }
            }
            if (weapon != null) weapon.ProcessInput(_isShootingHeld);
            ApplyTemperatureToChassis(_rawMoveInput);
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
            ApplyTemperatureToChassis(networkInput.Value);
        }
    }

    private void ApplyTemperatureToChassis(Vector2 input)
    {
        if (chassis == null) return;
        float currentTemp = networkTemperature.Value;
        if (currentTemp < 0)
        {
            float freezeFactor = Mathf.Abs(currentTemp) / 100f;
            float speedMultiplier = 1f - (freezeFactor * maxSpeedPenalty);
            input *= Mathf.Clamp(speedMultiplier, 0.1f, 1f);
        }
        chassis.SetMoveInput(input);
    }

    [Server]
    public void TakeDamage(float amount, PlayerTankBrain attacker = null)
    {
        if (isDead.Value) return;
        currentHealth.Value -= amount;
        if (currentHealth.Value <= 0) { Die(attacker); }
    }

    [Server]
    private void Die(PlayerTankBrain attacker)
    {
        isDead.Value = true;
        currentHealth.Value = 0;
        networkTemperature.Value = 0f;
        TargetApplyDeathImpulse(base.Owner);
        if (attacker != null && attacker != this)
        {
            attacker.TargetRewardKill(attacker.Owner);
            if (ServerRoomManager.Instance != null) { ServerRoomManager.Instance.RegisterKill(attacker.Owner); }
        }
        StartCoroutine(RespawnRoutine());
    }

    [TargetRpc]
    private void TargetRewardKill(NetworkConnection conn)
    {
        if (GarageUIManager.Instance != null) { GarageUIManager.Instance.AddBattleRewards(1000, 100); }
    }

    [TargetRpc]
    public void TargetApplyImpact(NetworkConnection conn, Vector3 aimDirection, float impactForce, Vector3 hitPoint)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.AddForceAtPosition(aimDirection * impactForce, hitPoint, ForceMode.Impulse);
        }
    }

    [TargetRpc]
    private void TargetApplyDeathImpulse(NetworkConnection conn)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), Random.Range(0.6f, 1.2f), Random.Range(-1f, 1f)).normalized;
            float force = rb.mass * 12f;
            float torque = rb.mass * 6f;
            rb.AddForce(randomDir * force, ForceMode.Impulse);
            rb.AddTorque(Random.onUnitSphere * torque, ForceMode.Impulse);
        }
    }

    private System.Collections.IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(5f);
        Transform spawnPoint = GameScripts.GameMode.SpawnManager.Instance.GetSafeSpawnPoint();
        Vector3 newPos = spawnPoint.position + Vector3.up * 2f;
        Quaternion newRot = spawnPoint.rotation;
        RpcResetPhysicsState(newPos, newRot);
        currentHealth.Value = maxHealth.Value;
        isDead.Value = false;
    }

    [ObserversRpc(BufferLast = true)]
    private void RpcResetPhysicsState(Vector3 pos, Quaternion rot)
    {
        transform.SetPositionAndRotation(pos, rot);
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = pos;
            rb.rotation = rot;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        Physics.SyncTransforms();
        if (chassis != null) chassis.SetMoveInput(Vector2.zero);
        if (turret != null)
        {
            turret.transform.localRotation = Quaternion.identity;
            _lastSentAngle = turret.transform.localEulerAngles.y;
        }
    }

    [ServerRpc]
    private void SubmitTurretAngleServer(float angle) { networkTurretAngle.Value = angle; }

    public void InitializeBrain(TankChassisController chassisCtrl, TurretController turretCtrl, WeaponController weaponCtrl, CameraController camCtrl)
    {
        chassis = chassisCtrl;
        turret = turretCtrl;
        weapon = weaponCtrl;
        camController = camCtrl;
        if (weapon != null) weapon.Initialize(this);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!base.IsOwner || isDead.Value) return;
        _rawMoveInput = context.ReadValue<Vector2>();
        SubmitMoveInputServer(_rawMoveInput);
    }

    [ServerRpc]
    private void SubmitMoveInputServer(Vector2 input) { networkInput.Value = input; }
    public void OnShoot(InputAction.CallbackContext context)
    {
        if (!base.IsOwner || weapon == null || isDead.Value)
        { _isShootingHeld = false; return; }
        if (Cursor.visible || Cursor.lockState == CursorLockMode.None)
        { _isShootingHeld = false; return; }
        if (context.started) _isShootingHeld = true;
        else if (context.canceled) _isShootingHeld = false;
    }
    [ServerRpc]
    public void CmdSubmitHitscanShoot(Vector3 aimDirection, bool isBlocked, NetworkObject hitNetObj, Vector3 hitPoint)
    {
        if (weapon != null) weapon.PerformServerPhysics(aimDirection, isBlocked, hitNetObj, hitPoint);
        RpcUpdateHitscanObservers(aimDirection, isBlocked);
    }
    [ObserversRpc(ExcludeOwner = true)]
    private void RpcUpdateHitscanObservers(Vector3 aimDirection, bool isBlocked)
    { if (weapon != null) weapon.PerformRemoteVisualShot(aimDirection, isBlocked); }
    [ServerRpc]
    public void CmdSubmitChargeStart() { RpcUpdateChargeObservers(); }
    [ObserversRpc(ExcludeOwner = true)]
    private void RpcUpdateChargeObservers()
    { if (weapon is RailgunGun railgun) railgun.PerformRemoteCharge(); }
    [ServerRpc]
    public void CmdSubmitProjectileShoot(Vector3 spawnPos, Vector3 aimDirection, int muzzleIndex)
    { RpcUpdateProjectileObservers(spawnPos, aimDirection, muzzleIndex); }
    [ObserversRpc(ExcludeOwner = true)]
    private void RpcUpdateProjectileObservers(Vector3 spawnPos, Vector3 aimDirection, int muzzleIndex)
    { if (weapon is ProjectileGun projGun) projGun.PerformRemoteProjectile(spawnPos, aimDirection, muzzleIndex); }
    [ServerRpc]
    public void CmdSubmitProjectileHit(Vector3 aimDirection, NetworkObject hitNetObj, Vector3 hitPoint)
    { if (weapon != null) weapon.PerformServerPhysics(aimDirection, false, hitNetObj, hitPoint); }
    [ServerRpc]
    public void CmdSetStreamState(bool isActive) { RpcSetStreamState(isActive); }
    [ObserversRpc(ExcludeOwner = true)]
    private void RpcSetStreamState(bool isActive)
    { if (weapon is StreamGun streamGun) streamGun.SetRemoteStreamState(isActive); }
    [ServerRpc]
    public void CmdSubmitStreamHits(NetworkObject[] hitObjs, float tempDelta, float baseDamage)
    {
        foreach (var obj in hitObjs)
        {
            PlayerTankBrain targetBrain = obj.GetComponent<PlayerTankBrain>();
            if (targetBrain != null && !targetBrain.isDead.Value)
            {
                float newTemp = targetBrain.networkTemperature.Value + tempDelta;
                targetBrain.networkTemperature.Value = Mathf.Clamp(newTemp, -100f, 100f);
                float finalDamage = baseDamage;
                if (targetBrain.networkTemperature.Value > 0)
                {
                    float damageMultiplier = targetBrain.networkTemperature.Value / 100f;
                    finalDamage += baseDamage * damageMultiplier;
                }
                targetBrain.TakeDamage(finalDamage, this);
            }
        }
    }

    public void OnLockTurret(InputAction.CallbackContext context)
    {
        if (!base.IsOwner || turret == null || isDead.Value) return;
        if (context.started || context.performed) turret.SetTurretLock(true);
        else if (context.canceled) turret.SetTurretLock(false);
    }

    public void OnCameraLook(InputAction.CallbackContext context)
    {
        if (!base.IsOwner || camController == null) return;
        if (GameScripts.UI.SettingsMenuController.IsOpen) return;
        camController.SetLookInput(context.ReadValue<Vector2>());
    }

    public void OnCameraZoom(InputAction.CallbackContext context)
    {
        if (!base.IsOwner || camController == null) return;
        if (GameScripts.UI.SettingsMenuController.IsOpen) return;
        camController.SetZoomInput(context.ReadValue<Vector2>().y);
    }

    public void OnFreeCursor(InputAction.CallbackContext context)
    {
        if (!base.IsOwner || camController == null) return;
        if (context.started)
        {
            bool isCurrentlyFree = Cursor.lockState == CursorLockMode.None;
            camController.SetFreeCursor(!isCurrentlyFree);
        }
    }
}