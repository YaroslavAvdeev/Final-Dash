using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    // Час, через який об'єкт буде знищено (наприклад, 2 секунди)
    public float lifetime = 2f; 

    void Start()
    {
        // Викликаємо функцію Destroy через заданий час
        Destroy(gameObject, lifetime);
    }
}