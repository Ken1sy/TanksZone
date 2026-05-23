using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;
using System;

public class PlayFabManager : MonoBehaviour
{
    // Паттерн Singleton, чтобы иметь доступ к базе данных из любого скрипта
    public static PlayFabManager Instance;

    [HideInInspector] public string myPlayFabId; // Уникальный ID игрока в базе
    [HideInInspector] public string myUsername;  // Никнейм игрока

    private void Awake()
    {
        // Делаем этот объект бессмертным
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Сцена 00_Bootstrap нужна только для инициализации. 
        // Как только менеджер родился, сразу грузим сцену логина (01_Auth)
        SceneManager.LoadScene("01_Auth");
    }

    // ==========================================
    // РЕГИСТРАЦИЯ
    // ==========================================
    public void RegisterUser(string username, string email, string password, Action<string> onSuccess, Action<string> onError)
    {
        var request = new RegisterPlayFabUserRequest
        {
            Username = username,
            Email = email,
            Password = password,
            RequireBothUsernameAndEmail = true
        };

        PlayFabClientAPI.RegisterPlayFabUser(request,
            result =>
            {
                // Если регистрация прошла успешно, СРАЗУ задаем DisplayName равный Логину
                var nameRequest = new UpdateUserTitleDisplayNameRequest
                {
                    DisplayName = username
                };

                PlayFabClientAPI.UpdateUserTitleDisplayName(nameRequest,
                    nameResult =>
                    {
                        // Имя успешно установлено!
                        myPlayFabId = result.PlayFabId;
                        myUsername = nameResult.DisplayName;

                        onSuccess?.Invoke("Регистрация успешна!");
                        SceneManager.LoadScene("02_Garage");
                    },
                    nameError =>
                    {
                        // Регистрация прошла, но имя почему-то не установилось (например, такое уже есть)
                        Debug.LogWarning("Аккаунт создан, но DisplayName не установлен: " + nameError.ErrorMessage);
                        myPlayFabId = result.PlayFabId;
                        myUsername = username; // Оставляем просто логин

                        onSuccess?.Invoke("Регистрация успешна!");
                        SceneManager.LoadScene("02_Garage");
                    });
            },
            error =>
            {
                onError?.Invoke(error.GenerateErrorReport());
            });
    }

    // ==========================================
    // ЛОГИН
    // ==========================================
    public void LoginUser(string username, string password, Action<string> onSuccess, Action<string> onError)
    {
        var request = new LoginWithPlayFabRequest
        {
            Username = username,
            Password = password,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };
        PlayFabClientAPI.LoginWithPlayFab(request,
            result =>
            {
                myPlayFabId = result.PlayFabId;
                if (result.InfoResultPayload.PlayerProfile != null)
                {
                    myUsername = result.InfoResultPayload.PlayerProfile.DisplayName;
                }

                onSuccess?.Invoke("Вход выполнен!");
                SceneManager.LoadScene("02_Garage");
            },
            error =>
            {
                onError?.Invoke(error.GenerateErrorReport());
            });
    }
}