using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TankChassisController : MonoBehaviour
{
    [Header("Parameters")]
    public float speed = 33f;
    public float acceleration = 60f;
    public float reverseAcceleration = 60f;
    public float brakingDeceleration = 110f;
    public float turnSpeed = 110f;
    public float turnAcceleration = 300f;
    public float sideAcceleration = 40f;
    public float weight = 1500f;
    public float damping = 1500f;

    [Header("Suspension Config")]
    public float suspensionRayOffsetY = 0.1f;
    public float maxRayLength = 0.43f;
    public float nominalRayLength = 0f;
    public int raysPerTrack = 5;
    public float trackSeparation = 2.7f;
    public float trackLength = 5f;
    public float springStiffness = 30000f;
    public LayerMask groundLayer;

    [Header("Arcade Wobble (Fake)")]
    public float wobbleFactor = 40000f;
    public float sideRollFactor = 8000f;

    [Header("Mode")]
    public bool driftMode=false;
    public float driftIntensity = 0.15f;

    [Header("Track Visuals")]
    public TrackBlendShapeAnimator leftTrackAnim;
    public TrackBlendShapeAnimator rightTrackAnim;

    [Header("Debug Gizmos")]
    public bool showGizmos = true;

    private Rigidbody rb;
    private TankTrack leftTrack = new TankTrack();
    private TankTrack rightTrack = new TankTrack();
    private Vector2 inputDirection;
    private float currentEngineForceMag = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = weight;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0f;
        rb.automaticCenterOfMass = false;
        rb.centerOfMass = new Vector3(0, 0.5f, 0);

        leftTrack.Initialize(raysPerTrack, -trackSeparation / 2, trackLength, suspensionRayOffsetY);
        rightTrack.Initialize(raysPerTrack, trackSeparation / 2, trackLength, suspensionRayOffsetY);
    }

    public void OnMove(InputAction.CallbackContext context) => inputDirection = context.ReadValue<Vector2>();

    void FixedUpdate()
    {
        leftTrack.UpdateTracks(rb, maxRayLength, nominalRayLength, springStiffness, damping, groundLayer);
        rightTrack.UpdateTracks(rb, maxRayLength, nominalRayLength, springStiffness, damping, groundLayer);
        int totalContacts = leftTrack.numContacts + rightTrack.numContacts;
        if (totalContacts == 0) return;

        int maxRays = raysPerTrack * 2;
        float contactFactor = Mathf.Clamp01((float)totalContacts / maxRays);

        ApplyLocomotion(totalContacts);
        ApplyRotation(contactFactor);
        ApplySideRoll(contactFactor);
        ApplyAntiDrift(contactFactor);
        ApplyWobble(contactFactor);
        UpdateTrackAnimations();
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

        float leftTrackSpeed = 0f;
        float rightTrackSpeed = 0f;

        #region old
        //// 1. Получаем локальную скорость танка (вперед/назад)
        //float localForwardSpeed = transform.InverseTransformDirection(rb.linearVelocity).z;

        //// 2. Получаем угловую скорость (поворот вокруг оси Y)
        //// rb.angularVelocity.y в радианах в секунду
        //float rotationSpeed = rb.angularVelocity.y * (trackSeparation / 2f);

        //// 3. Рассчитываем итоговую скорость для каждой стороны
        //// При повороте направо (angularVelocity.y > 0) левая гусеница едет быстрее вперед, 
        //// а правая — медленнее или назад.
        //float leftSpeed = localForwardSpeed + rotationSpeed;
        //float rightSpeed = localForwardSpeed - rotationSpeed;
        #endregion

        Debug.Log("l Contacts: " + leftTrack.numContacts + " | r Contacts: " + rightTrack.numContacts);

        // ПРОВЕРКА: Если танк касается земли хотя бы одним лучом
        if (leftTrack.numContacts >= 0 || rightTrack.numContacts >= 0)
        {
            // Используем реальную физику для точности
            float localForwardSpeed = transform.InverseTransformDirection(rb.linearVelocity).z;
            float rotationSpeed = rb.angularVelocity.y * (trackSeparation / 2f);

            leftTrackSpeed = localForwardSpeed + rotationSpeed;
            rightTrackSpeed = localForwardSpeed - rotationSpeed;
        }
        else
        {
            // Если в воздухе или перевернуты — крутим на основе ввода (Input)
            // moveInput и turnInput — это твои переменные из OnMove (Vector2)
            float forwardInput = inputDirection.y;
            float turnInput = inputDirection.x;

            // Эмулируем скорость на основе максимальной скорости танка
            leftTrackSpeed = (forwardInput + turnInput) * speed;
            rightTrackSpeed = (forwardInput - turnInput) * speed;

            Debug.Log("leftspeed: " + leftTrackSpeed + " | rightspeed: " + rightTrackSpeed);
        }

        // 4. Передаем скорости в аниматоры
        leftTrackAnim.UpdateTrackAnimation(leftTrackSpeed);
        rightTrackAnim.UpdateTrackAnimation(leftTrackSpeed);
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