using UnityEngine;

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

    private float currentLeftAnimSpeed;
    private float currentRightAnimSpeed;
    private TrackUVAnimator leftTrackAnim;
    private TrackUVAnimator rightTrackAnim;

    private Rigidbody rb;
    private TankTrack leftTrack = new TankTrack();
    private TankTrack rightTrack = new TankTrack();
    private Vector2 inputDirection;
    private float currentEngineForceMag = 0f;

    public void SetMoveInput(Vector2 input)
    {
        inputDirection = input;
    }

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

        rb = GetComponent<Rigidbody>();
        rb.mass = weight;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0f;
        rb.automaticCenterOfMass = true;

        leftTrack.Initialize(raysPerTrack, -trackSeparation / 2, trackLength, suspensionRayOffsetY);
        rightTrack.Initialize(raysPerTrack, trackSeparation / 2, trackLength, suspensionRayOffsetY);
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
        ApplyFriction(contactFactor);
        ApplyWobble(contactFactor);
    }

    private void ApplyLocomotion(float contactFactor)
    {
        // 1. Сохраняем фикс диагонального движения (чтобы скорость не падала в поворотах)
        float gasInput = inputDirection.y;
        if (Mathf.Abs(inputDirection.x) > 0.01f && Mathf.Abs(gasInput) > 0.01f)
        {
            gasInput = gasInput / Mathf.Max(Mathf.Abs(inputDirection.x), Mathf.Abs(gasInput));
        }

        float targetSpeed = gasInput * speed;
        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float usedAccel = 0f;

        bool isParking = false; // Флаг: включился ли ручник?

        if (Mathf.Abs(gasInput) < 0.01f)
        {
            if (Mathf.Abs(currentForwardSpeed) < 0.1f)
            {
                targetSpeed = 0f;
                // ВОЗВРАЩАЕМ жесткий тормоз: он идеально держит танк на месте от микро-скатываний
                usedAccel = brakingDeceleration * 5f;
                isParking = true;
            }
            else
            {
                targetSpeed = 0f;
                usedAccel = brakingDeceleration; // Плавное торможение накатом
            }
        }
        else
        {
            bool isAccelerating = Mathf.Sign(targetSpeed) == Mathf.Sign(currentForwardSpeed);
            usedAccel = isAccelerating ? acceleration : reverseAcceleration;
        }

        float forceMag = (targetSpeed - currentForwardSpeed) * weight * usedAccel * Time.fixedDeltaTime;

        // ========================================================
        // 2. ИСПРАВЛЕНИЕ КЛЕВКА (Прячем ручник от скрипта Wobble)
        // ========================================================
        if (isParking)
        {
            // Если танк почти остановился, мы плавно гасим визуальную раскачку в ноль.
            // Скрипт Wobble ничего не узнает о резком ручнике, и танк не клюнет носом!
            currentEngineForceMag = Mathf.Lerp(currentEngineForceMag, 0f, 10f * Time.fixedDeltaTime);
        }
        else
        {
            // В обычном движении передаем силу для раскачки как есть
            currentEngineForceMag = forceMag;
        }

        // Применяем саму силу движения к физике
        Vector3 force = transform.forward * (forceMag * contactFactor);
        rb.AddForce(force, ForceMode.Force);
    }

    private void ApplyRotation(float contactFactor)
    {
        float turnInput = inputDirection.x;

        // Читаем настройку из меню. 
        // 0 = Танковое управление (галочка снята). Танк всегда крутится куда нажато.
        // 1 = Автомобильное управление (галочка стоит). При езде назад перед уходит в обратную сторону.
        bool isCarSteering = PlayerPrefs.GetInt("InvertReverse", 0) == 1;

        // Если мы едем назад (нажата S) И игрок включил инверсию в настройках
        if (inputDirection.y < -0.01f && isCarSteering)
        {
            turnInput *= -1f; // Только тогда меняем сторону поворота
        }

        float targetAngularVel = turnInput * (turnSpeed * Mathf.Deg2Rad);
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

    private void ApplyFriction(float contactFactor)
    {
        if (contactFactor <= 0.01f) return;

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        float frictionMultiplier = driftMode ? driftIntensity : 1.0f;

        // 1. Добавляем Time.fixedDeltaTime, чтобы сила не была астрономической.
        // Умножаем на 10f для сохранения упругости анти-дрифта.
        float sideVelocityForce = -localVel.x * weight * sideAcceleration * Time.fixedDeltaTime * 10f;

        // 2. Радикально снижаем "бетонный" лимит сцепления. 
        // Раньше было * 2.0f (164 000 Ньютонов). Теперь * 0.2f (около 16 400 Ньютонов).
        // Теперь двигатель твоего танка сможет преодолеть эту силу и заставить болванку скользить вбок!
        float maxGripForce = weight * sideAcceleration * 0.5f;

        // Обрезаем силу трения (срыв гусениц)
        sideVelocityForce = Mathf.Clamp(sideVelocityForce, -maxGripForce, maxGripForce);

        Vector3 gravityForce = Physics.gravity * rb.mass;
        float gravitySideComponent = Vector3.Dot(gravityForce, transform.right);

        // Формируем финальный вектор
        Vector3 finalAntiDriftForce = transform.right * (sideVelocityForce - gravitySideComponent) * contactFactor * frictionMultiplier;

        rb.AddForce(finalAntiDriftForce, ForceMode.Force);
    }

    private void ApplyWobble(float contactFactor)
    {
        float accelForWobble = currentEngineForceMag / weight;
        float torqueX = accelForWobble * -1f * (wobbleFactor * 0.02f);
        torqueX *= contactFactor;
        rb.AddRelativeTorque(Vector3.right * torqueX, ForceMode.Force);
    }

    public void SetTrackAnimators(TrackUVAnimator lTracks, TrackUVAnimator rTracks)
    {
        leftTrackAnim = lTracks;
        rightTrackAnim = rTracks;
    }

    private void UpdateTrackAnimations()
    {
        if (leftTrackAnim == null || rightTrackAnim == null) return;

        // Если танк на земле, вычисляем физическую скорость гусениц (с учетом поворота)
        if (leftTrack.numContacts > 0 || rightTrack.numContacts > 0)
        {
            float localForwardSpeed = transform.InverseTransformDirection(rb.linearVelocity).z;
            float rotationSpeed = rb.angularVelocity.y * (trackSeparation / 2f);

            currentLeftAnimSpeed = localForwardSpeed + rotationSpeed;
            currentRightAnimSpeed = localForwardSpeed - rotationSpeed;
        }
        else
        {
            // Если танк в воздухе, крутим гусеницы от чистого инпута (педаль газа)
            float forwardInput = inputDirection.y;
            float turnInput = inputDirection.x;

            currentLeftAnimSpeed = Mathf.MoveTowards(currentLeftAnimSpeed, (forwardInput + turnInput) * 5f, 10f * Time.deltaTime);
            currentRightAnimSpeed = Mathf.MoveTowards(currentRightAnimSpeed, (forwardInput - turnInput) * 5f, 10f * Time.deltaTime);
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
                Gizmos.color = Color.green;
                Gizmos.DrawLine(worldOrigin, ray.hit.point);
                Gizmos.DrawWireSphere(ray.hit.point, 0.05f);

                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(ray.hit.point, Vector3.up * (ray.hit.distance * 2f));
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(worldOrigin, transform.TransformDirection(Vector3.down) * maxRayLength);
            }
        }
    }
    #endregion
}