using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-building Narrator UI with typewriter effect.
/// Listens to GameEventManager.OnNarratorSpeak and displays text
/// in a cinematic bottom-bar style.
///
/// Also shows a one-time "Press E to Interact" hint at game start.
/// 
/// Setup: Create an empty GameObject, add this component. Done.
/// </summary>
public class NarratorUI : MonoBehaviour
{
    [Header("Typewriter")]
    [SerializeField] private float charsPerSecond = 35f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Start Hint")]
    [Tooltip("Show 'Press E to Interact' hint at game start.")]
    [SerializeField] private bool showStartHint = true;
    [SerializeField] private float hintDelay = 2f;
    [SerializeField] private float hintDuration = 5f;

    [Header("UI Scaling")]
    [Tooltip("Base font size for the narrator text. The rest of the panel scales automatically based on this.")]
    [SerializeField] private int baseFontSize = 24;

    // ── UI References ──
    private Canvas _canvas;
    private GameObject _messagePrefab;
    private Transform _messageContainer;

    // ── Colors ──
    private readonly Color _bgColor = new Color(0.04f, 0.04f, 0.08f, 0.88f);
    private readonly Color _textColor = new Color(0.82f, 0.85f, 0.9f, 1f);
    private readonly Color _prefixColor = new Color(0.4f, 0.7f, 1f, 0.7f);
    private readonly Color _hintColor = new Color(0.6f, 0.65f, 0.72f, 1f);

    private void Start()
    {
        BuildUI();

        if (showStartHint)
        {
            StartCoroutine(ShowStartHint());
        }
    }

    private void OnEnable()
    {
        GameEventManager.OnNarratorSpeak += OnNarratorSpeak;
    }

    private void OnDisable()
    {
        GameEventManager.OnNarratorSpeak -= OnNarratorSpeak;
    }

    // ═══ Event Handler ══════════════════════════════════════════════

    private void OnNarratorSpeak(string message, float duration)
    {
        SpawnAndType(message, duration, false);
    }

    // ═══ Start Hint ═════════════════════════════════════════════════

    private IEnumerator ShowStartHint()
    {
        yield return new WaitForSeconds(hintDelay);
        SpawnAndType("Look at objects and press  E  to interact with them.", hintDuration, true);
    }

    // ═══ Typewriter ═════════════════════════════════════════════════

    private void SpawnAndType(string message, float duration, bool isHint)
    {
        GameObject clone = Instantiate(_messagePrefab, _messageContainer);
        clone.SetActive(true);
        StartCoroutine(TypewriterSequence(clone, message, duration, isHint));
    }

    private IEnumerator TypewriterSequence(GameObject panel, string message, float duration, bool isHint)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        Text prefixText = panel.transform.Find("Prefix").GetComponent<Text>();
        Text narratorText = panel.transform.Find("Message").GetComponent<Text>();

        // Style
        if (isHint)
        {
            prefixText.gameObject.SetActive(false);
            narratorText.color = _hintColor;
            narratorText.fontStyle = FontStyle.Italic;
            narratorText.fontSize = Mathf.RoundToInt(16 * (baseFontSize / 18f));
            
            // Adjust message text bounds to fill the prefix area too!
            RectTransform textRT = narratorText.rectTransform;
            textRT.anchoredPosition = new Vector2(20 * (baseFontSize / 18f), 0);
            textRT.sizeDelta = new Vector2(-40 * (baseFontSize / 18f), -20 * (baseFontSize / 18f));
        }

        // Fade in
        narratorText.text = "";
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        cg.alpha = 1f;

        // Typewriter effect
        float charInterval = 1f / charsPerSecond;
        for (int i = 0; i < message.Length; i++)
        {
            narratorText.text = message.Substring(0, i + 1);
            yield return new WaitForSeconds(charInterval);
        }

        // Hold for the specified duration
        yield return new WaitForSeconds(duration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        // Clean up
        Destroy(panel);
    }

    // ═══ UI Builder ═════════════════════════════════════════════════

