using FishNet;
using TMPro;
using UnityEngine;

namespace GameScripts.UI
{
    public class PerformanceDisplay : MonoBehaviour
    {
        [Header("UI Элементы")]
        public GameObject fps;
        public GameObject ping;
        public TMP_Text fpsText;
        public TMP_Text pingText;
        [Header("Настройки")]
        public float updateInterval = 0.5f;

        private float _timer;
        private int _frameCount;
        private float _timeAccumulator;

        private void Start()
        {
            if (fps != null) fps.SetActive(SettingsMenuController.ShowFPS);
            if (ping != null) ping.SetActive(SettingsMenuController.ShowPing);
        }

        private void Update()
        {
            bool wantShowFPS = SettingsMenuController.ShowFPS;
            bool wantShowPing = SettingsMenuController.ShowPing;
            if (fps != null && fps.activeSelf != wantShowFPS) { fps.SetActive(SettingsMenuController.ShowFPS); }
            if (ping != null && ping.activeSelf != wantShowPing) { ping.SetActive(wantShowPing); }
            if (!wantShowFPS && !wantShowPing) return;
            _timeAccumulator += Time.unscaledDeltaTime;
            _frameCount++;
            _timer += Time.unscaledDeltaTime;
            if (_timer >= updateInterval)
            {
                if (wantShowFPS && fpsText != null)
                {
                    float currentFps = _frameCount / _timeAccumulator;
                    string colorTag = "<color=#00FF00>"; // Зеленый
                    if (currentFps < 30) colorTag = "<color=#FF0000>"; // Красный
                    else if (currentFps < 60) colorTag = "<color=#FFFF00>"; // Желтый
                    fpsText.text = $"{colorTag}{Mathf.RoundToInt(currentFps)}</color>";
                }
                if (wantShowPing && pingText != null)
                {
                    if (InstanceFinder.IsOffline) { pingText.text = "<color=#888888>Offline</color>"; }
                    else
                    {
                        long ping = InstanceFinder.TimeManager.RoundTripTime;
                        string colorTag = "<color=#00FF00>";
                        if (ping > 150) colorTag = "<color=#FF0000>";
                        else if (ping > 80) colorTag = "<color=#FFFF00>";
                        pingText.text = $"{colorTag}{ping} ms</color>";
                    }
                }
                _frameCount = 0;
                _timeAccumulator = 0f;
                _timer = 0f;
            }
        }
    }
}