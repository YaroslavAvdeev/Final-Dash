using UnityEngine;
using System.Collections;

public class Hazard : MonoBehaviour
{
    [Header("Settings")]
    public int damageAmount = 1;      // Скільки життів знімати
    public float damageInterval = 1f; // Як часто бити (в секундах)

    private Coroutine damageCoroutine;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (damageCoroutine == null)
            {
                damageCoroutine = StartCoroutine(ApplyDamageOverTime(other.gameObject));
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
        }
    }

    private IEnumerator ApplyDamageOverTime(GameObject playerObject)
    {
        while (true)
        {
            PlayerLife playerLife = playerObject.GetComponent<PlayerLife>();
            
            if (playerLife != null)
            {
                playerLife.ReceiveImpact(damageAmount);

                // ВИКЛИКАЄМО ТРЯСІННЯ ПРИ КОЖНОМУ УДАРІ
                if (CameraFollow.Instance != null)
                {
                    // 0.15с - тривалість, 0.12f - сила (можна міняти за смаком)
                    CameraFollow.Instance.Shake(0.15f, 0.12f); 
                }
            }

            yield return new WaitForSeconds(damageInterval);
            
            if (playerObject == null)
            {
                damageCoroutine = null;
                yield break;
            }
        }
    }
}