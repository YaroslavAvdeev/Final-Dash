using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    // Сюди ми перетягнемо нашу панель з Юніті
    public GameObject pauseMenuPanel; 
    private bool isPaused = false;

    void Start()
    {
        // При запуску гри меню має бути приховане
        pauseMenuPanel.SetActive(false);
    }

    void Update()
    {
        // Перевіряємо натискання Esc
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        pauseMenuPanel.SetActive(true); // Показуємо меню
        Time.timeScale = 0f;           // Зупиняємо час
        isPaused = true;
    }

  public void ResumeGame()
{
    Debug.Log("Кнопка Resume натиснута!"); //щоб бачити в консолі, чи йде сигнал
    pauseMenuPanel.SetActive(false);
    Time.timeScale = 1f; // Цей рядок "оживляє" гру
    isPaused = false;
}

    public void GoToMenu()
    {
        Time.timeScale = 1f; // Важливо повернути час перед виходом!
        SceneManager.LoadScene(1); // Завантажує першу сцену в списку (зазвичай головне меню)
    }

public void Quit()
{
    Debug.Log("Кнопка Вихід натиснута!");
    // Application.Quit() НЕ ПРАЦЮЄ всередині редактора Unity. 
    // Щоб побачити результат, ми додаємо UnityEditor.EditorApplication.isPlaying:
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
}

}
