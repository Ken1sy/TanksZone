using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TankChassisController : MonoBehaviour
{
    [Header("Ground Collision")]
    public LayerMask groundLayer;

    [Header("Mode")]
    public bool driftMode = false;

    [Header("Debug Gizmos")]
    public bool showGizmos = true;

    private float speed;
    private float acceleration;
    private float reverseAcceleration;
    private float brakingDeceleration;
    private float turnSpeed;
    private float turnAcceleration;
    private float sideAcceleration;
    private float weight;
    private float damping;

    private float suspensionRayOffsetY;
    private float maxRayLength;
    private float nominalRayLength;
    private int raysPerTrack;
    private float trackSeparation;
    private float trackLength;
    private float springStiffness;

    private float wobbleFactor;
    private float sideRollFactor;
    private float driftIntensity;
    private float trackAirAcceleration;

    private Rigidbody rb;
    private TankTrack leftTrack = new TankTrack();
    private TankTrack rightTrack = new TankTrack();
    private Vector2 inputDirection;
    private float currentEngineForceMag = 0f;
    private float currentLeftAnimSpeed;
    private float currentRightAnimSpeed;
    private TrackBlendShapeAnimator leftTrackAnim;
    private TrackBlendShapeAnimator rightTrackAnim;

    public void OnMove(InputAction.CallbackContext context) => inputDirection = context.ReadValue<Vector2>();

    public void ApplySettings(TankSettings settings)
    {
        if (settings == null) return;

        this.speed = settings.speed;
        this.acceleration = settings.acceleration;
        this.reverseAcceleration = settings.reverseAcceleration;
        this.brakingDeceleration = settings.brakingDeceleration;
        this.turnSpeed = settings.turnSpeed;
        this.turnAcceleration = settings.turnAcceleration;
        this.sideAcceleration = settings.sideAcceleration;
        this.weight = settings.weight;
        this.damping = settings.damping;
        this.suspensionRayOffsetY = settings.suspensionRayOffsetY;
        this.maxRayLength = settings.maxRayLength;
        this.nominalRayLength = settings.nominalRayLength;
        this.raysPerTrack = settings.raysPerTrack;
        this.trackSeparation = settings.trackSeparation;
        this.trackLength = settings.trackLength;
        this.springStiffness = settings.springStiffness;
        this.wobbleFactor = settings.wobbleFactor;
        this.sideRollFactor = settings.sideRollFactor;
        this.driftIntensity = settings.driftIntensity;
        this.trackAirAcceleration = settings.trackAirAcceleration;

        rb = GetComponent<Rigidbody>();
        rb.mass = weight;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0f;
        rb.automaticCenterOfMass = false;
        rb.centerOfMass = new Vector3(0, 0.5f, 0);

        leftTrack.Initialize(raysPerTrack, -trackSeparation / 2, trackLength, suspensionRayOffsetY);
        rightTrack.Initialize(raysPerTrack, trackSeparation / 2, trackLength, suspensionRayOffsetY);
    }

    public void SetTrackAnimators(TrackBlendShapeAnimator lTracks, TrackBlendShapeAnimator rTracks)
    {
        leftTrackAnim = lTracks;
        rightTrackAnim = rTracks;
    }

    void FixedUpdate()
    {
        leftTrack.UpdateTracks(rb, maxRayLength, nominalRayLength, springStiffness, damping, groundLayer);
        rightTrack.UpdateTracks(rb, maxRayLength, nominalRayLength, springStiffness, damping, groundLayer);

        UpdateTrackAnimations();

        int totalContacts = leftTrack.numContacts + rightTrack.numContacts;
        if (totalContacts == 0) return;

        int maxRays = raysPerTrack * 2;
        float contactFactor = Mathf.Clamp01((float)totalContacts / maxRays);

        ApplyLocomotion(totalContacts);
        ApplyRotation(contactFactor);
        ApplySideRoll(contactFactor);
        ApplyAntiDrift(contactFactor);
        ApplyWobble(contactFactor);

    }

    private void ApplyLocomotion(int contactFactor)
    {
        float targetSpeed = inputDirection.y * speed;
        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float usedAccel = 0f;
        if (Mathf.Abs(inputDirection.y) < 0.01f)
        {

            if (Mathf.Abs(currentForwardSpeed) < 0.5f)
            {
                targetSpeed = 0f;
                usedAccel = acceleration * 5f;
            }
            else
            {
                targetSpeed = 0f;
                usedAccel = brakingDeceleration;
            }
        }
        else
        {
            bool isAccelerating = Mathf.Sign(targetSpeed) == Mathf.Sign(currentForwardSpeed);
            usedAccel = isAccelerating ? acceleration : reverseAcceleration;
        }
        float forceMag = (targetSpeed - currentForwardSpeed) * weight * usedAccel * Time.fixedDeltaTime;
        currentEngineForceMag = forceMag;
        Vector3 force = transform.forward * (forceMag * contactFactor);
        rb.AddForce(force, ForceMode.Force);
    }

    private void ApplyRotation(float contactFactor)
    {
        float targetAngularVel = inputDirection.x * (turnSpeed * Mathf.Deg2Rad);
        Vector3 currentAngularVel = transform.InverseTransformDirection(rb.angularVelocity);
        float effectiveTurnAccel = turnAcceleration * contactFactor;
        float newY = Mathf.MoveTowards(currentAngularVel.y, targetAngularVel, effectiveTurnAccel * Mathf.Deg2Rad * Time.fixedDeltaTime);
        rb.angularVelocity = transform.TransformDirection(new Vector3(currentAngularVel.x, newY, currentAngularVel.z));
    }

    private void ApplySideRoll(float contactFactor)
    {
        float currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float speedRatio = Mathf.Clamp01(Mathf.Abs(currentSpeed) / speed);
        float rollTorque = inputDirection.x * speedRatio * sideRollFactor * contactFactor;
        rb.AddRelativeTorque(Vector3.forward * rollTorque, ForceMode.Force);
    }

    private void ApplyAntiDrift(float contactFactor)
    {
        float frictionMultiplier = driftMode ? driftIntensity : 1.0f;
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        Vector3 antiDriftForce = transform.right * (-localVel.x * sideAcceleration * weight * Time.fixedDeltaTime * contactFactor * frictionMultiplier);
        rb.AddForce(antiDriftForce, ForceMode.Impulse);
    }

    private void ApplyWobble(float contactFactor)
    {
        float accelForWobble = currentEngineForceMag / weight;
        float torqueX = accelForWobble * -1f * (wobbleFactor * 0.02f);
        torqueX *= contactFactor;
        rb.AddRelativeTorque(Vector3.right * torqueX, ForceMode.Force);
    }
    private void UpdateTrackAnimations()
    {
        if (leftTrackAnim == null || rightTrackAnim == null) return;

        float targetLeftSpeed = 0f;
        float targetRightSpeed = 0f;

        if (leftTrack.numContacts > 0 || rightTrack.numContacts > 0)
        {
            float localForwardSpeed = transform.InverseTransformDirection(rb.linearVelocity).z;
            float rotationSpeed = rb.angularVelocity.y * (trackSeparation / 2f);

            targetLeftSpeed = localForwardSpeed + rotationSpeed;
            targetRightSpeed = localForwardSpeed - rotationSpeed;

            currentLeftAnimSpeed = targetLeftSpeed;
            currentRightAnimSpeed = targetRightSpeed;
        }
        else
        {
            float forwardInput = inputDirection.y;
            float turnInput = inputDirection.x;

            targetLeftSpeed = (forwardInput + turnInput) * speed;
            targetRightSpeed = (forwardInput - turnInput) * speed;

            currentLeftAnimSpeed = Mathf.MoveTowards(
                currentLeftAnimSpeed,
                targetLeftSpeed,
                trackAirAcceleration * Time.deltaTime
            );

            currentRightAnimSpeed = Mathf.MoveTowards(
                currentRightAnimSpeed,
                targetRightSpeed,
                trackAirAcceleration * Time.deltaTime
            );
        }

        leftTrackAnim.UpdateTrackAnimation(currentLeftAnimSpeed);
        rightTrackAnim.UpdateTrackAnimation(currentRightAnimSpeed);
    }

    #region Gizmos
    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(rb.worldCenterOfMass, 0.15f);
        DrawTrackGizmos(leftTrack);
        DrawTrackGizmos(rightTrack);
    }
    private void DrawTrackGizmos(TankTrack track)
    {
        foreach (var ray in track.rays)
        {
            Vector3 worldOrigin = transform.TransformPoint(ray.localOrigin);
            if (ray.hasCollision)
            {
                // Луч коснулся земли (Зеленый)
                Gizmos.color = Color.green;
                Gizmos.DrawLine(worldOrigin, ray.hit.point);
                Gizmos.DrawWireSphere(ray.hit.point, 0.05f);

                // Вектор силы (Желтый)
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(ray.hit.point, Vector3.up * (ray.hit.distance * 2f));
            }
            else
            {
                // Луч в воздухе (Красный)
                Gizmos.color = Color.red;
                Gizmos.DrawRay(worldOrigin, transform.TransformDirection(Vector3.down) * maxRayLength);
            }
        }
    }
    #endregion
}