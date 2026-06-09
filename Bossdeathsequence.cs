using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class BossDeathSequence : MonoBehaviour
{
    [Header("── Кінцева сцена ──")]
    [Tooltip("Точна назва сцени (як в File → Build Settings), без .unity")]
    public string endSceneName = "EndScene";

    [Header("── Затемнення ──")]
    [Tooltip("UI Image — чорний колір, Alpha=0, розтягнута на весь екран")]
    public Image  fadeImage;
    [Tooltip("Затримка після смерті боса перед затемненням (секунди)")]
    public float  delayBeforeFade = 12f;
    [Tooltip("Тривалість затемнення (секунди)")]
    public float  fadeDuration    = 2f;

    [Header("── Музика перемоги (опційно) ──")]
    public AudioClip   victoryMusic;
    public AudioSource musicSource;

    // ────────────────────────────────────────────────────────────────
    private SharkBossAI bossAI;
    private bool        sequenceStarted = false;

    void Start()
    {
        // Шукаємо боса в сцені
        bossAI = FindObjectOfType<SharkBossAI>();

        if (bossAI == null)
            Debug.LogError("[BossDeathSequence] ❌ SharkBossAI не знайдено в сцені!");
        else
            Debug.Log($"[BossDeathSequence] ✅ Бос знайдений: {bossAI.gameObject.name}");

        // Переконуємось що fade image невидима на старті
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            fadeImage.color = new Color(c.r, c.g, c.b, 0f);
            fadeImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[BossDeathSequence] ⚠ fadeImage не призначено — перехід без затемнення.");
        }
    }

    void Update()
    {
        if (sequenceStarted) return;

        // bossAI == null означає що GameObject знищено (Destroy після смерті)
        // бо акула робить Destroy(gameObject, 1.5f) в Die()
        bool bossIsDead = (bossAI == null) || bossAI.IsDead;

        if (bossIsDead)
        {
            sequenceStarted = true;
            StartCoroutine(DeathSequence());
        }
    }

    IEnumerator DeathSequence()
    {
        Debug.Log("[BossDeathSequence] 🎬 Бос мертвий — починаємо кінцеву послідовність.");

        // Музика перемоги
        if (victoryMusic != null && musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip   = victoryMusic;
            musicSource.loop   = false;
            musicSource.volume = 1f;
            musicSource.Play();
        }

        // Чекаємо перед затемненням
        yield return new WaitForSeconds(delayBeforeFade);

        Debug.Log("[BossDeathSequence] 🌑 Затемнення...");

        // Плавне затемнення
        if (fadeImage != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                Color c = fadeImage.color;
                fadeImage.color = new Color(c.r, c.g, c.b, alpha);
                yield return null;
            }
            Color fc = fadeImage.color;
            fadeImage.color = new Color(fc.r, fc.g, fc.b, 1f);
        }

        yield return new WaitForSeconds(0.3f);

        if (!string.IsNullOrEmpty(endSceneName))
        {
            Debug.Log($"[BossDeathSequence] 🎬 Завантажуємо сцену: '{endSceneName}'");
            SceneManager.LoadScene(endSceneName);
        }
        else
        {
            Debug.LogError("[BossDeathSequence] ❌ endSceneName порожній! Заповни назву сцени в інспекторі.");
        }
    }
}
