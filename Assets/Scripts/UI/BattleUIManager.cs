using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleUIManager : MonoBehaviour
{
    [Header("Настройки сцен")]
    [Tooltip("Точное название сцены ангара")]
    public string hangarSceneName = "GarageScene";

    public void ReturnToHangar()
    {
        Debug.Log("Возврат в ангар...");

        SceneManager.LoadScene(hangarSceneName);
    }
}