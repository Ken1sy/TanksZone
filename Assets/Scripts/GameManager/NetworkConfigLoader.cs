using UnityEngine;
using System.IO;
using FishNet;
using FishNet.Transporting.Tugboat;

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
            // ФИКС КОДИРОВКИ: Заставляем консоль выделенного сервера Unity понимать русский язык (UTF-8)
#if UNITY_SERVER || UNITY_EDITOR
            try
            {
                System.Console.OutputEncoding = System.Text.Encoding.UTF8;
                System.Console.InputEncoding = System.Text.Encoding.UTF8;
            }
            catch
            {
                // Игнорируем ошибку, если вдруг консоль недоступна (например в клиенте)
            }
#endif
        }

        private void Start()
        {
            Tugboat transport = InstanceFinder.NetworkManager.gameObject.GetComponent<Tugboat>();
            if (transport == null)
            {
                Debug.LogError("[Конфигуратор] Не найден компонент Tugboat на NetworkManager!");
                return;
            }

            // 1. НАСТРОЙКА СЕРВЕРА
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

            // 2. НАСТРОЙКА КЛИЕНТА
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
                // Сохраняем строго без метки BOM
                File.WriteAllText(configPath, JsonUtility.ToJson(defConfig, true), new System.Text.UTF8Encoding(false));

                transport.SetClientAddress(defConfig.ServerIP);
                transport.SetPort(defConfig.ServerPort);
                Debug.Log("[Клиент] Создан новый config.json");
            }
#endif
        }
    }
}