using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    [Header("Ціль")]
    public Transform target;

    [Header("Плавність")]
    public float smoothX = 0.12f;
    public float smoothY = 0.12f;

    [Header("Зміщення")]
    public float offsetY = 0f;
    public float offsetZ = -10f;

    [Header("Lookahead")]
    public float lookaheadDistance   = 2f;
    public float lookaheadSmoothness = 4f;
    public float lookaheadDeadzone   = 0.4f;
    public float lookaheadReturnSpeed = 1.5f;

    [Header("Boss Zone")]
    public bool  bossZoneActive = false;
    public float bossOrthoSize  = 7f;
    public float bossZoomSpeed  = 1.5f;

    [Header("Обмеження по Y")]
    public bool  useBoundsY = false;
    public float minY = -5f;
    public float maxY = 30f;

    private bool        canFollow        = true;
    private Vector3     shakeOffset      = Vector3.zero;
    private float       currentLookahead = 0f;
    private Vector3     currentVelocity  = Vector3.zero;
    private Camera      cam;
    private float       defaultOrthSize;
    private Rigidbody2D targetRb;

    public static CameraFollow Instance;

    private void Awake()
    {
        Instance = this;
        cam      = GetComponent<Camera>();
        if (cam != null) defaultOrthSize = cam.orthographicSize;
        if (target != null)
            targetRb = target.GetComponent<Rigidbody2D>();
    }

    private void OnEnable()  => AutoStartDialogue.OnDialogueStateChange += SetFollowState;
    private void OnDisable() => AutoStartDialogue.OnDialogueStateChange -= SetFollowState;

    private void LateUpdate()
    {
        if (!canFollow || target == null) return;

        float playerVelX = targetRb != null ? targetRb.linearVelocity.x : 0f;

        // --- Lookahead ---
        if (Mathf.Abs(playerVelX) > lookaheadDeadzone)
        {
            float wanted = Mathf.Sign(playerVelX) * lookaheadDistance;
            currentLookahead = Mathf.Lerp(
                currentLookahead, wanted,
                Time.deltaTime * lookaheadSmoothness);
        }
        else
        {
            currentLookahead = Mathf.Lerp(
                currentLookahead, 0f,
                Time.deltaTime * lookaheadReturnSpeed);
        }

        // --- Бажана позиція ---
        Vector3 desired = new Vector3(
            target.position.x + currentLookahead,
            target.position.y + offsetY,
            offsetZ
        );

        if (useBoundsY)
            desired.y = Mathf.Clamp(desired.y, minY, maxY);

        // --- Плавний рух ---
        float newX = Mathf.SmoothDamp(
            transform.position.x, desired.x, ref currentVelocity.x, smoothX);
        float newY = Mathf.SmoothDamp(
            transform.position.y, desired.y, ref currentVelocity.y, smoothY);

        transform.position = new Vector3(newX, newY, offsetZ) + shakeOffset;

        // --- Boss Zone ---
        if (cam != null)
        {
            float targetSize = bossZoneActive ? bossOrthoSize : defaultOrthSize;
            cam.orthographicSize = Mathf.Lerp(
                cam.orthographicSize, targetSize,
                Time.deltaTime * bossZoomSpeed);
        }
    }

    public void SetBossZone(bool active) => bossZoneActive = active;

    private void SetFollowState(bool dialogueActive) => canFollow = !dialogueActive;

    public void Shake(float duration, float magnitude)
        => StartCoroutine(ShakeRoutine(duration, magnitude));

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = 1f - (elapsed / duration);
            shakeOffset = new Vector3(
                Random.Range(-1f, 1f) * magnitude * t,
                Random.Range(-1f, 1f) * magnitude * t,
                0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        shakeOffset = Vector3.zero;
    }
}