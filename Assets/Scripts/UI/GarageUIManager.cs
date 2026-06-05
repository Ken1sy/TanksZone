using FishNet;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GarageUIManager : MonoBehaviour
{
    public static GarageUIManager Instance;

    [Header("Управление интерфейсом и Камерой")]
    public GameObject mainUIElement;
    public GarageCamera garageCameraScript;
    public GarageItemsManager itemsManager;

    private bool isMainUIOpen = false;

    [Header("UI Элементы Верхней Панели")]
    public TMP_Text xpAndRankText;
    public TMP_Text crystalsText;
    public Slider xpProgressBar;
    public Image rankIconImage;

    [Header("Иконки Званий")]
    public Sprite[] rankIcons;

    [Header("Настройки и Управление")]
    public GameObject settingsPanel;
    public Image muteButtonImage;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;

    [Header("Новое: Управление Битвами и Сценами")]
    public GameObject battleListPanel;
    public GameObject[] garageOnlyUIElements;
    public GameObject[] battleOnlyUIElements;
    public GameObject exitConfirmationPopup;

    public string battleSceneName = "03_BattleMap";
    public string garageSceneName = "02_Garage";

    private bool isInBattle = false;
    private int currentXp = 0;
    private int crystals = 0;
    private string playerName = "Командир";

    private readonly int[] rankXpThresholds = {
        0, 100, 500, 1500, 3700, 7100, 12300, 20000, 29000, 41000,
        57000, 76000, 98000, 125000, 156000, 192000, 233000, 280000,
        332000, 390000, 455000, 527000, 606000, 692000, 787000, 889000,
        1000000, 1122000, 1255000, 1400000, 1600000
    };

    private readonly string[] rankNames = {
        "Новобранец", "Рядовой", "Ефрейтор", "Капрал", "Мастер-капрал",
        "Сержант", "Штаб-сержант", "Мастер-сержант", "Первый сержант", "Сержант-майор",
        "Уорэнт-офицер 1", "Уорэнт-офицер 2", "Уорэнт-офицер 3", "Уорэнт-офицер 4", "Уорэнт-офицер 5",
        "Младший лейтенант", "Лейтенант", "Старший лейтенант", "Капитан", "Майор", "Подполковник", "Полковник", "Бригадир",
        "Генерал-майор", "Генерал-лейтенант", "Генерал", "Маршал", "Фельдмаршал", "Командор", "Генералиссимус", "Легенда"
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Transform rootObj = transform.root;
            DontDestroyOnLoad(rootObj.gameObject);
        }
        else
        {
            Destroy(transform.root.gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (xpAndRankText != null) xpAndRankText.text = "Загрузка профиля...";
        if (crystalsText != null) crystalsText.text = "...";
        if (xpProgressBar != null) xpProgressBar.value = 0f;

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (exitConfirmationPopup != null) exitConfirmationPopup.SetActive(false);
        if (mainUIElement != null) mainUIElement.SetActive(isMainUIOpen);

        SyncMuteIcon(GameScripts.UI.SettingsMenuController.IsMuted);

        if (garageCameraScript != null) garageCameraScript.SetUIState(isMainUIOpen);

        UpdateUIVisibilityForScene(true);
        LoadPlayerAccountInfo();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UnityEngine.EventSystems.EventSystem es = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        if (scene.name.Contains(battleSceneName))
        {
            isInBattle = true;
            UpdateUIVisibilityForScene(false);

            if (mainUIElement != null) mainUIElement.SetActive(false);
            if (battleListPanel != null) battleListPanel.SetActive(false);
            isMainUIOpen = false;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else if (scene.name.Contains(garageSceneName))
        {
            isInBattle = false;
            UpdateUIVisibilityForScene(true);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            garageCameraScript = FindAnyObjectByType<GarageCamera>();
            if (garageCameraScript != null) garageCameraScript.SetUIState(isMainUIOpen);

            LoadPlayerAccountInfo();

            if (itemsManager != null) itemsManager.PopulateGarage(true);
        }
    }

    private void UpdateUIVisibilityForScene(bool isGarage)
    {
        if (garageOnlyUIElements != null)
        {
            foreach (var el in garageOnlyUIElements)
            {
                if (el != null) el.SetActive(isGarage);
            }
        }

        if (battleOnlyUIElements != null)
        {
            foreach (var el in battleOnlyUIElements)
            {
                if (el != null) el.SetActive(!isGarage);
            }
        }
    }

    private void LoadPlayerAccountInfo()
    {
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(),
            result =>
            {
                playerName = result.AccountInfo.TitleInfo.DisplayName;
                if (string.IsNullOrEmpty(playerName))
                {
                    playerName = result.AccountInfo.Username;
                    if (!string.IsNullOrEmpty(playerName)) FixDisplayNameInDatabase(playerName);
                }

                PlayerPrefs.SetString("MyNickname", playerName);
                PlayerPrefs.Save();

                if (string.IsNullOrEmpty(playerName)) playerName = "Без Имени";
                LoadUserData();
            },
            error =>
            {
                Debug.LogError("Ошибка получения аккаунта: " + error.ErrorMessage);
                LoadUserData();
            }
        );
    }

    private void FixDisplayNameInDatabase(string newName)
    {
        var request = new UpdateUserTitleDisplayNameRequest { DisplayName = newName };
        PlayFabClientAPI.UpdateUserTitleDisplayName(request,
            res => Debug.Log("Display Name успешно синхронизирован: " + newName),
            err => Debug.LogWarning("Не удалось обновить Display Name (возможно, он уже занят)")
        );
    }

    private void LoadUserData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result =>
            {
                if (result.Data != null && result.Data.ContainsKey("XP") && result.Data.ContainsKey("Crystals"))
                {
                    currentXp = int.Parse(result.Data["XP"].Value);
                    crystals = int.Parse(result.Data["Crystals"].Value);
                    UpdateTopPanelUI();
                }
                else
                {
                    SaveInitialDataForNewPlayer();
                }
            },
            error => Debug.LogError("Ошибка получения данных игрока: " + error.ErrorMessage)
        );
    }

    private void SaveInitialDataForNewPlayer()
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "XP", "0" },
                { "Crystals", "5000" }
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result =>
            {
                currentXp = 0;
                crystals = 5000;
                UpdateTopPanelUI();
            },
            error => Debug.LogError("Ошибка сохранения начальных данных: " + error.ErrorMessage)
        );
    }

    private void UpdateTopPanelUI()
    {
        if (crystalsText != null) crystalsText.text = crystals.ToString();

        int currentRankIndex = 0;
        for (int i = rankXpThresholds.Length - 1; i >= 0; i--)
        {
            if (currentXp >= rankXpThresholds[i])
            {
                currentRankIndex = i;
                break;
            }
        }

        string currentRankName = rankNames[currentRankIndex];
        int currentRankBaseXp = rankXpThresholds[currentRankIndex];
        int nextRankXp = 0;

        int expectedRankValue = currentRankIndex + 1;
        PlayerPrefs.SetInt("MyRank", expectedRankValue);
        PlayerPrefs.Save();

        // ИСПРАВЛЕНИЕ 3: Теперь мы оповещаем СВОЙ ТАНК в игре о том, что у нас повысился ранг!
        if (PlayerTankBrain.LocalInstance != null)
        {
            PlayerTankBrain.LocalInstance.CmdUpdateRank(expectedRankValue);
        }

        if (currentRankIndex < rankXpThresholds.Length - 1) nextRankXp = rankXpThresholds[currentRankIndex + 1];
        else nextRankXp = currentXp;

        if (xpAndRankText != null)
        {
            xpAndRankText.text = $"{currentXp} / {nextRankXp} {currentRankName} {playerName}";
        }

        if (rankIconImage != null && rankIcons != null && rankIcons.Length > 0)
        {
            int iconIndex = Mathf.Clamp(currentRankIndex, 0, rankIcons.Length - 1);
            if (rankIcons[iconIndex] != null) rankIconImage.sprite = rankIcons[iconIndex];
        }

        if (xpProgressBar != null)
        {
            if (currentRankIndex >= rankXpThresholds.Length - 1) xpProgressBar.value = 1f;
            else
            {
                float xpIntoCurrentRank = currentXp - currentRankBaseXp;
                float xpNeededForNextRank = nextRankXp - currentRankBaseXp;
                xpProgressBar.value = xpIntoCurrentRank / xpNeededForNextRank;
            }
        }
    }

    public void AddBattleRewards(int xpAdded, int crystalsAdded)
    {
        currentXp += xpAdded;
        crystals += crystalsAdded;
        UpdateTopPanelUI();

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "XP", currentXp.ToString() },
                { "Crystals", crystals.ToString() }
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            res => Debug.Log($"[Бой] Вы уничтожили танк! Получено {xpAdded} опыта и {crystalsAdded} крис."),
            err => Debug.LogError("[Бой] Ошибка сохранения наград: " + err.ErrorMessage)
        );
    }

    public bool TrySpendCrystals(int amount)
    {
        if (crystals >= amount)
        {
            crystals -= amount;
            UpdateTopPanelUI();

            var request = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string> { { "Crystals", crystals.ToString() } }
            };

            PlayFabClientAPI.UpdateUserData(request,
                res => Debug.Log($"Успешное списание в PlayFab! Осталось кристаллов: {crystals}"),
                err => Debug.LogError("Ошибка синхронизации баланса: " + err.ErrorMessage)
            );

            return true;
        }
        return false;
    }

    public void ToggleMainGarageUI()
    {
        isMainUIOpen = !isMainUIOpen;
        if (mainUIElement != null) mainUIElement.SetActive(isMainUIOpen);
        if (garageCameraScript != null) garageCameraScript.SetUIState(isMainUIOpen);
        if (itemsManager != null) itemsManager.OnUIStateChanged(isMainUIOpen);
        if (isMainUIOpen && battleListPanel != null) battleListPanel.SetActive(false);
    }

    public void ToggleBattleListPanel()
    {
        if (battleListPanel != null)
        {
            bool isGoingToOpen = !battleListPanel.activeSelf;
            battleListPanel.SetActive(isGoingToOpen);
            if (isGoingToOpen && isMainUIOpen) ToggleMainGarageUI();
        }
    }

    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
        {
            bool willBeOpen = !settingsPanel.activeSelf;
            settingsPanel.SetActive(willBeOpen);

            if (willBeOpen)
            {
                // ИСПРАВЛЕНИЕ 2: Выдвигаем панель настроек на самый передний план (поверх всех ХП)
                settingsPanel.transform.SetAsLastSibling();
            }

            if (isInBattle)
            {
                if (willBeOpen)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
                else
                {
                    if (exitConfirmationPopup == null || !exitConfirmationPopup.activeSelf)
                    {
                        Cursor.visible = false;
                        Cursor.lockState = CursorLockMode.Locked;
                    }
                }
            }
        }
    }

    public void ToggleMute()
    {
        GameScripts.UI.SettingsMenuController.ToggleMuteGlobal();
    }

    public void SyncMuteIcon(bool isMutedState)
    {
        if (muteButtonImage != null && soundOnSprite != null && soundOffSprite != null)
        {
            muteButtonImage.sprite = isMutedState ? soundOffSprite : soundOnSprite;
        }
    }

    public void OnQuitButtonPressed()
    {
        if (isInBattle)
        {
            if (exitConfirmationPopup != null)
            {
                exitConfirmationPopup.SetActive(true);
                // На всякий случай попап выхода тоже выдвигаем вперед
                exitConfirmationPopup.transform.SetAsLastSibling();

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else ConfirmExitBattle();
        }
        else QuitGame();
    }

    public void ConfirmExitBattle()
    {
        if (exitConfirmationPopup != null) exitConfirmationPopup.SetActive(false);
        if (ServerRoomManager.Instance != null) ServerRoomManager.Instance.RequestLeaveRoom();
        SceneManager.LoadScene(garageSceneName);
    }

    public void CancelExitBattle()
    {
        if (exitConfirmationPopup != null) exitConfirmationPopup.SetActive(false);

        if (settingsPanel == null || !settingsPanel.activeSelf)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnApplicationQuit()
    {
        if (InstanceFinder.NetworkManager != null)
        {
            if (InstanceFinder.ServerManager != null) InstanceFinder.ServerManager.StopConnection(true);
            if (InstanceFinder.ClientManager != null) InstanceFinder.ClientManager.StopConnection();
        }
    }
}