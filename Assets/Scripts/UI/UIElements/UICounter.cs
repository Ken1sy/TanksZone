using UnityEngine;
using UnityEngine.UI;
using TMPro; // Пространство имен для TextMeshPro

public class UICounter : MonoBehaviour
{
    [Header("Компоненты UI")]
    [Tooltip("Поле ввода числа (TextMeshPro - InputField)")]
    public TMP_InputField numberInput;
    public Button plusButton;
    public Button minusButton;

    [Header("Ограничения")]
    public int minValue = 0;
    public int maxValue = 999;

    // Текущее значение счетчика
    private int _currentValue = 0;

    private void Awake()
    {
        // Ограничиваем ввод в InputField только цифрами
        numberInput.contentType = TMP_InputField.ContentType.IntegerNumber;
    }

    private void OnEnable()
    {
        // Подписываемся на события кнопок и изменения текста
        plusButton.onClick.AddListener(OnPlusClicked);
        minusButton.onClick.AddListener(OnMinusClicked);
        numberInput.onEndEdit.AddListener(OnInputSubmit);
        numberInput.onValueChanged.AddListener(OnInputChanged);
    }

    private void OnDisable()
    {
        // Отписываемся от событий для предотвращения утечек памяти
        plusButton.onClick.RemoveListener(OnPlusClicked);
        minusButton.onClick.RemoveListener(OnMinusClicked);
        numberInput.onEndEdit.RemoveListener(OnInputSubmit);
        numberInput.onValueChanged.RemoveListener(OnInputChanged);
    }

    private void Start()
    {
        // Инициализация стартового значения
        UpdateUI();
    }

    private void OnPlusClicked()
    {
        _currentValue++;
        ClampAndUpdate();
    }

    private void OnMinusClicked()
    {
        _currentValue--;
        ClampAndUpdate();
    }

    // Вызывается каждый раз при вводе нового символа
    private void OnInputChanged(string input)
    {
        if (int.TryParse(input, out int result))
        {
            // Если ввели число больше максимума, сразу корректируем
            if (result > maxValue)
            {
                _currentValue = maxValue;
                UpdateUI(); // Принудительно обновляем UI
            }
            else
            {
                _currentValue = result;
            }
        }
    }

    // Вызывается при завершении ввода (нажатие Enter или клик вне поля)
    private void OnInputSubmit(string input)
    {
        if (int.TryParse(input, out int result))
        {
            _currentValue = result;
        }
        else
        {
            _currentValue = minValue; // Защита от пустого поля
        }

        ClampAndUpdate();
    }

    private void ClampAndUpdate()
    {
        // Жестко ограничиваем значение в заданных рамках
        _currentValue = Mathf.Clamp(_currentValue, minValue, maxValue);
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Обновляем текст в InputField без вызова события onValueChanged (SetTextWithoutNotify)
        numberInput.SetTextWithoutNotify(_currentValue.ToString());
    }

    /// <summary>
    /// Публичный метод для получения текущего значения (понадобится для игровой логики)
    /// </summary>
    public int GetValue()
    {
        return _currentValue;
    }
}