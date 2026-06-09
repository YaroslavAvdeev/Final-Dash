using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEnder : MonoBehaviour
{
    // Назва сцени, яку потрібно завантажити наступною (наприклад, "Level_2" або "WinScene")
    [SerializeField] private string nextSceneName = "WinScene"; 

    // Викликається, коли інший об'єкт з Rigidbody2D/Collider2D входить у тригер
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Перевіряємо, чи це саме наш гравець, а не ворог чи монета
        // Ми перевіряємо тег "Player", тому обов'язково встановіть його персонажу!
        if (other.CompareTag("Player"))
        {
            Debug.Log("Гравець досяг кінця рівня!");
            
            // Завантажуємо наступну сцену
            SceneManager.LoadScene(nextSceneName); 
        }
    }
}