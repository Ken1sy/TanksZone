using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameScripts.UI
{
    public class SettingsMenuController : MonoBehaviour
    {
        public static SettingsMenuController Instance { get; private set; }
        public static bool IsOpen { get; private set; }
        public static bool ShowDamage { get; private set; } = true;
        public static bool ShowDropZones { get; private set; } = true;
        public static bool ShowFPS { get; private set; } = false;
        public static bool ShowPing { get; private set; } = false;
        public static float Volume { get; private set; } = 1f;
        public static bool IsMuted => Volume == 0f;
        public static float MouseSensitivity { get; private set; } = 0.07f;
        public static bool InvertMouseY { get; private set; } = false;
        public static bool InvertReverse { get; private set; } = false;

        [Header("Окна вкладок")]
        public GameObject[] tabPanels;
        [Header("Настройки: Игра")]
        public Toggle showDamageToggle;
        public Toggle showDropZonesToggle;
        public Toggle showFPSToggle;
        public Toggle showPingToggle;
        public Slider volumeSlider;
        [Header("Настройки: Графика")]
        public TMP_Dropdown resolutionDropdown;
        public Toggle fullscreenToggle;
        public TMP_Dropdown fpsLimitDropdown;
        public TMP_Dropdown vsyncDropdown;
        public TMP_Dropdown shadowsDropdown;
        [Header("Настройки: Управление")]
        public Slider sensitivitySlider;
        public Toggle invertMouseToggle;
        public Toggle invertReverseToggle;

        private readonly int[] _fpsLimits = { 30, 60, 120, 144, -1 };
        private void Awake() { Instance = this; InitializeResolutions(); LoadSettings(); }
        private void OnEnable() { IsOpen = true; UpdateUIFromSettings(); }
        private void OnDisable() { IsOpen = false; SaveSettings(); }

        private static float _savedVolumeBeforeMute = 1f;
        private Resolution[] _resolutions;

        private void Start()
        {
            if (showDamageToggle != null) showDamageToggle.onValueChanged.AddListener(OnShowDamageChanged);
            if (showDropZonesToggle != null) showDropZonesToggle.onValueChanged.AddListener(OnShowDropZonesChanged);
            if (showFPSToggle != null) showFPSToggle.onValueChanged.AddListener(OnShowFPSChanged);
            if (showPingToggle != null) showPingToggle.onValueChanged.AddListener(OnShowPingChanged);
            if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(SetResolution);
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
            if (fpsLimitDropdown != null) fpsLimitDropdown.onValueChanged.AddListener(SetFPSLimit);
            if (vsyncDropdown != null) vsyncDropdown.onValueChanged.AddListener(SetVSync);
            if (shadowsDropdown != null) shadowsDropdown.onValueChanged.AddListener(SetShadows);
            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            if (invertMouseToggle != null) invertMouseToggle.onValueChanged.AddListener(OnInvertMouseChanged);
            if (invertReverseToggle != null) invertReverseToggle.onValueChanged.AddListener(OnInvertReverseChanged);
            OpenTab(0);
        }

        public void OpenTab(int tabIndex)
        {
            for (int i = 0; i < tabPanels.Length; i++)
            { if (tabPanels[i] != null) tabPanels[i].SetActive(i == tabIndex); }
        }
        private void OnShowDamageChanged(bool isOn) { ShowDamage = isOn; }
        private void OnShowDropZonesChanged(bool isOn) { ShowDropZones = isOn; }
        private void OnShowFPSChanged(bool isOn) { ShowFPS = isOn; }
        private void OnShowPingChanged(bool isOn) { ShowPing = isOn; }
        private void OnVolumeChanged(float value)
        {
            Volume = value;
            AudioListener.volume = Volume;
            if (value > 0f) _savedVolumeBeforeMute = value;
            SyncGarageMuteIcon();
        }

        public static void ToggleMuteGlobal()
        {
            if (Volume > 0f) { _savedVolumeBeforeMute = Volume; SetVolumeGlobal(0f); }
            else
            {
                float volumeToRestore = _savedVolumeBeforeMute > 0f ? _savedVolumeBeforeMute : 1f;
                SetVolumeGlobal(volumeToRestore);
            }
        }

        private static void SetVolumeGlobal(float newVolume)
        {
            Volume = newVolume;
            AudioListener.volume = Volume;
            if (Instance != null && Instance.volumeSlider != null)
                Instance.volumeSlider.SetValueWithoutNotify(Volume);
            SyncGarageMuteIcon();
            if (Instance != null) Instance.SaveSettings();
        }

        private static void SyncGarageMuteIcon()
        { if (GarageUIManager.Instance != null) GarageUIManager.Instance.SyncMuteIcon(IsMuted); }
        private void InitializeResolutions()
        {
            if (resolutionDropdown == null) return;
            _resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();
            List<string> options = new List<string>();
            int currentResolutionIndex = 0;
            for (int i = 0; i < _resolutions.Length; i++)
            {
                string option = _resolutions[i].width + " x " + _resolutions[i].height + " @ " + _resolutions[i].refreshRateRatio.value.ToString("0") + "Hz";
                options.Add(option);
                if (_resolutions[i].width == Screen.currentResolution.width &&
                    _resolutions[i].height == Screen.currentResolution.height &&
                    _resolutions[i].refreshRateRatio.value == Screen.currentResolution.refreshRateRatio.value)
                { currentResolutionIndex = i; }
            }
            resolutionDropdown.AddOptions(options);
            if (!PlayerPrefs.HasKey("Set_ResIndex"))
            { PlayerPrefs.SetInt("Set_ResIndex", currentResolutionIndex); }
        }

        private void SetResolution(int index)
        {
            Resolution res = _resolutions[index];
            Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
        }
        private void SetFullscreen(bool isFullscreen)
        { Screen.fullScreenMode = isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed; }
        private void SetFPSLimit(int index) { Application.targetFrameRate = _fpsLimits[index]; }
        private void SetVSync(int index) { QualitySettings.vSyncCount = index; }
        private void SetShadows(int index)
        {
            switch (index)
            {
                case 0:
                    QualitySettings.shadows = ShadowQuality.Disable;
                    break;
                case 1:
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    break;
                case 2:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    break;
            }
        }
        private void OnSensitivityChanged(float value) { MouseSensitivity = value; }
        private void OnInvertMouseChanged(bool isOn) { InvertMouseY = isOn; }
        private void OnInvertReverseChanged(bool isOn) { InvertReverse = isOn; }
        private void SaveSettings()
        {
            PlayerPrefs.SetInt("Set_ShowDamage", ShowDamage ? 1 : 0);
            PlayerPrefs.SetInt("Set_ShowDropZones", ShowDropZones ? 1 : 0);
            PlayerPrefs.SetInt("Set_ShowFPS", ShowFPS ? 1 : 0);
            PlayerPrefs.SetInt("Set_ShowPing", ShowPing ? 1 : 0);
            PlayerPrefs.SetFloat("Set_Volume", Volume);
            PlayerPrefs.SetFloat("Set_SavedVol", _savedVolumeBeforeMute);
            PlayerPrefs.SetFloat("Set_Sensitivity", MouseSensitivity);
            PlayerPrefs.SetInt("Set_InvertMouse", InvertMouseY ? 1 : 0);
            PlayerPrefs.SetInt("Set_InvertReverse", InvertReverse ? 1 : 0);
            if (resolutionDropdown != null) PlayerPrefs.SetInt("Set_ResIndex", resolutionDropdown.value);
            if (fullscreenToggle != null) PlayerPrefs.SetInt("Set_Fullscreen", fullscreenToggle.isOn ? 1 : 0);
            if (fpsLimitDropdown != null) PlayerPrefs.SetInt("Set_FPSIndex", fpsLimitDropdown.value);
            if (vsyncDropdown != null) PlayerPrefs.SetInt("Set_VSyncIndex", vsyncDropdown.value);
            if (shadowsDropdown != null) PlayerPrefs.SetInt("Set_ShadowsIndex", shadowsDropdown.value);
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            ShowDamage = PlayerPrefs.GetInt("Set_ShowDamage", 1) == 1;
            ShowDropZones = PlayerPrefs.GetInt("Set_ShowDropZones", 1) == 1;
            ShowFPS = PlayerPrefs.GetInt("Set_ShowFPS", 0) == 1;
            ShowPing = PlayerPrefs.GetInt("Set_ShowPing", 0) == 1;
            Volume = PlayerPrefs.GetFloat("Set_Volume", 1f);
            _savedVolumeBeforeMute = PlayerPrefs.GetFloat("Set_SavedVol", 1f);
            AudioListener.volume = Volume;
            SyncGarageMuteIcon();
            MouseSensitivity = PlayerPrefs.GetFloat("Set_Sensitivity", 0.07f);
            InvertMouseY = PlayerPrefs.GetInt("Set_InvertMouse", 0) == 1;
            InvertReverse = PlayerPrefs.GetInt("Set_InvertReverse", 0) == 1;
            int resIndex = PlayerPrefs.GetInt("Set_ResIndex", 0);
            bool isFull = PlayerPrefs.GetInt("Set_Fullscreen", 1) == 1;
            int fpsIndex = PlayerPrefs.GetInt("Set_FPSIndex", 1);
            int vsyncIndex = PlayerPrefs.GetInt("Set_VSyncIndex", 1);
            int shadowsIndex = PlayerPrefs.GetInt("Set_ShadowsIndex", 2);
            if (_resolutions != null && resIndex < _resolutions.Length) SetResolution(resIndex);
            SetFullscreen(isFull);
            SetFPSLimit(fpsIndex);
            SetVSync(vsyncIndex);
            SetShadows(shadowsIndex);
            UpdateUIFromSettings();
        }

        private void UpdateUIFromSettings()
        {
            if (showDamageToggle != null) showDamageToggle.SetIsOnWithoutNotify(ShowDamage);
            if (showDropZonesToggle != null) showDropZonesToggle.SetIsOnWithoutNotify(ShowDropZones);
            if (showFPSToggle != null) showFPSToggle.SetIsOnWithoutNotify(ShowFPS);
            if (showPingToggle != null) showPingToggle.SetIsOnWithoutNotify(ShowPing);
            if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(Volume);
            if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(MouseSensitivity);
            if (invertMouseToggle != null) invertMouseToggle.SetIsOnWithoutNotify(InvertMouseY);
            if (invertReverseToggle != null) invertReverseToggle.SetIsOnWithoutNotify(InvertReverse);
            if (resolutionDropdown != null) resolutionDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt("Set_ResIndex", 0));
            if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("Set_Fullscreen", 1) == 1);
            if (fpsLimitDropdown != null) fpsLimitDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt("Set_FPSIndex", 1));
            if (vsyncDropdown != null) vsyncDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt("Set_VSyncIndex", 1));
            if (shadowsDropdown != null) shadowsDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt("Set_ShadowsIndex", 2));
        }
    }
}