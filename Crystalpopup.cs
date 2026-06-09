using UnityEngine;
using TMPro;
/// Префаб: порожній GameObject + TMP_Text + цей скрипт
/// Показує "+N" і плаває вгору потім зникає
public class CrystalPopup : MonoBehaviour
{
    [Header("Анімація")]
    public float lifetime = 0.8f;
    public float floatSpeed = 1.5f;
    public float fadeSpeed = 2f;

    private TMP_Text label;
    private float timer = 0f;
    private Color originalColor;

    // ФУНКЦІЯ ІНІЦІАЛІЗАЦІЇ / Пошук текстового компонента
    // Викликається автоматично при створенні об'єкту
    // Отримує посилання на TMP_Text компонент і зберігає оригінальний колір
    void Awake()
    {
        label = GetComponentInChildren<TMP_Text>();
        if (label != null)
            originalColor = label.color;
    }

    // ФУНКЦІЯ ВСТАНОВЛЕННЯ ЗНАЧЕННЯ / Відображення кількості кристалів
    // Приймає кількість кристалів (value) і виводить "+N" на екран
    // Викликається коли персонаж збирає крищали
    public void SetValue(int value)
    {
        if (label != null)
            label.text = "+" + value;
    }

    // ФУНКЦІЯ АНІМАЦІЇ / Оновлення позиції та прозорості
    // Викликається кожен кадр
    // 1. Піднімає текст вгору (плаваючий ефект)
    // 2. Робить текст прозорішим до повного зникнення
    // 3. Видаляє об'єкт після завершення анімації
    void Update()
    {
        timer += Time.deltaTime;

        // РУХ ВГОРУ - переміщення об'єкту по вісі Y
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // ПЛАВНЕ ЗНИКНЕННЯ - зменшення прозорості (alpha) від 1 до 0
        if (label != null)
        {
            float alpha = Mathf.Lerp(1f, 0f, timer / lifetime);
            label.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
        }

        // ВИДАЛЕННЯ ОБ'ЄКТУ - коли час анімації закінчився
        if (timer >= lifetime)
            Destroy(gameObject);
    }
}
