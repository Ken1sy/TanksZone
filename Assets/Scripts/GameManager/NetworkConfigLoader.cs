using FishNet;
using FishNet.Transporting.Tugboat;
using System.IO;
using UnityEngine;

namespace GameScripts.Network
{
    public class NetworkConfigLoader : MonoBehaviour
    {
        [System.Serializable]
        public class ClientConfig
        {
            public string ServerIP = "127.0.0.1";
            public ushort ServerPort = 7770;
        }

        private void Awake()
        {
#if UNITY_SERVER || UNITY_EDITOR
            try
            {
                System.Console.OutputEncoding = System.Text.Encoding.UTF8;
                System.Console.InputEncoding = System.Text.Encoding.UTF8;
            }
            catch { }
#endif
        }

        private void Start()
        {
            Tugboat transport = InstanceFinder.NetworkManager.gameObject.GetComponent<Tugboat>();
            if (transport == null) return;
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-bind" && i + 1 < args.Length)
                {
                    transport.SetServerBindAddress(args[i + 1], FishNet.Transporting.IPAddressType.IPv4);
                    Debug.Log($"[Сервер] Bind IP установлен на: {args[i + 1]}");
                }
                if (args[i] == "-port" && i + 1 < args.Length)
                {
                    if (ushort.TryParse(args[i + 1], out ushort port))
                    {
                        transport.SetPort(port);
                        Debug.Log($"[Сервер/Клиент] Порт установлен на: {port}");
                    }
                }
            }

#if !UNITY_SERVER
            string configPath = Path.Combine(Application.dataPath, "../config.json");
            if (Application.isEditor)
            {
                configPath = Path.Combine(Application.dataPath, "../config.json");
            }
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    ClientConfig config = JsonUtility.FromJson<ClientConfig>(json);
                    transport.SetClientAddress(config.ServerIP);
                    transport.SetPort(config.ServerPort);
                    Debug.Log($"[Клиент] Загружен конфиг подключения -> IP: {config.ServerIP}:{config.ServerPort}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Клиент] Ошибка чтения config.json: {e.Message}");
                }
            }
            else
            {
                ClientConfig defConfig = new ClientConfig();
                File.WriteAllText(configPath, JsonUtility.ToJson(defConfig, true), new System.Text.UTF8Encoding(false));
                transport.SetClientAddress(defConfig.ServerIP);
                transport.SetPort(defConfig.ServerPort);
                Debug.Log("[Клиент] Создан новый config.json");
            }
#endif
        }
    }
}