    private void BuildUI()
    {
        float scale = baseFontSize / 18f;

        // ── Canvas ──
        GameObject canvasObj = new GameObject("NarratorUI_Canvas");
        canvasObj.transform.SetParent(transform);
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 110;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // ── Flexible Vertical Container ──
        GameObject containerObj = new GameObject("MessageStackContainer");
        containerObj.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRT = containerObj.AddComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0.15f, 0);
        containerRT.anchorMax = new Vector2(0.85f, 1);
        containerRT.pivot = new Vector2(0.5f, 0);
        containerRT.anchoredPosition = new Vector2(0, 40 * scale);
        containerRT.sizeDelta = new Vector2(0, -80 * scale);

        var vlg = containerObj.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.LowerCenter;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 10 * scale;

        _messageContainer = containerObj.transform;

        // ── Message Box Prefab ──
        _messagePrefab = new GameObject("NarratorLine_Prefab");
        _messagePrefab.SetActive(false); // Hide the template
        _messagePrefab.transform.SetParent(transform, false); 
        
        // Let LayoutGroup control height rigidly
        var le = _messagePrefab.AddComponent<LayoutElement>();
        le.minHeight = 80 * scale;

        Image panelBg = _messagePrefab.AddComponent<Image>();
        panelBg.color = _bgColor;
        panelBg.raycastTarget = false;

        CanvasGroup cg = _messagePrefab.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;
        cg.alpha = 0f;

        // ── Prefix ("AI >") ──
        GameObject prefixObj = new GameObject("Prefix");
        prefixObj.transform.SetParent(_messagePrefab.transform, false);
        Text prefixText = prefixObj.AddComponent<Text>();
        prefixText.text = "AI >";
        prefixText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        prefixText.fontSize = Mathf.RoundToInt(14 * scale);
        prefixText.fontStyle = FontStyle.Bold;
        prefixText.color = _prefixColor;
        prefixText.alignment = TextAnchor.MiddleLeft;
        prefixText.raycastTarget = false;
        RectTransform prefRT = prefixText.rectTransform;
        prefRT.anchorMin = new Vector2(0, 0);
        prefRT.anchorMax = new Vector2(0, 1);
        prefRT.pivot = new Vector2(0, 0.5f);
        prefRT.anchoredPosition = new Vector2(20 * scale, 0);
        prefRT.sizeDelta = new Vector2(50 * scale, 0);

        // ── Message Text ──
        GameObject textObj = new GameObject("Message");
        textObj.transform.SetParent(_messagePrefab.transform, false);
        Text narratorText = textObj.AddComponent<Text>();
        narratorText.text = "";
        narratorText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        narratorText.fontSize = baseFontSize;
        narratorText.color = _textColor;
        narratorText.alignment = TextAnchor.MiddleLeft;
        narratorText.horizontalOverflow = HorizontalWrapMode.Wrap;
        narratorText.verticalOverflow = VerticalWrapMode.Overflow;
        narratorText.raycastTarget = false;
        RectTransform textRT = narratorText.rectTransform;
        textRT.anchorMin = new Vector2(0, 0);
        textRT.anchorMax = new Vector2(1, 1);
        textRT.pivot = new Vector2(0, 0.5f);
        textRT.anchoredPosition = new Vector2(75 * scale, 0);
        textRT.sizeDelta = new Vector2(-100 * scale, -20 * scale);

        // ── Accent line ──
        GameObject lineObj = new GameObject("AccentLine");
        lineObj.transform.SetParent(_messagePrefab.transform, false);
        RectTransform lineRT = lineObj.AddComponent<RectTransform>();
        lineRT.anchorMin = new Vector2(0, 1);
        lineRT.anchorMax = new Vector2(1, 1);
        lineRT.pivot = new Vector2(0.5f, 1);
        lineRT.anchoredPosition = Vector2.zero;
        lineRT.sizeDelta = new Vector2(0, Mathf.Max(2f, 2f * scale));
        Image lineImg = lineObj.AddComponent<Image>();
        lineImg.color = new Color(0.3f, 0.6f, 1f, 0.4f);
        lineImg.raycastTarget = false;
    }
}
