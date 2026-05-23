using FishNet;
using FishNet.Managing.Scened;
using UnityEngine;

public class NetworkMenuUI : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Точное название сцены с картой (как в Build Settings)")]
    public string mapSceneName = "GameMapScene"; // ВПИШИ СЮДА НАЗВАНИЕ СВОЕЙ СЦЕНЫ С КАРТОЙ!

    // Эту функцию повесь на кнопку "СОЗДАТЬ ИГРУ" (Хост)
    public void StartHostAndPlay()
    {
        // 1. Запускаем сервер и подключаем себя как клиента
        InstanceFinder.ServerManager.StartConnection();
        InstanceFinder.ClientManager.StartConnection();

        // 2. ЗАГРУЖАЕМ СЦЕНУ ПО СЕТИ. 
        // Сервер грузит карту, и FishNet заставит всех клиентов тоже загрузить её.
        SceneLoadData sld = new SceneLoadData(mapSceneName);
        sld.ReplaceScenes = ReplaceOption.All;
        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
    }

    // Эту функцию повесь на кнопку "ПОДКЛЮЧИТЬСЯ" (Клиент)
    public void ConnectToGame()
    {
        // Клиенту НЕ НУЖНО писать код загрузки сцены!
        // Как только он подключится, сервер сам скажет ему: "Мы сейчас на карте MapScene, загружай её".
        InstanceFinder.ClientManager.StartConnection();
    }
}