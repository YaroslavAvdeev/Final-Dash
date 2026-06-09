using UnityEngine;

public class InfiniteBackground : MonoBehaviour
{
    [Header("Швидкість руху шару")]
    public float scrollSpeed = 0.5f;

    [Header("Напрямок (1 = вправо, -1 = вліво)")]
    public float direction = -1f;

    [Header("Хаотичність швидкості")]
    public float speedVariation = 0.3f;
    public float speedChangeInterval = 3f;

    private Transform cam;
    private float spriteWidth;
    private GameObject leftTile;
    private GameObject rightTile;

    private float currentSpeed;
    private float targetSpeed;
    private float speedChangeTimer;

    // ФУНКЦІЯ ІНІЦІАЛІЗАЦІЇ / Підготовка нескінченого фону
    // Викликається при старті сцени
    // 1. Отримує посилання на камеру
    // 2. Розраховує ширину спрайту для циклювання
    // 3. Встановлює початкову швидкість та напрямок руху
    // 4. Вирівнює позицію фону з камерою
    // 5. Створює додаткові плитки зліва та справа для безперервного скролу
    void Start()
    {
        cam = Camera.main.transform;
        spriteWidth = GetComponent<SpriteRenderer>().bounds.size.x;

        currentSpeed = scrollSpeed;
        targetSpeed = scrollSpeed;
        speedChangeTimer = Random.Range(2f, speedChangeInterval);

        // ВИРІВНЮВАННЯ З КАМЕРОЮ - фон починається де камера
        Vector3 pos = transform.position;
        pos.x = cam.position.x;
        transform.position = pos;

        // СТВОРЕННЯ ДОДАТКОВИХ ПЛИТОК
        leftTile  = CreateTile(-spriteWidth);
        rightTile = CreateTile( spriteWidth);
    }

    // ФУНКЦІЯ ОНОВЛЕННЯ ПОЗИЦІЇ / Рух фону та плиток
    // Викликається кожен кадр після Update (LateUpdate)
    // 1. Оновлює швидкість фону (з хаотичністю)
    // 2. Рухає основний спрайт та обидві плитки в одному напрямку
    // 3. Проводить перевірку циклювання (LoopTiles) для безперервного скролу
    void LateUpdate()
    {
        UpdateSpeed();

        // РОЗРАХУНОК РУХУ - швидкість * напрямок * час
        float move = currentSpeed * direction * Time.deltaTime;

        // РУХ ВСІХ ПЛИТОК - рухаємо основну плитку та дві додаткові
        transform.position           += new Vector3(move, 0, 0);
        leftTile.transform.position  += new Vector3(move, 0, 0);
        rightTile.transform.position += new Vector3(move, 0, 0);

        LoopTiles();
    }

    // ФУНКЦІЯ ОНОВЛЕННЯ ШВИДКОСТІ / Хаотичне змінення швидкості фону
    // Викликається з LateUpdate кожен кадр
    // 1. Зменшує таймер зміни швидкості
    // 2. Коли таймер = 0 генерує нову цільову швидкість (з випадковим коливанням)
    // 3. Плавно змінює поточну швидкість до цільової (Lerp)
    // 4. Це створює ефект "дихання" фону
    void UpdateSpeed()
    {
        speedChangeTimer -= Time.deltaTime;
        if (speedChangeTimer <= 0f)
        {
            // ГЕНЕРАЦІЯ НОВОЇ ЦІЛЬОВОЇ ШВИДКОСТІ - випадкове коливання
            targetSpeed = scrollSpeed + Random.Range(-speedVariation, speedVariation);
            targetSpeed = Mathf.Max(0.05f, targetSpeed); // Мінімальна швидкість 0.05
            speedChangeTimer = Random.Range(
                speedChangeInterval * 0.5f,
                speedChangeInterval * 1.5f);
        }
        // ПЛАВНА ЗМІНА ШВИДКОСТІ - Lerp для гладкого переходу
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 0.4f);
    }

    // ФУНКЦІЯ ЦИКЛЮВАННЯ ПЛИТОК / Телепортування плиток для безперервного скролу
    // Викликається з LateUpdate кожен кадр
    // 1. Перевіряє відстань кожної плитки від камери
    // 2. Коли плитка занадто далеко вліво - переміщує її вправо
    // 3. Коли плитка занадто далеко вправо - переміщує її вліво
    // 4. Це створює ілюзію нескінченого фону
    void LoopTiles()
    {
        GameObject[] tiles = { leftTile, gameObject, rightTile };
        float camX = cam.position.x;

        foreach (var tile in tiles)
        {
            if (tile == null) continue;
            // РОЗРАХУНОК ВІДСТАНІ від камери
            float dist = tile.transform.position.x - camX;
            // ТЕЛЕПОРТУВАННЯ ВЛІВО - якщо плитка далеко вправо від камери
            if (dist < -spriteWidth * 1.5f)
                tile.transform.position += new Vector3(spriteWidth * 3f, 0, 0);
            // ТЕЛЕПОРТУВАННЯ ВПРАВО - якщо плитка далеко вліво від камери
            else if (dist > spriteWidth * 1.5f)
                tile.transform.position -= new Vector3(spriteWidth * 3f, 0, 0);
        }
    }

    // ФУНКЦІЯ СТВОРЕННЯ ПЛИТКИ / Клонування фону для циклювання
    // Викликається з Start (для лівої та правої плитки)
    // Приймає offsetX (зміщення по X осі)
    // 1. Створює новий GameObject з ім'ям
    // 2. Додає SpriteRenderer компонент з копією спрайту
    // 3. Копіює всі візуальні властивості (колір, сортування)
    // 4. Встановлює позицію з заданим зміщенням
    // 5. Встановлює масштаб як у оригіналу
    // 6. Повертає новий GameObject
    GameObject CreateTile(float offsetX)
    {
        GameObject tile = new GameObject(gameObject.name + "_tile");
        tile.transform.parent = transform.parent;

        // КОПІЮВАННЯ СПРАЙТУ - перенесення всіх властивостей
        SpriteRenderer orig = GetComponent<SpriteRenderer>();
        SpriteRenderer sr   = tile.AddComponent<SpriteRenderer>();
        sr.sprite           = orig.sprite;
        sr.sortingLayerName = orig.sortingLayerName;
        sr.sortingOrder     = orig.sortingOrder;
        sr.color            = orig.color;

        // ВСТАНОВЛЕННЯ ПОЗИЦІЇ та МАСШТАБУ
        tile.transform.position = new Vector3(
            transform.position.x + offsetX,
            transform.position.y,
            transform.position.z);
        tile.transform.localScale = transform.localScale;

        return tile;
    }
}
