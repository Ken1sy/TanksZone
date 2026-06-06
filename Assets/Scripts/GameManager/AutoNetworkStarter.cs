using FishNet;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoNetworkStarter : MonoBehaviour
{
    [Header("Настройки для тестов")]
    public bool startHostInEditor = true;
    [Header("Оффлайн Сцены")]
    public string[] offlineScenes = { "00_Bootstrap", "01_Auth" };

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        CheckAndStartNetwork(SceneManager.GetActiveScene().name);
    }
    private void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { CheckAndStartNetwork(scene.name); }
    private void CheckAndStartNetwork(string sceneName)
    {
        foreach (string offlineScene in offlineScenes) { if (sceneName == offlineScene) return; }
        Invoke(nameof(StartNetwork), 0.2f);
    }
    private void StartNetwork()
    {
        if (InstanceFinder.NetworkManager == null) return;
        if (InstanceFinder.ServerManager.Started || InstanceFinder.ClientManager.Started) return;
        if (Application.isBatchMode) { InstanceFinder.ServerManager.StartConnection(); return; }

#if UNITY_EDITOR
        if (startHostInEditor)
        {
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection();
        }
        else InstanceFinder.ClientManager.StartConnection();
#else
        InstanceFinder.ClientManager.StartConnection();
#endif
    }
}