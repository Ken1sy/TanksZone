using UnityEngine;

namespace GameScripts.AIM
{
    public class StreamParticleDetector : MonoBehaviour
    {
        [Tooltip("—сылка на саму пушку (StreamGun), котора€ управл€ет этими частицами")]
        public StreamGun gun;

        // ¬строенный метод Unity, который срабатывает, когда частица касаетс€ коллайдера
        private void OnParticleCollision(GameObject other)
        {
            // “олько стрелок просчитывает попадани€ (чтобы не дублировать сеть)
            if (gun == null || !gun.isLocalPlayer) return;

            // «ащита: частицы не должны дамажить собственный танк!
            if (other.transform.root == gun.transform.root) return;

            // ѕередаем попадание в буфер пушки
            gun.RegisterParticleHit(other);
        }
    }
}