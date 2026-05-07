using UnityEngine;

[CreateAssetMenu(fileName = "NewTankSkin", menuName = "Tank Game/Skin Config")]
public class TankSkinConfig : ScriptableObject
{
    public string skinId; // Например: "zeus"
    public Texture2D skinTexture; // Твой image.jpg
    public Texture2D previewTexture; // Твой preview.png

    public Vector2 tiling = new Vector2(5f, 5f);
}