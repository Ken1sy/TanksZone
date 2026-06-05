#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Linq;

public static class TanksZoneBuilder
{
    private const string SCENE_00 = "Assets/Scenes/00_Bootstrap.unity";
    private const string SCENE_01 = "Assets/Scenes/01_Auth.unity";
    private const string SCENE_02 = "Assets/Scenes/02_Garage.unity";
    private const string SCENE_03 = "Assets/Scenes/03_BattleMap.unity";
    private const string SCENE_04 = "Assets/Scenes/04_Server_Init.unity";

    // Файлы, которые НЕЛЬЗЯ удалять при очистке старого билда
    private static readonly string[] ProtectedFiles = { "config.json", "RunServer.bat" };

    [MenuItem("TanksZone Build/Build Client (Windows)")]
    public static void BuildClient()
    {
        string[] clientScenes = { SCENE_00, SCENE_01, SCENE_02, SCENE_03 };
        string buildPath = "Builds/Client/TanksZone_Client.exe";

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = clientScenes,
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
            subtarget = (int)StandaloneBuildSubtarget.Player
        };

        PerformBuild("Client", buildOptions);
    }

    [MenuItem("TanksZone Build/Build Server (Windows Server)")]
    public static void BuildServer()
    {
        string[] serverScenes = { SCENE_04, SCENE_03 };
        string buildPath = "Builds/Server/TanksZone_Server.exe";

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = serverScenes,
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
            subtarget = (int)StandaloneBuildSubtarget.Server
        };

        PerformBuild("Server", buildOptions);
    }

    [MenuItem("TanksZone Build/Build Both (Client + Server)")]
    public static void BuildBoth()
    {
        BuildServer();
        BuildClient();
    }

    private static void PerformBuild(string buildType, BuildPlayerOptions options)
    {
        Debug.Log($"[{buildType}] Подготовка к сборке...");

        string directory = Path.GetDirectoryName(options.locationPathName);

        // --- БЛОК ОЧИСТКИ ПАПКИ (С ЗАЩИТОЙ КОНФИГОВ) ---
        if (Directory.Exists(directory))
        {
            Debug.Log($"[{buildType}] Удаление старых файлов (кроме конфигов)...");
            DirectoryInfo di = new DirectoryInfo(directory);

            // Удаляем все файлы, КРОМЕ защищенных
            foreach (FileInfo file in di.GetFiles())
            {
                if (!ProtectedFiles.Contains(file.Name))
                {
                    file.Delete();
                }
            }
            // Удаляем все вложенные папки
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }
        else
        {
            Directory.CreateDirectory(directory);
        }
        // -----------------------------------------------

        Debug.Log($"[{buildType}] Начало компиляции...");

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[{buildType}] Сборка успешна! Время: {summary.totalTime.TotalSeconds:F1} сек. " +
                      $"Размер: {summary.totalSize / (1024 * 1024)} МБ. Сохранено в: {options.locationPathName}");

            if (buildType == "Server")
            {
                CreateServerBatFile(options.locationPathName);
            }
            else if (buildType == "Client")
            {
                CreateClientConfigFile(options.locationPathName);
            }
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError($"[{buildType}] Ошибка сборки! Откройте консоль для подробностей.");
        }
    }

    private static void CreateServerBatFile(string exeFullPath)
    {
        string directory = Path.GetDirectoryName(exeFullPath);
        string exeName = Path.GetFileName(exeFullPath);
        string batPath = Path.Combine(directory, "RunServer.bat");

        // Абсолютно чистый батник без русских букв, но с параметрами запуска!
        string[] batLines = {
            "@echo off",
            "chcp 65001",
            $"\"{exeName}\" -bind 0.0.0.0 -port 7770"
        };

        // Сохраняем жестко без BOM, чтобы командная строка 100% его прочитала
        File.WriteAllLines(batPath, batLines, new System.Text.UTF8Encoding(false));

        Debug.Log($"[{nameof(TanksZoneBuilder)}] Успешно создан файл запуска: {batPath}");
    }

    private static void CreateClientConfigFile(string exeFullPath)
    {
        string directory = Path.GetDirectoryName(exeFullPath);
        string configPath = Path.Combine(directory, "config.json");

        string jsonContent = "{\n  \"ServerIP\": \"127.0.0.1\",\n  \"ServerPort\": 7770\n}";

        if (!File.Exists(configPath))
        {
            File.WriteAllText(configPath, jsonContent, new System.Text.UTF8Encoding(false));
            Debug.Log($"[{nameof(TanksZoneBuilder)}] Файл config.json создан.");
        }
    }
}
#endif