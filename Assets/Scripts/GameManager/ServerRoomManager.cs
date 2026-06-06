using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct RoomPlayerData
{
    public int clientId;
    public string playerName;
    public int rankIndex;
    public int kills;
}

[System.Serializable]
public struct RoomData
{
    public int roomId;
    public BattleConfig config;
    public int currentPlayers;
    public RoomPlayerData[] players;
}

public class ServerRoomManager : NetworkBehaviour
{
    private static ServerRoomManager _instance;
    public static ServerRoomManager Instance
    {
        get { if (_instance == null) _instance = FindAnyObjectByType<ServerRoomManager>(); return _instance; }
    }

    [Header("Настройки")]
    public float emptyRoomTimeout = 600f;
    [Header("Сцена и Карты")]
    public string baseBattleScene = "03_BattleMap";
    public List<GameObject> mapPrefabs;
    [Header("Игрок")]
    public GameObject playerTankPrefab;
    public readonly SyncList<RoomData> activeRooms = new SyncList<RoomData>();

    private Dictionary<int, float> emptyRoomTimers = new Dictionary<int, float>();
    private Dictionary<int, Scene> serverRoomScenes = new Dictionary<int, Scene>();
    private Dictionary<NetworkConnection, int> pendingJoins = new Dictionary<NetworkConnection, int>();
    private Dictionary<NetworkConnection, int> connectionToRoom = new Dictionary<NetworkConnection, int>();
    private int nextRoomId = 1;

    private void Awake()
    {
        try
        {
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;
            System.Console.InputEncoding = System.Text.Encoding.UTF8;
        }
        catch { }
        if (_instance == null) _instance = this;
        else if (_instance != this) Destroy(gameObject);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;
        ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (NetworkManager != null) NetworkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;
        if (ServerManager != null) ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
    }

