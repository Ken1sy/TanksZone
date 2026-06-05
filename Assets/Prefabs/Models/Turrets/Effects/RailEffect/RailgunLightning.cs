using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RailgunLightning : MonoBehaviour
{
    [Header("Настройки молнии")]
    [SerializeField] private int segments = 12;        // Количество изломов
    [SerializeField] private float jitter = 1.5f;      // Разброс (насколько широкая молния)
    [SerializeField] private float duration = 0.35f;   // Время жизни (должно быть чуть меньше основного луча)
    [SerializeField] private float startWidth = 0.15f; // Толщина молнии

    private LineRenderer _lineRenderer;
    private Material _material;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.useWorldSpace = true;

        if (_lineRenderer.material != null)
        {
            _material = _lineRenderer.material;
        }
    }

    public void FireLightning(Vector3 startPoint, Vector3 endPoint)
    {
        _lineRenderer.positionCount = segments;
        _lineRenderer.widthMultiplier = startWidth;

        Vector3 direction = (endPoint - startPoint).normalized;
        float distance = Vector3.Distance(startPoint, endPoint);
        float step = distance / (segments - 1);

        // Находим перпендикуляры для смещения точек в стороны от центрального луча
        Vector3 up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) < 0.99f ? Vector3.up : Vector3.forward;
        Vector3 right = Vector3.Cross(direction, up).normalized;
        Vector3 localUp = Vector3.Cross(right, direction).normalized;

        for (int i = 0; i < segments; i++)
        {
            Vector3 pos = startPoint + direction * (step * i);

            // Первую и последнюю точки (дуло и цель) оставляем по центру
            if (i > 0 && i < segments - 1)
            {
                float offsetX = Random.Range(-jitter, jitter);
                float offsetY = Random.Range(-jitter, jitter);
                pos += right * offsetX + localUp * offsetY;
            }

            _lineRenderer.SetPosition(i, pos);
        }

        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        string colorProp = _material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        Color startColor = _material.GetColor(colorProp);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            _lineRenderer.widthMultiplier = Mathf.Lerp(startWidth, 0f, t);

            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            _material.SetColor(colorProp, c);

            yield return null;
        }
    }
}