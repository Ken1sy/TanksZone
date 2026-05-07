using UnityEngine;
using UnityEngine.SceneManagement;

public class GarageUIManager : MonoBehaviour
{
    [Header("Настройки сцен")]
    public string battleSceneName = "BattleScene";

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
    }
    public void StartBattle()
    {
        Debug.Log("Загрузка карты битвы...");
        SceneManager.LoadScene(battleSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Выход из игры...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}