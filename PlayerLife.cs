using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerLife : MonoBehaviour
{
    public bool isDead = false;

    private AudioSource audioSource;
    public AudioClip deathSound;
    public AudioClip jumpSound;
    public AudioClip damageSound;

    [Header("UI")]
    public GameObject lifeIconPrefab;
    public Transform livesPanelParent;
    private Image[] lifeIcons;

    [Header("Lives")]
    public int totalLives = 3;
    private int currentLivesCount;

    private PlayerController playerController;

    // ВЛАСТИВІСТЬ ПОТОЧНИХ ЖИЛЬ / Getter та Setter для управління життями
    // При встановленні значення автоматично оновлює UI
    public int CurrentLives
    {
        get { return currentLivesCount; }
        set
        {
            currentLivesCount = Mathf.Clamp(value, 0, totalLives);
            UpdateLifeIconsUI();
        }
    }

    // ФУНКЦІЯ ІНІЦІАЛІЗАЦІЇ / Підготовка системи життя
    // Викликається при створенні об'єкту
    // 1. Отримує посилання на PlayerController
    // 2. Завантажує життя з GameStateManager (якщо існує)
    // 3. Встановлює AudioSource для звуків
    // 4. Ініціалізує іконки життя на UI
    void Awake()
    {
        playerController = GetComponent<PlayerController>();

        // ЗАВАНТАЖЕННЯ ЗБЕРЕЖЕНОГО СТАНУ - якщо гравець повернувся на рівень
        if (GameStateManager.Instance != null && GameStateManager.Instance.currentLives > 0)
        {
            currentLivesCount = GameStateManager.Instance.currentLives;
        }
        else
        {
            currentLivesCount = totalLives;
        }

        // СТВОРЕННЯ AUDIO SOURCE якщо він не існує
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        InitializeLifeIcons();
        UpdateLifeIconsUI();
    }

    // ФУНКЦІЯ ІНІЦІАЛІЗАЦІЇ ІКОНОК / Створення іконок життя на UI
    // Викликається з Awake при старті гри
    // 1. Очищує панель від старих іконок
    // 2. Створює потрібну кількість іконок з префабу
    // 3. Зберігає посилання на всі іконки в массив
    private void InitializeLifeIcons()
    {
        if (lifeIconPrefab == null || livesPanelParent == null) return;

        // ВИДАЛЕННЯ СТАРИХ ІКОНОК - очищення панелі перед створенням нових
        foreach (Transform child in livesPanelParent)
        {
            Destroy(child.gameObject);
        }

        // СТВОРЕННЯ НОВИХ ІКОНОК - за кількістю totalLives
        lifeIcons = new Image[totalLives];

        for (int i = 0; i < totalLives; i++)
        {
            GameObject icon = Instantiate(lifeIconPrefab, livesPanelParent);
            lifeIcons[i] = icon.GetComponent<Image>();
        }
    }

    // ФУНКЦІЯ ОНОВЛЕННЯ UI ІКОНОК / Показування/сховання іконок життя
    // Викликається кожен раз коли змінюється кількість життя
    // 1. Показує іконки для наявних життів
    // 2. Сховує іконки для втрачених життів
    private void UpdateLifeIconsUI()
    {
        if (lifeIcons == null) return;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] != null)
                lifeIcons[i].enabled = (i < currentLivesCount);
        }
    }

    // ФУНКЦІЯ ОТРИМАННЯ УРОНУ / Зменшення кількості життя
    // Викликається з EnemyController або іншими системами при отриманні урану
    // Приймає damageValue (кількість втрачених життів)
    // 1. Перевіряє чи гравець ще живий
    // 2. Зменшує кількість життя на damageValue
    // 3. Оновлює UI іконки
    // 4. Відтворює ефект урану (червоне мерехтіння)
    // 5. Якщо життя кінчилися - викликає смерть гравця
    public void ReceiveImpact(int damageValue)
    {
        if (isDead) return;

        // ЗМЕНШЕННЯ ЖИТТЯ - віднімаємо значення урану
        currentLivesCount -= damageValue;
        currentLivesCount = Mathf.Clamp(currentLivesCount, 0, totalLives);

        // ЗБЕРЕЖЕННЯ СТАНУ - оновлюємо GameStateManager
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.currentLives = currentLivesCount;

        UpdateLifeIconsUI();

        // ЕФЕКТ УРАНУ - червоне мерехтіння гравця
        if (playerController != null)
        {
            playerController.PlayDamageEffect();
        }

        // ПЕРЕВІРКА НА СМЕРТЬ
        if (currentLivesCount <= 0)
        {
            HandleDeath();
        }
        else
        {
            PlaySound(damageSound);
        }
    }

    // ФУНКЦІЯ ЛІКУВАННЯ / Збільшення кількості життя на 1
    // Викликається при вході в зелене лікування
    // 1. Перевіряє чи гравець ще живий
    // 2. Перевіряє чи не на максимумі життя
    // 3. Збільшує кількість життя на 1
    // 4. Оновлює UI іконки
    // 5. Відтворює ефект лікування (зелене мерехтіння)
    public void HealOneLife()
    {
        if (isDead) return;

        if (currentLivesCount < totalLives)
        {
            currentLivesCount++;

            // ЗБЕРЕЖЕННЯ СТАНУ - оновлюємо GameStateManager
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.currentLives = currentLivesCount;

            UpdateLifeIconsUI();

            // ЕФЕКТ ЛІКУВАННЯ - зелене мерехтіння гравця
            if (playerController != null)
            {
                playerController.PlayHealEffect();
            }
        }
    }

    // ФУНКЦІЯ СМЕРТІ ГРАВЦЯ / Обробка смерті гравця
    // Викликається з ReceiveImpact коли життя = 0
    // 1. Зупиняє рух гравця
    // 2. Відключає рендер та колізію
    // 3. Скидає монети до початкового значення рівня
    // 4. Відтворює звук смерті
    // 5. Перезавантажує сцену після звуку
    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        // ЗВУК СМЕРТІ
        PlaySound(deathSound);

        // СКИДАННЯ МОНЕТ - видаляємо зібрані монети
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetCoinsToLevelStart();
        }

        // ЗУПИНКА РУХОМ - робимо гравця статичним
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        // ПРИХОВУВАННЯ ГРАВЦЯ - відключаємо рендер та колізію
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // ПЕРЕЗАВАНТАЖЕННЯ СЦЕНИ - чекаємо звуку смерті потім перезавантажуємо
        float delay = (deathSound != null) ? deathSound.length : 0.5f;
        Invoke(nameof(ReloadScene), delay);
    }

    // ФУНКЦІЯ ПЕРЕЗАВАНТАЖЕННЯ СЦЕНИ / Повернення на початок рівня
    // Викликається з HandleDeath через Invoke з затримкою
    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ФУНКЦІЯ ВІДТВОРЕННЯ ЗВУКУ / Програвання аудіокліпу
    // Викликається при отриманні урану, лікуванні, стрибку, смерті
    // Приймає clip (аудіокліп для відтворення)
    public void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}
