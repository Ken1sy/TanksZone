using UnityEngine;
using System.Collections.Generic;

public class TankSkinSwitcher : MonoBehaviour
{
    [Header("Настройки Шейдера")]
    public string skinTexturePropertyName = "_SkinTexture";
    public string skinTilingPropertyName = "_SkinTiling";

    private List<Material> tankMaterials = new List<Material>();

    private TankSkinConfig[] allSkinConfigs;
    private int currentIndex = 0;

    private bool isLocalPlayer = false;

    void Awake()
    {
        isLocalPlayer = GetComponent<PlayerTankBrain>() != null;
    }

    void Start()
    {
        if (isLocalPlayer && DeveloperConsole.Instance != null)
        {
            DeveloperConsole.Instance.AddCommand("setskin", CmdSetSkin);
        }
    }

    public void SetRenderers(Renderer hullRenderer, Renderer turretRenderer)
    {
        tankMaterials.Clear();
        if (hullRenderer != null) tankMaterials.Add(hullRenderer.material);
        if (turretRenderer != null) tankMaterials.Add(turretRenderer.material);

        LoadAllSkinsForTesting();
        if (allSkinConfigs != null && allSkinConfigs.Length > 0)
        {
            ApplyConfig(allSkinConfigs[currentIndex]);
        }
    }

    public void ApplySkinById(string skinId)
    {
        string path = $"Colormap/{skinId}/{skinId}_Config";
        TankSkinConfig config = Resources.Load<TankSkinConfig>(path);

        if (config != null)
        {
            ApplyConfig(config);

            if (isLocalPlayer) Debug.Log($"<color=green>Успех:</color> Загружен скин {skinId}");
        }
        else
        {
            if (isLocalPlayer) Debug.LogError($"Скин с ID '{skinId}' не найден по пути Resources/{path}");
        }
    }

    private void ApplyConfig(TankSkinConfig config)
    {
        foreach (Material mat in tankMaterials)
        {
            if (mat != null)
            {
                mat.SetTexture(skinTexturePropertyName, config.skinTexture);
                mat.SetVector(skinTilingPropertyName, config.tiling);
            }
        }
    }

    private void LoadAllSkinsForTesting()
    {
        if (allSkinConfigs != null && allSkinConfigs.Length > 0) return;

        allSkinConfigs = Resources.LoadAll<TankSkinConfig>("Colormap");
    }

    private void CmdSetSkin(string[] args)
    {
        if (args.Length == 0)
        {
            DeveloperConsole.Instance.LogMessage("Ошибка! Использование: setskin [skinId]", Color.yellow);
            return;
        }
        string skinId = args[0];
        ApplySkinById(skinId);
        DeveloperConsole.Instance.LogMessage($"Выполнено: танк перекрашен в {skinId}", Color.green);
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (DeveloperConsole.Instance != null && DeveloperConsole.Instance.IsOpen) return;

        if (allSkinConfigs == null || allSkinConfigs.Length == 0 || tankMaterials.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            currentIndex++;
            if (currentIndex >= allSkinConfigs.Length) currentIndex = 0;
            ApplyConfig(allSkinConfigs[currentIndex]);
        }
        else if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = allSkinConfigs.Length - 1;
            ApplyConfig(allSkinConfigs[currentIndex]);
        }
    }
}