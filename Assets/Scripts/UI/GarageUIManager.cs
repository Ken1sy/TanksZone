using UnityEngine;
using UnityEngine.SceneManagement;

public class GarageUIManager : MonoBehaviour
{
    [Header("Настройки сцен")]
    [Tooltip("Точное название сцены с картой битвы")]
    public string battleSceneName = "BattleScene";

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