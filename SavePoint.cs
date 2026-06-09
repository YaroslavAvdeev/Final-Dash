using UnityEngine;

public class SavePointLine : MonoBehaviour
{
    private bool isActivated = false;
    private SpriteRenderer spriteRenderer;

    [Header("Кольори")]
    public Color inactiveColor = Color.red;
    public Color activeColor = Color.green;

    [Header("Ефекти")]
    public float flashDuration = 0.2f;
    public int flashCount = 3;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.color = inactiveColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            isActivated = true;

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ActivateCheckpoint(transform.position);

                PlayerLife life = other.GetComponent<PlayerLife>();
                if (life != null)
                    GameStateManager.Instance.currentLives = life.CurrentLives;
            }

            StartCoroutine(FlashEffect());
        }
    }

    private System.Collections.IEnumerator FlashEffect()
    {
        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = activeColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = inactiveColor;
            yield return new WaitForSeconds(flashDuration);
        }
        spriteRenderer.color = activeColor;
    }
}