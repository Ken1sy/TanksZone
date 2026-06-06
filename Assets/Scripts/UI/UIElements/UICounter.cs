using TMPro; // Пространство имен для TextMeshPro
using UnityEngine;
using UnityEngine.UI;

public class UICounter : MonoBehaviour
{
    [Header("Компоненты UI")]
    public TMP_InputField numberInput;
    public Button plusButton;
    public Button minusButton;
    [Header("Ограничения")]
    public int minValue = 0;
    public int maxValue = 999;

    private int _currentValue = 0;
    private void Awake() { numberInput.contentType = TMP_InputField.ContentType.IntegerNumber; }
    private void OnEnable()
    {
        plusButton.onClick.AddListener(OnPlusClicked);
        minusButton.onClick.AddListener(OnMinusClicked);
        numberInput.onEndEdit.AddListener(OnInputSubmit);
        numberInput.onValueChanged.AddListener(OnInputChanged);
    }
    private void OnDisable()
    {
        plusButton.onClick.RemoveListener(OnPlusClicked);
        minusButton.onClick.RemoveListener(OnMinusClicked);
        numberInput.onEndEdit.RemoveListener(OnInputSubmit);
        numberInput.onValueChanged.RemoveListener(OnInputChanged);
    }
    private void Start() { UpdateUI(); }
    private void OnPlusClicked() { _currentValue++; ClampAndUpdate(); }
    private void OnMinusClicked() { _currentValue--; ClampAndUpdate(); }
    private void OnInputChanged(string input)
    {
        if (int.TryParse(input, out int result))
        {
            if (result > maxValue) { _currentValue = maxValue; UpdateUI(); }
            else { _currentValue = result; }
        }
    }
    private void OnInputSubmit(string input)
    {
        if (int.TryParse(input, out int result)) { _currentValue = result; }
        else { _currentValue = minValue; }
        ClampAndUpdate();
    }
    private void ClampAndUpdate() { _currentValue = Mathf.Clamp(_currentValue, minValue, maxValue); UpdateUI(); }
    private void UpdateUI() { numberInput.SetTextWithoutNotify(_currentValue.ToString()); }
    public int GetValue() { return _currentValue; }
}