using UnityEngine;

public class Crystal : MonoBehaviour
{
    [Header("Значення")]
    public int coinValue = 1;

    [Header("Звук")]
    public AudioClip collectSound;

    [Header("Popup текст")]
    public GameObject popupPrefab;

    [Header("Притягування до гравця")]
    public bool attractToPlayer = true;
    public float attractRange = 2f;
    public float attractSpeed = 6f;

    private Rigidbody2D rb;
    private Transform player;
    private bool isAttracting = false;
    private bool collected = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    void Update()
    {
        if (collected || player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (attractToPlayer && dist < attractRange)
        {
            isAttracting = true;
            if (rb != null) rb.gravityScale = 0f;
        }

        if (isAttracting)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                attractSpeed * Time.deltaTime
            );
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        collected = true;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.AddCoins(coinValue);

        SpawnPopup();

        PlayerLife playerLife = other.GetComponent<PlayerLife>();
        if (playerLife != null && collectSound != null)
            playerLife.PlaySound(collectSound);

        Destroy(gameObject);
    }

    void SpawnPopup()
    {
        if (popupPrefab == null) return;
        GameObject popup = Instantiate(popupPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        CrystalPopup cp = popup.GetComponent<CrystalPopup>();
        if (cp != null) cp.SetValue(coinValue);
    }
}