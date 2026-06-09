using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ════════════════════════════════════════════════════════════════
public class BossHealthBarUI : MonoBehaviour
{
    [Header("── Назва ──")]
    public string bossName = "SHARK BOSS";

    [Header("── Розмір та позиція ──")]
    [Tooltip("Ширина всієї полоски")]
    public float barWidth    = 1100f;
    [Tooltip("Висота смужки HP")]
    public float barHeight   = 50f;
    [Tooltip("Відступ від верху екрану")]
    public float topOffset   = 35f;
    [Tooltip("Горизонтальне зміщення від центру")]
    public float centerShift = 0f;

    [Header("── Анімація ──")]
    public float delaySpeed = 2.5f;
    public float fadeSpeed  = 4f;
    public float pulseIntensity = 0.08f;
    public float ragePulseSpeed = 3f;

    // ── Кольори ──────────────────────────────────────────────────
    private static readonly Color C_FULL   = new Color(0.20f, 1.00f, 0.50f, 1.00f);      // Яскрава зелень
    private static readonly Color C_MID    = new Color(1.00f, 0.85f, 0.00f, 1.00f);      // Жовтий
    private static readonly Color C_LOW    = new Color(1.00f, 0.20f, 0.20f, 1.00f);      // Червоний
    private static readonly Color C_RAGE   = new Color(1.00f, 0.35f, 0.00f, 1.00f);      // Помаранчевий
    private static readonly Color C_DELAY  = new Color(1.00f, 0.55f, 0.10f, 0.5f);      // Затримана смужка (прозора)
    private static readonly Color C_BG     = new Color(0.08f, 0.08f, 0.12f, 0.6f);      // Світліший фон
    private static readonly Color C_PANEL  = new Color(0.02f, 0.02f, 0.03f, 0.85f);     // Панель
    private static readonly Color C_BORDER = new Color(0.90f, 0.70f, 0.10f, 1.00f);     // Золота рамка
    private static readonly Color C_INNER  = new Color(0.10f, 0.10f, 0.15f, 0.85f);     // Внутрішня рамка
    private static readonly Color C_NAME   = new Color(1.00f, 0.95f, 0.40f, 1.00f);     // Назва золота
    private static readonly Color C_HPTXT  = new Color(0.90f, 0.90f, 0.95f, 1.00f);     // HP текст світло-сіра
    private static readonly Color C_SHINE  = new Color(1.00f, 1.00f, 1.00f, 0.25f);     // Сяйво
    private static readonly Color C_NOTCH  = new Color(1.00f, 1.00f, 1.00f, 0.15f);     // Насічки світлі

    private CanvasGroup   cg;
    private RectTransform fillRT;
    private RectTransform delayRT;
    private RectTransform shineRT;
    private Image         fillImg;
    private Image         delayImg;
    private TMP_Text      nameLabel;
    private TMP_Text      hpLabel;
    private RectTransform barContainerRT;
    private RectTransform barBgRT;

    private int   maxHp  = 1;
    private float fill   = 1f;
    private float delayed= 1f;
    private bool  ready  = false;
    private bool  shown  = false;
    private bool  isRage = false;
    private Coroutine ragePulseCoroutine;

    // ════════════════════════════════════════════════════════════
    void Start()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(centerShift, -topOffset);
        rt.sizeDelta        = new Vector2(barWidth + 100f, 170f);

        cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        BuildUI();
    }

    void BuildUI()
    {
        // 1. Фонова панель
        var panelImg = MakeImg("Panel", transform, C_PANEL);
        Full(panelImg.GetComponent<RectTransform>());

        // 2. Назва боса (ліворуч, більший шрифт)
        nameLabel = MakeTMP("Name", transform, bossName.ToUpper(), 36f, C_NAME,
            FontStyles.Bold, TextAlignmentOptions.Left,
            0f, 1f, 0.5f, 1f, new Vector2(25f, -18f), new Vector2(500f, 55f));

        // 3. HP текст (праворуч)
        hpLabel = MakeTMP("HP", transform, "", 22f, C_HPTXT,
            FontStyles.Normal, TextAlignmentOptions.Right,
            0.5f, 1f, 1f, 1f, new Vector2(-25f, -18f), new Vector2(500f, 55f));

        // 4. Золота лінія-розділювач (тонка)
        var sepRT = MakeImg("Sep", transform, C_BORDER).GetComponent<RectTransform>();
        sepRT.anchorMin = new Vector2(0.05f, 1f);
        sepRT.anchorMax = new Vector2(0.95f, 1f);
        sepRT.pivot = new Vector2(0.5f, 1f);
        sepRT.anchoredPosition = new Vector2(0f, -58f);
        sepRT.sizeDelta = new Vector2(0f, 2.5f);

        // 5. Зовнішня золота рамка
        barContainerRT = MakeImg("OuterBorder", transform, C_BORDER).GetComponent<RectTransform>();
        barContainerRT.anchorMin = new Vector2(0.05f, 1f);
        barContainerRT.anchorMax = new Vector2(0.95f, 1f);
        barContainerRT.pivot = new Vector2(0.5f, 1f);
        barContainerRT.anchoredPosition = new Vector2(0f, -68f);
        barContainerRT.sizeDelta = new Vector2(0f, barHeight + 8f);

        // 6. Внутрішня темна рамка
        var innerBorder = MakeImg("InnerBorder", barContainerRT, C_INNER);
        Full(innerBorder.GetComponent<RectTransform>(), 2.5f);

        // 7. Фон бару (світліший)
        var bgImg = MakeImg("BG", barContainerRT, C_BG);
        barBgRT = bgImg.GetComponent<RectTransform>();
        Full(barBgRT, 3f);

        // 8. Насічки 25% / 50% / 75% (світлі)
        for (int i = 1; i <= 3; i++)
        {
            var nRT = MakeImg($"Notch{i}", barBgRT, C_NOTCH).GetComponent<RectTransform>();
            nRT.anchorMin = new Vector2(i * 0.25f, 0f);
            nRT.anchorMax = new Vector2(i * 0.25f, 1f);
            nRT.sizeDelta = new Vector2(1.5f, 0f);
            nRT.offsetMin = new Vector2(-0.75f, 0f);
            nRT.offsetMax = new Vector2(0.75f, 0f);
        }

        // 9. Delayed смужка (затримана, прозора)
        delayRT = MakeImg("Delay", barBgRT, C_DELAY).GetComponent<RectTransform>();
        delayImg = delayRT.GetComponent<Image>();
        FillLeft(delayRT);

        // 10. Основна смужка HP (яскрава)
        fillImg = MakeImg("Fill", barBgRT, C_FULL);
        fillRT = fillImg.GetComponent<RectTransform>();
        FillLeft(fillRT);

        // 11. Анімований глянець (сяйво)
        shineRT = MakeImg("Shine", barBgRT, C_SHINE).GetComponent<RectTransform>();
        FillLeft(shineRT);

        // 12. Нижня акцентна лінія
        var botRT = MakeImg("BottomLine", transform, new Color(0.70f, 0.50f, 0.10f, 0.85f)).GetComponent<RectTransform>();
        botRT.anchorMin = new Vector2(0.05f, 1f);
        botRT.anchorMax = new Vector2(0.95f, 1f);
        botRT.pivot = new Vector2(0.5f, 1f);
        botRT.anchoredPosition = new Vector2(0f, -(68f + barHeight + 14f));
        botRT.sizeDelta = new Vector2(0f, 2.5f);
    }

    // ════════════════════════════════════════════════════════════
    //  UPDATE
    // ══════════════════════════════════════════��═════════════════
    void Update()
    {
        if (!ready) return;

        // Delayed bar animation
        if (Mathf.Abs(delayed - fill) > 0.001f)
        {
            delayed = Mathf.MoveTowards(delayed, fill, delaySpeed * Time.deltaTime * 0.12f);
            Scale(delayRT, delayed);
        }

        // Pulse effect при низькому HP
        if (fill < 0.3f && !isRage)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 6f) * pulseIntensity;
            fillRT.localScale = new Vector3(fill * pulse, fillRT.localScale.y, 1f);
        }

        // Shine animation (плавний рух)
        if (shineRT != null)
        {
            float shinePos = Mathf.PingPong(Time.time * 1.5f, 1f);
            shineRT.localScale = new Vector3(shinePos * 0.8f + 0.2f, shineRT.localScale.y, 1f);
        }

        // Name wiggle при rage mode
        if (isRage && nameLabel != null)
        {
            float wiggle = Mathf.Sin(Time.time * 8f) * 2.5f;
            nameLabel.rectTransform.anchoredPosition = new Vector2(25f + wiggle, -18f);
        }
    }

    // ════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ════════════════════════════════════════════════════════════
    public void Initialize(int max)
    {
        maxHp = Mathf.Max(1, max);
        fill = delayed = 1f;
        ready = true;
        Scale(fillRT, 1f);
        Scale(delayRT, 1f);
        
        if (fillImg != null) fillImg.color = C_FULL;
        if (hpLabel != null) hpLabel.text = $"{max} / {max}";
    }

    public void ShowBar()
    {
        if (shown) return;
        shown = true;
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    public void UpdateHealth(int hp)
    {
        if (!ready) Initialize(maxHp);
        
        fill = Mathf.Clamp01((float)hp / maxHp);
        Scale(fillRT, fill);

        // Динамічний колір (градієнт)
        if (fillImg != null)
        {
            fillImg.color = isRage ? C_RAGE : EvaluateGradient(fill);
        }

        if (hpLabel != null)
            hpLabel.text = $"{hp} / {maxHp}";

        StartCoroutine(HitShake());
        StartCoroutine(HitFlash());
    }

    public void SetRageMode(bool rage)
    {
        isRage = rage;

        if (nameLabel != null)
        {
            nameLabel.text = rage ? "⚡ " + bossName.ToUpper() + " ⚡" : bossName.ToUpper();
            nameLabel.color = rage ? C_RAGE : C_NAME;
        }

        if (rage)
        {
            if (fillImg != null) fillImg.color = C_RAGE;
            if (ragePulseCoroutine != null) StopCoroutine(ragePulseCoroutine);
            ragePulseCoroutine = StartCoroutine(RagePulse());
        }
        else
        {
            if (ragePulseCoroutine != null) StopCoroutine(ragePulseCoroutine);
        }
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOut());
    }

    // ═══════════════════════════════════════════════════════���════
    //  HELPERS
    // ════════════════════════════════════════════════════════════

    /// ✨ Градієнт кольору для HP
    Color EvaluateGradient(float t)
    {
        if (t > 0.6f)
            return Color.Lerp(C_MID, C_FULL, (t - 0.6f) / 0.4f);
        else if (t > 0.3f)
            return Color.Lerp(C_LOW, C_MID, (t - 0.3f) / 0.3f);
        else
            return Color.Lerp(new Color(0.9f, 0.10f, 0.10f), C_LOW, t / 0.3f);
    }

    // ════════════════════════════════════════════════════════════
    //  COROUTINES
    // ════════════════════════════════════════════════════════════
    IEnumerator FadeIn()
    {
        yield return new WaitForSeconds(0.15f);
        while (cg.alpha < 1f)
        {
            cg.alpha = Mathf.MoveTowards(cg.alpha, 1f, Time.deltaTime * fadeSpeed);
            yield return null;
        }
        cg.alpha = 1f;
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(0.4f);
        while (cg.alpha > 0f)
        {
            cg.alpha = Mathf.MoveTowards(cg.alpha, 0f, Time.deltaTime * (fadeSpeed * 0.5f));
            yield return null;
        }
        shown = false;
    }

    IEnumerator HitShake()
    {
        if (barContainerRT == null) yield break;
        Vector2 orig = barContainerRT.anchoredPosition;
        for (int i = 0; i < 4; i++)
        {
            barContainerRT.anchoredPosition = orig + new Vector2(
                Random.Range(-3f, 3f), Random.Range(-1f, 1f));
            yield return new WaitForSeconds(0.05f);
        }
        barContainerRT.anchoredPosition = orig;
    }

    IEnumerator HitFlash()
    {
        if (fillImg == null) yield break;
        Color prev = fillImg.color;
        fillImg.color = Color.white;
        yield return new WaitForSeconds(0.06f);
        fillImg.color = prev;
    }

    /// 🔥 Rage mode пульсація
    IEnumerator RagePulse()
    {
        while (isRage && fillImg != null)
        {
            float t = Mathf.PingPong(Time.time * ragePulseSpeed, 1f);
            fillImg.color = Color.Lerp(C_RAGE, new Color(1f, 0.6f, 0.2f), t * 0.5f);
            yield return null;
        }
    }

    // ════════════════════════════════════════════════════════════
    //  UI CONSTRUCTION HELPERS
    // ════════════════════════════════════════════════════════════
    Image MakeImg(string n, Transform p, Color c)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = c;
        return img;
    }

    TMP_Text MakeTMP(string n, Transform p, string text, float size, Color c,
        FontStyles style, TextAlignmentOptions align,
        float ax0, float ay0, float ax1, float ay1,
        Vector2 pos, Vector2 delta)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = c;
        t.fontStyle = style;
        t.alignment = align;
        
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(ax0, ay0);
        rt.anchorMax = new Vector2(ax1, ay1);
        rt.pivot = new Vector2(align == TextAlignmentOptions.Right ? 1f : 0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = delta;
        return t;
    }

    void Full(RectTransform rt, float i = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(i, i);
        rt.offsetMax = new Vector2(-i, -i);
    }

    void FillLeft(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0f, 0.5f);
    }

    void Scale(RectTransform rt, float v)
    {
        if (rt == null) return;
        var s = rt.localScale;
        rt.localScale = new Vector3(Mathf.Clamp01(v), s.y, s.z);
    }
}