using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    public static CrosshairUI Instance { get; private set; }

    private RectTransform rectTransform;
    private Image crosshairImage;

    private void Awake()
    {
        Instance = this;
        rectTransform = GetComponent<RectTransform>();
        crosshairImage = GetComponent<Image>();
    }
    public void UpdateCrosshair(Vector3 screenPosition, bool isBlocked, bool isCursorFree)
    {
        if (isCursorFree || screenPosition.z < 0) { crosshairImage.enabled = false; return; }
        crosshairImage.enabled = true;
        rectTransform.position = screenPosition;
    }
}