using UnityEngine;

[System.Serializable]
public class SuspensionRay
{
    public Vector3 localOrigin;
    public bool hasCollision;
    public RaycastHit hit;

    private float lastCompression;
    private const float MAX_SLOPE_ANGLE = 60f;

    public void UpdatePhysics(Rigidbody rb, Vector3 direction,
        float maxLen, float nominalLen, float springStiffness, float damping, LayerMask layerMask)
    {
        Vector3 worldOrigin = rb.transform.TransformPoint(localOrigin);
        Vector3 worldDir = rb.transform.TransformDirection(direction);
        PhysicsScene roomPhysics = rb.gameObject.scene.GetPhysicsScene();
        if (roomPhysics.Raycast(worldOrigin, worldDir, out hit, maxLen, layerMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.root == rb.transform.root) { hasCollision = false; return; }
            float groundAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (groundAngle > MAX_SLOPE_ANGLE)
            {
                hasCollision = false;
                lastCompression = 0;
                return;
            }
            hasCollision = true;
            float compression = maxLen - hit.distance;
            float compressionVelocity = (compression - lastCompression) / Time.fixedDeltaTime;
            lastCompression = compression;
            float springForce = (compression * springStiffness) + (compressionVelocity * damping);
            springForce = Mathf.Max(0, springForce);
            rb.AddForceAtPosition(-worldDir * springForce, hit.point);
        }
        else { hasCollision = false; lastCompression = 0; }
    }
}