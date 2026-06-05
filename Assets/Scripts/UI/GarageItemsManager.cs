using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;

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
    public string defaultHullID = "hull_viking";
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

    // Таблица опыта для вычисления ранга прямо в гараже
    private readonly int[] rankXpThresholds = {
        0, 100, 500, 1500, 3700, 7100, 12300, 20000, 29000, 41000,
        57000, 76000, 98000, 125000, 156000, 192000, 233000, 280000,
        332000, 390000, 455000, 527000, 606000, 692000, 787000, 889000,
        1000000, 1122000, 1255000, 1400000, 1600000
    };

    private void Start()
    {
        currentPlayerRankIndex = 0;
        LoadInventoryFromPlayFab();
    }

    // ==========================================
    // ОБЛАЧНЫЙ ИНВЕНТАРЬ (PLAYFAB)
    // ==========================================

    private void ApplyDefaultItemsLocally()
    {
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

                if (result.Data.ContainsKey("XP"))
                {
                    int currentXp = int.Parse(result.Data["XP"].Value);
                    currentPlayerRankIndex = 0;

                    for (int i = rankXpThresholds.Length - 1; i >= 0; i--)
                    {
                        if (currentXp >= rankXpThresholds[i])
                        {
                            currentPlayerRankIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    currentPlayerRankIndex = 0;
                }

                string savedTurret = result.Data.ContainsKey("Equipped_Turret") ? result.Data["Equipped_Turret"].Value : defaultTurretID;
                string savedHull = result.Data.ContainsKey("Equipped_Hull") ? result.Data["Equipped_Hull"].Value : defaultHullID;
                string savedPaint = result.Data.ContainsKey("Equipped_Paint") ? result.Data["Equipped_Paint"].Value : defaultPaintID;

                foreach (var item in itemsDatabase)
                {
                    if (item.itemID == defaultTurretID || item.itemID == defaultHullID || item.itemID == defaultPaintID)
                    {
                        item.isOwned = true;
                    }
                    else
                    {
                        item.isOwned = result.Data.ContainsKey("Owned_" + item.itemID);
                    }

                    if (item.category == ItemCategory.Turret) item.isEquipped = (item.itemID == savedTurret);
                    else if (item.category == ItemCategory.Hull) item.isEquipped = (item.itemID == savedHull);
                    else if (item.category == ItemCategory.Paint) item.isEquipped = (item.itemID == savedPaint);
                }

                selectedCard = null;
                selectedItemData = null;

                UpdateEquippedCache();
                PopulateGarage(false);
                RebuildTankPreview(null);

                Debug.Log($"Инвентарь и ранг ({currentPlayerRankIndex}) успешно загружены из облака PlayFab!");
            },
            error =>
            {
                Debug.LogError("Ошибка загрузки инвентаря с сервера: " + error.ErrorMessage);

                ApplyDefaultItemsLocally();
                UpdateEquippedCache();
                PopulateGarage(false);
                RebuildTankPreview(null);
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
                // ИСПРАВЛЕНИЕ: Теперь мы записываем надетые вещи в глобальные переменные для переноса в бой
                if (item.category == ItemCategory.Hull)
                {
                    equippedHullPrefab = item.item3DModel;
                    TankSetupData.SelectedHullID = item.itemID;
                }
                else if (item.category == ItemCategory.Turret)
                {
                    equippedTurretPrefab = item.item3DModel;
                    TankSetupData.SelectedTurretID = item.itemID;
                }
                else if (item.category == ItemCategory.Paint)
                {
                    equippedSkinId = item.paintSkinId;
                    TankSetupData.SelectedSkinID = item.paintSkinId;
                }
            }
        }
    }



    private Transform FindMountPoint(Transform parent)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in children)
        {
            if (t.name.ToLower() == "mount") return t;
        }
        return null;
    }

    private void RebuildTankPreview(GarageItemInfo previewItem)
    {
        if (hullSpawnPoint == null)
        {
            GameObject anchor = GameObject.Find("TankAnchor");
            if (anchor != null) hullSpawnPoint = anchor.transform;
        }

        if (previewHull != null)
        {
            previewHull.SetActive(false);
            Destroy(previewHull);
        }

        if (hullSpawnPoint == null)
        {
            Debug.LogWarning("Точка спавна танка не найдена! Назовите пустой объект в Гараже 'TankAnchor'.");
            return;
        }

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

            Transform mount = FindMountPoint(previewHull.transform);

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
            foreach (Renderer r in previewHull.GetComponentsInChildren<Renderer>(true))
            {
                ApplyToRenderer(r, config);
            }
        }

        if (previewTurret != null)
        {
            foreach (Renderer r in previewTurret.GetComponentsInChildren<Renderer>(true))
            {
                ApplyToRenderer(r, config);
            }
        }
    }

    private void ApplyToRenderer(Renderer rend, TankSkinConfig config)
    {
        if (rend == null) return;
        if (rend is ParticleSystemRenderer) return;

        float size = 5f;
        if (rend is MeshRenderer && rend.GetComponent<MeshFilter>() != null && rend.GetComponent<MeshFilter>().sharedMesh != null)
        {
            Vector3 boundsSize = rend.GetComponent<MeshFilter>().sharedMesh.bounds.size;
            size = Mathf.Max(boundsSize.x, boundsSize.z);
        }
        else if (rend is SkinnedMeshRenderer smr && smr.sharedMesh != null)
        {
            Vector3 boundsSize = smr.sharedMesh.bounds.size;
            size = Mathf.Max(boundsSize.x, boundsSize.z);
        }
        else
        {
            size = Mathf.Max(rend.bounds.size.x, rend.bounds.size.z);
        }

        if (size <= 0.01f) size = 5f;

        Vector2 tiling = new Vector2(config.baseTiling * size, config.baseTiling * size);

        foreach (Material mat in rend.materials)
        {
            UpdateMaterial(mat, config, tiling);
        }
    }

    private void UpdateMaterial(Material mat, TankSkinConfig config, Vector2 tiling)
    {
        if (mat == null) return;
        if (mat.HasProperty("_SkinTexture")) mat.SetTexture("_SkinTexture", config.skinTexture);
        if (mat.HasProperty("_SkinTiling")) mat.SetVector("_SkinTiling", tiling);
        if (mat.HasProperty("_SkinGridSize")) mat.SetVector("_SkinGridSize", config.gridSize);
        if (mat.HasProperty("_SkinAnimSpeed")) mat.SetFloat("_SkinAnimSpeed", config.animationSpeed);
        if (mat.HasProperty("_SkinTotalFrames")) mat.SetFloat("_SkinTotalFrames", config.totalFrames);
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

        SaveEquippedItemToCloud(selectedItemData);
        UpdateEquippedCache();
        PopulateGarage(true);
    }
}