    private void Update()
    {
        if (!IsServerInitialized) return;
        HandleEmptyRoomsCleanup();
    }

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            if (connectionToRoom.TryGetValue(conn, out int roomId))
            {
                connectionToRoom.Remove(conn);
                pendingJoins.Remove(conn);
                RemovePlayerFromRoom(conn, roomId);
            }
        }
    }

    private void HandleEmptyRoomsCleanup()
    {
        for (int i = activeRooms.Count - 1; i >= 0; i--)
        {
            RoomData room = activeRooms[i];
            if (room.currentPlayers == 0)
            {
                if (!emptyRoomTimers.ContainsKey(room.roomId)) emptyRoomTimers[room.roomId] = 0f;
                emptyRoomTimers[room.roomId] += Time.deltaTime;
                if (emptyRoomTimers[room.roomId] >= emptyRoomTimeout)
                {
                    Debug.Log($"[Server] Комната '{room.config.battleName}' (ID: {room.roomId}) удалена из-за неактивности.");
                    if (serverRoomScenes.TryGetValue(room.roomId, out Scene sceneToUnload))
                    {
                        if (sceneToUnload.IsValid() && sceneToUnload.isLoaded)
                            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneToUnload);
                        serverRoomScenes.Remove(room.roomId);
                    }
                    emptyRoomTimers.Remove(room.roomId);
                    activeRooms.RemoveAt(i);
                }
            }
            else { if (emptyRoomTimers.ContainsKey(room.roomId)) emptyRoomTimers.Remove(room.roomId); }
        }
    }
    public void RequestCreateRoom(BattleConfig config) { if (!IsSpawned) return; CmdCreateRoom(config); }
    public void RequestJoinRoom(int roomId)
    {
        if (!IsSpawned) return;
        string myName = PlayerPrefs.GetString("MyNickname", "Танкист");
        int myRank = PlayerPrefs.GetInt("MyRank", 1);
        CmdJoinRoom(roomId, myName, myRank);
    }
    public void RequestLeaveRoom() { if (!IsSpawned) return; CmdLeaveRoom(); }

    [ServerRpc(RequireOwnership = false)]
    private void CmdCreateRoom(BattleConfig config, NetworkConnection caller = null)
    {
        int roomId = nextRoomId++;
        RoomData newRoom = new RoomData
        {
            roomId = roomId,
            config = config,
            currentPlayers = 0,
            players = new RoomPlayerData[0]
        };
        activeRooms.Add(newRoom);
        Debug.Log($"[Server] Логическая комната '{config.battleName}' создана.");
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdJoinRoom(int roomId, string pName, int pRank, NetworkConnection caller = null)
    {
        bool roomExists = false;
        for (int i = 0; i < activeRooms.Count; i++)
        {
            if (activeRooms[i].roomId == roomId)
            {
                roomExists = true;
                RoomData updatedRoom = activeRooms[i];
                var pList = updatedRoom.players != null ? updatedRoom.players.ToList() : new List<RoomPlayerData>();
                if (!pList.Any(p => p.clientId == caller.ClientId))
                {
                    pList.Add(new RoomPlayerData
                    {
                        clientId = caller.ClientId,
                        playerName = pName,
                        rankIndex = pRank,
                        kills = 0
                    });
                }
                updatedRoom.players = pList.ToArray();
                updatedRoom.currentPlayers = updatedRoom.players.Length;
                activeRooms[i] = updatedRoom;
                break;
            }
        }
        if (!roomExists) return;
        pendingJoins[caller] = roomId;
        connectionToRoom[caller] = roomId;
        bool hasValidScene = false;
        if (serverRoomScenes.TryGetValue(roomId, out Scene existingScene))
        {
            if (existingScene.IsValid() && existingScene.isLoaded) hasValidScene = true;
            else serverRoomScenes.Remove(roomId);
        }
        if (hasValidScene)
        {
            string safeName = string.IsNullOrEmpty(existingScene.name) ? baseBattleScene : existingScene.name;
            SceneLookupData lookup = new SceneLookupData(existingScene.handle, safeName);
            SceneLoadData sld = new SceneLoadData(lookup) { ReplaceScenes = ReplaceOption.All };
            NetworkManager.SceneManager.LoadConnectionScenes(caller, sld);
        }
        else
        {
            SceneLoadData sld = new SceneLoadData(baseBattleScene)
            {
                Options = { AllowStacking = true, LocalPhysics = LocalPhysicsMode.Physics3D },
                ReplaceScenes = ReplaceOption.All
            };
            NetworkManager.SceneManager.LoadConnectionScenes(caller, sld);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdLeaveRoom(NetworkConnection caller = null)
    {
        if (connectionToRoom.TryGetValue(caller, out int roomId))
        {
            connectionToRoom.Remove(caller);
            pendingJoins.Remove(caller);
            RemovePlayerFromRoom(caller, roomId);
            NetworkObject[] playerObjs = caller.Objects.ToArray();
            foreach (var netObj in playerObjs) ServerManager.Despawn(netObj);
            if (serverRoomScenes.TryGetValue(roomId, out Scene sceneToUnload))
            {
                if (sceneToUnload.IsValid())
                {
                    string safeName = string.IsNullOrEmpty(sceneToUnload.name) ? baseBattleScene : sceneToUnload.name;
                    SceneLookupData lookup = new SceneLookupData(sceneToUnload.handle, safeName);
                    SceneUnloadData sud = new SceneUnloadData(lookup);
                    NetworkManager.SceneManager.UnloadConnectionScenes(caller, sud);
                }
                else serverRoomScenes.Remove(roomId);
            }
        }
    }

    private void RemovePlayerFromRoom(NetworkConnection conn, int roomId)
    {
        for (int i = 0; i < activeRooms.Count; i++)
        {
            if (activeRooms[i].roomId == roomId)
            {
                RoomData updatedRoom = activeRooms[i];
                if (updatedRoom.players != null)
                {
                    var pList = updatedRoom.players.ToList();
                    pList.RemoveAll(p => p.clientId == conn.ClientId);
                    updatedRoom.players = pList.ToArray();
                    updatedRoom.currentPlayers = updatedRoom.players.Length;
                }
                activeRooms[i] = updatedRoom;
                Debug.Log($"[Server] Игрок {conn.ClientId} покинул комнату {roomId}.");
                break;
            }
        }
    }
    [Server]
    public void RegisterKill(NetworkConnection killer)
    {
        if (connectionToRoom.TryGetValue(killer, out int roomId))
        {
            for (int i = 0; i < activeRooms.Count; i++)
            {
                if (activeRooms[i].roomId == roomId)
                {
                    RoomData updatedRoom = activeRooms[i];
                    if (updatedRoom.players != null)
                    {
                        var pList = updatedRoom.players.ToList();
                        int pIndex = pList.FindIndex(p => p.clientId == killer.ClientId);
                        if (pIndex != -1)
                        {
                            var pd = pList[pIndex];
                            pd.kills++;
                            pList[pIndex] = pd;
                            updatedRoom.players = pList.ToArray();
                            activeRooms[i] = updatedRoom;
                        }
                    }
                    break;
                }
            }
        }
    }

    private void OnSceneLoadEnd(SceneLoadEndEventArgs args)
    {
        if (!IsServerInitialized) return;
        if (args.QueueData.Connections != null && args.QueueData.Connections.Length > 0)
        {
            NetworkConnection caller = args.QueueData.Connections[0];
            if (pendingJoins.TryGetValue(caller, out int roomId))
            {
                Scene targetScene = default;
                if (serverRoomScenes.TryGetValue(roomId, out Scene cachedScene)
                    && cachedScene.IsValid()
                    && cachedScene.isLoaded)
                {
                    targetScene = cachedScene;
                }
                else if (args.LoadedScenes.Length > 0)
                {
                    targetScene = args.LoadedScenes[0];
                    serverRoomScenes[roomId] = targetScene;
                    RoomData roomData = activeRooms.FirstOrDefault(r => r.roomId == roomId);
                    GameObject prefabToSpawn = mapPrefabs.FirstOrDefault(p => p.name == roomData.config.mapId);
                    if (prefabToSpawn != null)
                    {
                        GameObject mapInstance = Instantiate(prefabToSpawn);
                        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(mapInstance, targetScene);
                        ServerManager.Spawn(mapInstance);
                    }
                }
                else { pendingJoins.Remove(caller); return; }
                if (playerTankPrefab != null)
                {
                    GameObject playerTank = Instantiate(playerTankPrefab);
                    if (GameScripts.GameMode.SpawnManager.Instance != null)
                    {
                        Transform safePoint = GameScripts.GameMode.SpawnManager.Instance.GetSafeSpawnPoint();
                        playerTank.transform.SetPositionAndRotation(safePoint.position + Vector3.up * 2f, safePoint.rotation);
                    }
                    else { playerTank.transform.position = new Vector3(0, 5f, 0); }
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(playerTank, targetScene);
                    ServerManager.Spawn(playerTank, caller);
                }
                TargetRoomSetupComplete(caller);
                pendingJoins.Remove(caller);
            }
        }
    }

    [TargetRpc]
    private void TargetRoomSetupComplete(NetworkConnection conn)
    {
        Scene garageScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("02_Garage");
        if (garageScene.isLoaded) UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(garageScene);
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            Scene s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (s.name.Contains(baseBattleScene))
            {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(s);
                break;
            }
        }
    }
}