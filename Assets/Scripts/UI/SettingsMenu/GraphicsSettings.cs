using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GraphicsSettings : MonoBehaviour
{
    [Header("UI Элементы")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;
    public Toggle fullscreenToggle;
    public Toggle vSyncToggle;

    // НОВОЕ: Ссылки для управления счетчиком FPS
    [Header("Настройки FPS")]
    public Toggle showFpsToggle;
    public FPSCounter fpsCounter;

    [Header("Яркость (URP)")]
    public Slider brightnessSlider;
    public Volume globalVolume;
    private ColorAdjustments _colorAdjustments;

    private Resolution[] _resolutions;

    private void Start()
    {
        // === Инициализация разрешений экрана ===
        Resolution[] rawResolutions = Screen.resolutions;
        List<Resolution> filteredResolutions = new List<Resolution>();
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        for (int i = 0; i < rawResolutions.Length; i++)
        {
            string option = rawResolutions[i].width + " x " + rawResolutions[i].height;
            if (!options.Contains(option))
            {
                options.Add(option);
                filteredResolutions.Add(rawResolutions[i]);
            }
            else
            {
                int index = options.IndexOf(option);
                filteredResolutions[index] = rawResolutions[i];
            }
        }
        _resolutions = filteredResolutions.ToArray();

        int currentResIndex = 0;
        for (int i = 0; i < _resolutions.Length; i++)
        {
            if (_resolutions[i].width == Screen.currentResolution.width &&
                _resolutions[i].height == Screen.currentResolution.height)
            {
                currentResIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);

        qualityDropdown.ClearOptions();
        List<string> qualityNames = new List<string>(QualitySettings.names);
        qualityDropdown.AddOptions(qualityNames);

        if (globalVolume != null) globalVolume.profile.TryGet(out _colorAdjustments);

        // === Загрузка сохранений ===
        int savedResIndex = PlayerPrefs.GetInt("ResPreference", currentResIndex);
        if (savedResIndex >= _resolutions.Length) savedResIndex = _resolutions.Length - 1;

        int savedQuality = PlayerPrefs.GetInt("QualityPreference", QualitySettings.GetQualityLevel());
        bool savedFullscreen = PlayerPrefs.GetInt("FullscreenPreference", Screen.fullScreen ? 1 : 0) == 1;
        bool savedVSync = PlayerPrefs.GetInt("VSyncPreference", 1) == 1;
        float savedBrightness = PlayerPrefs.GetFloat("BrightnessPreference", 0f);

        // Загружаем настройку FPS (по умолчанию включена - 1)
        bool savedShowFps = PlayerPrefs.GetInt("ShowFPSPreference", 1) == 1;

        // === Применение настроек в UI ===
        resolutionDropdown.value = savedResIndex;
        resolutionDropdown.RefreshShownValue();
        qualityDropdown.value = savedQuality;
        qualityDropdown.RefreshShownValue();
        fullscreenToggle.isOn = savedFullscreen;
        vSyncToggle.isOn = savedVSync;
        brightnessSlider.value = savedBrightness;

        if (showFpsToggle != null) showFpsToggle.SetIsOnWithoutNotify(savedShowFps);

        // === Физическое применение настроек ===
        SetResolution(savedResIndex);
        SetQuality(savedQuality);
        SetFullscreen(savedFullscreen);
        SetVSync(savedVSync);
        SetBrightness(savedBrightness);
        SetShowFPS(savedShowFps); // Включаем/выключаем FPS

        // === Подписка на события UI ===
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        qualityDropdown.onValueChanged.AddListener(SetQuality);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        vSyncToggle.onValueChanged.AddListener(SetVSync);
        brightnessSlider.onValueChanged.AddListener(SetBrightness);

        if (showFpsToggle != null) showFpsToggle.onValueChanged.AddListener(SetShowFPS);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = _resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, fullscreenToggle.isOn);
        PlayerPrefs.SetInt("ResPreference", resolutionIndex);
        PlayerPrefs.Save();
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        SetVSync(vSyncToggle.isOn);
        PlayerPrefs.SetInt("QualityPreference", qualityIndex);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("FullscreenPreference", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetVSync(bool isVSync)
    {
        QualitySettings.vSyncCount = isVSync ? 1 : 0;
        PlayerPrefs.SetInt("VSyncPreference", isVSync ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetBrightness(float value)
    {
        if (_colorAdjustments != null)
        {
            _colorAdjustments.postExposure.value = value;
            PlayerPrefs.SetFloat("BrightnessPreference", value);
            PlayerPrefs.Save();
        }
    }

    public void SetShowFPS(bool isShowing)
    {
        if (fpsCounter != null)
        {
            fpsCounter.showFPS = isShowing;

            if (fpsCounter.fpsText != null)
            {
                fpsCounter.fpsText.gameObject.SetActive(isShowing);
            }
        }

        PlayerPrefs.SetInt("ShowFPSPreference", isShowing ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(SetResolution);
        if (qualityDropdown != null) qualityDropdown.onValueChanged.RemoveListener(SetQuality);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
        if (vSyncToggle != null) vSyncToggle.onValueChanged.RemoveListener(SetVSync);
        if (brightnessSlider != null) brightnessSlider.onValueChanged.RemoveListener(SetBrightness);

        if (showFpsToggle != null) showFpsToggle.onValueChanged.RemoveListener(SetShowFPS);
    }
}