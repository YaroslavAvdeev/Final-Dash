using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
 
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(AudioSource))]
public class SharkBossAI : MonoBehaviour
{
    // ----------------------------------------------------------------
    //  INSPECTOR
    // ----------------------------------------------------------------
 
    [Header("-- Рух --")]
    public float moveSpeed          = 3f;
    public float chaseSpeed         = 6f;
    public float patrolDistance     = 5f;
    public float detectionRange     = 10f;
    public float stopChasingRange   = 14f;
 
    [Header("-- Земля --")]
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float     groundCheckRadius = 0.25f;
 
    [Header("-- Атака списом --")]
    public float attackRange        = 2.5f;
    public float attackCooldown     = 0.6f;
    public int   spearDamage        = 1;
    public float spearKnockbackX    = 7f;
    public float spearKnockbackY    = 5f;
    public bool  useDashAttack      = true;
    public float dashAttackSpeed    = 6f;
    [Tooltip("Перетягни Collider2D з дочірнього 'SpearHitbox' (isTrigger=true)")]
    public Collider2D spearHitboxCollider;
    [Tooltip("Перетягни BoxCollider2D з дочірнього 'SharkHeadTrigger' (isTrigger=true)")]
    public Collider2D headTriggerCollider;
    public float spearAttackDistance = 2f;
    public float spearAttackWidth    = 1.4f;
    public float spearAttackHeight   = 0.8f;
 
    [Header("-- Stomp -- урон акулі --")]
    [Tooltip("Наскільки вище центра колайдера вважається 'верхівка' для стомпу")]
    public float stompHeadZone      = 0.3f;
    [Tooltip("Мін. швидкість падіння гравця (від'ємна, напр. -0.5)")]
    public float stompMinFallSpeed  = -0.5f;
    [Tooltip("Скільки стомпів поспіль до контратаки")]
    public int   maxSafeCombo       = 3;
    [Tooltip("Секунд між стомпами щоб вважались 'поспіль'")]
    public float safeHitInterval    = 1.5f;
    [Tooltip("Час скидання комбо")]
    public float comboResetTime     = 3f;
    [Tooltip("Сила відскоку вгору після стомпу (як EnemyAI -- передається в ApplyBounce або linearVelocity.y)")]
    public float stompBounceX       = 14f;
 
    [Header("-- Контратака --")]
    public int   counterAttackDamage   = 2;
    public float counterKnockbackForce = 18f;
    public float counterKnockbackY     = 20f;
 
    [Header("-- HP --")]
    public int   maxHealth    = 12;
    public float stunDuration = 0.35f;
    public int   hitsToRage   = 4;
 
    [Header("-- Rage Mode --")]
    public float rageMoveSpeedMultiplier  = 1.6f;
    public float rageChaseSpeedMultiplier = 1.8f;
    public float rageAttackCooldown       = 0.35f;
    public float dashSpeed                = 15f;
    public float dashDuration             = 0.28f;
    public float dashCooldown             = 3.5f;
    [Tooltip("Скільки разів мигає помаранчевим при вході в rage")]
    public int   rageFlashCount           = 10;
 
    [Header("-- Платформи --")]
    public string platformTag           = "Platform";
    public int    platformsToKeepInRage = 2;
 
    [Header("-- Пила --")]
    public string sawTag            = "Saw";
    public float  sawDamageCooldown = 0.5f;
 
    [Header("-- Anti-Cheese --")]
    public float maxChaseHeightDifference = 4f;
    public float giveUpTime              = 2.5f;
    public float ignoreAfterGiveUp       = 2.5f;
 
    [Header("-- Поведінка / AI --")]
    [Tooltip("Дистанція зупинки перед атакою")]
    public float preferredRange     = 3.5f;
    [Tooltip("Скільки стоїть і дивиться перед атакою")]
    public float waitBeforeApproach = 1.0f;
    [Tooltip("Час відходу після атаки")]
    public float repositionTime     = 0.8f;
    [Tooltip("Шанс відійти вбік замість назад")]
    public float sidestepChance     = 0.4f;
    [Tooltip("Швидкість rush-атаки")]
    public float rushSpeed          = 11f;
    [Tooltip("Час rush-забігу (сек)")]
    public float rushDuration       = 0.55f;
    [Tooltip("Cooldown між rush-атаками")]
    public float rushCooldown       = 4f;
    [Tooltip("Наскільки акула передбачає рух гравця (0=не передбачає, 1=сильно)")]
    [Range(0f, 1f)]
    public float predictionFactor   = 0.35f;
    [Tooltip("Шанс фейкового замаху (підбіг і відступив)")]
    [Range(0f, 1f)]
    public float fakeOutChance      = 0.2f;
    [Tooltip("Пауза 'замирання' перед атакою -- виглядає як замах")]
    public float preAttackPause     = 0.18f;
 
    [Header("-- Фази бою --")]
    [Tooltip("HP нижче якого починається Фаза 2 (0..1 = відсоток)")]
    public float phase2Threshold    = 0.66f;
    [Tooltip("HP нижче якого починається Фаза 3 (0..1 = відсоток)")]
    public float phase3Threshold    = 0.33f;
    public float phase2SpeedMult    = 1.3f;
    public float phase2WaitReduce   = 0.3f;
    public float phase3SpeedMult    = 1.6f;
    public float phase3WaitReduce   = 0.7f;
 
    [Header("-- Feel / Juice --")]
    public float hitStopDuration     = 0.06f;
    public float postAttackGraceTime = 0.35f;
 
    [Header("-- Камера --")]
    [Tooltip("Тривалість тряски камери коли гравець отримує урон")]
    public float cameraShakeDuration  = 0.2f;
    [Tooltip("Сила тряски камери коли гравець отримує урон")]
    public float cameraShakeMagnitude = 0.3f;
 
    [Header("-- Кристали при смерті --")]
    public GameObject crystalPrefab;
    public int        crystalDropCount = 3;
 
    [Header("-- Звуки --")]
    public AudioClip attackSound;
    public AudioClip hurtSound;
    public AudioClip dieSound;
    public AudioClip runStepSound;
    public AudioClip counterAttackSound;
    public AudioClip stompSound;
 
    [Header("-- Посилання --")]
    public LayerMask      playerLayer;
    public BossHealthBarUI healthBar;
    public ParticleSystem hitEffect;
    public ParticleSystem stompEffect;
    public ParticleSystem rageEffect;
 
    // ----------------------------------------------------------------
    //  PRIVATE
    // ----------------------------------------------------------------
 
    private Rigidbody2D    rb;
    private Animator       anim;
    private Collider2D     col;
    private SpriteRenderer sprite;
    private AudioSource    audioSource;
    private AudioSource    runAudioSource;
 
    private Transform   player;
    private PlayerLife  playerLife;
    private Rigidbody2D playerRb;
    private Collider2D        playerCol;
    private PlayerController  playerController;
 
    private bool isDead                  = false;
    // публічна властивість для BossDeathSequence
    public  bool IsDead                  => isDead;
 
