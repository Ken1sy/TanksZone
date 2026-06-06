using UnityEngine;

public class TrackUVAnimator : MonoBehaviour
{
    [Header("Settings")]
    public Renderer trackRenderer;
    public int materialIndex = 0;
    public string texturePropertyName = "_BaseMap";
    public Vector2 scrollAxis = new Vector2(0.5f, 0f);

    private Material trackMaterial;
    private Vector2 currentOffset = Vector2.zero;

    void Start()
    {
        if (trackRenderer != null) { trackMaterial = trackRenderer.materials[materialIndex]; }
        else
        {
            trackRenderer = GetComponent<Renderer>();
            if (trackRenderer != null) trackMaterial = trackRenderer.materials[materialIndex];
        }
    }

    public void UpdateTrackAnimation(float trackSpeed)
    {
        if (trackMaterial == null) return;
        currentOffset += (scrollAxis * trackSpeed) * Time.deltaTime;
        currentOffset.x = Mathf.Repeat(currentOffset.x, 1.0f);
        currentOffset.y = Mathf.Repeat(currentOffset.y, 1.0f);
        trackMaterial.SetTextureOffset(texturePropertyName, currentOffset);
    }
}