using FishNet;
using FishNet.Object;
using UnityEngine;

public class ServerBootstrap : MonoBehaviour
{
    [Header("Префаб менеджера комнат")]
    [Tooltip("Перетащи сюда СИНИЙ ПРЕФАБ ServerRoomManager из папки Prefabs")]
    public NetworkObject serverRoomManagerPrefab;

    private void Start()
    {
        // Подписываемся на событие запуска сервера
        InstanceFinder.ServerManager.OnServerConnectionState += OnServerStateChanged;
    }

    private void OnDestroy()
    {
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnServerConnectionState -= OnServerStateChanged;
    }

    private void OnServerStateChanged(FishNet.Transporting.ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == FishNet.Transporting.LocalConnectionState.Started)
        {
            // Как только Сервер проснулся - динамически спавним Менеджер Комнат!
            NetworkObject roomManager = Instantiate(serverRoomManagerPrefab);
            InstanceFinder.ServerManager.Spawn(roomManager);

            Debug.Log("[ServerBootstrap] Global ServerRoomManager is compitly spawned on every scene!");
        }
    }
}