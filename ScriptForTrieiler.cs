using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class TrailerToMenu : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string menuSceneName = "MainMenu";

    void Start()
    {
        // Підписка на подію завершення відео
        videoPlayer.loopPointReached += OnVideoEnd;
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        LoadMenu();
    }

    /*void Update()
    {
        // Пропуск трейлера по натисканню
        if (Input.anyKeyDown)
        {
            LoadMenu();
        }
    }*/

    void LoadMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }
}