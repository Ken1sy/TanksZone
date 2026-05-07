using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [Header("UI Elements")]
    public GameObject settingsPanel;
    public Button openButton;
    public Button closeButton;

    private void Start()
    {
        IsOpen = false;
        settingsPanel?.SetActive(false);
        openButton?.onClick.AddListener(OpenSettings);
        closeButton?.onClick.AddListener(CloseSettings);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && settingsPanel.activeSelf)
        {
            CloseSettings();
        }
    }

    public void OpenSettings()
    {
        IsOpen = true;
        settingsPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseSettings()
    {
        IsOpen = false;
        settingsPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        if (openButton != null) openButton.onClick.RemoveListener(OpenSettings);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseSettings);
    }
}