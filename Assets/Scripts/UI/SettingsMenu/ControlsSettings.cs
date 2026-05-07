using UnityEngine;
using UnityEngine.UI;

public class ControlsSettings : MonoBehaviour
{
    [Header("UI Ссылки")]
    public Toggle invertReverseToggle;

    private const string INVERT_PREF_KEY = "InvertReverse";

    void Start()
    {
        // Загружаем сохраненную настройку. 
        // 0 - выключено (по умолчанию), 1 - включено
        bool isInverted = PlayerPrefs.GetInt(INVERT_PREF_KEY, 0) == 1;

        if (invertReverseToggle != null)
        {
            // Устанавливаем галочку в правильное положение (без вызова события)
            invertReverseToggle.SetIsOnWithoutNotify(isInverted);

            // Подписываемся на клик игрока
            invertReverseToggle.onValueChanged.AddListener(OnInvertReverseChanged);
        }
    }

    private void OnInvertReverseChanged(bool isOn)
    {
        // Сохраняем настройку в память устройства
        PlayerPrefs.SetInt(INVERT_PREF_KEY, isOn ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("Инверсия заднего хода: " + (isOn ? "ВКЛ" : "ВЫКЛ"));
    }
}