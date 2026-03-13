using UnityEngine;

[CreateAssetMenu(fileName = "HornetCFG", menuName = "Scriptable Objects/HornetCFG")]
[System.Serializable]
public class HullConfig : ScriptableObject
{
    [Header("Movement")]
    public float speed = 30f;
    public float acceleration = 8f;
    public float reverseAcceleration = 8f;
    public float brakingDeceleration = 12f;
    public float turnSpeed = 110f;
    public float turnAcceleration = 350f;
    public float sideAcceleration = 40f;
    public float weight = 1500f;
    public float damping = 1500f;

    [Header("Suspension")]
    public float suspensionRayOffsetY = 0.1f;
    public float maxRayLength = 0.43f;
    public float nominalRayLength = 0f;
    public int raysPerTrack = 5;
    public float trackSeparation = 2.7f;
    public float trackLength = 5f;
    public float springStiffness = 30000f;

    [Header("Visuals & Arcade")]
    public float wobbleFactor = 500000f;
    public float sideRollFactor = 20000f;
    public float driftIntensity = 0.01f;
    public float trackAirAcceleration = 50f;
}
