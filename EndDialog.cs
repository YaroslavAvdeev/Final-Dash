using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; // <-- НОВИЙ ПРОСТІР ІМЕН

public class EndGameDialogue : MonoBehaviour
{
    public delegate void GameStateDelegate(bool dialogueIsActive);
    public static event GameStateDelegate OnDialogueStateChange;

    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    [Header("Репліки")]
    public string[] lines;
    public float typingSpeed = 0.03f;

    [Header("Наступна Сцена")] // <-- ЗМІНЕНИЙ РОЗДІЛ
    [Tooltip("Ім'я сцени, на яку потрібно перейти після завершення діалогу (наприклад, 'MainMenu').")]
    public string nextSceneName = "MainMenu"; 

    [Header("Звук")]
    public AudioSource typingAudioSource;
    public AudioClip typingSoundClip;
    [Range(0, 1)]
    public float soundVolume = 0.5f;
    [Tooltip("Звук гратиме кожні N символів. Наприклад, 2.")]
    public int charactersBeforeSound = 2;

    [Header("Музика Діалогу")]
    [Tooltip("AudioSource, який відтворюватиме музику/фоновий звук діалогу.")]
    public AudioSource dialogueMusicSource;
    [Tooltip("Кліп музики, яка має грати на фоні діалогу.")]
    public AudioClip dialogueMusicClip;
    
    [Header("Керування грою")]
    [Tooltip("Фонова музика, яку потрібно вимкнути на час діалогу. ОБОВ'ЯЗКОВО ПРИЗНАЧТЕ!")]
    public AudioSource backgroundMusicSource;
    
    // Прибираємо PlayerPrefs/playOnce, оскільки фінал зазвичай має відтворюватися завжди
    // private const string PlayedKey = "IntroDialoguePlayed"; 

    [Header("Кнопки")]
    public Key continueKey = Key.Space;
    public Key skipKey = Key.Escape;

    private int index = 0;
    private bool isTyping = false;
    private bool isDialogueActive = false;
    private int characterCount = 0;

    private void Awake()
    {
        // Логіка для AudioSource (без змін)
        if (typingAudioSource == null)
        {
            typingAudioSource = GetComponent<AudioSource>();
        }
        if (typingAudioSource == null)
        {
            typingAudioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        if (dialoguePanel == null || dialogueText == null)
        {
            Debug.LogError("EndGameDialogue: Не призначено dialoguePanel або dialogueText! Катсцена не запуститься.");
            this.enabled = false;
            return;
        }

        // Починаємо діалог
        StartDialogue();
    }

    private void Update()
    {
        if (!isDialogueActive)
            return;

        bool inputPressed = Keyboard.current[continueKey].wasPressedThisFrame ||
                             Keyboard.current[skipKey].wasPressedThisFrame ||
                             Mouse.current.leftButton.wasPressedThisFrame;

        if (inputPressed)
        {
            if (isTyping)
            {
                // Швидкий пропуск друку
                StopAllCoroutines();
                dialogueText.text = lines[index];
                isTyping = false;
            }
            else
            {
                NextLine();
            }
        }
    }

    private void StartDialogue()
    {
        isDialogueActive = true;

        // Повідомляємо інші скрипти, що діалог активний
        SetGameControl(true);

        // Пауза фонової музики гри
        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.Pause();
        }

        // Запуск музики діалогу
        if (dialogueMusicSource != null && dialogueMusicClip != null)
        {
            if (dialogueMusicSource.clip != dialogueMusicClip || !dialogueMusicSource.isPlaying)
            {
                dialogueMusicSource.clip = dialogueMusicClip;
                dialogueMusicSource.loop = true;
                dialogueMusicSource.Play();
            }
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        index = 0;
        StartCoroutine(TypeLine());
    }

    private void EndDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        SetGameControl(false);
        isDialogueActive = false;

        // Поновлення фонової музики гри (або її вимкнення, якщо вона більше не потрібна)
        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.Stop(); // У фінальній сцені, мабуть, краще зупинити
        }

        // Зупинка музики діалогу
        if (dialogueMusicSource != null && dialogueMusicSource.isPlaying)
        {
            dialogueMusicSource.Stop();
        }

        // НОВИЙ КЛЮЧОВИЙ МОМЕНТ: Завантажуємо наступну сцену
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log($"Завантаження сцени: {nextSceneName}");
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
             Debug.LogError("EndGameDialogue: Не призначено ім'я наступної сцени! Діалог завершено, але переходу не відбулося.");
             // Якщо немає сцени, просто вимикаємо компонент
             this.enabled = false;
        }
    }

    private void SetGameControl(bool dialogueIsActive)
    {
        OnDialogueStateChange?.Invoke(dialogueIsActive);
    }

    private IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.text = "";
        characterCount = 0;

        if (index >= lines.Length)
        {
            EndDialogue();
            yield break;
        }

        string line = lines[index];

        foreach (char c in line)
        {
            dialogueText.text += c;

            if (typingSoundClip != null && typingAudioSource != null)
            {
                characterCount++;
                if (c != ' ' && characterCount >= charactersBeforeSound)
                {
                    typingAudioSource.PlayOneShot(typingSoundClip, soundVolume);
                    characterCount = 0;
                }
            }

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
    }

    private void NextLine()
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
}