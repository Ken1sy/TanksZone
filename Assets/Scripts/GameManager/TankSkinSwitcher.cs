using UnityEngine;
using System.Collections.Generic;

public class TankSkinSwitcher : MonoBehaviour
{
    [Header("Настройки Шейдера")]
    public string skinTexturePropertyName = "_SkinTexture";
    public string skinTilingPropertyName = "_SkinTiling";

    private Renderer hullRenderer;
    private Renderer turretRenderer;
    private Material hullMaterial;
    private Material turretMaterial;

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

            // КОМАНДЫ ДЛЯ НАСТРОЙКИ (Live Editor)
            DeveloperConsole.Instance.AddCommand("tiling", CmdSetTiling);
            DeveloperConsole.Instance.AddCommand("animspeed", CmdSetAnimSpeed);
            DeveloperConsole.Instance.AddCommand("grid", CmdSetGrid);
            DeveloperConsole.Instance.AddCommand("frames", CmdSetFrames);
        }
    }

    public void SetRenderers(Renderer hullRend, Renderer turretRend)
    {
        hullRenderer = hullRend;
        turretRenderer = turretRend;

        if (hullRenderer != null) hullMaterial = hullRenderer.material;
        if (turretRenderer != null) turretMaterial = turretRenderer.material;

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
            if (isLocalPlayer) Debug.Log($"Загружен скин {skinId}");
        }
    }

    private void ApplyConfig(TankSkinConfig config)
    {
        if (config == null) return;

        // Применяем к корпусу
        if (hullRenderer != null && hullMaterial != null)
        {
            float hullSize = Mathf.Max(hullRenderer.bounds.size.x, hullRenderer.bounds.size.z);
            Vector2 autoTiling = new Vector2(config.baseTiling * hullSize, config.baseTiling * hullSize);
            UpdateMaterial(hullMaterial, config, autoTiling);
        }

        // Применяем к пушке
        if (turretRenderer != null && turretMaterial != null)
        {
            float turretSize = Mathf.Max(turretRenderer.bounds.size.x, turretRenderer.bounds.size.z);
            Vector2 autoTiling = new Vector2(config.baseTiling * turretSize, config.baseTiling * turretSize);
            UpdateMaterial(turretMaterial, config, autoTiling);
        }
    }

    private void UpdateMaterial(Material mat, TankSkinConfig config, Vector2 tiling)
    {
        mat.SetTexture(skinTexturePropertyName, config.skinTexture);
        mat.SetVector(skinTilingPropertyName, tiling);
        mat.SetVector("_SkinGridSize", config.gridSize);
        mat.SetFloat("_SkinAnimSpeed", config.animationSpeed);
        mat.SetFloat("_SkinTotalFrames", config.totalFrames);
    }

    private void SaveConfig(TankSkinConfig config)
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(config);
        // Необязательно, но полезно для мгновенного сохранения файла на диск
        UnityEditor.AssetDatabase.SaveAssets();
#endif
        ApplyConfig(config);
    }

    // ==========================================
    // ОБРАБОТЧИКИ КОМАНД
    // ==========================================

    private void CmdSetTiling(string[] args)
    {
        if (args.Length == 0) return;
        if (float.TryParse(args[0].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float val))
        {
            allSkinConfigs[currentIndex].baseTiling = val;
            SaveConfig(allSkinConfigs[currentIndex]);
            DeveloperConsole.Instance.LogMessage($"Tiling установлен: {val}", Color.green);
        }
    }

    private void CmdSetAnimSpeed(string[] args)
    {
        if (args.Length == 0) return;
        if (float.TryParse(args[0].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float val))
        {
            allSkinConfigs[currentIndex].animationSpeed = val;
            SaveConfig(allSkinConfigs[currentIndex]);
            DeveloperConsole.Instance.LogMessage($"Скорость анимации: {val}", Color.cyan);
        }
    }

    private void CmdSetGrid(string[] args)
    {
        if (args.Length < 2) return;
        if (float.TryParse(args[0], out float x) && float.TryParse(args[1], out float y))
        {
            allSkinConfigs[currentIndex].gridSize = new Vector2(x, y);
            SaveConfig(allSkinConfigs[currentIndex]);
            DeveloperConsole.Instance.LogMessage($"Сетка (Grid) изменена на: {x}x{y}", Color.yellow);
        }
    }

    private void CmdSetFrames(string[] args)
    {
        if (args.Length == 0) return;
        if (float.TryParse(args[0], out float val))
        {
            allSkinConfigs[currentIndex].totalFrames = val;
            SaveConfig(allSkinConfigs[currentIndex]);
            DeveloperConsole.Instance.LogMessage($"Лимит кадров: {val}", Color.magenta);
        }
    }

    private void CmdSetSkin(string[] args)
    {
        if (args.Length == 0) return;
        ApplySkinById(args[0]);
    }

    private void LoadAllSkinsForTesting()
    {
        if (allSkinConfigs != null && allSkinConfigs.Length > 0) return;
        allSkinConfigs = Resources.LoadAll<TankSkinConfig>("Colormap");
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        if (DeveloperConsole.Instance != null && DeveloperConsole.Instance.IsOpen) return;
        if (allSkinConfigs == null || allSkinConfigs.Length == 0) return;

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

        if (Input.GetKeyDown(KeyCode.F5)) ApplyConfig(allSkinConfigs[currentIndex]);
    }
}