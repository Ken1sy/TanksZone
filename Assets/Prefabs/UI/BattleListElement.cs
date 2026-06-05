using UnityEngine;
using UnityEngine.UI;
using System;

public class BattleListElement : MonoBehaviour
{
    [Header("UI Ёлементы плашки")]
    public Text battleNameText;
    public Text mapNameText;
    public Text scoreText;
    public Image modeIconImage;

    [Header("—прайты режимов")]
    public Sprite iconDM;
    public Sprite iconTDM;
    public Sprite iconCTF;
    public Sprite iconCP;

    [Header(" нопка/‘он")]
    public Button selectButton;
    public GameObject selectedHighlight;

    private RoomData myRoomData;
    private Action<RoomData> onSelectedCallback;

    public void Setup(RoomData data, Action<RoomData> callback)
    {
        myRoomData = data;
        onSelectedCallback = callback;

        if (battleNameText != null) battleNameText.text = data.config.battleName;
        if (mapNameText != null) mapNameText.text = data.config.mapId;

        if (scoreText != null)
        {
            scoreText.text = $"{data.currentPlayers}";
        }

        if (modeIconImage != null)
        {
            switch (data.config.gameMode)
            {
                case GameMode.DM: modeIconImage.sprite = iconDM; break;
                case GameMode.TDM: modeIconImage.sprite = iconTDM; break;
                case GameMode.CTF: modeIconImage.sprite = iconCTF; break;
                case GameMode.CP: modeIconImage.sprite = iconCP; break;
            }
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnClicked);
        }

        SetHighlight(false);
    }

    private void OnClicked()
    {
        onSelectedCallback?.Invoke(myRoomData);
    }

    public void SetHighlight(bool isSelected)
    {
        if (selectedHighlight != null)
        {
            selectedHighlight.SetActive(isSelected);
        }
    }

    public int GetRoomId()
    {
        return myRoomData.roomId;
    }
}