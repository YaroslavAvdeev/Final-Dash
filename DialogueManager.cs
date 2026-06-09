using UnityEngine;
using TMPro;
using System.Collections;

// DialogueManager відповідає за всю взаємодію з UI
public class DialogueManager : MonoBehaviour
{
    // Синглтон (дозволяє іншим скриптам легко до нього звертатися)
    public static DialogueManager Instance { get; private set; }

    [Header("UI Компоненти")]
    public CanvasGroup dialoguePanel;      // Панель для керування прозорістю
    public TextMeshProUGUI dialogueText;   // Текст для відображення

    [Header("Налаштування Ефекту Друкування")]
    public float typingSpeed = 0.03f;
    public float messageDuration = 3f;

    [Header("Звук")]
    public AudioSource typeSound;

    private Coroutine currentTypingRoutine;
    private bool isShowingMessage = false;

    void Awake()
    {
        // Налаштування синглтона
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Перевірка, щоб DialogueManager не знищувався при зміні сцени, 
            // DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        // Приховуємо панель при запуску
        if (dialoguePanel != null)
        {
            dialoguePanel.alpha = 0;
            dialoguePanel.blocksRaycasts = false;
        }
        else
        {
            Debug.LogError("❌ DialogueManager: Canvas Group не призначений!");
        }
    }

    // ЗОВНІШНІЙ МЕТОД: Викликається, щоб показати повідомлення
    public bool ShowMessage(string msg)
    {
        if (dialoguePanel == null || dialogueText == null || isShowingMessage)
        {
            if (isShowingMessage) Debug.Log("⏳ DialogueManager: Повідомлення вже відображається.");
            return false;
        }

        // Зупиняємо попереднє повідомлення, якщо воно друкувалося
        if (currentTypingRoutine != null)
            StopCoroutine(currentTypingRoutine);
            
        dialogueText.text = ""; // Очищаємо текст

        currentTypingRoutine = StartCoroutine(TypeMessageRoutine(msg));
        return true;
    }

    private IEnumerator TypeMessageRoutine(string msg)
    {
        isShowingMessage = true;

        // 1. Показуємо панель
        dialoguePanel.alpha = 1;
        dialoguePanel.blocksRaycasts = true;

        // 2. Ефект друкування
        foreach (char c in msg)
        {
            dialogueText.text += c;

            // ПОКРАЩЕННЯ: Використовує PlayOneShot, щоб не було накладання звуків
            if (typeSound != null && typeSound.clip != null)
                typeSound.PlayOneShot(typeSound.clip);

            yield return new WaitForSeconds(typingSpeed);
        }

        // 3. Чекаємо заданий час
        yield return new WaitForSeconds(messageDuration);

        // 4. Ховаємо панель
        dialoguePanel.alpha = 0;
        dialoguePanel.blocksRaycasts = false;
        
        isShowingMessage = false;
    }
}
