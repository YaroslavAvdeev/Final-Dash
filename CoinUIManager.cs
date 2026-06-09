using UnityEngine;
using TMPro;

public class CoinUIManager : MonoBehaviour
{
    public static CoinUIManager Instance;
    public TMP_Text coinText;

    [Header("Анімація при зміні")]
    public float punchScale = 1.3f;
    public float punchDuration = 0.15f;
    private Vector3 originalScale;
    private Coroutine punchCoroutine;

    private void Awake()
    {
        // Завжди перепризначаємо Instance на поточний об'єкт цієї сцени
        Instance = this;

        if (coinText != null)
            originalScale = coinText.transform.localScale;
    }

    private void OnEnable()
    {
        // OnEnable викликається після Awake — GameStateManager вже точно є
        Instance = this;
        if (coinText != null && GameStateManager.Instance != null)
            coinText.text = GameStateManager.Instance.totalCoins.ToString();
    }

    public void UpdateCoinDisplay(int value)
    {
        if (coinText == null) return;
        coinText.text = value.ToString();

        if (punchCoroutine != null) StopCoroutine(punchCoroutine);
        punchCoroutine = StartCoroutine(PunchAnim());
    }

    private System.Collections.IEnumerator PunchAnim()
    {
        if (coinText == null) yield break;
        coinText.transform.localScale = originalScale * punchScale;
        float t = 0f;
        while (t < punchDuration)
        {
            t += Time.deltaTime;
            coinText.transform.localScale = Vector3.Lerp(
                originalScale * punchScale, originalScale, t / punchDuration);
            yield return null;
        }
        coinText.transform.localScale = originalScale;
    }
}