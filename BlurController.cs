using UnityEngine;

public class BlurController : MonoBehaviour
{
    public CanvasGroup blurGroup;
    public float fadeSpeed = 3f;

    private float targetAlpha = 0f;

    void Update()
    {
        blurGroup.alpha = Mathf.Lerp(blurGroup.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
    }

    public void EnableBlur(bool enable)
    {
        targetAlpha = enable ? 1f : 0f;
    }
}
