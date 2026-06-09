using UnityEngine;
using System.Collections;

public class HeartPickup : MonoBehaviour
{
    public AudioClip healSound;

    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private bool pickedUp = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (pickedUp) return;

        PlayerLife playerLife = other.GetComponent<PlayerLife>();

        if (playerLife != null && playerLife.CurrentLives < playerLife.totalLives)
        {
            pickedUp = true;

            playerLife.HealOneLife();

            if (healSound != null)
                audioSource.PlayOneShot(healSound);

            col.enabled = false;

            StartCoroutine(PickupAnimation());
        }
    }

    IEnumerator PickupAnimation()
    {
        float duration = 0.25f;
        float time = 0f;

        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * 1.3f;

        Color startColor = spriteRenderer.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            spriteRenderer.color = Color.Lerp(startColor, endColor, t);

            yield return null;
        }

        Destroy(gameObject);
    }
}
