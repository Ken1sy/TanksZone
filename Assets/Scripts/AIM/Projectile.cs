using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 200f;
    public float lifetime = 3f;
    public float impactForce = 1500f; // Сила удара по врагу

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Двигаем снаряд вперед
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        Rigidbody hitRb = other.attachedRigidbody;

        if (hitRb != null)
        {
            // Рассчитываем вектор удара (в ту же сторону, куда летел снаряд)
            Vector3 forceVector = transform.forward * impactForce;

            // Прикладываем силу в точку касания для реалистичного вращения
            // Используем Impulse для мгновенного толчка
            hitRb.AddForceAtPosition(forceVector, transform.position, ForceMode.Impulse);
        }

        // Эффекты и уничтожение
        Debug.Log("Попадание в " + other.name);
        Destroy(gameObject);
    }
}