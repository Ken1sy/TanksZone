using UnityEngine;
using TMPro;
using FishNet;

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
        [Tooltip("Как часто обновлять цифры на экране (в секундах). 0.5 - оптимально, чтобы цифры не мельтешили.")]
        public float updateInterval = 0.5f;

        private float _timer;
        private int _frameCount;
        private float _timeAccumulator;

        private void Start()
        {
            // Убеждаемся, что при старте игры тексты скрыты или показаны согласно настройкам
            if (fps != null) fps.SetActive(SettingsMenuController.ShowFPS);
            if (ping != null) ping.SetActive(SettingsMenuController.ShowPing);
        }

        private void Update()
        {
            // 1. Получаем глобальные настройки из нашего менеджера
            bool wantShowFPS = SettingsMenuController.ShowFPS;
            bool wantShowPing = SettingsMenuController.ShowPing;

            // 2. Включаем или выключаем объекты текста, если настройки изменились
            if (fps != null && fps.activeSelf != wantShowFPS)
            {
                fps.SetActive(SettingsMenuController.ShowFPS);
            }

            if (ping != null && ping.activeSelf != wantShowPing)
            {
                ping.SetActive(wantShowPing);
            }

            // 3. Если игрок всё отключил — выходим, чтобы не тратить ресурсы компьютера на вычисления
            if (!wantShowFPS && !wantShowPing) return;

            // 4. Считаем кадры для FPS
            _timeAccumulator += Time.unscaledDeltaTime;
            _frameCount++;
            _timer += Time.unscaledDeltaTime;

            // 5. Обновляем текст раз в полсекунды (updateInterval)
            if (_timer >= updateInterval)
            {
                // ОБНОВЛЕНИЕ ФПС
                if (wantShowFPS && fpsText != null)
                {
                    float currentFps = _frameCount / _timeAccumulator;

                    // Делаем цвет зависимым от просадок (Зеленый = 60+, Желтый = 30-60, Красный = <30)
                    string colorTag = "<color=#00FF00>"; // Зеленый
                    if (currentFps < 30) colorTag = "<color=#FF0000>"; // Красный
                    else if (currentFps < 60) colorTag = "<color=#FFFF00>"; // Желтый

                    fpsText.text = $"{colorTag}{Mathf.RoundToInt(currentFps)}</color>";
                }

                // ОБНОВЛЕНИЕ ПИНГА
                if (wantShowPing && pingText != null)
                {
                    // Проверяем, подключены ли мы к серверу FishNet
                    if (InstanceFinder.IsOffline)
                    {
                        pingText.text = "<color=#888888>Offline</color>";
                    }
                    else
                    {
                        // ИСПРАВЛЕНИЕ: Используем тип long вместо uint
                        long ping = InstanceFinder.TimeManager.RoundTripTime;

                        string colorTag = "<color=#00FF00>";
                        if (ping > 150) colorTag = "<color=#FF0000>";
                        else if (ping > 80) colorTag = "<color=#FFFF00>";

                        pingText.text = $"{colorTag}{ping} ms</color>";
                    }
                }

                // Сбрасываем счетчики для следующего интервала
                _frameCount = 0;
                _timeAccumulator = 0f;
                _timer = 0f;
            }
        }
    }
}