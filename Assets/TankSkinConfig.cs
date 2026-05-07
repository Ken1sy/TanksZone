using UnityEngine;

[CreateAssetMenu(fileName = "NewTankSkin", menuName = "Tank Game/Skin Config")]
public class TankSkinConfig : ScriptableObject
{
    public string skinId;
    public Texture2D skinTexture;
    public Texture2D previewTexture;

    [Header("Тайлинг (Плотность на 1 метр)")]
    public float baseTiling = 1.5f;

    [Header("Анимация (Sprite Sheet)")]
    public Vector2 gridSize = new Vector2(1, 1);
    public float animationSpeed = 0f;            
    public float totalFrames = 1f;               
}