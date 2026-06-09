using UnityEngine;

public class PlatformMover : MonoBehaviour
{
    public Transform targetPoint;
    public float speed = 2f;

    private Vector3 startPosition;
    private bool movingToTarget = true;

    private const float MIN_DISTANCE = 0.05f;

    // ФУНКЦІЯ ІНІЦІАЛІЗАЦІЇ / Підготовка рухомої платформи
    // Викликається при старті сцени
    // 1. Зберігає початкову позицію платформи (стартову точку)
    // 2. Встановлює тег "MovingPlatform" для розпізнавання гравцем
    void Start()
    {
        startPosition = transform.position;

        // Виставляємо тег для розпізнавання
        gameObject.tag = "MovingPlatform";
    }

    // ФУНКЦІЯ РУХУ ПЛАТФОРМИ / Переміщення платформи між двома точками
    // Викликається за розписанням фізики (FixedUpdate)
    // 1. Перевіряє чи призначена цільова точка
    // 2. Рухає платформу від стартової позиції до цільової і назад
    // 3. Використовує Vector3.MoveTowards для гладкого руху
    // 4. Коли платформа досягає цілі - змінює напрямок на протилежний
    void FixedUpdate()
    {
        if (targetPoint == null) return;

        // ВИБІР ЦІЛІ - рухаємо до targetPoint або назад до стартової позиції
        Vector3 target = movingToTarget ? targetPoint.position : startPosition;

        // РУХ ПЛАТФОРМИ - гладкий рух до цілі з постійною швидкістю
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.fixedDeltaTime
        );

        // ПЕРЕВІРКА ДОСЯГНЕННЯ ЦІЛІ - коли платформа близько до цілі
        if (Vector3.Distance(transform.position, target) < MIN_DISTANCE)
            movingToTarget = !movingToTarget; // Змінюємо напрямок руху
    }

    // ФУНКЦІЯ ВХІДНОЇ КОЛІЗІЇ / Прив'язка гравця до платформи
    // Викликається коли гравець входить в колізію з платформою
    // 1. Перевіряє чи це гравець (за тегом "Player")
    // 2. Робить гравця дочірнім об'єктом платформи
    // 3. Тепер гравець рухається разом з платформою
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            collision.collider.transform.SetParent(transform);
        }
    }

    // ФУНКЦІЯ ВИХІДНОЇ КОЛІЗІЇ / Відв'язка гравця від платформи
    // Викликається коли гравець залишає колізію з платформою
    // 1. Перевіряє чи це гравець (за тегом "Player")
    // 2. Робить гравця незалежним об'єктом (SetParent(null))
    // 3. Гравець більше не рухається з платформою
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            collision.collider.transform.SetParent(null);
        }
    }
}
