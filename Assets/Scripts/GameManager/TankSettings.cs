using UnityEngine;

[System.Serializable]
public class TankSettings
{
    public float speed;
    public float acceleration;
    public float reverseAcceleration;
    public float brakingDeceleration;
    public float turnSpeed;
    public float turnAcceleration;
    public float sideAcceleration;
    public float weight;
    public float damping;

    public float suspensionRayOffsetY;
    public float maxRayLength;
    public float nominalRayLength;
    public int raysPerTrack;
    public float trackSeparation;
    public float trackLength;
    public float springStiffness;

    public float wobbleFactor;
    public float sideRollFactor;
    public float driftIntensity;
    public float trackAirAcceleration;
}
