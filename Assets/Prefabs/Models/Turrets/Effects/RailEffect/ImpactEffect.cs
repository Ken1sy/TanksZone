using UnityEngine;

public class ImpactEffect : MonoBehaviour
{
    void Start()
    {
        Destroy(gameObject, 3f);
    }
}