    private bool isChasing               = false;
    private bool isStunned               = false;
    private bool isRageMode              = false;
    private bool isDashing               = false;
    private bool isAttacking             = false;
    private bool isEscaping              = false;
    private bool ignorePlayer            = false;
    private bool isGrounded              = false;
    private bool counterAttackInProgress = false;
    private bool isAttackFrameActive     = false;
    private bool damageDealtThisAttack   = false;
 
    private float lastAttackTime     = -999f;
    private float lastAttackEndTime  = -999f;
    private float lastDashTime       = -999f;
    private float lastSawDamageTime  = -999f;
    private float lastStompTime      = -999f;
    private float attackStartTime    = -999f;
    private float maxAttackDuration  = 1.5f;
    private float stuckUnderPlatformTime = 0f;
 
    private enum BossState { Patrol, Approach, Wait, Reposition, Rush, PreAttackPause }
    private BossState bossState  = BossState.Patrol;
    private float     stateTimer = 0f;
 
    private int   currentPhase    = 1;
    private float lastRushTime    = -999f;
    private bool  isRushing       = false;
    private float repositionDir   = -1f;
    private bool  pendingFakeOut  = false;
 
    private int   currentHealth     = 0;
    private int   consecutiveStomps = 0;
 
    private Color   originalColor;
    private float   originalGravityScale;
    private Vector2 startPosition;
    private Vector2 patrolTarget;
 
    private List<GameObject> allPlatforms      = new List<GameObject>();
    private List<GameObject> disabledPlatforms = new List<GameObject>();
 
    // ----------------------------------------------------------------
    //  START
    // ----------------------------------------------------------------
 
    // ФУНКЦІЯ ІНІЦІАЛІЗАЦІЇ / Підготовка боса при старті сцени
    // 1. Отримує всі необхідні компоненти (Rigidbody2D, Animator, тощо)
    // 2. Знаходить гравця за тегом "Player" і зберігає посилання
    // 3. Ініціалізує health bar
    // 4. Налаштовує AudioSource для звуків та кроків
    // 5. Вимикає hitbox списа за замовчуванням
    // 6. Знаходить всі платформи на сцені
    // 7. Виводить діагностику у консоль якщо щось не призначено
    void Start()
    {
        rb     = GetComponent<Rigidbody2D>();
        anim   = GetComponent<Animator>();
        col    = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
 
        rb.bodyType               = RigidbodyType2D.Dynamic;
        rb.constraints            = RigidbodyConstraints2D.FreezeRotation;
        rb.mass                   = 5f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
 
        originalGravityScale = rb.gravityScale;
        originalColor        = sprite.color;
        currentHealth        = maxHealth;
        startPosition        = transform.position;
        SetNextPatrolTarget();
 
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player     = p.transform;
            playerLife = p.GetComponent<PlayerLife>();
            playerRb   = p.GetComponent<Rigidbody2D>();
            playerCol        = p.GetComponent<Collider2D>();
            playerController = p.GetComponent<PlayerController>();
        }
        else Debug.LogWarning("[SharkBossAI] Гравець не знайдений!");
 
        if (healthBar != null) healthBar.Initialize(maxHealth);
 
        // БЕЗПЕЧНА ІНІЦІАЛІЗАЦІЯ AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.LogWarning("[SharkBossAI] AudioSource відсутній -- додав новий компонент.");
        }
 
        if (audioSource != null)
        {
            audioSource.spatialBlend    = 0f;
            audioSource.volume          = 1f;
            audioSource.playOnAwake     = false;
        }
        else
        {
            Debug.LogError("[SharkBossAI] Не вдалося отримати або створити AudioSource!");
        }
 
        // ДРУГИЙ AudioSource для звуку кроків під час бігу
        runAudioSource = gameObject.AddComponent<AudioSource>();
        if (runAudioSource != null)
        {
            runAudioSource.clip         = runStepSound;
            runAudioSource.loop         = true;
            runAudioSource.playOnAwake  = false;
            runAudioSource.spatialBlend = 0f;
            runAudioSource.volume       = 0.6f;
        }
        else
        {
            Debug.LogError("[SharkBossAI] Не вдалося створити runAudioSource!");
        }
 
        if (spearHitboxCollider != null)
            spearHitboxCollider.enabled = false;
 
        FindAllPlatforms();
 
        // АВТО-ДІАГНОСТИКА при старті -- перевірка обов'язкових полів
        if (groundCheck == null)
            Debug.LogError("[SharkBossAI] groundCheck не призначений! Створи дочірній Empty об'єкт внизу акули і перетягни сюди.");
        if (groundLayer == 0)
            Debug.LogWarning("[SharkBossAI] groundLayer не призначений! Постав шар підлоги (наприклад Ground).");
        if (playerLife == null)
            Debug.LogError("[SharkBossAI] PlayerLife не знайдено на гравці! Переконайся що скрипт є і тег 'Player' виставлено.");
        if (playerRb == null)
            Debug.LogError("[SharkBossAI] Rigidbody2D не знайдено на гравці!");
        if (spearHitboxCollider == null)
            Debug.LogWarning("[SharkBossAI] spearHitboxCollider не призначений. Атака спрацює тільки через LateUpdate overlap.");
 
        Debug.Log($"[SharkBossAI] v5.2 Ініціалізовано. Платформ: {allPlatforms.Count}");
    }
 
#if UNITY_EDITOR
    void OnValidate()
    {
        var rb2d = GetComponent<Rigidbody2D>();
        if (rb2d != null && rb2d.gravityScale == 0)
            Debug.LogWarning("[SharkBossAI] Rigidbody2D.gravityScale = 0! Постав 2-3 щоб акула стояла на землі.");
        var col2d = GetComponent<Collider2D>();
        if (col2d != null && col2d.isTrigger)
            Debug.LogError("[SharkBossAI] Основний BoxCollider2D акули має isTrigger=true! Вимкни це -- має бути звичайний колайдер.");
    }
