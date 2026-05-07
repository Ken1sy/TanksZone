using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
        // Открытие/закрытие консоли
        if (Input.GetKeyDown(KeyCode.BackQuote)) // BackQuote - это тильда (~)
        {
            ToggleConsole();
        }

        // ==========================================
        // НОВОЕ: Обработка горячих клавиш
        // ==========================================
        if (IsOpen)
        {
            HandleHotkeys();
        }
    }

    private void HandleHotkeys()
    {
        // 1. Автодополнение по клавише TAB
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            AutoCompleteCommand();
        }

        // 2. История команд: Стрелка ВВЕРХ
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (commandHistory.Count > 0 && historyIndex > 0)
            {
                historyIndex--;
                SetInputFieldText(commandHistory[historyIndex]);
            }
        }
        // 3. История команд: Стрелка ВНИЗ
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (historyIndex < commandHistory.Count - 1)
            {
                historyIndex++;
                SetInputFieldText(commandHistory[historyIndex]);
            }
            else
            {
                // Если спустились в самый низ - очищаем поле ввода
                historyIndex = commandHistory.Count;
                SetInputFieldText("");
            }
        }
    }

    private void AutoCompleteCommand()
    {
        string currentInput = inputField.text.ToLower().Trim();
        if (string.IsNullOrEmpty(currentInput)) return;

        // Ищем первую команду, которая начинается с введенного текста
        foreach (var cmd in commands.Keys)
        {
            if (cmd.StartsWith(currentInput))
            {
                SetInputFieldText(cmd + " "); // Добавляем пробел в конце для удобства ввода аргументов
                break;
            }
        }
    }

    private void SetInputFieldText(string text)
    {
        inputField.text = text;
        inputField.caretPosition = inputField.text.Length; // Переносим курсор в самый конец строки
        inputField.ActivateInputField(); // Удерживаем фокус на поле
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

            // Сбрасываем индекс истории в самый низ при открытии
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
        {
            commands.Add(lowerCmd, commandAction);
        }
    }

    private void ProcessCommand(string inputValue)
    {
        // Убираем случайный символ тильды, если он попал в поле при закрытии/открытии
        inputValue = inputValue.Replace("`", "").Replace("ё", "").Trim();

        if (string.IsNullOrWhiteSpace(inputValue))
        {
            inputField.ActivateInputField();
            return;
        }

        LogMessage("> " + inputValue, Color.white);

        // ==========================================
        // НОВОЕ: Сохраняем команду в историю
        // ==========================================
        // Сохраняем, только если это не дубликат предыдущей команды
        if (commandHistory.Count == 0 || commandHistory[commandHistory.Count - 1] != inputValue)
        {
            commandHistory.Add(inputValue);
        }
        historyIndex = commandHistory.Count; // Сбрасываем ползунок истории

        string[] parts = inputValue.Split(' ');
        string command = parts[0].ToLower();

        string[] args = new string[parts.Length - 1];
        Array.Copy(parts, 1, args, 0, parts.Length - 1);

        if (commands.ContainsKey(command))
        {
            try
            {
                commands[command].Invoke(args);
            }
            catch (Exception e)
            {
                LogMessage("Ошибка: " + e.Message, Color.red);
            }
        }
        else
        {
            LogMessage("Неизвестная команда: " + command, new Color(1f, 0.6f, 0f)); // Оранжевый цвет
        }

        SetInputFieldText("");
    }

    public void LogMessage(string message, Color color)
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        // Формируем новую строку
        string newLine = $"<color=#{colorHex}>{message}</color>";

        // Добавляем строку в список
        logLines.Add(newLine);

        // Если строк стало слишком много — удаляем самую старую (верхнюю)
        if (logLines.Count > maxLogLines)
        {
            logLines.RemoveAt(0);
        }

        // Склеиваем все строки через Enter и отдаем компоненту Text
        logText.text = string.Join("\n", logLines);

        // Обновляем размеры UI и крутим скролл вниз
        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}