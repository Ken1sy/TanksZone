using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// Режимы игры
public enum GameMode
{
    DM = 0,   // Каждый сам за себя
    TDM = 1,  // Командный бой
    CTF = 2,  // Захват флага
    CP = 3    // Захват точек
}

// Конфигурация отдельной карты в базе
[System.Serializable]
public class MapInfo
{
    public string mapId;           // Уникальный ID (например, "sandbox")
    public string mapName;         // Название для интерфейса ("Песочница")
    public Sprite previewImage;    // Картинка превью (черный экран сверху)
    public int maxPlayers = 16;    // Максимальное количество игроков, которое тянет эта карта
}

// Итоговая конфигурация битвы (её мы будем отправлять на сервер FishNet)
[System.Serializable]
public struct BattleConfig
{
    public string battleName;
    public string mapId;
    public GameMode gameMode;
    public int maxPlayers;
    public int timeLimitMinutes;
    public int scoreLimit;
}

public class BattleCreationManager : MonoBehaviour
{
    [Header("База Карт")]
    public List<MapInfo> mapsDatabase;

    [Header("UI: Основные поля")]
    public TMP_InputField battleNameInput;       // Название битвы
    public Image mapPreviewImage;                // Картинка карты
    public TMP_Dropdown mapDropdown;             // Список карт
    public TMP_Dropdown gameModeDropdown;        // Список режимов

    [Header("UI: Поля ввода чисел (Input Fields)")]
    public TMP_InputField playersInput;          // Поле ввода количества игроков
    public TMP_InputField timeInput;             // Поле ввода времени
    public TMP_InputField scoreInput;            // Поле ввода счета
    public TMP_Text scoreLabelText;              // Текст подписи (Фраги/Флаги/Очки)

    [Header("UI: Кнопки")]
    public Button createBattleButton;

    // Текущие выбранные настройки
    private MapInfo selectedMap;

    private void Start()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        // 1. Настройка списка карт
        if (mapDropdown != null)
        {
            mapDropdown.ClearOptions();
            List<string> mapNames = new List<string>();
            foreach (var map in mapsDatabase)
            {
                mapNames.Add(map.mapName);
            }
            mapDropdown.AddOptions(mapNames);
            mapDropdown.onValueChanged.AddListener(OnMapSelected);
        }

        // 2. Настройка списка режимов
        if (gameModeDropdown != null)
        {
            gameModeDropdown.ClearOptions();
            gameModeDropdown.AddOptions(new List<string> { "DM", "TDM", "CTF", "CP" });
            gameModeDropdown.onValueChanged.AddListener(OnGameModeSelected);
        }

        // 3. Настройка полей ввода чисел
        if (playersInput != null)
        {
            playersInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            playersInput.onEndEdit.AddListener(ValidatePlayersInput);
        }

        if (timeInput != null)
        {
            timeInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            timeInput.onEndEdit.AddListener(ValidateTimeInput);
        }

        if (scoreInput != null)
        {
            scoreInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            scoreInput.onEndEdit.AddListener(ValidateScoreInput);
        }

        if (createBattleButton != null)
        {
            createBattleButton.onClick.AddListener(OnCreateBattleClicked);
        }

        // Применяем начальные значения (выбираем первую карту и первый режим)
        if (mapsDatabase.Count > 0) OnMapSelected(0);
        OnGameModeSelected(0);

        // Ставим дефолтные значения в поля ввода
        if (playersInput != null) playersInput.text = "8";
        if (timeInput != null) timeInput.text = "15";
        if (scoreInput != null) scoreInput.text = "0";

