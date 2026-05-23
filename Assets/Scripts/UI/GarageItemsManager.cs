using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using PlayFab; // НОВОЕ: Подключаем PlayFab
using PlayFab.ClientModels; // НОВОЕ: Подключаем модели данных PlayFab

public enum ItemCategory
{
    Turret = 0,   // Пушки
    Hull = 1,     // Корпуса
    Paint = 2,    // Краски
    Supply = 3,   // Припасы
    Special = 4   // Специальное
}

[System.Serializable]
public class GarageItemInfo
{
    public string itemID;
    public string itemName;
    public Sprite itemIcon;
    public int price;
    public bool isOwned;
    public bool isEquipped;
    public int requiredRankIndex;
    public int itemCount = -1;

    [Header("Блокировка")]
    public Sprite rankLockSprite;

    [Header("Категория")]
    public ItemCategory category;

    [Header("Информация (Правая панель)")]
    [TextArea(3, 5)]
    public string itemDescription;

    [Header("3D Сцена")]
    public GameObject item3DModel;
    public string paintSkinId;
}

public class GarageItemsManager : MonoBehaviour
{
    [Header("СТАРТОВЫЙ НАБОР (Для новых игроков)")]
    public string defaultTurretID = "turret_smoky";
    public string defaultHullID = "hull_hunter";
    public string defaultPaintID = "paint_green";

    [Header("Настройки UI (Список)")]
    public GameObject itemCardPrefab;
    public Transform itemsContainer;

    [Header("Настройки UI (Правая Панель)")]
    public TMP_Text selectedItemNameText;
    public TMP_Text selectedItemDescriptionText;
    public Button buyButton;
    public Button equipButton;

    [Header("Настройки 3D Сцены")]
    public Transform hullSpawnPoint;
    public float tankYOffset = 0.0f;

    private GameObject previewHull;
    private GameObject previewTurret;

    private GameObject equippedHullPrefab;
    private GameObject equippedTurretPrefab;
    private string equippedSkinId;

    [Header("База предметов")]
    public List<GarageItemInfo> itemsDatabase;

    [Header("Иконка блокировки по умолчанию")]
    public Sprite defaultLockSprite;

    private int currentPlayerRankIndex = 0;
    private GarageItemCard selectedCard;
    private GarageItemInfo selectedItemData;

    private ItemCategory currentCategory = ItemCategory.Turret;

    private void Start()
    {
        currentPlayerRankIndex = 0; // Временно "Ефрейтор"

        // 1. Устанавливаем дефолтный (стартовый) танк, пока данные грузятся с сервера
        ApplyDefaultItemsLocally();
        UpdateEquippedCache();
        PopulateGarage(false);
        RebuildTankPreview(null);

        // 2. АСИНХРОННО запрашиваем реальный инвентарь из облака PlayFab!
        LoadInventoryFromPlayFab();
    }

    // ==========================================
    // ОБЛАЧНЫЙ ИНВЕНТАРЬ (PLAYFAB)
    // ==========================================

    private void ApplyDefaultItemsLocally()
    {
        // Временно снимаем всё и надеваем только стартовый набор
        foreach (var item in itemsDatabase)
        {
            if (item.itemID == defaultTurretID || item.itemID == defaultHullID || item.itemID == defaultPaintID)
            {
                item.isOwned = true;
                item.isEquipped = true;
            }
            else
            {
                item.isOwned = false;
                item.isEquipped = false;
            }
        }
    }

