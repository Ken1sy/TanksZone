using UnityEngine;

[System.Serializable]
public struct TankSettings
{
    [Header("Боевые характеристики")]
    public float maxHealth;
    [Header("Движение")]
    public float speed;
    public float acceleration;
    public float reverseAcceleration;
    public float brakingDeceleration;
    public float turnSpeed;
    public float turnAcceleration;
    public float sideAcceleration;
    public float weight;
    public float damping;
    [Header("Подвеска")]
    public float suspensionRayOffsetY;
    public float maxRayLength;
    public float nominalRayLength;
    public int raysPerTrack;
    public float trackSeparation;
    public float trackLength;
    public float springStiffness;
    [Header("Эффекты")]
    public float wobbleFactor;
    public float sideRollFactor;
    public float driftIntensity;
}