using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; // Потрібно для перевірки натискання клавіші

public class HouseDialogueTrigger : MonoBehaviour
{
    // Налаштування UI
    public GameObject dialoguePanel; 
    public TextMeshProUGUI dialogueText; 
    
    [Header("Репліки")]
    public string[] lines; 
    public float typingSpeed = 0.03f; 
    
    [Header("Налаштування Взаємодії")]
    public Key jumpKey = Key.Space; // Ключ, який буде використовуватися для стрибка та діалогу

    // Внутрішні змінні
    private int index = 0;
    private bool playerInRange = false;
    private bool isTyping = false; // Відстежує, чи відбувається набір тексту
    private Coroutine typeCoroutine; // Посилання на поточну корутину друкування

    private void Start()
    {
        // Ховаємо діалогову панель при старті
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        // Перевіряємо, чи гравець у зоні та чи натиснута клавіша взаємодії (Space, яку ми використовуємо)
        if (playerInRange && Keyboard.current[jumpKey].wasPressedThisFrame)
        {
            if (dialoguePanel.activeInHierarchy)
            {
                // Якщо діалог активний, обробляємо введення
                HandleUserInput();
            }
            else
            {
                // Якщо діалог не активний, запускаємо його
                ShowDialogue(); 
            }
        }
    }

    // =======================================================
    //                       ТРИГЕРИ ЗІТКНЕНЬ
    // =======================================================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
            // після натискання гравцем кнопки у Update().
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            index = 0;
            
            // Зупиняємо діалог та друкування
            StopAllCoroutines(); 
            isTyping = false;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }
    }

    // =======================================================
    //                       ЛОГІКА ДІАЛОГУ
    // =======================================================

    private void HandleUserInput()
    {
        if (isTyping)
        {
            // Це змушує гравця чекати на повний набір тексту.
            return; 
        }
        else
        {
            // Якщо текст набраний повністю, переходимо до наступної репліки.
            NextLine();
        }
    }

    public void ShowDialogue()
    {
        if (dialoguePanel == null || dialogueText == null || lines.Length == 0)
        {
            Debug.LogError("❌ DialogueTrigger: Не заповнені посилання в інспекторі або немає реплік!");
            return;
        }

        dialoguePanel.SetActive(true);
        index = 0; // Завжди починаємо з першої репліки при старті
        typeCoroutine = StartCoroutine(TypeRoutine());
    }

    private IEnumerator TypeRoutine()
    {
        isTyping = true;
        dialogueText.text = "";

        if (index >= lines.Length)
        {
            dialoguePanel.SetActive(false);
            isTyping = false;
            yield break;
        }

        // --- ДРУКУВАННЯ ТЕКСТУ ---
        string currentLine = lines[index];
        foreach (char c in currentLine)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        
        // Після завершення друкування
        isTyping = false;
    }

    public void NextLine()
    {
        if (index < lines.Length - 1)
        {
            // Переходимо до наступної репліки
            index++;
            typeCoroutine = StartCoroutine(TypeRoutine());
        }
        else
        {
            // Кінець діалогу
            dialoguePanel.SetActive(false);
            index = 0; // Скидаємо для наступного входу
        }
    }
}
