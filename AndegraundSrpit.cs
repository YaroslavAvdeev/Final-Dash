using UnityEngine;

public class CaveParallaxLayer : MonoBehaviour
{
    [Range(0f, 1f)]
    public float parallaxFactorX = 0.3f;

    private Transform cam;
    private float spriteWidth;
    private GameObject leftTile;
    private GameObject rightTile;

    private float anchorX;
    private float anchorY;
    private float anchorZ;
    private float camAnchorX;
    private bool  isReady = false;

    // ФУНКЦІЯ ІНІЦІАЛІЗАЦІЇ / Підготовка паралакса
    // Викликається при старті сцени
    // Отримує посилання на камеру, розраховує розміри спрайту
    // Створює додаткові плитки зліва та справа для безперервного скролу
    void Start()
    {
        cam = Camera.main.transform;

        anchorX = transform.position.x;
        anchorY = transform.position.y;
        anchorZ = transform.position.z;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            spriteWidth = sr.bounds.size.x;
            leftTile    = CreateTile(-spriteWidth);
            rightTile   = CreateTile( spriteWidth);
        }
    }

    // ФУНКЦІЯ АКТИВАЦІЇ ПАРАЛАКСА / Запуск ефекту
    // Викликається з CaveZone коли гравець входить в зону
    // Запам'ятовує поточну позицію камери як точку відліку
    public void Activate()
    {
        // Запам'ятовуємо де камера ЗАРАЗ — тобто вже в печері
        camAnchorX = cam.position.x;
        isReady    = true;
    }

    // ФУНКЦІЯ ОНОВЛЕННЯ ПОЗИЦІЇ / Зміщення фону на основі руху камери
    // Викликається кожен кадр після Update
    // Рухає фон з урахуванням коефіцієнту паралакса
    // Рухає плитки разом з основним об'єктом
    void LateUpdate()
    {
        if (!isReady) return;

        // РОЗРАХУНОК ПАРАЛАКСА - рух відносно моменту входу в печеру
        float camDeltaX = cam.position.x - camAnchorX;
        float newX      = anchorX + camDeltaX * (1f - parallaxFactorX);

        // ОНОВЛЕННЯ ПОЗИЦІЇ основного об'єкту та плиток
        transform.position = new Vector3(newX, anchorY, anchorZ);

        if (leftTile  != null) leftTile.transform.position  = new Vector3(newX - spriteWidth, anchorY, anchorZ);
        if (rightTile != null) rightTile.transform.position = new Vector3(newX + spriteWidth, anchorY, anchorZ);

        LoopTiles();
    }

    // ФУНКЦІЯ ЦИКЛУВАННЯ ПЛИТОК / Переміщення плиток назад при виході з видимості
    // Викликається з LateUpdate
    // Робить фон безінечним - при скролу плитки телепортуються назад
    void LoopTiles()
    {
        float camX = cam.position.x;
        GameObject[] tiles = { leftTile, gameObject, rightTile };
        foreach (var tile in tiles)
        {
            if (tile == null) continue;
            // ПЕРЕВІРКА ВИДИМОСТІ - якщо плитка далеко, телепортуємо її назад
            float dist = tile.transform.position.x - camX;
            if (dist < -spriteWidth * 1.5f)
                tile.transform.position += new Vector3(spriteWidth * 3f, 0, 0);
            else if (dist > spriteWidth * 1.5f)
                tile.transform.position -= new Vector3(spriteWidth * 3f, 0, 0);
        }
    }

    // ФУНКЦІЯ СТВОРЕННЯ ПЛИТКИ / Клонування фону
    // Викликається з Start (для лівої та правої плитки)
    // Приймає зміщення по X (offsetX)
    // Повертає новий GameObject з копією спрайту та його властивостей
    GameObject CreateTile(float offsetX)
    {
        var tile = new GameObject(gameObject.name + "_tile");
        tile.transform.parent = transform.parent;

        // КОПІЮВАННЯ СПРАЙТУ - перенесення всіх візуальних властивостей
        var orig = GetComponent<SpriteRenderer>();
        var sr   = tile.AddComponent<SpriteRenderer>();
        sr.sprite           = orig.sprite;
        sr.sortingLayerName = orig.sortingLayerName;
        sr.sortingOrder     = orig.sortingOrder;
        sr.color            = orig.color;

        // ВСТАНОВЛЕННЯ ПОЗИЦІЇ та МАСШТАБУ нової плитки
        tile.transform.position   = new Vector3(
            transform.position.x + offsetX,
            transform.position.y,
            transform.position.z);
        tile.transform.localScale = transform.localScale;
        return tile;
    }
}
