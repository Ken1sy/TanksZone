using Unity.VisualScripting;
using UnityEngine;

public class TrackBlendShapeAnimator : MonoBehaviour
{
    [Header("Settings")]
    public int blendShapeIndex = 0;
    public SkinnedMeshRenderer trackRenderer;

    [Tooltip("Длина одного сегмента гусеницы(m)")]
    public float segmentLength = 0.209188f;

    private float currentWeight = 0f;

    public void UpdateTrackAnimation(float trackSpeed)
    {
        float delta = (trackSpeed * Time.deltaTime) / segmentLength;

        currentWeight += delta;
        currentWeight = Mathf.Repeat(currentWeight, 1.0f);

        trackRenderer.SetBlendShapeWeight(blendShapeIndex, currentWeight * 100f);
    }
}