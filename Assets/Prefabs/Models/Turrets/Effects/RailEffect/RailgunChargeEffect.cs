using UnityEngine;

public class RailgunChargeEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem sparksParticles;
    [SerializeField] private ParticleSystem coreParticles;

    public void StartCharge(float chargeTime)
    {
        sparksParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        coreParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var sparksMain = sparksParticles.main;
        sparksMain.duration = chargeTime;

        var coreMain = coreParticles.main;
        coreMain.duration = chargeTime;
        coreMain.startLifetime = chargeTime;

        sparksParticles.Play();
        coreParticles.Play();
    }

    public void FinishCharge()
    {
        // Мгновенно удаляем все летящие искры в момент выстрела, чтобы они не висели в пустоте
        sparksParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Ядро оставляем как есть, оно уже сжалось в 0 по кривой Size
        coreParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    public void StopCharge()
    {
        sparksParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        coreParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}