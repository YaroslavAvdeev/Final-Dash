using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    [Header("Player State")]
    public int currentLives = 3;
    public int totalCoins = 0;

    [Header("Checkpoint")]
    public Vector3 spawnPosition;
    private int coinsAtCheckpoint = 0;

    private int coinsAtLevelStart = 0;
    private string lastLoadedScene = "";

    // ФУНКЦІЯ ІНІЦІАЛІЗАЦІЇ / Singleton для управління станом гри
    // Викликається при створенні об'єкту
    // 1. Встановлює Instance як singleton
    // 2. Робить об'єкт наслідуючим сцени (DontDestroyOnLoad)
    // 3. Реєструє слухача для завантаження нових сцен
    // 4. Зберігає початкову кількість монет рівня
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            coinsAtLevelStart = totalCoins;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ФУНКЦІЯ АКТИВАЦІЇ ЧЕКПОІНТА / Збереження позиції гравця та монет
    // Викликається при вході гравця у чекпоінт
    // Приймає position (позиція чекпоінта)
    // 1. Зберігає позицію чекпоінта
    // 2. Зберігає кількість монет на момент активації чекпоінта
    // 3. Це дозволяє гравцю повернутися сюди при смерті
    public void ActivateCheckpoint(Vector3 position)
    {
        spawnPosition = position;
        coinsAtCheckpoint = totalCoins; // зберігаємо монети на момент чекпоінта
        Debug.Log($"Checkpoint activated at {position}. Coins saved: {coinsAtCheckpoint}");
    }

    // ФУНКЦІЯ РЕСПАВНУ ГРАВЦЯ / Повернення гравця на чекпоінт
    // Викликається при смерті гравця
    // 1. Знаходить гравця по тегу "Player"
    // 2. Переміщує гравця на позицію чекпоінта
    // 3. Відновлює монети до значення при активації чекпоінта
    // 4. Оновлює UI дисплей монет
    public void RespawnPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            player.transform.position = spawnPosition;

        // Відновлюємо монети і UI
        totalCoins = coinsAtCheckpoint;
        CoinUIManager.Instance?.UpdateCoinDisplay(totalCoins);
        Debug.Log("Player respawned at checkpoint.");
    }

    // ФУНКЦІЯ ЗАВАНТАЖЕННЯ СЦЕНИ / Обновлення стану при завантаженні нової сцени
    // Викликається автоматично коли Unity завантажує нову сцену
    // Приймає scene (інформація про сцену) та mode (режим завантаження)
    // 1. Перевіряє чи це новий запуск рівня (а не смерть/респавн)
    // 2. Якщо новий рівень - скидає монети на початок рівня
    // 3. Скидає чекпоінт при новому старті гри
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Якщо це новий запуск рівня (а не смерть)
        if (scene.name != lastLoadedScene)
        {
            coinsAtLevelStart = totalCoins;
            lastLoadedScene = scene.name;

            // ❗ Скидаємо чекпоінт при новому старті гри
            spawnPosition = Vector3.zero;
        }

        StartCoroutine(AfterSceneLoad());
    }

    // ФУНКЦІЯ ПІСЛЯ ЗАВАНТАЖЕННЯ СЦЕНИ / Корутина для затримки позиціонування гравця
    // Викликається з OnSceneLoaded з затримкою в 1 фрейм
    // 1. Чекає один фрейм щоб всі об'єкти завантажились
    // 2. Знаходить гравця на новій сцені
    // 3. Якщо є збережена позиція чекпоінта - переміщує гравця туди
    // 4. Оновлює UI
    private System.Collections.IEnumerator AfterSceneLoad()
    {
        yield return null;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null && spawnPosition != Vector3.zero)
            player.transform.position = spawnPosition;

        RefreshUI();
    }

    // ФУНКЦІЯ ОНОВЛЕННЯ UI / Синхронізація дисплею монет
    // Викликається кожен раз коли змінюється кількість монет
    // Знаходить CoinUIManager і оновлює дисплей монет
    private void RefreshUI()
    {
        CoinUIManager ui = CoinUIManager.Instance;
        if (ui != null)
            ui.UpdateCoinDisplay(totalCoins);
    }

    // ФУНКЦІЯ СКИДАННЯ ГРИ / Повернення всіх параметрів до початкових значень
    // Викликається при скиданні гри (меню, перезапуск)
    // 1. Скидає монети на 0
    // 2. Скидає живі на 3
    // 3. Скидає позицію чекпоінта
    // 4. Скидає всі збережені дані про рівень
    public void ResetGame()
    {
        totalCoins = 0;
        currentLives = 3;
        spawnPosition = Vector3.zero;
        coinsAtLevelStart = 0;
        lastLoadedScene = "";
        coinsAtCheckpoint = 0;

        RefreshUI();
        Debug.Log("GAME RESET");
    }

    // ФУНКЦІЯ ДОДАВАННЯ МОНЕТ / Збільшення кількості монет
    // Викликається при збиранні монети на рівні
    // Приймає amount (кількість монет для додавання)
    // 1. Збільшує totalCoins
    // 2. Оновлює UI дисплей
    public void AddCoins(int amount)
    {
        totalCoins += amount;
        RefreshUI();
    }

    // ФУНКЦІЯ СКИДАННЯ МОНЕТ ДО ПОЧАТКУ РІВНЯ / Видалення зібраних монет при смерті
    // Викликається з PlayerLife при смерті гравця
    // Повертає монети до значення яке було на початку рівня
    public void ResetCoinsToLevelStart()
    {
        totalCoins = coinsAtLevelStart;
        RefreshUI();
    }

    // ФУНКЦІЯ ЗБЕРЕЖЕННЯ ПРОГРЕСУ / Фіксація монет як нове значення початку рівня
    // Викликається при завершенні рівня (перемога)
    // Це значення буде використане як точка для скидання при наступній смерті
    public void SaveProgressAtLevelEnd()
    {
        coinsAtLevelStart = totalCoins;
    }

    // ФУНКЦІЯ ОЧИЩЕННЯ / Видалення слухача при знищенні об'єкту
    // Викликається коли об'єкт знищується
    // Видаляє реєстрацію слухача для запобігання помилок
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
