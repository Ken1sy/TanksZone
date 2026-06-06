using System.IO;
using UnityEngine;

public class JsonHotReloader : MonoBehaviour
{
    private TankChassisController controller;
    private string filePath;
    private FileSystemWatcher watcher;
    private bool fileChanged = false;

    void Start()
    {
        controller = GetComponent<TankChassisController>();
        string fileName = TankSetupData.SelectedHullID + ".cfg";
        filePath = Path.Combine(Application.streamingAssetsPath, "Configs", fileName);
        if (File.Exists(filePath)) SetupWatcher();
    }

    private void SetupWatcher()
    {
        watcher = new FileSystemWatcher();
        watcher.Path = Path.GetDirectoryName(filePath);
        watcher.Filter = Path.GetFileName(filePath);
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.Changed += (s, e) => fileChanged = true;
        watcher.EnableRaisingEvents = true;
    }
    void Update() { if (fileChanged) { fileChanged = false; ReloadSettings(); } }
    private void ReloadSettings()
    {
        try
        {
            System.Threading.Thread.Sleep(50);
            string jsonText = File.ReadAllText(filePath);
            TankSettings newSettings = JsonUtility.FromJson<TankSettings>(jsonText);
            if (controller != null) controller.ApplySettings(newSettings);
        }
        catch (System.Exception e) { Debug.LogError("Ошибка при перезагрузке: " + e.Message); }
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