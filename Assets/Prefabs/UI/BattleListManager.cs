using UnityEngine;
using UnityEngine.UI;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using TMPro;
using System.Linq; // НОВОЕ: Для сортировки массива игроков по фрагам

public class BattleListManager : MonoBehaviour
{
    [Header("Настройки Списка Битв")]
    public GameObject battleCellPrefab;
    public Transform contentContainer;

    [Header("Правая Панель (Информация)")]
    public TMP_Text selectedMapNameText;
    public TMP_Text selectedTimeText;
    public TMP_Text selectedModeText;
    public Button joinButton;

    [Header("Список Игроков")]
    public GameObject playerCellPrefab;
    public Transform playersContentContainer;
    public Sprite[] rankIcons;

    private bool isSubscribedToServer = false;
    private RoomData currentSelectedRoom;
    private List<BattleListElement> spawnedElements = new List<BattleListElement>();

    private void Start()
    {
        ClearSelectionPanel();
        if (joinButton != null)
        {
            joinButton.interactable = false;
            joinButton.onClick.AddListener(OnJoinButtonClicked);
        }
    }

    private void Update()
    {
        if (!isSubscribedToServer && ServerRoomManager.Instance != null)
        {
            ServerRoomManager.Instance.activeRooms.OnChange += OnActiveRoomsChanged;
            isSubscribedToServer = true;
            RefreshEntireList();
        }
    }

    private void OnDestroy()
    {
        if (isSubscribedToServer && ServerRoomManager.Instance != null)
        {
            ServerRoomManager.Instance.activeRooms.OnChange -= OnActiveRoomsChanged;
        }
    }

    private void OnActiveRoomsChanged(SyncListOperation op, int index, RoomData oldItem, RoomData newItem, bool asServer)
    {
        RefreshEntireList();
    }

    private void RefreshEntireList()
    {
        foreach (Transform child in contentContainer) Destroy(child.gameObject);
        spawnedElements.Clear();

        if (ServerRoomManager.Instance == null) return;

        foreach (RoomData room in ServerRoomManager.Instance.activeRooms)
        {
            GameObject newCell = Instantiate(battleCellPrefab, contentContainer);
            BattleListElement elementScript = newCell.GetComponentInChildren<BattleListElement>();

            if (elementScript != null)
            {
                elementScript.Setup(room, OnRoomSelected);
                spawnedElements.Add(elementScript);

                if (currentSelectedRoom.roomId == room.roomId)
                {
                    elementScript.SetHighlight(true);
                    OnRoomSelected(room);
                }
            }
        }

        bool stillExists = false;
        foreach (var room in ServerRoomManager.Instance.activeRooms)
        {
            if (room.roomId == currentSelectedRoom.roomId) stillExists = true;
        }

        if (!stillExists) ClearSelectionPanel();
    }

    private void OnRoomSelected(RoomData data)
    {
        currentSelectedRoom = data;

        foreach (var element in spawnedElements)
        {
            element.SetHighlight(element.GetRoomId() == data.roomId);
        }

        if (selectedMapNameText != null) selectedMapNameText.text = data.config.mapId;
        if (selectedModeText != null) selectedModeText.text = data.config.gameMode.ToString();

        if (selectedTimeText != null)
        {
            selectedTimeText.text = data.config.timeLimitMinutes > 0 ? $"{data.config.timeLimitMinutes} мин." : "Безлимит";
        }

        if (joinButton != null)
        {
            joinButton.interactable = (data.currentPlayers < data.config.maxPlayers);
        }

        // Обновляем список игроков справа
        UpdatePlayerList(data);
    }

    private void ClearSelectionPanel()
    {
        currentSelectedRoom = default;

        if (selectedMapNameText != null) selectedMapNameText.text = "Выберите битву";
        if (selectedModeText != null) selectedModeText.text = "-";
        if (selectedTimeText != null) selectedTimeText.text = "-";

        if (joinButton != null) joinButton.interactable = false;

        if (playersContentContainer != null)
        {
            foreach (Transform child in playersContentContainer) Destroy(child.gameObject);
        }
    }

    private void UpdatePlayerList(RoomData data)
    {
        if (playersContentContainer == null || playerCellPrefab == null) return;

        // Очищаем старые плашки
        foreach (Transform child in playersContentContainer) Destroy(child.gameObject);

        // ИСПРАВЛЕНИЕ: Используем РЕАЛЬНЫЕ данные сервера
        if (data.players != null && data.players.Length > 0)
        {
            // Умная сортировка (LINQ): у кого больше убийств, тот и будет выше в списке!
            var sortedPlayers = data.players.OrderByDescending(p => p.kills).ToList();

            foreach (var player in sortedPlayers)
            {
                GameObject newCell = Instantiate(playerCellPrefab, playersContentContainer);
                BattlePlayerListElement elementScript = newCell.GetComponentInChildren<BattlePlayerListElement>();

                if (elementScript != null)
                {
                    elementScript.Setup(player.playerName, player.kills, player.rankIndex, rankIcons);
                }
            }
        }
    }

    private void OnJoinButtonClicked()
    {
        if (currentSelectedRoom.roomId != 0 && ServerRoomManager.Instance != null)
        {
            // Здесь метод теперь сам подтянет ник и ранг из PlayerPrefs
            ServerRoomManager.Instance.RequestJoinRoom(currentSelectedRoom.roomId);
        }
    }
}