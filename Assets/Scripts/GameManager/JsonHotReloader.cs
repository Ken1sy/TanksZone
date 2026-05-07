using UnityEngine;
using System.IO;

public class JsonHotReloader : MonoBehaviour
{
    private TankChassisController controller;
    private string filePath;
    private FileSystemWatcher watcher;
    private bool fileChanged = false;

    void Start()
    {
        controller = GetComponent<TankChassisController>();

        // Формируем путь к файлу (например, Hornet.json)
        // Если вы используете TankSetupData для выбора корпуса:
        string fileName = TankSetupData.SelectedHullID + ".cfg";
        filePath = Path.Combine(Application.streamingAssetsPath, "Configs", fileName);

        if (File.Exists(filePath))
        {
            SetupWatcher();
        }
    }

    private void SetupWatcher()
    {
        // Настраиваем слежку за конкретной папкой
        watcher = new FileSystemWatcher();
        watcher.Path = Path.GetDirectoryName(filePath);
        watcher.Filter = Path.GetFileName(filePath);

        // На какие изменения реагировать
        watcher.NotifyFilter = NotifyFilters.LastWrite;

        // Подписываемся на событие изменения
        watcher.Changed += (s, e) => fileChanged = true;

        watcher.EnableRaisingEvents = true;
        Debug.Log($"<color=cyan>Слежу за изменениями в {watcher.Filter}...</color>");
    }

    void Update()
    {
        // Проверяем флаг в главном потоке Unity
        if (fileChanged)
        {
            fileChanged = false;
            ReloadSettings();
        }
    }

    private void ReloadSettings()
    {
        try
        {
            // Небольшая задержка, чтобы файл успел "освободиться" после сохранения в Windows
            System.Threading.Thread.Sleep(50);

            string jsonText = File.ReadAllText(filePath);
            TankSettings newSettings = JsonUtility.FromJson<TankSettings>(jsonText);

            if (controller != null)
            {
                controller.ApplySettings(newSettings);
                Debug.Log("<color=green>Параметры танка обновлены из JSON!</color>");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Ошибка при горячей перезагрузке: " + e.Message);
        }
    }

    private void OnDestroy()
    {
        if (watcher != null)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
    }
}