#endif
 
    // ----------------------------------------------------------------
    //  UPDATE
    // ----------------------------------------------------------------
 
    // ФУНКЦІЯ ОБРОБКИ КОЖНОГО КАДРУ / Головна логіка поведінки боса
    // 1. Зупиняє всю логіку якщо гравець мертвий або бос мертвий
    // 2. Виконує safety reset якщо атака зависла довше ніж maxAttackDuration
    // 3. Скидає лічильник стомп-комбо після паузи comboResetTime
    // 4. Перевіряє anti-cheese (гравець надто високо -- бос здається і чекає)
    // 5. Запускає rage dash якщо умови виконані
    // 6. Запускає атаку списом якщо гравець у зоні attackRange
    // 7. Керує стейт-машиною переслідування або патрулю
    void Update()
    {
        if (playerLife != null && playerLife.isDead) { FreezeEnemy(); return; }
        if (isDead || player == null) return;
 
        if (isStunned || isDashing || isEscaping)
        {
            UpdateRunSound(false);
            return;
        }
 
        // SAFETY RESET АТАКИ -- якщо анімація зависла понад maxAttackDuration
        if (isAttacking && Time.time > attackStartTime + maxAttackDuration)
        {
            Debug.LogWarning("[SharkBossAI] SAFETY reset attack");
            ResetAttackState();
        }
 
        // СКИДАННЯ СТОМП-КОМБО після тривалої паузи між стрибками
        if (Time.time - lastStompTime > comboResetTime)
            consecutiveStomps = 0;
 
        CheckGround();
 
        float dist = Vector2.Distance(transform.position, player.position);
        float dirX = player.position.x - transform.position.x;
 
        // ANTI-CHEESE -- якщо гравець надто високо і бос не може дістати
        float heightDiff = player.position.y - transform.position.y;
        if (isChasing && heightDiff > maxChaseHeightDifference)
        {
            stuckUnderPlatformTime += Time.deltaTime;
            if (stuckUnderPlatformTime >= giveUpTime)
            {
                isChasing    = false;
                ignorePlayer = true;
                stuckUnderPlatformTime = 0f;
                StartCoroutine(ResetIgnoreAfter(ignoreAfterGiveUp));
                UpdateRunSound(false);
                return;
            }
        }
        else stuckUnderPlatformTime = 0f;
 
        // RAGE DASH -- доступний тільки в режимі rage, якщо cooldown минув
        if (!ignorePlayer && isRageMode && !isAttacking &&
            Mathf.Abs(dirX) > 0.1f && dist <= detectionRange && dist > attackRange &&
            Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(DashAttack());
            return;
        }
 
        // АТАКА СПИСОМ -- якщо гравець у зоні ближнього бою
        if (!ignorePlayer && dist <= attackRange && dist > 0.2f && !isAttacking)
        {
            DoSpearAttack();
            UpdateRunSound(false);
            return;
        }
 
        if (isAttacking)
        {
            UpdateRunSound(false);
            return;
        }
 
        // СТЕЙТ-МАШИНА -- переслідування якщо гравець у зоні виявлення
        if (!ignorePlayer && dist <= detectionRange)
        {
            if (!isChasing && healthBar != null) healthBar.ShowBar();
            isChasing = true;
            UpdateBossMovement(dist, dirX);
            return;
        }
 
        if (isChasing && dist > stopChasingRange)
        {
            isChasing = false;
            SetState(BossState.Patrol);
        }
        if (bossState == BossState.Patrol || !isChasing)
        {
            Patrol();
            UpdateRunSound(true);
        }
    }
 
    // ----------------------------------------------------------------
    //  ANIMATION EVENTS
    // ----------------------------------------------------------------
 
    // ФУНКЦІЇ АНІМАЦІЙНИХ ПОДІЙ / Викликаються з Unity Animator
    // EnableSpearHitbox -- вмикає hitbox в момент удару (з анімації)
    // DisableSpearHitbox -- вимикає hitbox після удару
    // EndAttack -- завершує атаку і запускає відступ
    public void EnableSpearHitbox()
    {
        isAttackFrameActive   = true;
        damageDealtThisAttack = false;
        if (spearHitboxCollider != null) spearHitboxCollider.enabled = true;
    }
 
    public void DisableSpearHitbox()
    {
        isAttackFrameActive = false;
        if (spearHitboxCollider != null) spearHitboxCollider.enabled = false;
    }
 
    public void EndAttack()
    {
        ResetAttackState();
        OnAttackFinishedReposition();
    }
 
    public void HitPlayer()     => EnableSpearHitbox();
    public void EnableHitbox()  => EnableSpearHitbox();
    public void DisableHitbox() => DisableSpearHitbox();
 
    // ----------------------------------------------------------------
    //  LATE UPDATE -- fallback перевірка попадання
    // ----------------------------------------------------------------
 
    // ФУНКЦІЯ РЕЗЕРВНОЇ ПЕРЕВІРКИ / Перевіряє попадання якщо hitbox не спрацював через тригер
    // Викликається кожен кадр після Update
    // Активна тільки коли isAttackFrameActive = true і урон ще не нанесено
    void LateUpdate()
    {
        if (!isAttackFrameActive || damageDealtThisAttack) return;
        if (player == null || playerLife == null || playerLife.isDead) return;
        CheckSpearHit();
    }
 
    // ФУНКЦІЯ ПЕРЕВІРКИ ПОПАДАННЯ СПИСА / Overlap-перевірка перед босом
    // 1. Розраховує позицію та розмір хітбокса в залежності від напрямку
    // 2. Перевіряє всі колайдери в зоні атаки
    // 3. Ігнорує удар зверху (щоб стомп не рахувався як спис)
    // 4. Наносить урон якщо гравець потрапив у зону
    void CheckSpearHit()
    {
        float   facing    = transform.localScale.x;
        Vector2 boxCenter = new Vector2(transform.position.x + spearAttackDistance * facing, transform.position.y);
        Vector2 boxSize   = new Vector2(spearAttackWidth, spearAttackHeight);
 
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f))
        {
            if (!hit.CompareTag("Player")) continue;
            float headTop     = col != null ? col.bounds.max.y : transform.position.y + stompHeadZone;
            float playerBottom = hit.bounds.min.y;
            if (playerBottom > headTop - 0.1f) continue;
            DealSpearDamage(hit);
            return;
        }
    }
 
    public void OnAttackHitboxTriggered(Collider2D other)
    {
        if (!isAttackFrameActive || damageDealtThisAttack) return;
        if (playerLife == null || playerLife.isDead) return;
        if (!other.CompareTag("Player")) return;
 
        float headTop     = col != null ? col.bounds.max.y : transform.position.y + stompHeadZone;
        float playerBottom = other.bounds.min.y;
        if (playerBottom > headTop - 0.1f) return;
 
        DealSpearDamage(other);
    }
 
    // ФУНКЦІЯ НАНЕСЕННЯ УРОНУ СПИСОМ / Завдає урон, відкидає гравця, запускає ефекти
    // 1. Позначає що урон вже нанесено (щоб не вдарити двічі)
    // 2. Наносить урон через PlayerLife.ReceiveImpact
    // 3. Трясе камеру
    // 4. Відкидає гравця в сторону від акули
    // 5. Запускає ефект частинок і HitStop
    void DealSpearDamage(Collider2D target)
    {
        damageDealtThisAttack = true;
        playerLife.ReceiveImpact(spearDamage);
 
        CameraFollow.Instance?.Shake(cameraShakeDuration, cameraShakeMagnitude);
 
        if (playerRb != null && playerRb.bodyType == RigidbodyType2D.Dynamic)
        {
            float dir = target.transform.position.x > transform.position.x ? 1f : -1f;
            playerRb.linearVelocity = new Vector2(dir * spearKnockbackX, spearKnockbackY);
        }
        SpawnEffect(hitEffect, target.transform.position);
        StartCoroutine(HitStop());
    }
 
    // ФУНКЦІЯ СКИДАННЯ СТАНУ АТАКИ / Повертає всі флаги у вихідний стан
    void ResetAttackState()
    {
        isAttacking           = false;
        isAttackFrameActive   = false;
        damageDealtThisAttack = false;
        lastAttackEndTime     = Time.time;
        if (spearHitboxCollider != null) spearHitboxCollider.enabled = false;
    }
 
    // ----------------------------------------------------------------
    //  КОЛІЗІЇ
    // ----------------------------------------------------------------
 
    public  float bodyHitCooldown = 0.8f;
    private bool  ignorePlayerCollision = false;
 
    // ФУНКЦІЯ ОБРОБКИ ЗІТКНЕННЯ / Реагує на контакт з гравцем або пилою
    // 1. Якщо зіткнення з пилою -- акула отримує урон з cooldown
    // 2. Якщо зіткнення з гравцем -- перевіряє чи це стомп зверху
    // 3. Стомп визначається по нормалі контакту або позиції гравця вище центру акули
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(sawTag))
        {
            if (Time.time >= lastSawDamageTime + sawDamageCooldown)
            {
                lastSawDamageTime = Time.time;
                TakeDamage(false);
            }
            return;
        }
 
        if (!collision.gameObject.CompareTag("Player")) return;
        if (isDead || playerLife == null || playerLife.isDead) return;
        if (ignorePlayerCollision) return;
        if (Time.time - lastStompTime < 0.15f) return;
 
        bool playerFallingDown = playerRb != null && playerRb.linearVelocity.y < 1f;
        bool playerAboveShark  = false;
        if (playerCol != null && col != null)
            playerAboveShark = playerCol.bounds.min.y >= col.bounds.center.y - 0.1f;
 
        bool hitFromAbove = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y < -0.5f)
            {
                hitFromAbove = true;
                break;
            }
        }
 
        if ((hitFromAbove || playerAboveShark) && playerFallingDown)
        {
            if (counterAttackInProgress) return;
            HandleStompHit(collision.gameObject);
        }
    }
 
    void OnCollisionStay2D(Collision2D collision)
    {
    }
 
    // ФУНКЦІЯ УРОНУ ВІД ТІЛА / Наносить урон при фізичному контакті збоку
    void DealBodyDamage(GameObject playerObj)
    {
        playerLife.ReceiveImpact(spearDamage);
        CameraFollow.Instance?.Shake(cameraShakeDuration, cameraShakeMagnitude);
 
        if (playerRb != null && playerRb.bodyType == RigidbodyType2D.Dynamic)
        {
            float dir = playerObj.transform.position.x > transform.position.x ? 1f : -1f;
            playerRb.linearVelocity = new Vector2(dir * spearKnockbackX, spearKnockbackY);
        }
        SpawnEffect(hitEffect, playerObj.transform.position);
    }
 
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(sawTag) && Time.time >= lastSawDamageTime + sawDamageCooldown)
        {
            lastSawDamageTime = Time.time;
            TakeDamage(false);
        }
    }
 
    public void OnHeadTriggerEnter(Collider2D other) { }
    public void OnBodyTriggerEnter(Collider2D other) { }
 
    // ФУНКЦІЯ ОБРОБКИ СТОМПУ / Логіка коли гравець стрибнув на акулу
    // 1. Рахує кількість стомпів поспіль
    // 2. Якщо перевищено maxSafeCombo -- запускає контратаку
    // 3. Інакше -- акула отримує урон, гравець відскакує, акула відбігає
    void HandleStompHit(GameObject playerObj)
    {
        consecutiveStomps = (Time.time - lastStompTime < safeHitInterval)
            ? consecutiveStomps + 1 : 1;
        lastStompTime = Time.time;
 
        if (consecutiveStomps > maxSafeCombo)
        {
            consecutiveStomps = 0;
            StartCoroutine(CounterAttackPlayer(playerObj));
            return;
        }
 
        Debug.Log($"[SharkBossAI] STOMP {consecutiveStomps}/{maxSafeCombo}");
        PlaySound(stompSound != null ? stompSound : hurtSound);
        SpawnEffect(stompEffect, transform.position + Vector3.up * 0.5f);
        StartCoroutine(HitStop());
 
        CameraFollow.Instance?.Shake(0.15f, 0.25f);
 
        TakeDamage(true);
 
        if (playerCol != null && col != null)
            StartCoroutine(IgnoreCollisionTemporarily(0.5f));
 
        if (playerController != null)
            playerController.ApplyBounce(stompBounceX, transform);
        else if (playerRb != null && playerRb.bodyType == RigidbodyType2D.Dynamic)
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, stompBounceX);
 
        StartCoroutine(StompSequence(playerObj));
    }
 
    // КОРУТИНА ПОСЛІДОВНОСТІ ПІСЛЯ СТОМПУ / Акула відбігає і потім атакує у відповідь
    // 1. Чекає 0.1 секунди
    // 2. Відбігає в протилежний від гравця бік
    // 3. Повертається і дивиться на гравця
    // 4. Запускає контр-раш через StompCounterRush
    IEnumerator StompSequence(GameObject playerObj)
    {
        yield return new WaitForSeconds(0.1f);
        if (isDead) yield break;
 
        float retreatDir = transform.position.x > playerObj.transform.position.x ? 1f : -1f;
        float elapsed = 0f;
        AnimSetBool("IsRunning", true);
        while (elapsed < 0.4f && !isDead && !isStunned)
        {
            rb.linearVelocity    = new Vector2(retreatDir * chaseSpeed * 1.2f, rb.linearVelocity.y);
            transform.localScale = new Vector3(retreatDir, 1f, 1f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        StopHorizontal();
 
        FacePlayer();
        yield return new WaitForSeconds(0.3f);
        if (isDead) yield break;
 
        if (!isStunned)
            StartCoroutine(StompCounterRush());
    }
 
    // КОРУТИНА КОНТР-РАШУ ПІСЛЯ СТОМПУ / Швидкий забіг у напрямку гравця після отримання стомпу
    IEnumerator StompCounterRush()
    {
        if (player == null || isDead) yield break;
 
        float dir = player.position.x > transform.position.x ? 1f : -1f;
        transform.localScale = new Vector3(dir, 1f, 1f);
 
        StopHorizontal();
        AnimSetBool("IsRunning", false);
        yield return new WaitForSeconds(0.15f);
 
        AnimSetBool("IsRunning", true);
        float elapsed = 0f;
        float duration = 0.35f;
        float prevX = transform.position.x;
        while (elapsed < duration && !isDead && !isStunned)
        {
            if (player != null)
                dir = player.position.x > transform.position.x ? 1f : -1f;
            rb.linearVelocity    = new Vector2(dir * rushSpeed * 0.85f, rb.linearVelocity.y);
            transform.localScale = new Vector3(dir, 1f, 1f);
            elapsed += Time.deltaTime;
            yield return null;
 
            if (Mathf.Abs(transform.position.x - prevX) < 0.01f && elapsed > 0.05f) break;
            prevX = transform.position.x;
        }
 
        StopHorizontal();
        SetState(BossState.Wait);
    }
 
    // ----------------------------------------------------------------
    //  АТАКА СПИСОМ
    // ----------------------------------------------------------------
 
    // ФУНКЦІЯ ЗАПУСКУ АТАКИ СПИСОМ / Перевіряє cooldown і запускає корутину атаки
    void DoSpearAttack()
    {
        if (isAttacking) return;
        float cooldown = isRageMode ? rageAttackCooldown : attackCooldown;
        if (Time.time < lastAttackTime + cooldown) return;
 
        StartCoroutine(SpearAttackWithPause());
    }
 
    // КОРУТИНА АТАКИ СПИСОМ / Пауза перед ударом, потім власне атака
    // 1. Зупиняє рух і фіксує напрямок на гравця
    // 2. Чекає preAttackPause (виглядає як замах)
    // 3. Перевіряє чи гравець ще в зоні атаки
    // 4. При useDashAttack -- короткий ривок вперед під час удару
    // 5. Запускає анімацію Attack і звук
    IEnumerator SpearAttackWithPause()
    {
        isAttacking     = true;
        attackStartTime = Time.time;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        AnimSetBool("IsRunning", false);
 
        float lockedDir = player != null && player.position.x > transform.position.x ? 1f : -1f;
        transform.localScale = new Vector3(lockedDir, 1f, 1f);
 
        yield return new WaitForSeconds(preAttackPause);
 
        if (isDead || isStunned) { ResetAttackState(); yield break; }
 
        if (player != null && Vector2.Distance(transform.position, player.position) > attackRange * 1.5f)
        {
            ResetAttackState();
            yield break;
        }
 
        lastAttackTime        = Time.time;
        attackStartTime       = Time.time;
        damageDealtThisAttack = false;
 
        if (useDashAttack && isGrounded)
            rb.linearVelocity = new Vector2(lockedDir * dashAttackSpeed, rb.linearVelocity.y);
 
        AnimSetTrigger("Attack");
        PlaySound(attackSound);
    }
 
    // ----------------------------------------------------------------
    //  DAMAGE / DEATH
    // ----------------------------------------------------------------
 
    // ФУНКЦІЯ ОТРИМАННЯ УРОНУ / Зменшує HP, запускає стан, перевіряє смерть
    // 1. Ігнорує урон якщо бос мертвий або приголомшений
    // 2. Зменшує currentHealth і оновлює health bar
    // 3. Перевіряє чи потрібно входити в Rage Mode
    // 4. Запускає корутину приголомшення
    // 5. Якщо fromStomp -- відбігає і тимчасово ігнорує гравця
    // 6. Якщо HP <= 0 -- викликає Die()
    public void TakeDamage(bool fromStomp)
    {
        if (isDead || isStunned) return;
 
        currentHealth--;
        PlaySound(hurtSound);
        if (healthBar != null) healthBar.UpdateHealth(currentHealth);
 
        if (!isRageMode && (maxHealth - currentHealth) >= hitsToRage)
            EnterRageMode();
 
        StartCoroutine(StunEnemy());
 
        if (fromStomp)
        {
            StartCoroutine(EscapeFromPlayer());
            StartCoroutine(IgnorePlayerTemporarily());
        }
 
        if (currentHealth <= 0) Die();
    }
 
    public void TakeDamage() => TakeDamage(false);
 
    // ФУНКЦІЯ ВХОДУ В RAGE MODE / Підсилює акулу і прибирає частину платформ
    void EnterRageMode()
    {
        isRageMode = true;
        if (rageEffect != null) rageEffect.Play();
        if (healthBar != null) healthBar.SetRageMode(true);
        DisableRandomPlatforms();
        Debug.Log("[SharkBossAI] RAGE MODE!");
        StartCoroutine(RageEntryFlash());
    }
 
    // КОРУТИНА МИГАННЯ ПРИ ВХОДІ В RAGE / Мигає помаранчевим кілька разів
    IEnumerator RageEntryFlash()
    {
        Color orange = new Color(1f, 0.55f, 0f);
        for (int i = 0; i < rageFlashCount; i++)
        {
            if (sprite) sprite.color = orange;
            yield return new WaitForSeconds(0.055f);
            if (sprite) sprite.color = originalColor;
            yield return new WaitForSeconds(0.055f);
        }
        if (sprite) sprite.color = originalColor;
    }
 
    // КОРУТИНА ПРИГОЛОМШЕННЯ / Тимчасово заморожує акулу і запускає анімацію болю
    // 1. Заморожує горизонтальний рух через constraints
    // 2. Запускає анімацію Hurt
    // 3. Мигає червоним і білим
    // 4. Відновлює рух після завершення
    IEnumerator StunEnemy()
    {
        isStunned = true;
        UpdateRunSound(false);
 
        rb.constraints    = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
 
        if (anim != null)
        {
            AnimResetTrigger("Attack");
            AnimResetTrigger("Hurt");
            AnimSetTrigger("Hurt");
        }
 
        yield return StartCoroutine(StunFlash());
 
        if (sprite) sprite.color = originalColor;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
 
        if (anim != null)
        {
            AnimResetTrigger("Hurt");
            AnimSetBool("IsRunning", false);
        }
 
        isStunned = false;
    }
 
    // КОРУТИНА МИГАННЯ ПРИ ПРИГОЛОМШЕННІ / Мигає червоним 4 рази
    IEnumerator StunFlash()
    {
        for (int i = 0; i < 4; i++)
        {
            if (sprite) sprite.color = Color.red;
            yield return new WaitForSeconds(0.07f);
            if (sprite) sprite.color = Color.white;
            yield return new WaitForSeconds(0.07f);
        }
    }
 
    // ФУНКЦІЯ СМЕРТІ / Вимикає акулу і викидає кристали
    // 1. Зупиняє рух і вимикає фізику
    // 2. Запускає анімацію Die і звук
    // 3. Відновлює всі платформи
    // 4. Викидає кристали з випадковими швидкостями
    // 5. Знищує об'єкт через 1.5 секунди
    void Die()
    {
        isDead = true;
        UpdateRunSound(false);
        PlaySound(dieSound);
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale   = originalGravityScale;
        col.enabled       = false;
        rb.simulated      = false;
        if (sprite) sprite.color = originalColor;
        AnimSetTrigger("Die");
        if (healthBar != null) healthBar.Hide();
        RestoreAllPlatforms();
        ResetAttackState();
 
        for (int i = 0; i < crystalDropCount; i++)
        {
            if (crystalPrefab == null) break;
            Vector3 offset = new Vector3(Random.Range(-0.6f, 0.6f), 0.3f, 0f);
            GameObject crystal = Instantiate(crystalPrefab, transform.position + offset, Quaternion.identity);
            Rigidbody2D crb = crystal.GetComponent<Rigidbody2D>();
            if (crb != null)
                crb.linearVelocity = new Vector2(Random.Range(-4f, 4f), Random.Range(4f, 8f));
        }
 
        Debug.Log("[SharkBossAI] DEAD");
        Destroy(gameObject, 1.5f);
    }
 
    // ФУНКЦІЯ ЗАМОРОЗКИ / Зупиняє акулу поки гравець мертвий
    public void FreezeEnemy()
    {
        if (isDead) return;
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
        ResetAttackState();
        isChasing = isDashing = isEscaping = false;
        AnimSetBool("IsRunning", false);
        UpdateRunSound(false);
    }
 
    // ----------------------------------------------------------------
    //  КОНТРАТАКА
    // ----------------------------------------------------------------
 
    // КОРУТИНА КОНТРАТАКИ / Відповідь на надмірну кількість стомпів поспіль
    // 1. Наносить counterAttackDamage гравцю
    // 2. Сильно відкидає гравця вгору і в сторону
    // 3. Мигає помаранчевим щоб сигналізувати про контратаку
    IEnumerator CounterAttackPlayer(GameObject playerObj)
    {
        counterAttackInProgress = true;
        StartCoroutine(CounterFlash());
        PlaySound(counterAttackSound != null ? counterAttackSound : attackSound);
 
        playerObj.GetComponent<PlayerLife>()?.ReceiveImpact(counterAttackDamage);
 
        CameraFollow.Instance?.Shake(cameraShakeDuration * 1.5f, cameraShakeMagnitude * 1.5f);
 
        Rigidbody2D prb = playerObj.GetComponent<Rigidbody2D>();
        if (prb != null && prb.bodyType == RigidbodyType2D.Dynamic)
        {
            float dir = playerObj.transform.position.x > transform.position.x ? 1f : -1f;
            prb.linearVelocity = new Vector2(dir * counterKnockbackForce, counterKnockbackY);
        }
 
        SpawnEffect(hitEffect, playerObj.transform.position);
        StartCoroutine(HitStop());
 
        yield return new WaitForSeconds(0.4f);
        counterAttackInProgress = false;
    }
 
    // КОРУТИНА МИГАННЯ КОНТРАТАКИ / Мигає між білим і помаранчевим 6 разів
    IEnumerator CounterFlash()
    {
        Color orange = new Color(1f, 0.55f, 0f);
        for (int i = 0; i < 6; i++)
        {
            if (sprite) sprite.color = Color.white;
            yield return new WaitForSeconds(0.04f);
            if (sprite) sprite.color = orange;
            yield return new WaitForSeconds(0.04f);
        }
        if (sprite) sprite.color = originalColor;
    }
 
    // ----------------------------------------------------------------
    //  СТЕЙТ-МАШИНА
    // ----------------------------------------------------------------
 
    // ФУНКЦІЯ ПЕРЕВІРКИ ЗЕМЛІ / Оновлює isGrounded через OverlapCircle
    void CheckGround()
    {
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }
 
    // ФУНКЦІЯ РУХУ БОСА / Головна стейт-машина поведінки під час переслідування
    // Стани: Approach (рух до гравця), Wait (стоїть і вичікує), 
    //        Reposition (відхід після атаки), Rush (різкий забіг), PreAttackPause (замах)
    void UpdateBossMovement(float dist, float dirX)
    {
        stateTimer += Time.deltaTime;
        UpdatePhase();
 
        float curWait  = CurrentWaitTime();
        float curSpeed = CurrentSpeed();
 
        switch (bossState)
        {
            case BossState.Approach:
            {
                if (dist <= preferredRange)
                {
                    SetState(BossState.Wait);
                    StopHorizontal();
                    break;
                }
                Vector2 predictedPos = PredictPlayerPosition();
                float   predictedDir = predictedPos.x - transform.position.x;
                MoveToward(predictedDir, curSpeed);
                break;
            }
 
            case BossState.Wait:
            {
                FacePlayer();
                StopHorizontal();
                UpdateRunSound(false);
 
                if (dist <= attackRange && dist > 0.2f && !isAttacking)
                {
                    DoSpearAttack();
                    break;
                }
 
                if (dist > preferredRange * 2.5f)
                {
                    SetState(BossState.Approach);
                    break;
                }
 
                if (stateTimer >= curWait)
                {
                    if (pendingFakeOut)
                    {
                        pendingFakeOut = false;
                        StartCoroutine(DoFakeOut(dirX));
                        break;
                    }
 
                    bool canRush = currentPhase >= 2 &&
                                   Time.time >= lastRushTime + rushCooldown &&
                                   dist > preferredRange;
 
                    if (canRush)
                        StartCoroutine(DoRush(dirX));
                    else
                    {
                        pendingFakeOut = (currentPhase >= 2 && Random.value < fakeOutChance);
                        SetState(BossState.Approach);
                    }
                }
                break;
            }
 
            case BossState.PreAttackPause:
            {
                StopHorizontal();
                FacePlayer();
                UpdateRunSound(false);
                break;
            }
 
            case BossState.Rush:
                break;
 
            case BossState.Reposition:
            {
                rb.linearVelocity    = new Vector2(repositionDir * curSpeed * 0.8f, rb.linearVelocity.y);
                transform.localScale = new Vector3(repositionDir > 0f ? 1f : -1f, 1f, 1f);
                AnimSetBool("IsRunning", true);
                UpdateRunSound(true);
 
                if (stateTimer >= repositionTime)
                    SetState(BossState.Wait);
                break;
            }
 
            case BossState.Patrol:
            default:
                SetState(BossState.Approach);
                break;
        }
    }
 
    // ФУНКЦІЯ ПЕРЕДБАЧЕННЯ ПОЗИЦІЇ ГРАВЦЯ / Розраховує куди рухатиметься гравець
    // Враховує поточну швидкість гравця і час до досягнення його позиції
    Vector2 PredictPlayerPosition()
    {
        if (playerRb == null || player == null)
            return player != null ? (Vector2)player.position : transform.position;
 
        float   travelTime = Vector2.Distance(transform.position, player.position) / Mathf.Max(1f, CurrentSpeed());
        Vector2 predicted  = (Vector2)player.position + playerRb.linearVelocity * travelTime * predictionFactor;
        predicted.y = player.position.y;
        return predicted;
    }
 
    // КОРУТИНА ФЕЙКОВОГО ЗАМАХУ / Акула підбігає і відступає не вдаривши
    // Використовується для обманних маневрів у фазі 2+
    IEnumerator DoFakeOut(float dirX)
    {
        SetState(BossState.PreAttackPause);
        float dir = dirX > 0f ? 1f : -1f;
        transform.localScale = new Vector3(dir, 1f, 1f);
 
        float elapsed = 0f;
        float fakeTime = 0.2f + Random.Range(0f, 0.15f);
        AnimSetBool("IsRunning", true);
        UpdateRunSound(true);
 
        while (elapsed < fakeTime && !isDead && !isStunned)
        {
            rb.linearVelocity = new Vector2(dir * rushSpeed * 0.6f, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }
 
        elapsed = 0f;
        float backTime = 0.25f;
        float backDir  = -dir;
        while (elapsed < backTime && !isDead && !isStunned)
        {
            rb.linearVelocity = new Vector2(backDir * chaseSpeed, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }
 
        StopHorizontal();
        SetState(BossState.Wait);
    }
 
    // КОРУТИНА РАШ-АТАКИ / Швидкий забіг через всю арену
    // Доступний з фази 2, має окремий cooldown
    IEnumerator DoRush(float dirX)
    {
        SetState(BossState.Rush);
        isRushing    = true;
        lastRushTime = Time.time;
 
        float dir = dirX > 0f ? 1f : -1f;
        transform.localScale = new Vector3(dir, 1f, 1f);
 
        StopHorizontal();
        AnimSetBool("IsRunning", false);
        yield return new WaitForSeconds(0.2f);
 
        AnimSetBool("IsRunning", true);
        float elapsed = 0f;
        float prevX   = transform.position.x;
        while (elapsed < rushDuration && !isDead && !isStunned)
        {
            rb.linearVelocity = new Vector2(dir * rushSpeed, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
 
            if (Mathf.Abs(transform.position.x - prevX) < 0.01f && elapsed > 0.05f) break;
            prevX = transform.position.x;
        }
 
        StopHorizontal();
        isRushing = false;
 
        if (currentPhase >= 3)
            SetState(BossState.Approach);
        else
            SetState(BossState.Wait);
    }
 
    // ФУНКЦІЯ ОНОВЛЕННЯ ФАЗИ / Перевіряє поточний відсоток HP і змінює фазу
    void UpdatePhase()
    {
        float hpRatio = (float)currentHealth / maxHealth;
        int   newPhase = hpRatio <= phase3Threshold ? 3 : hpRatio <= phase2Threshold ? 2 : 1;
 
        if (newPhase != currentPhase)
        {
            currentPhase = newPhase;
            OnPhaseChanged(newPhase);
        }
    }
 
    // ФУНКЦІЯ ЗМІНИ ФАЗИ / Реакція на перехід між фазами бою
    // Фаза 2 -- вхід в Rage Mode
    // Фаза 3 -- підвищення швидкості і скорочення часу відходу
    void OnPhaseChanged(int phase)
    {
        Debug.Log($"[SharkBossAI] ФАЗА {phase}!");
        if (phase >= 2 && !isRageMode) EnterRageMode();
        if (phase == 3)
        {
            chaseSpeed     = Mathf.Max(chaseSpeed, 7f);
            repositionTime = Mathf.Max(0.3f, repositionTime - 0.2f);
        }
        StartCoroutine(PhaseTransitionFlash());
    }
 
    // КОРУТИНА СПАЛАХУ ПЕРЕХОДУ ФАЗИ / Візуальний сигнал для гравця
    IEnumerator PhaseTransitionFlash()
    {
        Color flashColor = currentPhase == 3 ? Color.white : new Color(1f, 0.55f, 0f);
        for (int i = 0; i < 8; i++)
        {
            if (sprite) sprite.color = flashColor;
            yield return new WaitForSeconds(0.07f);
            if (sprite) sprite.color = originalColor;
            yield return new WaitForSeconds(0.07f);
        }
        if (sprite) sprite.color = originalColor;
    }
 
    // ФУНКЦІЯ ПОТОЧНОЇ ШВИДКОСТІ / Розраховує швидкість з урахуванням фази і rage
    float CurrentSpeed()
    {
        float base_ = isRageMode ? chaseSpeed * rageChaseSpeedMultiplier : chaseSpeed;
        if      (currentPhase == 3) return base_ * phase3SpeedMult;
        else if (currentPhase == 2) return base_ * phase2SpeedMult;
        return base_;
    }
 
    // ФУНКЦІЯ ПОТОЧНОГО ЧАСУ ОЧІКУВАННЯ / Скорочується з кожною фазою
    float CurrentWaitTime()
    {
        if      (currentPhase == 3) return Mathf.Max(0.1f, waitBeforeApproach - phase3WaitReduce);
        else if (currentPhase == 2) return Mathf.Max(0.3f, waitBeforeApproach - phase2WaitReduce);
        return waitBeforeApproach;
    }
 
    // ФУНКЦІЯ РУХУ В НАПРЯМКУ / Рухає акулу в заданий бік з анімацією бігу
    void MoveToward(float dirX, float speed)
    {
        float dir = dirX > 0f ? 1f : -1f;
        rb.linearVelocity    = new Vector2(dir * speed, rb.linearVelocity.y);
        transform.localScale = new Vector3(dir, 1f, 1f);
        AnimSetBool("IsRunning", true);
        UpdateRunSound(true);
    }
 
    // ФУНКЦІЯ ЗУПИНКИ ГОРИЗОНТАЛЬНОГО РУХУ / Обнуляє X швидкість і зупиняє анімацію бігу
    void StopHorizontal()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        AnimSetBool("IsRunning", false);
    }
 
    // ФУНКЦІЯ ВСТАНОВЛЕННЯ СТАНУ / Переключає стейт-машину і скидає таймер
    void SetState(BossState newState)
    {
        if (newState == BossState.Reposition && player != null)
        {
            float away = transform.position.x > player.position.x ? 1f : -1f;
            if (Random.value < sidestepChance) away *= -1f;
            repositionDir = away;
        }
        bossState  = newState;
        stateTimer = 0f;
    }
 
    // ФУНКЦІЯ ВІДХОДУ ПІСЛЯ АТАКИ / Переводить в стан Reposition якщо не rush
    void OnAttackFinishedReposition()
    {
        if (!ignorePlayer && !isRushing)
            SetState(BossState.Reposition);
    }
 
    // ФУНКЦІЯ ПОВОРОТУ ДО ГРАВЦЯ / Оновлює localScale.x щоб дивитись на гравця
    void FacePlayer()
    {
        if (player == null) return;
        float dir = player.position.x > transform.position.x ? 1f : -1f;
        transform.localScale = new Vector3(dir, 1f, 1f);
    }
 
    // ФУНКЦІЯ ПАТРУЛЮ / Рух між випадковими точками поки гравець поза зоною виявлення
    void Patrol()
    {
        if (bossState != BossState.Patrol) return;
        AnimSetBool("IsRunning", true);
        float dir = patrolTarget.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity    = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        transform.localScale = new Vector3(dir, 1f, 1f);
        if (Vector2.Distance(transform.position, patrolTarget) < 0.5f) SetNextPatrolTarget();
    }
 
    // ФУНКЦІЯ НАСТУПНОЇ ТОЧКИ ПАТРУЛЮ / Встановлює випадкову точку в радіусі patrolDistance
    void SetNextPatrolTarget() =>
        patrolTarget = new Vector2(startPosition.x + Random.Range(-patrolDistance, patrolDistance), transform.position.y);
 
    // ФУНКЦІЯ ЗВУКУ КРОКІВ / Вмикає або вимикає looping звук бігу
    void UpdateRunSound(bool moving)
    {
        if (runStepSound == null || runAudioSource == null) return;
        if (moving  && !runAudioSource.isPlaying) runAudioSource.Play();
        if (!moving &&  runAudioSource.isPlaying) runAudioSource.Stop();
    }
 
    // ----------------------------------------------------------------
    //  ПЛАТФОРМИ
    // ----------------------------------------------------------------
 
    // ФУНКЦІЯ ПОШУКУ ПЛАТФОРМ / Знаходить всі платформи за тегом platformTag
    void FindAllPlatforms() =>
        allPlatforms = GameObject.FindGameObjectsWithTag(platformTag).ToList();
 
    // ФУНКЦІЯ ВИМКНЕННЯ ПЛАТФОРМ / В режимі rage прибирає частину платформ
    // Залишає platformsToKeepInRage штук, решту напівпрозорими і без колізії
    void DisableRandomPlatforms()
    {
        if (allPlatforms.Count <= platformsToKeepInRage) return;
        RestoreAllPlatforms();
        disabledPlatforms.Clear();
        foreach (GameObject p in allPlatforms.OrderBy(_ => Random.value).Skip(platformsToKeepInRage))
        {
            if (p == null) continue;
            SetPlatformState(p, false);
            disabledPlatforms.Add(p);
        }
    }
 
    // ФУНКЦІЯ ВІДНОВЛЕННЯ ПЛАТФОРМ / Повертає всі платформи у вихідний стан
    void RestoreAllPlatforms()
    {
        foreach (GameObject p in allPlatforms)
            if (p != null) SetPlatformState(p, true);
    }
 
    // ФУНКЦІЯ СТАНУ ПЛАТФОРМИ / Вмикає або вимикає платформу (колізія + прозорість)
    void SetPlatformState(GameObject platform, bool enabled)
    {
        var eff = platform.GetComponent<PlatformEffector2D>();
        var pc  = platform.GetComponent<Collider2D>();
        var ps  = platform.GetComponent<SpriteRenderer>();
        if (eff != null) eff.enabled = enabled; else if (pc != null) pc.enabled = enabled;
        if (ps != null) { Color c = ps.color; ps.color = new Color(c.r, c.g, c.b, enabled ? 1f : 0.3f); }
    }
 
    // ----------------------------------------------------------------
    //  COROUTINES
    // ----------------------------------------------------------------
 
    // КОРУТИНА DASH-АТАКИ / Різкий ривок у напрямку гравця в rage режимі
    IEnumerator DashAttack()
    {
        isDashing    = true;
        lastDashTime = Time.time;
        float dir   = player.position.x > transform.position.x ? 1f : -1f;
        float t     = 0f;
        float prevX = transform.position.x;
        transform.localScale = new Vector3(dir, 1f, 1f);
        while (t < dashDuration)
        {
            rb.linearVelocity = new Vector2(dir * dashSpeed, rb.linearVelocity.y);
            t += Time.deltaTime;
            yield return null;
 
            if (Mathf.Abs(transform.position.x - prevX) < 0.01f && t > 0.05f) break;
            prevX = transform.position.x;
        }
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        isDashing = false;
    }
 
    // КОРУТИНА ВІДХОДУ ВІД ГРАВЦЯ / Відбігає в протилежну сторону після отримання стомпу
    IEnumerator EscapeFromPlayer()
    {
        isEscaping = true;
        float dir  = player != null ? (player.position.x > transform.position.x ? -1f : 1f) : -1f;
        float t    = 0f;
        while (t < 0.4f)
        {
            rb.linearVelocity = new Vector2(dir * chaseSpeed, rb.linearVelocity.y);
            t += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        isEscaping = false;
    }
 
    // КОРУТИНА ТИМЧАСОВОГО ІГНОРУВАННЯ ГРАВЦЯ / Не переслідує гравця 0.5 секунди після стомпу
    IEnumerator IgnorePlayerTemporarily()
    {
        ignorePlayer = true;
        isChasing    = false;
        yield return new WaitForSeconds(0.5f);
        ignorePlayer = false;
    }
 
    // КОРУТИНА ТИМЧАСОВОГО ІГНОРУВАННЯ КОЛІЗІЇ / Вимикає фізичну колізію між акулою і гравцем
    IEnumerator IgnoreCollisionTemporarily(float duration)
    {
        ignorePlayerCollision = true;
        if (col != null && playerCol != null)
            Physics2D.IgnoreCollision(col, playerCol, true);
        yield return new WaitForSeconds(duration);
        if (col != null && playerCol != null)
            Physics2D.IgnoreCollision(col, playerCol, false);
        ignorePlayerCollision = false;
    }
 
    // КОРУТИНА ВІДНОВЛЕННЯ ПЕРЕСЛІДУВАННЯ / Після паузи anti-cheese знову дозволяє чейсити
    IEnumerator ResetIgnoreAfter(float t)
    {
        yield return new WaitForSeconds(t);
        ignorePlayer = false;
    }
 
    // КОРУТИНА HIT STOP / Короткий стоп-кадр при влучанні (уповільнює час)
    IEnumerator HitStop()
    {
        if (hitStopDuration <= 0f) yield break;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
    }
 
    // ----------------------------------------------------------------
    //  HELPERS
    // ----------------------------------------------------------------
 
    // ФУНКЦІЯ ВІДТВОРЕННЯ ЗВУКУ / Безпечне відтворення AudioClip через PlayOneShot
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }
 
    // ФУНКЦІЯ СПАВНУ ЕФЕКТУ / Створює ефект частинок у точці і знищує його після завершення
    void SpawnEffect(ParticleSystem ps, Vector3 pos)
    {
        if (ps == null) return;
        ParticleSystem fx = Instantiate(ps, pos, Quaternion.identity);
        fx.Play();
        Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax + 0.5f);
    }
 
    // ----------------------------------------------------------------
    //  SAFE ANIMATOR HELPERS
    // ----------------------------------------------------------------
 
    // ФУНКЦІЇ БЕЗПЕЧНОЇ РОБОТИ З ANIMATOR / Перевіряють наявність параметра перед встановленням
    // Захищають від помилок якщо параметр відсутній в Animator Controller
    void AnimSetBool(string param, bool val)
    {
        if (anim == null) return;
        foreach (var p in anim.parameters)
            if (p.name == param && p.type == AnimatorControllerParameterType.Bool)
                { anim.SetBool(param, val); return; }
    }
 
    void AnimSetTrigger(string param)
    {
        if (anim == null) return;
        foreach (var p in anim.parameters)
            if (p.name == param && p.type == AnimatorControllerParameterType.Trigger)
                { anim.SetTrigger(param); return; }
    }
 
    void AnimResetTrigger(string param)
    {
        if (anim == null) return;
        foreach (var p in anim.parameters)
            if (p.name == param && p.type == AnimatorControllerParameterType.Trigger)
                { anim.ResetTrigger(param); return; }
    }
 
    // ----------------------------------------------------------------
    //  GIZMOS
    // ----------------------------------------------------------------
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
 
        Collider2D c = GetComponent<Collider2D>();
        if (c != null)
        {
            float sharkTop        = c.bounds.max.y;
            float stompZoneBottom = sharkTop - stompHeadZone;
            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            Vector3 center = new Vector3(transform.position.x, (sharkTop + stompZoneBottom) * 0.5f, 0f);
            Vector3 size   = new Vector3(c.bounds.size.x, stompHeadZone, 0f);
            Gizmos.DrawWireCube(center, size);
        }
 
        float facing = Application.isPlaying ? transform.localScale.x : 1f;
        Gizmos.color = new Color(1f, 0.7f, 0f, 0.4f);
        Vector3 ac   = new Vector3(transform.position.x + spearAttackDistance * facing, transform.position.y, 0f);
        Gizmos.DrawWireCube(ac, new Vector3(spearAttackWidth, spearAttackHeight, 0f));
 
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, preferredRange);
    }
#endif
}
