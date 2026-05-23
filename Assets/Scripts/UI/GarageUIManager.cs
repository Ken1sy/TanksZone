using FishNet;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class GarageUIManager : MonoBehaviour
{
    [Header("Управление интерфейсом и Камерой")]
    public GameObject mainUIElement; // Массив элементов, которые нужно скрывать (список пушек, правая панель)
    public GarageCamera garageCameraScript; // Ссылка на наш новый скрипт камеры
    public GarageItemsManager itemsManager;
    private bool isMainUIOpen = false; // Состояние по умолчанию

    [Header("UI Элементы Верхней Панели")]
    public TMP_Text xpAndRankText; // Текст внутри прогресс-бара: "(0 / 100) Новобранец Игрок"
    public TMP_Text crystalsText;  // Текст с количеством кристаллов
    public Slider xpProgressBar;
    public Image rankIconImage;

    [Header("Иконки Званий")]
    public Sprite[] rankIcons;

    [Header("Настройки и Управление")]
    public GameObject settingsPanel;
    public Image muteButtonImage;    // Ссылка на картинку внутри кнопки звука
    public Sprite soundOnSprite;     // Иконка включенного звука
    public Sprite soundOffSprite;
    private bool isMuted = false;

    [Header("Данные Игрока (Текущие)")]
    private int currentXp = 0;
    private int crystals = 0;
    private string playerName = "Командир";

    // Таблица званий (перенесена из твоего документа)
    // Индексы совпадают: 0 = Новобранец, 1 = Рядовой и т.д.
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

    private void Start()
    {
        // Включаем курсор для интерфейса гаража
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (xpAndRankText != null) xpAndRankText.text = "Загрузка профиля...";
        if (crystalsText != null) crystalsText.text = "...";
        if (xpProgressBar != null) xpProgressBar.value = 0f;

        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (mainUIElement != null) mainUIElement.SetActive(isMainUIOpen);

        if (muteButtonImage != null && soundOnSprite != null && soundOffSprite != null)
        {
            muteButtonImage.sprite = isMuted ? soundOffSprite : soundOnSprite;
        }

        if (garageCameraScript != null) garageCameraScript.SetUIState(isMainUIOpen);

        LoadPlayerAccountInfo();
    }

    // 1. Сначала узнаем Никнейм игрока
    private void LoadPlayerAccountInfo()
    {
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(),
            result =>
            {
                // Сначала пробуем взять Display Name
                playerName = result.AccountInfo.TitleInfo.DisplayName;

                // Если он пустой, берем Username (который мы генерировали при регистрации)
                if (string.IsNullOrEmpty(playerName))
                {
                    playerName = result.AccountInfo.Username;

                    // И сразу же чиним базу данных: записываем Username в поле DisplayName
                    if (!string.IsNullOrEmpty(playerName))
                    {
                        FixDisplayNameInDatabase(playerName);
                    }
                }

                // Защита от непредвиденных ошибок
                if (string.IsNullOrEmpty(playerName)) playerName = "Без Имени";

                // Переходим к загрузке опыта и кристаллов
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

    // 2. Узнаем опыт и кристаллы из базы данных PlayFab
    private void LoadUserData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result =>
            {
                // Проверяем, есть ли у этого игрока сохраненные данные
                if (result.Data != null && result.Data.ContainsKey("XP") && result.Data.ContainsKey("Crystals"))
                {
                    currentXp = int.Parse(result.Data["XP"].Value);
                    crystals = int.Parse(result.Data["Crystals"].Value);
                    UpdateTopPanelUI();
                }
                else
                {
                    // ДАННЫХ НЕТ: Это новый игрок! Выдаем стартовый капитал.
                    SaveInitialDataForNewPlayer();
                }
            },
            error => Debug.LogError("Ошибка получения данных игрока: " + error.ErrorMessage)
        );
    }

    // 3. Создаем начальные данные для новичка и сохраняем их в базу
    private void SaveInitialDataForNewPlayer()
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "XP", "0" },
                { "Crystals", "1000" } // Выдаем 500 кристаллов новичку!
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result =>
            {
                Debug.Log("Новому игроку успешно выдан стартовый капитал!");
                currentXp = 0;
                crystals = 1000;
                UpdateTopPanelUI();
            },
            error => Debug.LogError("Ошибка сохранения начальных данных: " + error.ErrorMessage)
        );
    }

    // 4. Отрисовываем всё на экране (вычисляем звание)
    private void UpdateTopPanelUI()
    {
        // Обновляем текст кристаллов
        if (crystalsText != null) crystalsText.text = crystals.ToString();

        // Вычисляем текущее звание на основе опыта
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

        if (currentRankIndex < rankXpThresholds.Length - 1)
        {
            nextRankXp = rankXpThresholds[currentRankIndex + 1];
        }
        else
        {
            nextRankXp = currentXp; // Достигнуто максимальное звание
        }

        // Обновляем текст
        if (xpAndRankText != null)
        {
            xpAndRankText.text = $"{currentXp} / {nextRankXp} {currentRankName} {playerName}";
        }

        if (rankIconImage != null && rankIcons != null && rankIcons.Length > 0)
        {
            // Берем индекс текущего звания. Если иконок в массиве меньше, чем званий, 
            // берем последнюю доступную иконку, чтобы избежать ошибки
            int iconIndex = Mathf.Clamp(currentRankIndex, 0, rankIcons.Length - 1);

            if (rankIcons[iconIndex] != null)
            {
                rankIconImage.sprite = rankIcons[iconIndex];
            }
        }


        // Обновляем заполнение полоски прогресса
        if (xpProgressBar != null)
        {
            if (currentRankIndex >= rankXpThresholds.Length - 1)
            {
                xpProgressBar.value = 1f;
            }
            else
            {
                float xpIntoCurrentRank = currentXp - currentRankBaseXp;
                float xpNeededForNextRank = nextRankXp - currentRankBaseXp;
                // Slider по умолчанию работает от 0 до 1, поэтому передаем процент (доли единицы)
                xpProgressBar.value = xpIntoCurrentRank / xpNeededForNextRank;
            }
        }
    }

    // Этот метод вызывается менеджером предметов при покупке
    public bool TrySpendCrystals(int amount)
    {
        if (crystals >= amount)
        {
            // Списываем локально
            crystals -= amount;
            UpdateTopPanelUI();

            // Сохраняем новые данные в базу PlayFab
            var request = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>
                {
                    { "Crystals", crystals.ToString() }
                }
            };

            PlayFabClientAPI.UpdateUserData(request,
                res => Debug.Log($"Успешное списание в PlayFab! Осталось кристаллов: {crystals}"),
                err => Debug.LogError("Ошибка синхронизации баланса: " + err.ErrorMessage)
            );

            return true;
        }

        // Денег не хватило
        return false;
    }

    // ==========================================
    // МЕТОДЫ ДЛЯ КНОПОК УПРАВЛЕНИЯ
    // ==========================================
    public void ToggleMainGarageUI()
    {
        isMainUIOpen = !isMainUIOpen;

        // Включаем или выключаем основные элементы
        if (mainUIElement != null)
        {
            mainUIElement.SetActive(isMainUIOpen);
        }

        // Говорим камере плавно сменить ракурс
        if (garageCameraScript != null)
        {
            garageCameraScript.SetUIState(isMainUIOpen);
        }

        if (itemsManager != null)
        {
            itemsManager.OnUIStateChanged(isMainUIOpen);
        }
    }

    // Кнопка: Настройки (показать/скрыть панель)
    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
        {
            // Если панель была включена, она выключится, и наоборот
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }

    // Кнопка: Включение/выключение звука
    public void ToggleMute()
    {
        isMuted = !isMuted;
        // AudioListener.volume управляет глобальной громкостью в Unity
        // 0f = полная тишина, 1f = 100% громкости
        AudioListener.volume = isMuted ? 0f : 1f;

        // Меняем иконку на кнопке
        if (muteButtonImage != null && soundOnSprite != null && soundOffSprite != null)
        {
            muteButtonImage.sprite = isMuted ? soundOffSprite : soundOnSprite;
        }
    }

    public void QuitGame()
    {
        Debug.Log("Выход из игры...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif

    }
    private void OnApplicationQuit()
    {
        if (InstanceFinder.NetworkManager != null)
        {
            if (InstanceFinder.ServerManager != null)
            {
                InstanceFinder.ServerManager.StopConnection(true);
            }

            if (InstanceFinder.ClientManager != null)
            {
                InstanceFinder.ClientManager.StopConnection();
            }

            Debug.Log("Сетевые соединения успешно закрыты. Выходим...");
        }
    }
}