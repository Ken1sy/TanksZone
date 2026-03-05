using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TankTrack
{
    public List<SuspensionRay> rays = new List<SuspensionRay>();
    public int numContacts;

    public void Initialize(int count, float xOffset, float length, float yOffset)
    {
        rays.Clear();
        float step = length / (count - 1);
        float startZ = length / 2f;

        for (int i = 0; i < count; i++)
        {
            var ray = new SuspensionRay();
            // Распределяем лучи вдоль гусеницы
            ray.localOrigin = new Vector3(xOffset, yOffset, startZ - (i * step));
            rays.Add(ray);
        }
    }

    public void UpdateTracks(Rigidbody rb, float maxLen, float nominalLen, float spring, float damping, LayerMask mask)
    {
        numContacts = 0;
        foreach (var ray in rays)
        {
            ray.UpdatePhysics(rb, Vector3.down, maxLen, nominalLen, spring, damping, mask);
            if (ray.hasCollision) numContacts++;
        }
    }
}