using UnityEngine;
/// Моб з 3 фазами:
/// HP 3 → нормальний
/// HP 2 → швидший (фаза 2)
/// HP 1 → дуже швидкий + стрибає (фаза 3 / rage)
/// Смерть → ділиться на 2 маленьких клони (EnemyClone.cs) + дроп кристалів
public class EnemyAI : MonoBehaviour
{
    [Header("Patrol")]
    public float patrolSpeed = 2f;
    public float patrolDistance = 3f;

    [Header("Chase")]
    public float chaseSpeed = 3.5f;
    public float detectionRange = 5f;

    [Header("Health")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Combat")]
    public int contactDamage = 1;
    public float damageCooldown = 1f;

    [Header("Stomp")]
    public float stompBounceForce = 14f;
    public float stompGraceTime = 0.6f;
    public float stompCooldown = 0.5f;
    public float stompIgnoreCollisionTime = 0.4f;

    [Header("Фаза 2 (2 HP)")]
    public float phase2SpeedMultiplier = 1.4f;
    public Color phase2Color = new Color(1f, 0.6f, 0f);

    [Header("Фаза 3 / Rage (1 HP)")]
    public float phase3SpeedMultiplier = 2f;
    public float jumpForce = 9f;
    public float jumpInterval = 1.0f;
    public Color phase3Color = new Color(1f, 0.15f, 0.15f);

    [Header("Клони при смерті")]
    public GameObject clonePrefab;
    public float cloneSpawnOffset = 0.4f;

    [Header("Кристали при смерті")]
    public GameObject crystalPrefab;
    public int crystalDropCount = 2;

    [Header("Roll Visual")]
    public float rollSpeedBase = 150f;
    public float rollSpeedMax  = 500f;
    public Transform visualTransform;

    [Header("Edge & Ground")]
    public bool stopAtEdges = true;
    public float edgeCheckDistance = 0.3f;
    public float edgeCheckDepth = 0.5f;
    public LayerMask groundLayer;

    [Header("Hit Effect")]
    public AudioClip hitSound;
    public AudioClip phase2Sound;
    public AudioClip phase3Sound;
    public float hitFlashDuration = 0.15f;
    private AudioSource audioSource;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private Transform player;
    private PlayerLife playerLife;
    private PlayerController playerController;
    private Collider2D playerCollider;

    private Vector2 startPos;
    private int patrolDir = 1;
    private float lastDamageTime = -99f;
    private float lastStompTime  = -99f;
    private float jumpTimer = 0f;
    private bool isDead = false;
    private bool isGrounded = false;
    private bool isJumping = false;
    private int currentPhase = 1;
    private Color originalColor;

    private float currentPatrolSpeed;
    private float currentChaseSpeed;
    private float currentDetectionRange;
    private float currentDamageCooldown;

    private bool jumpPhaseActive => currentPhase >= 3;

    // ФУНКЦІЯ ІНІЦІАЛІЗАЦІЇ / Підготовка моба з 3 фазами
    // Викликається при старті сцени
    // 1. Отримує всі необхідні компоненти (Rigidbody, Collider, SpriteRenderer)
    // 2. Встановлює здоров'я на максимум
    // 3. Зберігає початкову позицію для петляння
    // 4. Налаштовує візуальний спрайт та аудіо
    // 5. Знаходить гравця для переслідування
    void Start()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        currentHealth = maxHealth;
        startPos = transform.position;

        if (visualTransform == null && transform.childCount > 0)
            visualTransform = transform.GetChild(0);
        if (visualTransform == null)
            visualTransform = transform;

        sr = visualTransform.GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        currentPatrolSpeed    = patrolSpeed;
        currentChaseSpeed     = chaseSpeed;
        currentDetectionRange = detectionRange;
        currentDamageCooldown = damageCooldown;

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
    }

