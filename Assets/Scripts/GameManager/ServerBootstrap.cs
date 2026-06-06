using FishNet;
using FishNet.Object;
using UnityEngine;

public class ServerBootstrap : MonoBehaviour
{
    [Header("Префаб менеджера комнат")]
    public NetworkObject serverRoomManagerPrefab;
    private void Start() { InstanceFinder.ServerManager.OnServerConnectionState += OnServerStateChanged; }
    private void OnDestroy()
    {
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnServerConnectionState -= OnServerStateChanged;
    }
    private void OnServerStateChanged(FishNet.Transporting.ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == FishNet.Transporting.LocalConnectionState.Started)
        {
            NetworkObject roomManager = Instantiate(serverRoomManagerPrefab);
            InstanceFinder.ServerManager.Spawn(roomManager);
            Debug.Log("[ServerBootstrap] Global ServerRoomManager is compitly spawned on every scene!");
        }
    }
}