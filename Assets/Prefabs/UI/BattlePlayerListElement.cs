using UnityEngine;
using UnityEngine.UI;

public class BattlePlayerListElement : MonoBehaviour
{
    [Header("UI Элементы Игрока")]
    public Text nicknameText;   // Текст с ником (admin, Игрок и т.д.)
    public Text scoreText;      // Текст со счетом (убийства)
    public Image rankIcon;      // Иконка звания

    /// <summary>
    /// Метод заполнения данных игрока. Вызывается из BattleListManager.
    /// </summary>
    public void Setup(string playerName, int score, int rankIndex, Sprite[] rankSprites)
    {
        if (nicknameText != null) nicknameText.text = playerName;
        if (scoreText != null) scoreText.text = score.ToString();

        // Отображение иконки ранга
        if (rankIcon != null && rankSprites != null && rankSprites.Length > 0)
        {
            // Помним, что ранг мы сохраняли с +1. Значит для массива вычитаем 1.
            int arrayIndex = Mathf.Clamp(rankIndex - 1, 0, rankSprites.Length - 1);
            rankIcon.sprite = rankSprites[arrayIndex];
        }
    }
}