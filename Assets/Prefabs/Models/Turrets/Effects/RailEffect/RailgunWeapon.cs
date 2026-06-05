using System.Collections;
using UnityEngine;

public class RailgunWeapon : MonoBehaviour
{
    [Header("Настройки оружия")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private float maxDistance = 300f;
    [SerializeField] private float chargeTime = 1.1f;
    [SerializeField] private LayerMask hitMask;

    [Header("Визуал")]
    [SerializeField] private RailgunHybridBeam beamPrefab;
    [SerializeField] private RailgunChargeEffect chargeController; // <-- Новый контроллер заряда
    [SerializeField] private GameObject impactEffectPrefab;

    private bool _isCharging = false;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryFire();
        }
    }

    public void TryFire()
    {
        if (_isCharging) return;
        StartCoroutine(FireSequence());
    }

    private IEnumerator FireSequence()
    {
        _isCharging = true;

        if (chargeController != null)
        {
            chargeController.StartCharge(chargeTime);
        }

        yield return new WaitForSeconds(chargeTime);

        // ИЗМЕНЕНО: Вызываем мягкое завершение вместо жесткой очистки
        if (chargeController != null)
        {
            chargeController.FinishCharge();
        }

        ExecuteHitscan();

        _isCharging = false;
    }

    private void ExecuteHitscan()
    {
        muzzlePoint = this.transform;
        Vector3 targetPoint = muzzlePoint.position + muzzlePoint.forward * maxDistance;

        if (Physics.Raycast(muzzlePoint.position, muzzlePoint.forward, out RaycastHit hit, maxDistance, hitMask))
        {
            targetPoint = hit.point;

            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }

        if (beamPrefab != null)
        {
            RailgunHybridBeam beam = Instantiate(beamPrefab);
            beam.FireBeam(muzzlePoint.position, targetPoint);
        }
    }
}