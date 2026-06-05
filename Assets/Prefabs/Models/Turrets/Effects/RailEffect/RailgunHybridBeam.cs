using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RailgunHybridBeam : MonoBehaviour
{
    [Header("Настройки сплошного луча")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float startWidth = 0.5f;

    private LineRenderer _lineRenderer;
    private RailgunLightning[] _lightnings; // <-- Заменили на скрипт молний
    private Material _beamMaterial;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.useWorldSpace = true;

        // Находим все дочерние молнии
        _lightnings = GetComponentsInChildren<RailgunLightning>();

        if (_lineRenderer.material != null)
        {
            _beamMaterial = _lineRenderer.material;
        }
    }

    public void FireBeam(Vector3 startPoint, Vector3 endPoint)
    {
        // 1. Отрисовка неразрывного стержня
        _lineRenderer.positionCount = 2;
        _lineRenderer.SetPosition(0, startPoint);
        _lineRenderer.SetPosition(1, endPoint);
        _lineRenderer.widthMultiplier = startWidth;

        // 2. Мгновенная генерация всех дочерних молний
        foreach (var lightning in _lightnings)
        {
            lightning.FireLightning(startPoint, endPoint);
        }

        StartCoroutine(FadeOutSequence());
    }

    private IEnumerator FadeOutSequence()
    {
        float elapsedTime = 0f;
        string colorProp = _beamMaterial.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        Color startColor = _beamMaterial.GetColor(colorProp);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            _lineRenderer.widthMultiplier = Mathf.Lerp(startWidth, 0f, t);

            Color fadedColor = startColor;
            fadedColor.a = Mathf.Lerp(1f, 0f, t);
            _beamMaterial.SetColor(colorProp, fadedColor);

            yield return null;
        }

        Destroy(gameObject);
    }
}