using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelExit : MonoBehaviour
{
    public Vector3 level2SpawnPosition;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerLife life = other.GetComponent<PlayerLife>();

        if (GameStateManager.Instance != null)
        {
            // 1. Зберігаємо життя
            GameStateManager.Instance.currentLives = life.CurrentLives;
            
            // 2. Зберігаємо позицію для наступного рівня
            GameStateManager.Instance.spawnPosition = level2SpawnPosition;

            // 3. КРИТИЧНО ВАЖЛИВО: Фіксуємо кристали!
            GameStateManager.Instance.SaveProgressAtLevelEnd();
        }

        // Перед завантаженням підписуємося на подію (це залишаємо, як було)
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene("Level2");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Level2") return;

        // Відписуємося відразу, щоб не спрацювало двічі
        SceneManager.sceneLoaded -= OnSceneLoaded;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // Ставимо гравця в нову позицію
        player.transform.position = GameStateManager.Instance.spawnPosition;

        PlayerLife life = player.GetComponent<PlayerLife>();
        if (life != null)
        {
            life.CurrentLives = GameStateManager.Instance.currentLives;
        }
    }
}
