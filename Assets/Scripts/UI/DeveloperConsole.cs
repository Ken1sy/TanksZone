using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DeveloperConsole : MonoBehaviour
{
    public static DeveloperConsole Instance { get; private set; }
    public bool IsOpen => consoleCanvas != null && consoleCanvas.activeSelf;

    [Header("UI Ссылки")]
    public GameObject consoleCanvas;
    public InputField inputField;
    public Text logText;
    public ScrollRect scrollRect;
    [Header("Настройки Консоли")]
    public int maxLogLines = 200;

    private List<string> logLines = new List<string>();
    private Dictionary<string, Action<string[]>> commands = new Dictionary<string, Action<string[]>>();
    private List<string> commandHistory = new List<string>();
    private int historyIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        consoleCanvas.SetActive(false);
        inputField.onSubmit.AddListener(ProcessCommand);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote)) { ToggleConsole(); }
        if (IsOpen) { HandleHotkeys(); }
    }

    private void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) { AutoCompleteCommand(); }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (commandHistory.Count > 0 && historyIndex > 0)
            { historyIndex--; SetInputFieldText(commandHistory[historyIndex]); }
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (historyIndex < commandHistory.Count - 1)
            {
                historyIndex++;
                SetInputFieldText(commandHistory[historyIndex]);
            }
            else
            {
                historyIndex = commandHistory.Count;
                SetInputFieldText("");
            }
        }
    }

    private void AutoCompleteCommand()
    {
        string currentInput = inputField.text.ToLower().Trim();
        if (string.IsNullOrEmpty(currentInput)) return;
        foreach (var cmd in commands.Keys)
        {
            if (cmd.StartsWith(currentInput))
            { SetInputFieldText(cmd + " "); break; }
        }
    }

    private void SetInputFieldText(string text)
    {
        inputField.text = text;
        inputField.caretPosition = inputField.text.Length;
        inputField.ActivateInputField();
    }

    private void ToggleConsole()
    {
        bool isActive = !consoleCanvas.activeSelf;
        consoleCanvas.SetActive(isActive);
        PlayerInput playerInput = FindAnyObjectByType<PlayerInput>();
        if (isActive)
        {
            if (playerInput != null) playerInput.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetInputFieldText("");
            historyIndex = commandHistory.Count;
        }
        else
        {
            if (playerInput != null) playerInput.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void AddCommand(string commandName, Action<string[]> commandAction)
    {
        string lowerCmd = commandName.ToLower();
        if (!commands.ContainsKey(lowerCmd))
        { commands.Add(lowerCmd, commandAction); }
    }

    private void ProcessCommand(string inputValue)
    {
        inputValue = inputValue.Replace("`", "").Replace("ё", "").Trim();
        if (string.IsNullOrWhiteSpace(inputValue)) { inputField.ActivateInputField(); return; }
        LogMessage("> " + inputValue, Color.white);
        if (commandHistory.Count == 0 || commandHistory[commandHistory.Count - 1] != inputValue)
        { commandHistory.Add(inputValue); }
        historyIndex = commandHistory.Count;
        string[] parts = inputValue.Split(' ');
        string command = parts[0].ToLower();
        string[] args = new string[parts.Length - 1];
        Array.Copy(parts, 1, args, 0, parts.Length - 1);
        if (commands.ContainsKey(command))
        {
            try { commands[command].Invoke(args); }
            catch (Exception e) { LogMessage("Ошибка: " + e.Message, Color.red); }
        }
        else { LogMessage("Неизвестная команда: " + command, new Color(1f, 0.6f, 0f)); }
        SetInputFieldText("");
    }

    public void LogMessage(string message, Color color)
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        string newLine = $"<color=#{colorHex}>{message}</color>";
        logLines.Add(newLine);
        if (logLines.Count > maxLogLines) { logLines.RemoveAt(0); }
        logText.text = string.Join("\n", logLines);
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null) { scrollRect.verticalNormalizedPosition = 0f; }
    }
}