    // ФУНКЦІЯ ОНОВЛЕННЯ ЛОГІКИ / Основний цикл руху моба
    // Викликається кожен кадр
    // 1. Перевіряє чи моб живий
    // 2. Розраховує чи моб на землі та на хилі
    // 3. Визначає дистанцію до гравця
    // 4. Вибирає дію: переслідування або патруль
    // 5. Запускає стрибки у фазі 3 (rage)
    // 6. Обертає спрайт на основі швидкості (roll animation)
    void Update()
    {
        if (isDead) return;

        // ПЕРЕВІРКА ЗЕМЛІ - чи моб стоїть на землі
        isGrounded = Physics2D.OverlapBox(
            new Vector2(transform.position.x, transform.position.y - col.bounds.extents.y - 0.05f),
            new Vector2(col.bounds.size.x * 0.8f, 0.1f),
            0f, groundLayer);

        // СКИДАННЯ СТАНУ СТРИБКА
        if (isGrounded && rb.linearVelocity.y <= 0.1f)
            isJumping = false;

        // РОЗРАХУНОК ДИСТАНЦІЇ до гравця
        float dist = player != null
            ? Vector2.Distance(transform.position, player.position)
            : Mathf.Infinity;

        // ВИБІР ДІЇ - переслідування або патруль
        if (!isJumping)
        {
            if (player != null && (currentPhase == 3 || dist <= currentDetectionRange))
                ChasePlayer();
            else
                Patrol();
        }

        // СТРИБКИ У ФАЗІ 3 - автоматичні стрибки для агресивності
        if (jumpPhaseActive && isGrounded && !isJumping)
        {
            jumpTimer -= Time.deltaTime;
            if (jumpTimer <= 0f)
            {
                DoJump();
                jumpTimer = jumpInterval;
            }
        }

        // ROLL ANIMATION - обертання спрайту на основі швидкості
        float speed = rb.linearVelocity.magnitude;
        float roll  = Mathf.Lerp(rollSpeedBase, rollSpeedMax, speed / 10f);
        if (isGrounded && Mathf.Abs(rb.linearVelocity.x) > 0.05f)
            visualTransform.Rotate(0f, 0f, -Mathf.Sign(rb.linearVelocity.x) * roll * Time.deltaTime);
        else if (!isGrounded)
            visualTransform.rotation = Quaternion.identity;
    }

    // ФУНКЦІЯ СТРИБКА / Активація стрибка у фазі 3
    // Викликається з Update у фазі 3 (rage mode)
    // 1. Встановлює isJumping = true
    // 2. Розраховує напрямок до гравця
    // 3. Застосовує вертикальну та горизонтальну силу
    void DoJump()
    {
        isJumping = true;
        float dir = player != null ? Mathf.Sign(player.position.x - transform.position.x) : patrolDir;
        rb.linearVelocity = new Vector2(dir * currentChaseSpeed, jumpForce);
    }

    // ФУНКЦІЯ ПАТРУЛЯ / Рух туди-сюди в межах дистанції
    // Викликається з Update коли гравець не виявлений
    // 1. Перевіряє край платформи (якщо stopAtEdges = true)
    // 2. Перевіряє максимальну дистанцію патруля
    // 3. Змінює напрямок при досягненні ліміту
    // 4. Рухає моба в поточному напрямку
    void Patrol()
    {
        if (stopAtEdges && IsAtEdge()) { patrolDir *= -1; Flip(); }
        else if (Mathf.Abs(transform.position.x - startPos.x) >= patrolDistance) { patrolDir *= -1; Flip(); }
        rb.linearVelocity = new Vector2(patrolDir * currentPatrolSpeed, rb.linearVelocity.y);
    }

