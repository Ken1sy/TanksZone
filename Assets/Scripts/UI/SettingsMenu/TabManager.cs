using UnityEngine;
using UnityEngine.UI;

public class TabManager : MonoBehaviour
{
    [Header("Вкладки (Кнопки Toggle)")]
    public Toggle tabGame;
    public Toggle tabGraphics;
    public Toggle tabControls;
    public Toggle tabAccount;

    [Header("Страницы (Объекты контента)")]
    public GameObject pageGame;
    public GameObject pageGraphics;
    public GameObject pageControls;
    public GameObject pageAccount;

    private void Start()
    {
        // Подписываем наши страницы на клики по вкладкам
        // (isOn) означает, что код сработает только для той вкладки, которая стала активной
        tabGame.onValueChanged.AddListener((isOn) => { if (isOn) ShowPage(pageGame); });
        tabGraphics.onValueChanged.AddListener((isOn) => { if (isOn) ShowPage(pageGraphics); });
        tabControls.onValueChanged.AddListener((isOn) => { if (isOn) ShowPage(pageControls); });
        tabAccount.onValueChanged.AddListener((isOn) => { if (isOn) ShowPage(pageAccount); });

        // При старте принудительно показываем первую страницу, а остальные скрываем
        ShowPage(pageGame);
    }

    private void ShowPage(GameObject activePage)
    {
        // 1. Сначала жестко выключаем вообще все страницы
        if (pageGame != null) pageGame.SetActive(false);
        if (pageGraphics != null) pageGraphics.SetActive(false);
        if (pageControls != null) pageControls.SetActive(false);
        if (pageAccount != null) pageAccount.SetActive(false);

        // 2. Включаем только ту, которую передали в метод
        if (activePage != null)
        {
            activePage.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        // Убираем слушателей при удалении объекта (правило хорошего кода)
        if (tabGame != null) tabGame.onValueChanged.RemoveAllListeners();
        if (tabGraphics != null) tabGraphics.onValueChanged.RemoveAllListeners();
        if (tabControls != null) tabControls.onValueChanged.RemoveAllListeners();
        if (tabAccount != null) tabAccount.onValueChanged.RemoveAllListeners();
    }
}