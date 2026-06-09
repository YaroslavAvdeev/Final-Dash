using UnityEngine;

public class DisappearingPlatform : MonoBehaviour
{
    public float disappearDelay = 1.25f; // Час перед зникненням — швидше
    public float respawnTime = 1f;       // Швидше відновлення

    private bool triggered = false;
    private SpriteRenderer sr;
    private Collider2D col;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!triggered && collision.collider.CompareTag("Player"))
        {
            triggered = true;
            Invoke(nameof(Disappear), disappearDelay);
        }
    }

    void Disappear()
    {
        sr.enabled = false;
        col.enabled = false;
        Invoke(nameof(Respawn), respawnTime);
    }

    void Respawn()
    {
        sr.enabled = true;
        col.enabled = true;
        triggered = false;
    }
}
