using FishNet;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoNetworkStarter : MonoBehaviour
{
    [Header("Настройки для тестов")]
    [Tooltip("Если включено, в редакторе Unity игра будет запускаться как Хост (Сервер + Клиент), чтобы можно было сразу тестировать.")]
    public bool startHostInEditor = true;

    [Header("Оффлайн Сцены")]
    [Tooltip("Сцены, в которых сеть НЕ должна запускаться (например, меню авторизации). Впиши сюда точные названия сцен.")]
    public string[] offlineScenes = { "00_Bootstrap", "01_Auth" };

    private void Start()
    {
        // Подписываемся на событие загрузки новых сцен
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Проверяем текущую сцену при старте
        CheckAndStartNetwork(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckAndStartNetwork(scene.name);
    }

    private void CheckAndStartNetwork(string sceneName)
    {
        // 1. Проверяем, находимся ли мы в оффлайн-сцене
        foreach (string offlineScene in offlineScenes)
        {
            if (sceneName == offlineScene)
            {
                Debug.Log($"<color=grey>[Network] Сцена {sceneName} является оффлайновой. Ожидание входа в игру...</color>");
                return; // Прерываем запуск сети
            }
        }

        // 2. Если мы вошли в Гараж (или другую игровую сцену) - запускаем сеть
        Invoke(nameof(StartNetwork), 0.2f);
    }

    private void StartNetwork()
    {
        // Если FishNet не найден на сцене - отмена
        if (InstanceFinder.NetworkManager == null) return;

        // Если сеть уже запущена - отмена
        if (InstanceFinder.ServerManager.Started || InstanceFinder.ClientManager.Started) return;

        // 1. ПРОВЕРКА НА ВЫДЕЛЕННЫЙ СЕРВЕР (DEDICATED SERVER)
        if (Application.isBatchMode)
        {
            Debug.Log("<color=yellow>[Network] Запуск Выделенного Сервера (Dedicated Server)...</color>");
            InstanceFinder.ServerManager.StartConnection();
            return;
        }

        // 2. ПРОВЕРКА НА РЕДАКТОР UNITY
#if UNITY_EDITOR
        if (startHostInEditor)
        {
            Debug.Log("<color=yellow>[Network] Запуск Хоста (Сервер + Клиент) для теста в редакторе...</color>");
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection();
        }
        else
        {
            Debug.Log("<color=yellow>[Network] Запуск Клиента в редакторе...</color>");
            InstanceFinder.ClientManager.StartConnection();
        }
#else
        // 3. ДЛЯ РЕАЛЬНЫХ ИГРОКОВ (Скомпилированная игра с графикой)
        Debug.Log("<color=yellow>[Network] Запуск Клиента и подключение к серверу...</color>");
        InstanceFinder.ClientManager.StartConnection();
#endif
    }
}