    private void LoadInventoryFromPlayFab()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result =>
            {
                if (result.Data == null) return;

                // 1. Получаем ID надетых предметов (или берем стандартные, если пусто)
                string savedTurret = result.Data.ContainsKey("Equipped_Turret") ? result.Data["Equipped_Turret"].Value : defaultTurretID;
                string savedHull = result.Data.ContainsKey("Equipped_Hull") ? result.Data["Equipped_Hull"].Value : defaultHullID;
                string savedPaint = result.Data.ContainsKey("Equipped_Paint") ? result.Data["Equipped_Paint"].Value : defaultPaintID;

                foreach (var item in itemsDatabase)
                {
                    // 2. Проверяем покупки (если ключа "Owned_..." нет, значит предмет не куплен)
                    if (item.itemID == defaultTurretID || item.itemID == defaultHullID || item.itemID == defaultPaintID)
                    {
                        item.isOwned = true;
                    }
                    else
                    {
                        item.isOwned = result.Data.ContainsKey("Owned_" + item.itemID);
                    }

                    // 3. Проверяем экипировку
                    if (item.category == ItemCategory.Turret) item.isEquipped = (item.itemID == savedTurret);
                    else if (item.category == ItemCategory.Hull) item.isEquipped = (item.itemID == savedHull);
                    else if (item.category == ItemCategory.Paint) item.isEquipped = (item.itemID == savedPaint);
                }

                // 4. Обновляем гараж реальными данными с сервера
                UpdateEquippedCache();
                PopulateGarage(false);
                RebuildTankPreview(null);

                Debug.Log("Инвентарь успешно загружен из облака PlayFab!");
            },
            error =>
            {
                Debug.LogError("Ошибка загрузки инвентаря с сервера: " + error.ErrorMessage);
            });
    }

    private void SaveEquippedItemToCloud(GarageItemInfo item)
    {
        string key = "";
        if (item.category == ItemCategory.Turret) key = "Equipped_Turret";
        else if (item.category == ItemCategory.Hull) key = "Equipped_Hull";
        else if (item.category == ItemCategory.Paint) key = "Equipped_Paint";

        if (string.IsNullOrEmpty(key)) return;

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { key, item.itemID }
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            res => Debug.Log($"Экипировка [{item.itemName}] сохранена на сервере!"),
            err => Debug.LogError("Ошибка сохранения на сервер: " + err.ErrorMessage)
        );
    }


    // ==========================================
    // ЛОГИКА 3D СЦЕНЫ (СБОРКА ТАНКА)
    // ==========================================

    private void UpdateEquippedCache()
    {
        equippedHullPrefab = null;
        equippedTurretPrefab = null;
        equippedSkinId = "";

        foreach (var item in itemsDatabase)
        {
            if (item.isEquipped)
            {
                if (item.category == ItemCategory.Hull) equippedHullPrefab = item.item3DModel;
                else if (item.category == ItemCategory.Turret) equippedTurretPrefab = item.item3DModel;
                else if (item.category == ItemCategory.Paint) equippedSkinId = item.paintSkinId;
            }
        }
    }

    private void RebuildTankPreview(GarageItemInfo previewItem)
    {
        if (previewHull != null) Destroy(previewHull);

        GameObject hullToSpawn = equippedHullPrefab;
        GameObject turretToSpawn = equippedTurretPrefab;
        string skinToApply = equippedSkinId;

        if (previewItem != null)
        {
            if (previewItem.category == ItemCategory.Hull) hullToSpawn = previewItem.item3DModel;
            else if (previewItem.category == ItemCategory.Turret) turretToSpawn = previewItem.item3DModel;
            else if (previewItem.category == ItemCategory.Paint) skinToApply = previewItem.paintSkinId;
        }

        if (hullToSpawn != null && hullSpawnPoint != null)
        {
            previewHull = Instantiate(hullToSpawn, hullSpawnPoint);
            previewHull.transform.localPosition = new Vector3(0f, tankYOffset, 0f);
            previewHull.transform.localRotation = Quaternion.identity;

            Transform mount = previewHull.transform.Find("mount");

            if (mount != null && turretToSpawn != null)
            {
                previewTurret = Instantiate(turretToSpawn, mount);
                previewTurret.transform.localPosition = Vector3.zero;
                previewTurret.transform.localRotation = Quaternion.identity;
            }

            ApplySkinToPreview(skinToApply);
        }
    }

    private void ApplySkinToPreview(string skinId)
    {
        if (string.IsNullOrEmpty(skinId)) return;

        string path = $"Colormap/{skinId}/{skinId}_Config";
        TankSkinConfig config = Resources.Load<TankSkinConfig>(path);

        if (config == null) return;

        if (previewHull != null)
        {
            Renderer hullRend = previewHull.GetComponent<Renderer>();
            if (hullRend != null)
            {
                float hullSize = Mathf.Max(hullRend.bounds.size.x, hullRend.bounds.size.z);
                Vector2 tiling = new Vector2(config.baseTiling * hullSize, config.baseTiling * hullSize);
                UpdateMaterial(hullRend.material, config, tiling);
            }
        }

        if (previewTurret != null)
        {
            Renderer turretRend = previewTurret.GetComponent<Renderer>();
            if (turretRend != null)
            {
                float turretSize = Mathf.Max(turretRend.bounds.size.x, turretRend.bounds.size.z);
                Vector2 tiling = new Vector2(config.baseTiling * turretSize, config.baseTiling * turretSize);
                UpdateMaterial(turretRend.material, config, tiling);
            }
        }
    }

    private void UpdateMaterial(Material mat, TankSkinConfig config, Vector2 tiling)
    {
        mat.SetTexture("_SkinTexture", config.skinTexture);
        mat.SetVector("_SkinTiling", tiling);
        mat.SetVector("_SkinGridSize", config.gridSize);
        mat.SetFloat("_SkinAnimSpeed", config.animationSpeed);
        mat.SetFloat("_SkinTotalFrames", config.totalFrames);
    }

    // ==========================================
    // СВЯЗЬ С ИНТЕРФЕЙСОМ И СПИСОК
    // ==========================================

    public void OnUIStateChanged(bool isUIOpen)
    {
        if (isUIOpen)
        {
            if (selectedItemData != null) RebuildTankPreview(selectedItemData);
        }
        else
        {
            RebuildTankPreview(null);
        }
    }

    public void SetCategory(int categoryIndex)
    {
        currentCategory = (ItemCategory)categoryIndex;
        selectedCard = null;
        selectedItemData = null;

        PopulateGarage(true);
    }

    private int GetItemSortWeight(GarageItemInfo item)
    {
        if (item.isEquipped) return 0;
        if (item.isOwned) return 1;

        bool isLocked = currentPlayerRankIndex < item.requiredRankIndex;
        if (!isLocked) return 2;

        return 3;
    }

    public void PopulateGarage(bool autoPreview = true)
    {
        foreach (Transform child in itemsContainer)
        {
            Destroy(child.gameObject);
        }

        GarageItemCard cardToSelect = null;
        GarageItemInfo dataToSelect = null;
        GarageItemCard firstCardInList = null;
        GarageItemInfo firstItemData = null;

        string previouslySelectedID = selectedItemData != null ? selectedItemData.itemID : "";

        List<GarageItemInfo> itemsToDisplay = new List<GarageItemInfo>();
        foreach (var item in itemsDatabase)
        {
            if (item.category == currentCategory)
            {
                itemsToDisplay.Add(item);
            }
        }

        itemsToDisplay.Sort((a, b) =>
        {
            int weightA = GetItemSortWeight(a);
            int weightB = GetItemSortWeight(b);

            if (weightA != weightB) return weightA.CompareTo(weightB);

            return a.price.CompareTo(b.price);
        });

        foreach (var item in itemsToDisplay)
        {
            GameObject newCardObj = Instantiate(itemCardPrefab, itemsContainer);
            GarageItemCard cardScript = newCardObj.GetComponent<GarageItemCard>();

            if (cardScript != null)
            {
                bool isLocked = currentPlayerRankIndex < item.requiredRankIndex;
                Sprite iconToUse = item.rankLockSprite != null ? item.rankLockSprite : defaultLockSprite;

                cardScript.SetupCard(
                    id: item.itemID,
                    name: item.itemName,
                    icon: item.itemIcon,
                    price: item.price,
                    owned: item.isOwned,
                    equipped: item.isEquipped,
                    isLockedByRank: isLocked,
                    lockSprite: iconToUse,
                    itemCount: item.itemCount
                );

                if (firstCardInList == null)
                {
                    firstCardInList = cardScript;
                    firstItemData = item;
                }

                if (item.itemID == previouslySelectedID)
                {
                    cardToSelect = cardScript;
                    dataToSelect = item;
                }

                cardScript.OnCardClicked.AddListener(() => OnCardSelected(cardScript, item));
            }
        }

        if (cardToSelect == null && firstCardInList != null)
        {
            cardToSelect = firstCardInList;
            dataToSelect = firstItemData;
        }

        if (cardToSelect != null)
        {
            OnCardSelected(cardToSelect, dataToSelect, autoPreview);
        }
        else
        {
            ClearRightPanel();
        }
    }

    private void OnCardSelected(GarageItemCard clickedCard, GarageItemInfo itemData, bool applyPreview = true)
    {
        if (selectedCard != null)
        {
            selectedCard.SetSelected(false);
        }

        selectedCard = clickedCard;
        selectedItemData = itemData;
        selectedCard.SetSelected(true);

        UpdateRightPanel(itemData);
        if (applyPreview) RebuildTankPreview(itemData);
    }

    // ==========================================
    // ЛОГИКА ПРАВОЙ ПАНЕЛИ И КНОПОК
    // ==========================================

    private void UpdateRightPanel(GarageItemInfo item)
    {
        if (selectedItemNameText != null) selectedItemNameText.text = item.itemName;
        if (selectedItemDescriptionText != null) selectedItemDescriptionText.text = item.itemDescription;

        bool isLockedByRank = currentPlayerRankIndex < item.requiredRankIndex;

        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(!item.isOwned);
            buyButton.interactable = !isLockedByRank;
        }

        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(item.isOwned);
            equipButton.interactable = !item.isEquipped;
        }
    }

    private void ClearRightPanel()
    {
        if (selectedItemNameText != null) selectedItemNameText.text = "";
        if (selectedItemDescriptionText != null) selectedItemDescriptionText.text = "";
        if (buyButton != null) buyButton.gameObject.SetActive(false);
        if (equipButton != null) equipButton.gameObject.SetActive(false);
    }

    public void OnBuyButtonClicked()
    {
        if (selectedItemData == null || selectedItemData.isOwned) return;

        GarageUIManager uiManager = FindAnyObjectByType<GarageUIManager>();

        if (uiManager != null)
        {
            if (uiManager.TrySpendCrystals(selectedItemData.price))
            {
                selectedItemData.isOwned = true;

                // ОБЛАКО: Сохраняем покупку прямо в PlayFab
                var request = new UpdateUserDataRequest
                {
                    Data = new Dictionary<string, string>
                    {
                        { "Owned_" + selectedItemData.itemID, "1" }
                    }
                };

                PlayFabClientAPI.UpdateUserData(request,
                    res => Debug.Log($"Предмет {selectedItemData.itemName} добавлен в инвентарь PlayFab!"),
                    err => Debug.LogError("Ошибка сохранения покупки: " + err.ErrorMessage)
                );

                Debug.Log($"<color=green>Поздравляем с покупкой: {selectedItemData.itemName}!</color>");
                PopulateGarage(true);
            }
            else
            {
                Debug.LogWarning("Недостаточно кристаллов для покупки!");
            }
        }
    }

    public void OnEquipButtonClicked()
    {
        if (selectedItemData == null) return;

        foreach (var item in itemsDatabase)
        {
            if (item.category == selectedItemData.category)
            {
                item.isEquipped = false;
            }
        }

        selectedItemData.isEquipped = true;

        // ОБЛАКО: Сохраняем экипировку
        SaveEquippedItemToCloud(selectedItemData);

        UpdateEquippedCache();
        PopulateGarage(true);
        UpdateRightPanel(selectedItemData);
        RebuildTankPreview(selectedItemData);
    }
}