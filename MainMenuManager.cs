using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] 
    private string gameSceneName = "LAST";

    public void StartGame()
    {
        Debug.Log("Запуск нової гри...");

        //  1. Скидаємо діалоги (старий варіант з PlayerPrefs)
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // 2. Скидаємо стан гри (кристали, життя і т.д.)
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetGame();
        }

        AutoStartDialogueReset();

        // 4. Завантажуємо рівень
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Вихід з гри...");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // 🔥 Reset для нової системи діалогів (HashSet)
    void AutoStartDialogueReset()
    {
        AutoStartDialogue.playedScenes.Clear();
    }
}
