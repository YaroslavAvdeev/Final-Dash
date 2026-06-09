using UnityEngine;

public class MusicZoneTrigger : MonoBehaviour
{
    [Header("Музика")]
    public AudioClip newMusic;
    [Range(0f, 1f)]
    public float volume = 1f;
    public bool loop = true;

    [Header("Перехід")]
    public float fadeDuration = 0.5f;

    [Header("AudioSource з музикою")]
    [Tooltip("Перетягни сюди об'єкт який грає музику на сцені")]
    public AudioSource musicSource; // просто перетягни в інспекторі

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        if (newMusic == null) return;
        if (musicSource == null) return;

        triggered = true;
        StartCoroutine(SwitchMusic());
    }

    private System.Collections.IEnumerator SwitchMusic()
    {
        // Плавно затухає стара
        float startVolume = musicSource.volume;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }

        // Міняємо
        musicSource.Stop();
        musicSource.clip   = newMusic;
        musicSource.loop   = loop;
        musicSource.volume = 0f;
        musicSource.Play();

        // Плавно появляється нова
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, volume, t / fadeDuration);
            yield return null;
        }

        musicSource.volume = volume;
    }
}