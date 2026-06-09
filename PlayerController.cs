using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 9f;
    public float jumpForce = 13f;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    [Header("Jump Logic")]
    public int maxJumps = 2;
    private int jumpsLeft;
    public float coyoteTime = 0.2f;
    private float coyoteCounter;

    [Header("Detection")]
    public LayerMask groundLayer;
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.35f, 0.03f);

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sprite;
    private PlayerLife playerLife;
    private bool isGrounded;
    private float horizontal;

    private float bounceLockUntil = -1f;
    private bool isBouncing => Time.time < bounceLockUntil;

    // ФУНКЦІЯ ІНІЦІАЛІЗАЦІЇ / Підготовка компонентів гравця
    // Викликається при створенні об'єкту
    // Отримує всі необхідні компоненти (Rigidbody, Animator, SpriteRenderer, PlayerLife)
    // Встановлює параметри гравітації та режиму детектування колізій
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        playerLife = GetComponent<PlayerLife>();
        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    // ФУНКЦІЯ ОСНОВНОГО ОНОВЛЕННЯ / Обробка введення та фізики гравця
    // Викликається кожен кадр
    // 1. Отримує введення від гравця (вліво/вправо/стрибок)
    // 2. Перевіряє чи гравець на землі
    // 3. Керує часом койота (можна стрибнути відразу після падіння)
    // 4. Керує подвійним стрибком
    // 5. Застосовує спеціальну гравітацію (швидший падіння, коротший стрибок при відпусканні)
    // 6. Оновлює анімацію та розворот гравця
    void Update()
    {
        if (playerLife != null && playerLife.isDead) return;

        // ВВЕДЕННЯ - отримання горизонтального напрямку
        horizontal = Input.GetAxisRaw("Horizontal");
        // ПЕРЕВІРКА ЗЕМЛІ - чи гравець дотикається землі
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        // УПРАВЛІННЯ КОЙОТОЮ - дозволяє стрибнути трохи після падіння з платформи
        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            coyoteCounter = coyoteTime;
            jumpsLeft = maxJumps;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        // ОБРОБКА СТРИБКА - перевірка чи кнопка стрибка натиснута
        if (Input.GetButtonDown("Jump"))
            if (coyoteCounter > 0f || jumpsLeft > 0) Jump();

        // ГРАВІТАЦІЯ - застосовуємо особливу гравітацію для більш точного управління
        // Під час bounce не застосовуємо власну гравітацію
        if (!isBouncing)
        {
            // ШВИДКИЙ ПАДІННЯ - гравець падає швидше при падінні
            if (rb.linearVelocity.y < 0)
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
            // КОРОТКИЙ СТРИБОК - якщо відпустити кнопку, стрибок коротший
            else if (rb.linearVelocity.y > 0 && !Input.GetButton("Jump"))
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }

        UpdateAnimations();
        Flip();
    }

    // ФУНКЦІЯ СТРИБКА / Запуск стрибка гравця
    // Викликається з Update при натисканні кнопки Jump
    // 1. Надає вертикальну силу (jumpForce)
    // 2. Зменшує кількість залишених стрибків
    // 3. Скидає койота-счетчик
    // 4. Відтворює звук стрибка
    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpsLeft--;
        coyoteCounter = 0;
        if (playerLife != null) playerLife.PlaySound(playerLife.jumpSound);
    }

    // ФУНКЦІЯ ФІЗИЧНОГО ОНОВЛЕННЯ / Горизонтальний рух гравця
    // Викликається за розписанням фізики (FixedUpdate)
    // 1. Застосовує горизонтальну швидкість на основі введення
    // 2. Зберігає вертикальну швидкість (не змінює падіння/стрибок)
    // 3. Не застосовується під час відскоку (bounce)
    void FixedUpdate()
    {
        if (playerLife != null && playerLife.isDead) return;
        if (isBouncing) return;
        rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
    }

    // ФУНКЦІЯ ВІДСКОКУ / Штовхання гравця після стомпу моба
    // Викликається з EnemyController при стомпу
    // Приймає verticalForce (вертикальна сила) та enemyTransform (гравець штовхається від моба)
    // 1. Встановлює час блокування (0.35 сек - гравець не може рухатися)
    // 2. Розраховує горизонтальну силу відштовху від моба
    // 3. Якщо гравець прямо над мобом, штовхає в бік руху гравця
    // 4. Застосовує Impulse силу до гравця
    public void ApplyBounce(float verticalForce, Transform enemyTransform = null)
    {
        bounceLockUntil = Time.time + 0.35f;

        // РОЗРАХУНОК ГОРИЗОНТАЛЬНОГО ШТОВХУ від моба
        float horizontalForce = 0f;
        if (enemyTransform != null)
        {
            // НАПРЯМОК ВІД МОБА - знак показує куди штовхати
            float dir = Mathf.Sign(transform.position.x - enemyTransform.position.x);
            // Якщо гравець прямо над мобом (dir ~ 0) — штовхаємо в бік руху
            if (Mathf.Abs(dir) < 0.1f)
                dir = horizontal != 0 ? Mathf.Sign(horizontal) : 1f;
            horizontalForce = dir * verticalForce * 0.6f;
        }

        if (playerLife != null && playerLife.isDead) return;
        if (rb.bodyType == RigidbodyType2D.Static) return;
        // ВИДАЛЕННЯ ПОПЕРЕДНЬОЇ ШВИДКОСТІ та ЗАСТОСУВАННЯ НОВОЇ СИЛИ
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(horizontalForce, verticalForce), ForceMode2D.Impulse);
    }

    // ФУНКЦІЯ ПЕРЕВІРКИ ЗЕМЛІ / Повернення статусу гравця
    // Викликається з інших скриптів для перевірки чи гравець на землі
    // Повертає true якщо гравець дотикається землі
    public bool IsGrounded() => isGrounded;

    // ФУНКЦІЯ ОНОВЛЕННЯ АНІМАЦІЙ / Встановлення параметрів аніматора
    // Викликається з Update кожен кадр
    // 1. IsRunning - срібляє коли гравець рухається на землі
    // 2. IsJumping - срібляє коли гравець в повітрі
    void UpdateAnimations()
    {
        anim.SetBool("IsRunning", horizontal != 0 && isGrounded);
        anim.SetBool("IsJumping", !isGrounded);
    }

    // ФУНКЦІЯ РОЗВОРТУ ГРАВЦЯ / Зміна напрямку спрайту
    // Викликається з Update кожен кадр
    // Розворачує гравця в напрямку руху (ліво/право)
    void Flip()
    {
        if (horizontal > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (horizontal < 0) transform.localScale = new Vector3(-1, 1, 1);
    }

    // ФУНКЦІЯ ЕФЕКТУ УРОНУ / Червоне мерехтіння при отриманні урону
    // Викликається з PlayerLife при отриманні урону
    // 1. Змінює колір на червоний
    // 2. Чекає 0.2 сек
    // 3. Повертає оригінальний білий колір
    public void PlayDamageEffect() => StartCoroutine(DamageFlicker());
    private System.Collections.IEnumerator DamageFlicker()
    {
        sprite.color = Color.red;
        yield return new WaitForSeconds(0.2f);
        sprite.color = Color.white;
    }

    // ФУНКЦІЯ ЕФЕКТУ ЛІКУВАННЯ / Зелене мерехтіння при отриманні лікування
    // Викликається з PlayerLife при отриманні лікування
    // 1. Змінює колір на зелений
    // 2. Чекає 0.3 сек
    // 3. Повертає оригінальний білий колір
    public void PlayHealEffect() => StartCoroutine(HealFlicker());
    private System.Collections.IEnumerator HealFlicker()
    {
        sprite.color = Color.green;
        yield return new WaitForSeconds(0.3f);
        sprite.color = Color.white;
    }

    // ФУНКЦІЯ ВХІДНОЇ КОЛІЗІЇ / Прив'язка гравця до рухомої платформи
    // Викликається при вході гравця в колізію
    // Якщо гравець торкається рухомої платформи, він стає її дочірнім об'єктом
    private void OnCollisionEnter2D(Collision2D col)
    { if (col.gameObject.CompareTag("MovingPlatform")) transform.SetParent(col.transform); }

    // ФУНКЦІЯ ВИХІДНОЇ КОЛІЗІЇ / Відв'язка гравця від рухомої платформи
    // Викликається при виході гравця з колізії
    // Якщо гравець залишає рухому платформу, він перестає бути її дочірнім об'єктом
    private void OnCollisionExit2D(Collision2D col)
    { if (col.gameObject.CompareTag("MovingPlatform")) transform.SetParent(null); }
}
