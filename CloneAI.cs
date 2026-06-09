using UnityEngine;
/// Маленький клон що спавниться при смерті основного моба.
/// Вбивається з 1 стомпу. Швидший і менший.
public class EnemyClone : MonoBehaviour
{
    [Header("Рух")]
    [Header("Дроп")]
    public GameObject crystalPrefab;
    public int crystalDropCount = 1;

    [Header("Рух")]
    public float moveSpeed = 4f;
    public float detectionRange = 6f;

    [Header("Combat")]
    public int contactDamage = 1;
    public float damageCooldown = 0.8f;

    [Header("Stomp")]
    public float stompBounceForce = 10f;
    public float stompCooldown = 0.4f;
    public float stompGraceTime = 0.5f;
    public float stompIgnoreCollisionTime = 0.3f;

    [Header("Patrol")]
    public float patrolDistance = 2f;
    public LayerMask groundLayer;

    [Header("Hit Effect")]
    public AudioClip hitSound;
    public float hitFlashDuration = 0.1f;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private AudioSource audioSource;

    private Transform player;
    private PlayerLife playerLife;
    private PlayerController playerController;
    private Collider2D playerCollider;

    private Vector2 startPos;
    private int patrolDir = 1;
    private float lastDamageTime = -99f;
    private float lastStompTime  = -99f;
    private bool isDead = false;
    private bool isGrounded = false;
    private Color originalColor;

    void Start()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr  = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;
        startPos = transform.position;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;


        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player           = playerObj.transform;
            playerLife       = playerObj.GetComponent<PlayerLife>();
            playerController = playerObj.GetComponent<PlayerController>();
            playerCollider   = playerObj.GetComponent<CapsuleCollider2D>();
            if (playerCollider == null)
                playerCollider = playerObj.GetComponent<BoxCollider2D>();
        }

        // Відразу розбігаються в різні сторони
        patrolDir = Random.value > 0.5f ? 1 : -1;
        rb.linearVelocity = new Vector2(patrolDir * moveSpeed, 3f); // невеличкий підстрибок при спавні
    }

    void Update()
    {
        if (isDead) return;

        isGrounded = Physics2D.OverlapBox(
            new Vector2(transform.position.x, transform.position.y - col.bounds.extents.y - 0.05f),
            new Vector2(col.bounds.size.x * 0.8f, 0.1f),
            0f, groundLayer);

        float dist = player != null
            ? Vector2.Distance(transform.position, player.position)
            : Mathf.Infinity;

        if (player != null && dist <= detectionRange)
            ChasePlayer();
        else
            Patrol();
    }

    void Patrol()
    {
        if (Mathf.Abs(transform.position.x - startPos.x) >= patrolDistance)
        {
            patrolDir *= -1;
            Flip();
        }
        rb.linearVelocity = new Vector2(patrolDir * moveSpeed, rb.linearVelocity.y);
    }

    void ChasePlayer()
    {
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        if (Mathf.Sign(dir) != Mathf.Sign(transform.localScale.x)) Flip();
    }

    void Flip() { Vector3 s = transform.localScale; s.x *= -1; transform.localScale = s; }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        Rigidbody2D playerRb = collision.rigidbody;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f)
            {
                if (Time.time - lastStompTime < stompCooldown) return;
                lastStompTime = Time.time;

                Die(); // 1 удар = смерть клона

                if (playerCollider != null && col != null)
                    StartCoroutine(IgnoreCollisionTemp(playerCollider, col));

                if (playerController != null)
                    playerController.ApplyBounce(stompBounceForce, transform);
                else if (playerRb != null)
                    playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, stompBounceForce);

                return;
            }
        }

        if (Time.time - lastStompTime < stompGraceTime) return;
        if (Time.time - lastDamageTime >= damageCooldown)
        {
            lastDamageTime = Time.time;
            if (playerLife != null) playerLife.ReceiveImpact(contactDamage);
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;
        if (!collision.gameObject.CompareTag("Player")) return;
        if (Time.time - lastStompTime < stompGraceTime) return;
        if (Time.time - lastDamageTime >= damageCooldown)
        {
            lastDamageTime = Time.time;
            if (playerLife != null) playerLife.ReceiveImpact(contactDamage);
        }
    }

    private System.Collections.IEnumerator IgnoreCollisionTemp(Collider2D a, Collider2D b)
    {
        Physics2D.IgnoreCollision(a, b, true);
        yield return new WaitForSeconds(stompIgnoreCollisionTime);
        if (a != null && b != null) Physics2D.IgnoreCollision(a, b, false);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound);

        rb.linearVelocity = Vector2.zero;
        if (col != null) col.enabled = false;
        StopAllCoroutines();
        if (sr != null) sr.color = Color.red;
        for (int i = 0; i < crystalDropCount; i++)
        {
            if (crystalPrefab != null)
            {
                Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), 0.3f, 0f);
                GameObject c = Instantiate(crystalPrefab, transform.position + offset, Quaternion.identity);
                Rigidbody2D crb = c.GetComponent<Rigidbody2D>();
                if (crb != null)
                    crb.linearVelocity = new Vector2(Random.Range(-2f, 2f), Random.Range(3f, 5f));
            }
        }
        Destroy(gameObject, 0.15f);
    }
}
