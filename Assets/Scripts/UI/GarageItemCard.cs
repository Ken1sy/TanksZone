using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GarageItemCard : MonoBehaviour
{
    [Header("UI Элементы Карточки")]
    public Text ItemNameText;
    public Image ItemImage;
    public Text ItemPriceText;
    public Text ItemCountText;
    [Header("Панели")]
    public GameObject ItemPricePanel;
    [Header("Блокировка и Фокус")]
    public Image RankAccessLock;
    [Header("Состояния Фона")]
    public GameObject bgAccessible;
    public GameObject bgAccessibleSelected;
    public GameObject bgEquipped;
    public GameObject bgEquippedSelected;
    public GameObject bgUnaccessible;
    public GameObject bgUnaccessibleSelected;
    [Header("Рамки / Оверлеи")]
    public GameObject equippedBorder;
    [Header("События")]
    public UnityEvent OnCardClicked;

    private string itemID;
    private bool isOwned;
    private bool isEquipped;
    private bool isLocked;

    public void SetupCard(string id, string name, Sprite icon,
        int price, bool owned, bool equipped, bool isLockedByRank, Sprite lockSprite = null, int itemCount = -1)
    {
        itemID = id;
        isOwned = owned;
        isEquipped = equipped;
        isLocked = isLockedByRank;
        if (ItemNameText != null) ItemNameText.text = name;
        if (ItemImage != null) ItemImage.sprite = icon;
        if (isOwned) { if (ItemPricePanel != null) ItemPricePanel.SetActive(false); }
        else
        {
            if (ItemPricePanel != null) ItemPricePanel.SetActive(true);
            if (ItemPriceText != null) ItemPriceText.text = price.ToString();
        }

        if (ItemCountText != null)
        {
            if (itemCount > 0)
            {
                ItemCountText.gameObject.SetActive(true);
                ItemCountText.text = "x" + itemCount.ToString();
            }
            else { ItemCountText.gameObject.SetActive(false); }
        }
        if (RankAccessLock != null)
        {
            RankAccessLock.gameObject.SetActive(isLocked);
            if (isLocked && lockSprite != null) { RankAccessLock.sprite = lockSprite; }
        }
        UpdateBackgroundState(false);
    }
    public void SetSelected(bool isSelected) { UpdateBackgroundState(isSelected); }
    public void SetEquipped(bool equipped)
    {
        isEquipped = equipped;
        bool isCurrentlySelected = false;
        if (bgAccessibleSelected != null && bgAccessibleSelected.activeSelf) isCurrentlySelected = true;
        if (bgEquippedSelected != null && bgEquippedSelected.activeSelf) isCurrentlySelected = true;
        if (bgUnaccessibleSelected != null && bgUnaccessibleSelected.activeSelf) isCurrentlySelected = true;
        UpdateBackgroundState(isCurrentlySelected);
    }
    private void UpdateBackgroundState(bool isSelected)
    {
        if (bgAccessible != null) bgAccessible.SetActive(false);
        if (bgAccessibleSelected != null) bgAccessibleSelected.SetActive(false);
        if (bgEquipped != null) bgEquipped.SetActive(false);
        if (bgEquippedSelected != null) bgEquippedSelected.SetActive(false);
        if (bgUnaccessible != null) bgUnaccessible.SetActive(false);
        if (bgUnaccessibleSelected != null) bgUnaccessibleSelected.SetActive(false);
        if (isLocked)
        {
            if (isSelected) { if (bgUnaccessibleSelected != null) bgUnaccessibleSelected.SetActive(true); }
            else { if (bgUnaccessible != null) bgUnaccessible.SetActive(true); }
        }
        else if (isEquipped)
        {
            if (isSelected) { if (bgEquippedSelected != null) bgEquippedSelected.SetActive(true); }
            else { if (bgEquipped != null) bgEquipped.SetActive(true); }
        }
        else
        {
            if (isSelected) { if (bgAccessibleSelected != null) bgAccessibleSelected.SetActive(true); }
            else { if (bgAccessible != null) bgAccessible.SetActive(true); }
        }
        if (equippedBorder != null) { equippedBorder.SetActive(isEquipped); }
    }
    public void Click() { OnCardClicked?.Invoke(); }
}