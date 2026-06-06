using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GameMode { DM, TDM, CTF, CP }

[System.Serializable]
public class MapInfo
{
    public string mapId;
    public string mapName;
    public Sprite previewImage;
    public int maxPlayers = 16;
}

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
    public TMP_InputField battleNameInput;
    public Image mapPreviewImage;
    public TMP_Dropdown mapDropdown;
    public TMP_Dropdown gameModeDropdown;
    [Header("UI: Поля ввода чисел (Input Fields)")]
    public TMP_InputField playersInput;
    public TMP_InputField timeInput;
    public TMP_InputField scoreInput;
    public TMP_Text scoreLabelText;
    [Header("UI: Кнопки")]
    public Button createBattleButton;

    private MapInfo selectedMap;

    private void Start() { InitializeUI(); }

    private void InitializeUI()
    {
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
        if (gameModeDropdown != null)
        {
            gameModeDropdown.ClearOptions();
            gameModeDropdown.AddOptions(new List<string> { "DM", "TDM", "CTF", "CP" });
            gameModeDropdown.onValueChanged.AddListener(OnGameModeSelected);
        }
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
        if (mapsDatabase.Count > 0) OnMapSelected(0);
        OnGameModeSelected(0);
        if (playersInput != null) playersInput.text = "8";
        if (timeInput != null) timeInput.text = "15";
        if (scoreInput != null) scoreInput.text = "0";
        if (battleNameInput != null) { battleNameInput.text = "Новая битва"; }
    }

    private void OnMapSelected(int index)
    {
        if (index < 0 || index >= mapsDatabase.Count) return;
        selectedMap = mapsDatabase[index];
        if (mapPreviewImage != null && selectedMap.previewImage != null)
        {
            mapPreviewImage.sprite = selectedMap.previewImage;
        }
        if (playersInput != null) { ValidatePlayersInput(playersInput.text); }
    }

    private void OnGameModeSelected(int index)
    {
        GameMode mode = (GameMode)index;
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

    private void ValidatePlayersInput(string value)
    {
        if (int.TryParse(value, out int result))
        {
            int max = selectedMap != null ? selectedMap.maxPlayers : 16;
            if (result > max) result = max;
            if (result < 2) result = 2;
            playersInput.text = result.ToString();
        }
        else { playersInput.text = "2"; }
    }

    private void ValidateTimeInput(string value)
    {
        if (int.TryParse(value, out int result))
        {
            if (result > 60) result = 60;
            if (result < 0) result = 0;
            timeInput.text = result.ToString();
        }
        else { timeInput.text = "15"; }
    }

    private void ValidateScoreInput(string value)
    {
        if (int.TryParse(value, out int result))
        {
            if (result > 999) result = 999;
            if (result < 0) result = 0;
            scoreInput.text = result.ToString();
        }
        else { scoreInput.text = "0"; }
    }
    private void OnCreateBattleClicked()
    {
        int timeLimit = 0;
        int scoreLimit = 0;
        int maxPlayers = 8;
        if (timeInput != null) int.TryParse(timeInput.text, out timeLimit);
        if (scoreInput != null) int.TryParse(scoreInput.text, out scoreLimit);
        if (playersInput != null) int.TryParse(playersInput.text, out maxPlayers);
        if (timeLimit <= 0 && scoreLimit <= 0) return;
        string finalBattleName = battleNameInput.text.Trim();
        if (string.IsNullOrEmpty(finalBattleName)) finalBattleName = "Без названия";
        BattleConfig newConfig = new BattleConfig
        {
            battleName = finalBattleName,
            mapId = selectedMap.mapId,
            gameMode = (GameMode)gameModeDropdown.value,
            maxPlayers = maxPlayers,
            timeLimitMinutes = timeLimit,
            scoreLimit = scoreLimit
        };
        if (ServerRoomManager.Instance != null) ServerRoomManager.Instance.RequestCreateRoom(newConfig);
    }
}