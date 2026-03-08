using Unity.VisualScripting;
using UnityEngine;

public class SmartAim : MonoBehaviour
{
    [Header("Aim Settings")]
    public float maxDistance = 100f;
    public float verticalAngleUp = 15f;
    public float verticalAngleDown = 15f;
    public int raysPerAngle = 10;
    public LayerMask targetLayer;
    public LayerMask obstacleLayer;

    [Header("Wall Protection")]
    public float muzzleClearanceRadius = 0.15f;

    [Header("Debug")]
    public bool showGizmos = true;

    private bool targetFoundInFrame;
    private Vector3 currentTargetPoint;
    public Transform muzzlePoint;

    private void Start()
    {
        muzzlePoint = transform.Find("muzzle_point");
    }
    // Этот метод теперь вызывается каждый кадр для обновления Gizmos
    private void Update()
    {
        if (muzzlePoint == null || !showGizmos) return;

        // Постоянно сканируем пространство, чтобы видеть зеленый луч до выстрела
        ScanForTarget(muzzlePoint);
    }

    public Vector3 GetAimDirection(Transform turretBase, Transform muzzle, out bool isBlocked)
    {
        isBlocked = CheckIfBlocked(turretBase, muzzle);
        if (isBlocked) return muzzle.forward;

        return ScanForTarget(muzzle);
    }

    private bool CheckIfBlocked(Transform turretBase, Transform muzzle)
    {
        return Physics.Linecast(turretBase.position, muzzle.position, obstacleLayer) ||
               Physics.CheckSphere(muzzle.position, muzzleClearanceRadius, obstacleLayer);
    }
    private Vector3 ScanForTarget(Transform muzzle)
    {
        targetFoundInFrame = false;

        float totalAngle = verticalAngleUp + verticalAngleDown;
        float step = totalAngle / raysPerAngle;

        for (int i = 0; i <= raysPerAngle; i++)
        {
            float currentAngle = -verticalAngleDown + (step * i);
            Vector3 direction = Quaternion.AngleAxis(currentAngle, muzzle.right) * muzzle.forward;

            if (Physics.Raycast(muzzle.position, direction, out RaycastHit hit, maxDistance, targetLayer | obstacleLayer))
            {
                if ((targetLayer.value & (1 << hit.collider.gameObject.layer)) > 0)
                {
                    targetFoundInFrame = true;
                    currentTargetPoint = hit.point; // Запоминаем для Gizmos
                    return (hit.point - muzzle.position).normalized;
                }
            }
        }
        return muzzle.forward;
    }
    private void OnDrawGizmos()
    {
        if (!showGizmos || Application.isPlaying == false) return;
        if (muzzlePoint == null) return;

        // 1. Отрисовка ГРАНИЦ зоны поиска (Верхний и Нижний лучи)
        Gizmos.color = Color.gray;
        Vector3 topDir = Quaternion.AngleAxis(verticalAngleUp, -muzzlePoint.right) * muzzlePoint.forward;
        Vector3 bottomDir = Quaternion.AngleAxis(-verticalAngleDown, -muzzlePoint.right) * muzzlePoint.forward;

        Gizmos.DrawRay(muzzlePoint.position, topDir * 10f);
        Gizmos.DrawRay(muzzlePoint.position, bottomDir * 10f);

        // 2. Отрисовка луча К ЦЕЛИ (только если цель в прицеле)
        if (Application.isPlaying && targetFoundInFrame)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(muzzlePoint.position, currentTargetPoint);
            Gizmos.DrawWireSphere(currentTargetPoint, 0.2f);
        }

        // Проверка блокировки ствола
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(muzzlePoint.position, muzzleClearanceRadius);
    }
}