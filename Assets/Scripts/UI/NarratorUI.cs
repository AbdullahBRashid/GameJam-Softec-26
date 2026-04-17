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
    private CanvasGroup _canvasGroup;
    private RectTransform _panel;
    private Text _narratorText;
    private Text _prefixText;
    private Image _panelBg;

    // ── State ──
    private Coroutine _activeCoroutine;
    private bool _isShowing = false;

    // ── Colors ──
    private readonly Color _bgColor = new Color(0.04f, 0.04f, 0.08f, 0.88f);
    private readonly Color _textColor = new Color(0.82f, 0.85f, 0.9f, 1f);
    private readonly Color _prefixColor = new Color(0.4f, 0.7f, 1f, 0.7f);
    private readonly Color _hintColor = new Color(0.6f, 0.65f, 0.72f, 1f);

    private void Start()
    {
        BuildUI();
        _canvasGroup.alpha = 0f;

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
        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);

        _activeCoroutine = StartCoroutine(TypewriterSequence(message, duration, false));
    }

    // ═══ Start Hint ═════════════════════════════════════════════════

    private IEnumerator ShowStartHint()
    {
        yield return new WaitForSeconds(hintDelay);

        // Show "Press E to Interact" as a subtle hint
        _prefixText.text = "";
        _prefixText.gameObject.SetActive(false);
        _narratorText.color = _hintColor;
        _narratorText.fontStyle = FontStyle.Italic;
        _narratorText.fontSize = Mathf.RoundToInt(16 * (baseFontSize / 18f));

        _activeCoroutine = StartCoroutine(TypewriterSequence(
            "Look at objects and press  E  to interact with them.", hintDuration, true));
    }

    // ═══ Typewriter ═════════════════════════════════════════════════

    private IEnumerator TypewriterSequence(string message, float duration, bool isHint)
    {
        _isShowing = true;

        // Reset style for narrator messages
        if (!isHint)
        {
            _prefixText.gameObject.SetActive(true);
            _prefixText.text = "AI >";
            _narratorText.color = _textColor;
            _narratorText.fontStyle = FontStyle.Normal;
            _narratorText.fontSize = baseFontSize;
        }

        // Fade in
        _narratorText.text = "";
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        _canvasGroup.alpha = 1f;

        // Typewriter effect
        float charInterval = 1f / charsPerSecond;
        for (int i = 0; i < message.Length; i++)
        {
            _narratorText.text = message.Substring(0, i + 1);
            yield return new WaitForSeconds(charInterval);
        }

        // Hold for the specified duration
        yield return new WaitForSeconds(duration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        _canvasGroup.alpha = 0f;
        _isShowing = false;
        _activeCoroutine = null;
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

        // ── Panel (bottom bar) ──
        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        _panel = panelObj.AddComponent<RectTransform>();
        _panel.anchorMin = new Vector2(0.15f, 0);
        _panel.anchorMax = new Vector2(0.85f, 0);
        _panel.pivot = new Vector2(0.5f, 0);
        _panel.anchoredPosition = new Vector2(0, 40 * scale);
        _panel.sizeDelta = new Vector2(0, 80 * scale);

        _panelBg = panelObj.AddComponent<Image>();
        _panelBg.color = _bgColor;
        _panelBg.raycastTarget = false;

        _canvasGroup = panelObj.AddComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        // ── Prefix ("AI >") ──
        GameObject prefixObj = new GameObject("Prefix");
        prefixObj.transform.SetParent(panelObj.transform, false);
        _prefixText = prefixObj.AddComponent<Text>();
        _prefixText.text = "AI >";
        _prefixText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _prefixText.fontSize = Mathf.RoundToInt(14 * scale);
        _prefixText.fontStyle = FontStyle.Bold;
        _prefixText.color = _prefixColor;
        _prefixText.alignment = TextAnchor.MiddleLeft;
        _prefixText.raycastTarget = false;
        RectTransform prefRT = _prefixText.rectTransform;
        prefRT.anchorMin = new Vector2(0, 0);
        prefRT.anchorMax = new Vector2(0, 1);
        prefRT.pivot = new Vector2(0, 0.5f);
        prefRT.anchoredPosition = new Vector2(20 * scale, 0);
        prefRT.sizeDelta = new Vector2(50 * scale, 0);

        // ── Message Text ──
        GameObject textObj = new GameObject("Message");
        textObj.transform.SetParent(panelObj.transform, false);
        _narratorText = textObj.AddComponent<Text>();
        _narratorText.text = "";
        _narratorText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _narratorText.fontSize = baseFontSize;
        _narratorText.color = _textColor;
        _narratorText.alignment = TextAnchor.MiddleLeft;
        _narratorText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _narratorText.verticalOverflow = VerticalWrapMode.Overflow;
        _narratorText.raycastTarget = false;
        RectTransform textRT = _narratorText.rectTransform;
        textRT.anchorMin = new Vector2(0, 0);
        textRT.anchorMax = new Vector2(1, 1);
        textRT.pivot = new Vector2(0, 0.5f);
        textRT.anchoredPosition = new Vector2(75 * scale, 0);
        textRT.sizeDelta = new Vector2(-100 * scale, -20 * scale);

        // ── Accent line (top edge of panel) ──
        GameObject lineObj = new GameObject("AccentLine");
        lineObj.transform.SetParent(panelObj.transform, false);
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
