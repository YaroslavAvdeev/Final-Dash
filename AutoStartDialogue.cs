using System.Collections;
using System.Collections.Generic; 
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class AutoStartDialogue : MonoBehaviour
{
    public delegate void GameStateDelegate(bool dialogueIsActive);
    public static event GameStateDelegate OnDialogueStateChange;

    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    [Header("Репліки")]
    [TextArea(2, 4)]
    public string[] lines;
    public float typingSpeed = 0.03f;

    [Header("Звук друку")]
    public AudioSource audioSource;
    public AudioClip typeSound;
    public float soundCooldown = 0.04f;

    [Header("Музика сцени")]
    public AudioSource backgroundMusic;

    [Header("PlayerPrefs")]
    public string dialoguePlayedKey;
    public bool playOnce = true;

    [Header("Кнопки")]
    public Key continueKey = Key.Space;
    public Key skipKey = Key.Escape;

    private int index;
    private bool isTyping;
    private bool isDialogueActive;
    private float lastSoundTime;

    public static HashSet<string> playedScenes = new HashSet<string>();

    // ФУНКЦІЯ ІНІЦІАЛІЗАЦІЇ / Підготовка діалогової системи
    // Викликається при старті сцени
    // 1. Перевіряє чи всі необхідні UI компоненти призначені
    // 2. Перевіряє чи є репліки для діалогу
    // 3. Встановлює ключ для PlayerPrefs (унікальний для кожної сцени)
    // 4. Перевіряє чи діалог уже показаний на цій сцені (якщо playOnce = true)
    // 5. Запускає діалог якщо все готово
    void Start()
    {
        if (dialoguePanel == null || dialogueText == null)
        {
            Debug.LogError("dialoguePanel або dialogueText не призначені!");
            enabled = false;
            return;
        }

        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("lines порожній!");
            enabled = false;
            return;
        }

        // 🔥 автоматичний ключ по сцені
        if (string.IsNullOrEmpty(dialoguePlayedKey))
        {
            dialoguePlayedKey = "dialogue_" + SceneManager.GetActiveScene().name;
        }

        // ПЕРЕВІРКА ЧИ ДІАЛОГ УЖЕ ПОКАЗАНИЙ - якщо playOnce = true то не показуємо другий раз
        if (playOnce && PlayerPrefs.GetInt(dialoguePlayedKey, 0) == 1)
        {
            dialoguePanel.SetActive(false);
            enabled = false;
            return;
        }

        StartDialogue();
    }

    // ФУНКЦІЯ ОБРОБКИ ВВЕДЕННЯ / Слухання кнопок від гравця
    // Викликається кожен кадр
    // 1. Перевіряє чи діалог активний
    // 2. Слухає кнопки (Space, Escape, ліва кнопка миші)
    // 3. Якщо текст ще друкується - показує весь текст одразу
    // 4. Якщо текст вже показаний - переходить на наступну репліку
    void Update()
    {
        if (!isDialogueActive) return;
        if (Keyboard.current == null) return;

        // ПЕРЕВІРКА БУДЬ-ЯКОЇ КНОПКИ для продовження діалогу
        bool pressed =
            Keyboard.current[continueKey].wasPressedThisFrame ||
            Keyboard.current[skipKey].wasPressedThisFrame ||
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (!pressed) return;

        // ЯКЩО ЕЩЕ ДРУКУЄТЬСЯ - показуємо весь текст одразу
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = lines[index];
            isTyping = false;
        }
        else
        {
            // ЯКЩО ТЕКСТ ПОКАЗАНИЙ - переходимо на наступну репліку
            NextLine();
        }
    }

    // ФУНКЦІЯ ЗАПУСКУ ДІАЛОГУ / Початок показу репік
    // Викликається з Start або при виклику скрипту
    // 1. Встановлює isDialogueActive = true
    // 2. Скидає індекс на першу репліку (index = 0)
    // 3. ПАУЗУЄ ГРУ - встановлює Time.timeScale = 0
    // 4. Паузує фонову музику
    // 5. Викликає подію OnDialogueStateChange для сповіщення інших систем
    // 6. Активує панель діалогу на екрані
    // 7. Запускає корутину для друку першої репліки
    void StartDialogue()
    {
        isDialogueActive = true;
        index = 0;

        // ПАУЗА ГРИ - час зупиняється поки діалог активний
        Time.timeScale = 0f;

        // ПАУЗА МУЗИКИ
        if (backgroundMusic != null && backgroundMusic.isPlaying)
            backgroundMusic.Pause();

        // СПОВІЩЕННЯ ІНШИХ СИСТЕМ про активацію діалогу
        OnDialogueStateChange?.Invoke(true);

        dialoguePanel.SetActive(true);
        StartCoroutine(TypeLine());
    }

    // ФУНКЦІЯ ДРУКУ ТЕКСТУ / Поступовий вивід тексту репліки по символам
    // Викликається з StartDialogue та NextLine
    // 1. Встановлює isTyping = true
    // 2. Очищує текстовое поле
    // 3. Для кожного символа репліки:
    //    - Додає символ до тексту
    //    - Відтворює звук друку (якщо це не пробіл)
    //    - Чекає typingSpeed перед наступним символом (реальний час, не гарниємий)
    // 4. Коли весь текст надрукований - встановлює isTyping = false
    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.text = "";

        // ЦИКЛ ДЛЯ КОЖНОГО СИМВОЛА РЕПЛІКИ
        foreach (char c in lines[index])
        {
            dialogueText.text += c;

            // ЗВУК ДРУКУ - тільки для не-пробілу, з cooldown
            if (c != ' ' && audioSource != null && typeSound != null &&
                Time.realtimeSinceStartup - lastSoundTime >= soundCooldown)
            {
                audioSource.PlayOneShot(typeSound);
                lastSoundTime = Time.realtimeSinceStartup;
            }

            // ЗАТРИМКА ПЕРЕД НАСТУПНИМ СИМВОЛОМ (використовуємо realtimeSinceStartup для паузи гри)
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
    }

    // ФУНКЦІЯ НАСТУПНОЇ РЕПЛІКИ / Перехід до наступної репліки або закінчення діалогу
    // Викликається з Update при натисканні кнопки
    // 1. Перевіряє чи є наступна репліка (index < lines.Length - 1)
    // 2. Якщо є - збільшує index та запускає друк наступної репліки
    // 3. Якщо це остання репліка - викликає EndDialogue
    void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    // ФУНКЦІЯ ЗАКІНЧЕННЯ ДІАЛОГУ / Скиданя діалогової системи та повернення до гри
    // Викликається з NextLine коли всі репліки показані
    // 1. Встановлює isDialogueActive = false
    // 2. Сховає панель діалогу
    // 3. ВІДНОВЛЮЄ ГУЛВИХ - встановлює Time.timeScale = 1
    // 4. Відновлює фонову музику
    // 5. Викликає подію OnDialogueStateChange для сповіщення інших систем
    // 6. Якщо playOnce = true - зберігає в PlayerPrefs що діалог вже показаний
    // 7. Відключає скрипт
    void EndDialogue()
    {
        isDialogueActive = false;

        // ПРИХОВУВАННЯ ПАНЕЛІ
        dialoguePanel.SetActive(false);

        // ПОВЕРНЕННЯ ДО НОРМАЛЬНОГО ЧАСУ - гра продовжується
        Time.timeScale = 1f;

        // ВІДНОВЛЕННЯ МУЗИКИ
        if (backgroundMusic != null)
            backgroundMusic.UnPause();

        // СПОВІЩЕННЯ ІНШИХ СИСТЕМ про деактивацію діалогу
        OnDialogueStateChange?.Invoke(false);

        // ЗБЕРЕЖЕННЯ що ДІАЛОГ УЖЕ ПОКАЗАНИЙ
        if (playOnce)
        {
            PlayerPrefs.SetInt(dialoguePlayedKey, 1);
            PlayerPrefs.Save();
        }

        enabled = false;
    }

    // ФУНКЦІЯ ОЧИЩЕННЯ / Безпечне завершення при знищенні об'єкту
    // Викликається коли об'єкт знищується (приховується або видаляється сцена)
    // 1. Перевіряє чи діалог ще активний
    // 2. Якщо активний - повертає час на нормальну швидкість
    // 3. Відновлює музику (щоб не залишилась паузована)
    // 4. Викликає подію для сповіщення інших систем
    void OnDestroy()
    {
        if (isDialogueActive)
        {
            Time.timeScale = 1f;
            if (backgroundMusic != null) backgroundMusic.UnPause();
            OnDialogueStateChange?.Invoke(false);
        }
    }
}
