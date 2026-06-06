using System.Collections.Generic;
using UnityEngine;

namespace GameScripts.Visuals
{
    public class TankTemperatureVisuals : MonoBehaviour
    {
        [Header("Связь")]
        public PlayerTankBrain brain;
        [Header("Настройки Шейдера")]
        public string colorPropertyName = "_TemperatureColor";
        public string altColorPropertyName = "_BaseColor";
        [Header("Цвета температур")]
        public Color fireColor = new Color(1f, 0.3f, 0f);
        public Color freezeColor = new Color(0f, 0.8f, 1f);
        public Color normalColor = Color.white;
        [Header("Тестирование (Редактор)")]
        public bool useDebugTemperature = false;
        [Range(-100f, 100f)]
        public float debugTemperature = 0f;
        private MaterialPropertyBlock _mpb;
        private List<Renderer> _renderers = new List<Renderer>();
        private float _displayTemp = 0f;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (brain == null) brain = GetComponent<PlayerTankBrain>();
        }

        private void Update()
        {
            float targetTemp = 0f;
            if (useDebugTemperature) { targetTemp = debugTemperature; }
            else if (brain != null) { targetTemp = brain.networkTemperature.Value; }
            _displayTemp = Mathf.Lerp(_displayTemp, targetTemp, Time.deltaTime * 5f);
            if (_renderers.Count == 0) { FindTankRenderers(); }
            if (_renderers.Count == 0) return;
            Color targetColor = normalColor;
            if (_displayTemp > 0) { targetColor = Color.Lerp(normalColor, fireColor, _displayTemp / 100f); }
            else if (_displayTemp < 0) { targetColor = Color.Lerp(normalColor, freezeColor, Mathf.Abs(_displayTemp) / 100f); }
            foreach (var rend in _renderers)
            {
                if (rend == null) continue;
                rend.GetPropertyBlock(_mpb);
                _mpb.SetColor(colorPropertyName, targetColor);
                _mpb.SetColor(altColorPropertyName, targetColor);
                rend.SetPropertyBlock(_mpb);
            }
        }

        private void FindTankRenderers()
        {
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
            foreach (var r in allRenderers)
            {
                if (!(r is ParticleSystemRenderer) && !(r is TrailRenderer)) { _renderers.Add(r); }
            }
        }
    }
}