    // ФУНКЦІЯ ПЕРЕВІРКИ КРАЮ / Raycast для виявлення краю платформи
    // Викликається з Patrol
    // 1. Кидає промінь вниз від краю моба
    // 2. Якщо промінь не попадає - це край платформи
    // 3. Повертає true якщо край знайдено
    bool IsAtEdge()
    {
        Vector2 origin = (Vector2)transform.position
            + Vector2.right * patrolDir * (col.bounds.extents.x + edgeCheckDistance)
            + Vector2.down  * col.bounds.extents.y;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, edgeCheckDepth, groundLayer);
        Debug.DrawRay(origin, Vector2.down * edgeCheckDepth, hit ? Color.green : Color.red);
        return !hit;
    }

    // ФУНКЦІЯ ПЕРЕСЛІДУВАННЯ ГРАВЦЯ / Рух до гравця
    // Викликається з Update коли гравець виявлений
    // 1. Розраховує напрямок до гравця
    // 2. Рухає моба до гравця з поточною швидкістю переслідування
    // 3. Розворачує спрайт у напрямку гравця
    void ChasePlayer()
    {
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * currentChaseSpeed, rb.linearVelocity.y);
        if (Mathf.Sign(dir) != Mathf.Sign(transform.localScale.x)) Flip();
    }

    // ФУНКЦІЯ РОЗВОРТУ / Зміна напрямку спрайту
    // Викликається при зміні напрямку руху
    void Flip() { Vector3 s = transform.localScale; s.x *= -1; transform.localScale = s; }

    // ФУНКЦІЯ ВХІДНОЇ КОЛІЗІЇ / Обробка дотику гравця
    // Викликається при контакті з гравцем
    // 1. Перевіряє чи це стомп (нормаль колізії вказує вниз)
    // 2. Якщо стомп - наносить урон мобу та штовхає гравця вверх
    // 3. Якщо контакт зверху - наносить урон гравцю
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        Rigidbody2D playerRb = collision.rigidbody;

        // ПЕРЕВІРКА СТОМПУ - контакт зверху
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f)
            {
                if (Time.time - lastStompTime < stompCooldown) return;
                lastStompTime = Time.time;

                TakeDamage();

                if (playerCollider != null && col != null)
                    StartCoroutine(IgnoreCollisionTemp(playerCollider, col));

                // ШТОВХАННЯ ГРАВЦЯ ВВЕРХ
                if (playerController != null)
                    playerController.ApplyBounce(stompBounceForce, transform);
                else if (playerRb != null)
                    playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, stompBounceForce);

                return;
            }
        }

        // КОНТАКТ ЗБОКУ - урон гравцю
        if (Time.time - lastStompTime < stompGraceTime) return;
        if (Time.time - lastDamageTime >= currentDamageCooldown)
        {
            lastDamageTime = Time.time;
            if (playerLife != null) playerLife.ReceiveImpact(contactDamage);
        }
    }

    // ФУНКЦІЯ ПРОДОВЖЕННЯ КОЛІЗІЇ / Тривалий контакт з гравцем
    // Викликається кожен кадр коли моб дотикається гравця
    // Наносить урон гравцю якщо достатньо часу пройшло з останнього удару
    void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;
        if (!collision.gameObject.CompareTag("Player")) return;
        if (Time.time - lastStompTime < stompGraceTime) return;
        if (Time.time - lastDamageTime >= currentDamageCooldown)
        {
            lastDamageTime = Time.time;
            if (playerLife != null) playerLife.ReceiveImpact(contactDamage);
        }
    }

    // ФУНКЦІЯ ІГНОРУВАННЯ КОЛІЗІЇ ТИМЧАСОВО / Відключення колізії після стомпу
    // Викликається з OnCollisionEnter2D коли виявлено стомп
    // 1. Відключає колізію між мобом та гравцем
    // 2. Чекає заданий час
    // 3. Повертає колізію назад
    private System.Collections.IEnumerator IgnoreCollisionTemp(Collider2D a, Collider2D b)
    {
        Physics2D.IgnoreCollision(a, b, true);
        yield return new WaitForSeconds(stompIgnoreCollisionTime);
        if (a != null && b != null) Physics2D.IgnoreCollision(a, b, false);
    }

    // ФУНКЦІЯ ОТРИМАННЯ УРАНУ / Зменшення здоров'я та активація фаз
    // Викликається при стомпу гравцем
    // 1. Зменшує здоров'я на 1
    // 2. Відтворює звук удару
    // 3. Якщо здоров'я = 0 - спускає смерть
    // 4. При переході на фазу 2 (2 HP) - збільшує швидкість та змінює колір на оранжевий
    // 5. При переході на фазу 3 (1 HP) - максимально збільшує швидкість, змінює колір на червоний і активує стрибки
    public void TakeDamage()
    {
        if (isDead) return;
        currentHealth--;

        if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(HitFlash());

        // ФАЗА 2 - 2 HP: швидший, оранжевий
        if (currentHealth == 2 && currentPhase == 1)
        {
            currentPhase = 2;
            currentPatrolSpeed = patrolSpeed * phase2SpeedMultiplier;
            currentChaseSpeed  = chaseSpeed  * phase2SpeedMultiplier;
            if (sr != null) sr.color = phase2Color;
            if (phase2Sound != null && audioSource != null)
                audioSource.PlayOneShot(phase2Sound);
        }

        // ФАЗА 3 / RAGE - 1 HP: дуже швидкий, червоний, стрибає
        if (currentHealth == 1 && currentPhase == 2)
        {
            currentPhase = 3;
            currentPatrolSpeed    = patrolSpeed * phase3SpeedMultiplier;
            currentChaseSpeed     = chaseSpeed  * phase3SpeedMultiplier;
            currentDetectionRange = detectionRange * 1.5f;
            currentDamageCooldown = damageCooldown * 0.5f;
            if (sr != null) sr.color = phase3Color;
            if (phase3Sound != null && audioSource != null)
                audioSource.PlayOneShot(phase3Sound);
            jumpTimer = 0f;
        }
    }

    // ФУНКЦІЯ МЕРЕХТІННЯ ПІД ЧАС УДАРУ / Білий блиск при отриманні урану
    // Викликається з TakeDamage
    // 1. Змінює колір на білий
    // 2. Чекає hitFlashDuration
    // 3. Повертає колір на оригінальний (залежно від фази)
    private System.Collections.IEnumerator HitFlash()
    {
        if (sr == null) yield break;
        Color current = sr.color;
        sr.color = Color.white;
        yield return new WaitForSeconds(hitFlashDuration);
        if (!isDead)
            sr.color = currentPhase == 3 ? phase3Color : (currentPhase == 2 ? phase2Color : originalColor);
    }

    // ФУНКЦІЯ СМЕРТІ / Розділення на 2 клони та дроп кристалів
    // Викликається коли здоров'я = 0
    // 1. Встановлює isDead = true
    // 2. Створює 2 маленьких клони зліва та справа
    // 3. Випускає кристали які підстрибують
    // 4. Змінює колір на червоний та видаляє моба
    void Die()
    {
        if (isDead) return;
        isDead = true;

        // Спавн двох клонів
        if (clonePrefab != null)
        {
            Vector3 left  = transform.position + Vector3.left  * cloneSpawnOffset;
            Vector3 right = transform.position + Vector3.right * cloneSpawnOffset;
            Instantiate(clonePrefab, left,  Quaternion.identity);
            Instantiate(clonePrefab, right, Quaternion.identity);
        }

        // Дроп кристалів — підстрибують в рандомний бік
        for (int i = 0; i < crystalDropCount; i++)
        {
            if (crystalPrefab == null) break;
            Vector3 offset = new Vector3(Random.Range(-0.4f, 0.4f), 0.3f, 0f);
            GameObject c = Instantiate(crystalPrefab, transform.position + offset, Quaternion.identity);
            Rigidbody2D crb = c.GetComponent<Rigidbody2D>();
            if (crb != null)
                crb.linearVelocity = new Vector2(Random.Range(-3f, 3f), Random.Range(4f, 7f));
        }

        rb.linearVelocity = Vector2.zero;
        if (col != null) col.enabled = false;
        StopAllCoroutines();
        if (sr != null) sr.color = Color.red;
        Destroy(gameObject, 0.15f);
    }

    // ФУНКЦІЯ GIZMO ВІДОБРАЖЕННЯ / Налагодження меж патруля
    // Викликається тільки в редакторі для візуалізації
    // Показує жовту лінію для дистанції патруля та червоне коло для дистанції виявлення
    void OnDrawGizmosSelected()
    {
        Vector2 center = Application.isPlaying ? startPos : (Vector2)transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(center + Vector2.left * patrolDistance, center + Vector2.right * patrolDistance);
        Gizmos.color = currentPhase == 3 ? Color.red : (currentPhase == 2 ? new Color(1f,0.6f,0f) : new Color(1f,0.5f,0f));
        Gizmos.DrawWireSphere(transform.position, currentDetectionRange);
    }
}
