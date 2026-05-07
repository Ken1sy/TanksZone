using UnityEngine;
using TMPro;

public class DebugPanelController : MonoBehaviour
{
    [Header("UI Ссылки")]
    public TextMeshProUGUI entitiesText;
    public TextMeshProUGUI hullText;
    public TextMeshProUGUI turretText;
    public TextMeshProUGUI skinText;

    [Header("Настройки")]
    public float updateInterval = 0.5f; // Обновляем панель каждые полсекунды
    private float timer;

    private TankAssembler localTankAssembler;

    void Start()
    {
        FindLocalPlayer();
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Таймер для оптимизации (чтобы не искать объекты каждый кадр)
        if (timer >= updateInterval)
        {
            timer = 0f;
            UpdateDebugInfo();
        }
    }

    private void FindLocalPlayer()
    {
        // Ищем на сцене именно наш танк (тот, у которого есть мозг игрока)
        PlayerTankBrain brain = FindAnyObjectByType<PlayerTankBrain>();
        if (brain != null)
        {
            localTankAssembler = brain.GetComponent<TankAssembler>();
        }
    }

    private void UpdateDebugInfo()
    {
        TankAssembler[] tanks = FindObjectsByType<TankAssembler>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (entitiesText != null)
            entitiesText.text = $"кол-во танков: {tanks.Length}";

        // === 2. ДАННЫЕ ИГРОКА ===
        if (localTankAssembler != null)
        {
            // Берем данные напрямую из твоего ассемблера
            string hull = localTankAssembler.manualHullId;
            string skin = localTankAssembler.manualSkinId;

            // Название пушки берем из префаба, если он назначен
            string turret = localTankAssembler.manualTurretPrefab != null ?
                            localTankAssembler.manualTurretPrefab.name : "None";

            if (hullText != null) hullText.text = $"корпус: {hull}";
            if (turretText != null) turretText.text = $"пушка: {turret}";
            if (skinText != null) skinText.text = $"скин: {skin}";
        }
        else
        {
            // Если танка нет (уничтожен или мы в меню) - пытаемся найти его снова
            FindLocalPlayer();

            if (hullText != null) hullText.text = "корпус: None";
            if (turretText != null) turretText.text = "пушка: None";
            if (skinText != null) skinText.text = "скин: None";
        }
    }
}