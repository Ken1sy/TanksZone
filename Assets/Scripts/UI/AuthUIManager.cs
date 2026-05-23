using UnityEngine;
using UnityEngine.UI;

public class AuthUIManager : MonoBehaviour
{
    [Header("Поля ввода (Регистрация)")]
    public GameObject registerPanel;
    public InputField regLoginInput;
    public InputField regEmailInput;
    public InputField regPasswordInput;
    public InputField regPasswordConfirmInput;

    [Header("Поля ввода (Логин)")]
    public GameObject loginPanel;
    public InputField loginLoginInput;
    public InputField loginPasswordInput;
    public Toggle rememberMeToggle;

    [Header("Вывод ошибок")]
    public Text loginMessageText;
    public Text regMessageText;
    public Button[] allButtons;

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        ShowLoginPanel();

        // НОВОЕ: Проверяем, сохранял ли игрок данные в прошлый раз
        if (PlayerPrefs.HasKey("SavedLogin") && PlayerPrefs.HasKey("SavedPassword"))
        {
            loginLoginInput.text = PlayerPrefs.GetString("SavedLogin");
            loginPasswordInput.text = PlayerPrefs.GetString("SavedPassword");

            if (rememberMeToggle != null)
            {
                rememberMeToggle.isOn = true;
            }

            // Опционально: если хочешь, чтобы игра входила автоматически без нажатия на кнопку,
            // просто раскомментируй строку ниже:
            // OnLoginButtonClicked(); 
        }
    }

    public void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        if (loginMessageText != null) loginMessageText.text = "";
    }

    public void ShowRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        if (regMessageText != null) regMessageText.text = "";
    }

    // Кнопка: ЗАРЕГИСТРИРОВАТЬСЯ
    public void OnRegisterButtonClicked()
    {
        // 1. Проверяем, совпадают ли пароли
        if (regPasswordInput.text != regPasswordConfirmInput.text)
        {
            Debug.Log("Ошибка: Пароли не совпадают!");
            if (regMessageText != null)
            {
                regMessageText.text = "Ошибка: Пароли не совпадают!";
                regMessageText.color = Color.red;
            }
            return; // Прерываем регистрацию
        }

        // 2. Проверяем длину пароля (PlayFab требует минимум 6 символов)
        if (regPasswordInput.text.Length < 6)
        {
            Debug.Log("Пароль должен быть не менее 6 символов!");
            if (regMessageText != null)
            {
                regMessageText.text = "Пароль должен быть не менее 6 символов!";
                regMessageText.color = Color.red;
            }
            return;
        }

        SetButtonsInteractable(false);

        Debug.Log("Связь с сервером...");
        if (regMessageText != null)
        {
            regMessageText.text = "Связь с сервером...";
            regMessageText.color = Color.yellow;
        }

        // 4. Отправляем данные в PlayFab
        PlayFabManager.Instance.RegisterUser(
            regLoginInput.text,
            regEmailInput.text,
            regPasswordInput.text,
            onSuccess: (msg) =>
            {
                Debug.Log(msg);
                if (regMessageText != null) { regMessageText.text = msg; regMessageText.color = Color.green; }
            },
            onError: (err) =>
            {
                Debug.Log(err);
                if (regMessageText != null) { regMessageText.text = err; regMessageText.color = Color.red; }
                SetButtonsInteractable(true);
            }
        );
    }

    // Кнопка: ВОЙТИ
    public void OnLoginButtonClicked()
    {
        SetButtonsInteractable(false);
        Debug.Log("Авторизация...");
        if (loginMessageText != null)
        {
            loginMessageText.text = "Авторизация...";
            loginMessageText.color = Color.yellow;
        }

        PlayFabManager.Instance.LoginUser(
            loginLoginInput.text,
            loginPasswordInput.text,
            onSuccess: (msg) =>
            {
                // НОВОЕ: Если галочка стоит - сохраняем данные. Если нет - удаляем из памяти.
                if (rememberMeToggle != null && rememberMeToggle.isOn)
                {
                    PlayerPrefs.SetString("SavedLogin", loginLoginInput.text);
                    PlayerPrefs.SetString("SavedPassword", loginPasswordInput.text);
                    PlayerPrefs.Save();
                }
                else
                {
                    PlayerPrefs.DeleteKey("SavedLogin");
                    PlayerPrefs.DeleteKey("SavedPassword");
                }

                if (loginMessageText != null) { loginMessageText.text = msg; loginMessageText.color = Color.green; }
            },
            onError: (err) =>
            {
                if (loginMessageText != null) { loginMessageText.text = err; loginMessageText.color = Color.red; }
                SetButtonsInteractable(true);
            }
        );
    }

    private void SetButtonsInteractable(bool state)
    {
        foreach (var btn in allButtons)
        {
            if (btn != null) btn.interactable = state;
        }
    }
}