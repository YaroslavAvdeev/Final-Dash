using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class EndGameTrigger : MonoBehaviour
{
    [Header("Налаштування рівня")]
    public int requiredScore = 10;      // Кількість кристалів для проходження
    public string nextSceneName;        // Назва наступної сцени (рівня)

    [Header("UI Повідомлення (TextMeshPro)")]
    public TextMeshProUGUI hintText;    // Сюди перетягни текст з Canvas
    public float typingSpeed = 0.05f;   // Швидкість появи літер
    public float displayDuration = 3f;  // Скільки секунд текст висить після друку

    [Header("Звукові ефекти")]
    public AudioClip typeSound;         // Звук тапання літер
    private AudioSource audioSource;

    private Coroutine typingCoroutine;

    void Start()
    {
        // Налаштовуємо звук
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) 
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Ховаємо текст на старті гри
        if (hintText != null)
        {
            hintText.text = "";
            hintText.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Перевіряємо, чи це гравець
        if (other.CompareTag("Player"))
        {
            int currentCoins = 0;
            if (GameStateManager.Instance != null)
            {
                currentCoins = GameStateManager.Instance.totalCoins;
            }

            // Якщо кристалів достатньо — йдемо далі
            if (currentCoins >= requiredScore)
            {
                if (!string.IsNullOrEmpty(nextSceneName))
                {
                    SceneManager.LoadScene(nextSceneName);
                }
                else
                {
                    Debug.LogWarning("Назва наступної сцени не вказана!");
                }
            }
            else
            {
                // Якщо не вистачає — запускаємо друк тексту
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                typingCoroutine = StartCoroutine(TypeText(requiredScore - currentCoins));
            }
        }
    }

    private IEnumerator TypeText(int missingAmount)
    {
        if (hintText == null) yield break;

        hintText.gameObject.SetActive(true);
        string fullMessage = "Ой! Мені не вистачає ще " + missingAmount + " кристалів!";
        hintText.text = ""; // Очищуємо поле перед друком

        foreach (char letter in fullMessage.ToCharArray())
        {
            hintText.text += letter;

            // Граємо звук, якщо він призначений
            if (typeSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(typeSound);
            }

            // Чекаємо перед наступною літерою
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        // Чекаємо кілька секунд і ховаємо текст
        yield return new WaitForSeconds(displayDuration);
        
        hintText.text = "";
        hintText.gameObject.SetActive(false);
    }
}