        // Дефолтное название комнаты
        if (battleNameInput != null)
        {
            battleNameInput.text = "Новая битва";
        }
    }

    // ==========================================
    // СОБЫТИЯ UI
    // ==========================================

    private void OnMapSelected(int index)
    {
        if (index < 0 || index >= mapsDatabase.Count) return;

        selectedMap = mapsDatabase[index];

        // Меняем картинку превью
        if (mapPreviewImage != null && selectedMap.previewImage != null)
        {
            mapPreviewImage.sprite = selectedMap.previewImage;
        }

        // Если текущее вписанное количество игроков больше, чем тянет новая карта - убавляем
        if (playersInput != null)
        {
            ValidatePlayersInput(playersInput.text);
        }
    }

    private void OnGameModeSelected(int index)
    {
        GameMode mode = (GameMode)index;

        // Меняем подпись к счету в зависимости от режима
        if (scoreLabelText != null)
        {
            switch (mode)
            {
                case GameMode.DM:
                case GameMode.TDM:
                    scoreLabelText.text = "Фраги";
                    break;
                case GameMode.CTF:
                    scoreLabelText.text = "Флаги";
                    break;
                case GameMode.CP:
                    scoreLabelText.text = "Очки";
                    break;
            }
        }
    }

    // ==========================================
    // ВАЛИДАЦИЯ ВВОДА (ОГРАНИЧИТЕЛИ)
    // ==========================================

    private void ValidatePlayersInput(string value)
    {
        if (int.TryParse(value, out int result))
        {
            int max = selectedMap != null ? selectedMap.maxPlayers : 16;
            if (result > max) result = max;
            if (result < 2) result = 2; // Минимум 2 игрока
            playersInput.text = result.ToString();
        }
        else
        {
            playersInput.text = "2";
        }
    }

    private void ValidateTimeInput(string value)
    {
        if (int.TryParse(value, out int result))
        {
            if (result > 60) result = 60; // Максимум 60 минут
            if (result < 0) result = 0;
            timeInput.text = result.ToString();
        }
        else
        {
            timeInput.text = "15";
        }
    }

    private void ValidateScoreInput(string value)
    {
        if (int.TryParse(value, out int result))
        {
            if (result > 999) result = 999; // Максимум 999 очков
            if (result < 0) result = 0;
            scoreInput.text = result.ToString();
        }
        else
        {
            scoreInput.text = "0";
        }
    }

    // ==========================================
    // СОЗДАНИЕ КОНФИГУРАЦИИ
    // ==========================================

    private void OnCreateBattleClicked()
    {
        int timeLimit = 0;
        int scoreLimit = 0;
        int maxPlayers = 8;

        if (timeInput != null) int.TryParse(timeInput.text, out timeLimit);
        if (scoreInput != null) int.TryParse(scoreInput.text, out scoreLimit);
        if (playersInput != null) int.TryParse(playersInput.text, out maxPlayers);

        // Проверка: мы не можем запустить бой, если и время и счет равны 0 (бой будет бесконечным)
        if (timeLimit <= 0 && scoreLimit <= 0)
        {
            Debug.LogWarning("Укажите лимит времени или лимит счета!");
            return;
        }

        // Проверка названия
        string finalBattleName = battleNameInput.text.Trim();
        if (string.IsNullOrEmpty(finalBattleName))
        {
            finalBattleName = "Без названия";
        }

        BattleConfig newConfig = new BattleConfig
        {
            battleName = finalBattleName,
            mapId = selectedMap.mapId, // ВНИМАНИЕ: это должно быть точное имя сцены, например "03_BattleScene_Sandbox"
            gameMode = (GameMode)gameModeDropdown.value,
            maxPlayers = maxPlayers,
            timeLimitMinutes = timeLimit,
            scoreLimit = scoreLimit
        };

        // НОВОЕ: Отправляем запрос на сервер!
        if (ServerRoomManager.Instance != null)
        {
            ServerRoomManager.Instance.RequestCreateRoom(newConfig);
            Debug.Log("Запрос на создание комнаты отправлен серверу...");

            // Здесь можно добавить скрытие панели создания битвы:
            // gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("ServerRoomManager не найден на сцене! Вы подключены к серверу FishNet?");
        }
    }
}