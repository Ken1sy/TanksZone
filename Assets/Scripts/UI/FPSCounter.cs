using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    public bool showFPS = true;

    float deltaTime;

    void Update()
    {
        if (showFPS)
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

            float fps = 1.0f / deltaTime;

            fpsText.text = "FPS: " + Mathf.Ceil(fps).ToString();
        }
    }
}