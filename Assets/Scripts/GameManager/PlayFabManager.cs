using PlayFab;
using PlayFab.ClientModels;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager Instance;
    [HideInInspector] public string myPlayFabId;
    [HideInInspector] public string myUsername;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
    }
    private void Start() { SceneManager.LoadScene("01_Auth"); }
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
                var nameRequest = new UpdateUserTitleDisplayNameRequest { DisplayName = username };
                PlayFabClientAPI.UpdateUserTitleDisplayName(nameRequest,
                    nameResult =>
                    {
                        myPlayFabId = result.PlayFabId;
                        myUsername = nameResult.DisplayName;
                        onSuccess?.Invoke("Регистрация успешна!");
                        SceneManager.LoadScene("02_Garage");
                    },
                    nameError =>
                    {
                        Debug.LogWarning("Аккаунт создан, но DisplayName не установлен: " + nameError.ErrorMessage);
                        myPlayFabId = result.PlayFabId;
                        myUsername = username;
                        onSuccess?.Invoke("Регистрация успешна!");
                        SceneManager.LoadScene("02_Garage");
                    });
            },
            error => { onError?.Invoke(error.GenerateErrorReport()); });
    }

    public void LoginUser(string username, string password, Action<string> onSuccess, Action<string> onError)
    {
        var request = new LoginWithPlayFabRequest
        {
            Username = username,
            Password = password,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams { GetPlayerProfile = true }
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
            error => { onError?.Invoke(error.GenerateErrorReport()); });
    }
}