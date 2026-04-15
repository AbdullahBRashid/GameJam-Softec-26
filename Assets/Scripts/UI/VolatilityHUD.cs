using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-building Volatility Meter HUD.
/// Creates its own Canvas and UI elements programmatically — zero editor setup.
/// 
/// Features:
///   • Smooth animated fill bar with color gradient (green → amber → red)
///   • Glowing pulse effect when in danger zone
///   • Shake on volatility spike
///   • Bug warning indicator with active bug name
///   • Percentage text readout
///
/// Setup: Create an empty GameObject, add this component. Done.
/// </summary>
public class VolatilityHUD : MonoBehaviour
{
    [Header("Position")]
    [Tooltip("Anchor position on screen.")]
    [SerializeField] private HUDPosition hudPosition = HUDPosition.TopRight;

    [Header("Sizing")]
    [SerializeField] private float barWidth = 220f;
    [SerializeField] private float barHeight = 18f;
    [SerializeField] private float padding = 20f;

    [Header("Animation")]
    [SerializeField] private float fillSpeed = 4f;
    [SerializeField] private float shakeIntensity = 6f;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float pulseSpeed = 2.5f;

    // ── UI References (auto-created) ──
    private Canvas _canvas;
    private RectTransform _rootPanel;
    private Image _barBackground;
    private Image _barFill;
    private Image _barGlow;
    private Image _dangerOverlay;
    private Text _percentText;
    private Text _labelText;
    private Text _bugWarningText;
    private RectTransform _barFillRect;

    // ── State ──
    private float _displayedFill = 0f;
    private float _targetFill = 0f;
    private float _shakeTimer = 0f;
    private Vector2 _originalPos;
    private bool _inDangerZone = false;
    private Coroutine _pulseCoroutine;

    // ── Colors ──
    private readonly Color _colorSafe = new Color(0.18f, 0.85f, 0.45f, 1f);       // Emerald green
    private readonly Color _colorWarning = new Color(1f, 0.75f, 0.1f, 1f);        // Amber
    private readonly Color _colorDanger = new Color(0.95f, 0.22f, 0.22f, 1f);     // Red
    private readonly Color _colorCritical = new Color(1f, 0.1f, 0.1f, 1f);        // Bright red
    private readonly Color _bgColor = new Color(0.08f, 0.08f, 0.12f, 0.85f);      // Dark glass
    private readonly Color _barBgColor = new Color(0.15f, 0.15f, 0.2f, 0.9f);     // Darker inner
    private readonly Color _glowBase = new Color(1f, 0.2f, 0.2f, 0f);             // Red glow (starts invisible)
    private readonly Color _textColor = new Color(0.85f, 0.88f, 0.92f, 1f);       // Cool white
    private readonly Color _textDim = new Color(0.5f, 0.52f, 0.58f, 1f);          // Dim label

    public enum HUDPosition { TopRight, TopLeft, BottomRight, BottomLeft }

    // ═══ Lifecycle ══════════════════════════════════════════════════

    private void Start()
    {
        BuildUI();
        _originalPos = _rootPanel.anchoredPosition;
    }

    private void OnEnable()
    {
        GameEventManager.OnVolatilityChanged += OnVolatilityChanged;
        GameEventManager.OnMechanicalBugTriggered += OnBugTriggered;
        GameEventManager.OnMechanicalBugEnded += OnBugEnded;
    }

    private void OnDisable()
    {
        GameEventManager.OnVolatilityChanged -= OnVolatilityChanged;
        GameEventManager.OnMechanicalBugTriggered -= OnBugTriggered;
        GameEventManager.OnMechanicalBugEnded -= OnBugEnded;
    }

    private void Update()
    {
        // Smooth fill animation
        _displayedFill = Mathf.Lerp(_displayedFill, _targetFill, Time.deltaTime * fillSpeed);
        _barFill.fillAmount = _displayedFill;

        // Color gradient
        Color barColor = GetBarColor(_displayedFill);
        _barFill.color = barColor;

        // Percentage text
        int pct = Mathf.RoundToInt(_displayedFill * 100f);
        _percentText.text = $"{pct}%";
        _percentText.color = _displayedFill > 0.6f ? _colorDanger : _textColor;

        // Shake effect
        if (_shakeTimer > 0f)
        {
            _shakeTimer -= Time.deltaTime;
            float magnitude = shakeIntensity * (_shakeTimer / shakeDuration);
            Vector2 offset = new Vector2(
                Random.Range(-magnitude, magnitude),
                Random.Range(-magnitude, magnitude)
            );
            _rootPanel.anchoredPosition = _originalPos + offset;
        }
        else
        {
            _rootPanel.anchoredPosition = _originalPos;
        }

        // Danger zone pulse
        bool shouldPulse = _displayedFill >= 0.75f;
        if (shouldPulse && !_inDangerZone)
        {
            _inDangerZone = true;
            _pulseCoroutine = StartCoroutine(PulseGlow());
        }
        else if (!shouldPulse && _inDangerZone)
        {
            _inDangerZone = false;
            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            _barGlow.color = _glowBase;
            _dangerOverlay.color = new Color(1, 0, 0, 0);
        }
    }

    // ═══ Event Handlers ═════════════════════════════════════════════

    private void OnVolatilityChanged(float current, float delta)
    {
        if (VolatilityManager.Instance != null)
            _targetFill = VolatilityManager.Instance.NormalizedVolatility;

        // Shake on positive spikes
        if (delta > 1f)
            _shakeTimer = shakeDuration;
    }

    private void OnBugTriggered(MechanicalBugType bug)
    {
        _bugWarningText.text = $"⚠ {FormatBugName(bug)}";
        _bugWarningText.gameObject.SetActive(true);
    }

    private void OnBugEnded(MechanicalBugType bug)
    {
        _bugWarningText.gameObject.SetActive(false);
    }

    // ═══ Color Logic ════════════════════════════════════════════════

    private Color GetBarColor(float t)
    {
        if (t < 0.4f)
            return Color.Lerp(_colorSafe, _colorWarning, t / 0.4f);
        else if (t < 0.75f)
            return Color.Lerp(_colorWarning, _colorDanger, (t - 0.4f) / 0.35f);
        else
            return Color.Lerp(_colorDanger, _colorCritical, (t - 0.75f) / 0.25f);
    }

    // ═══ Pulse Coroutine ════════════════════════════════════════════

    private IEnumerator PulseGlow()
    {
        while (_inDangerZone)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

            // Glow around bar
            Color glow = _glowBase;
            glow.a = Mathf.Lerp(0.05f, 0.35f, t);
            _barGlow.color = glow;

            // Subtle red overlay on the entire panel
            _dangerOverlay.color = new Color(1f, 0f, 0f, Mathf.Lerp(0f, 0.08f, t));

            yield return null;
        }
    }

    // ═══ UI Builder ═════════════════════════════════════════════════

    private void BuildUI()
    {
        // ── Canvas ──
        GameObject canvasObj = new GameObject("VolatilityHUD_Canvas");
        canvasObj.transform.SetParent(transform);
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // ── Root Panel (glass background) ──
        _rootPanel = CreateRect("Panel", canvasObj.transform, barWidth + 40f, barHeight + 62f);
        Image panelBg = _rootPanel.gameObject.AddComponent<Image>();
        panelBg.color = _bgColor;
        // Round corners effect via sprite — we'll skip for now, use solid

        // Danger overlay (covers the panel, pulsing red when high volatility)
        RectTransform overlayRect = CreateRect("DangerOverlay", _rootPanel, 0, 0);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;
        _dangerOverlay = overlayRect.gameObject.AddComponent<Image>();
        _dangerOverlay.color = new Color(1, 0, 0, 0);
        _dangerOverlay.raycastTarget = false;

        // ── Position the panel ──
        SetAnchorPosition(_rootPanel);

        // ── Label ──
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(_rootPanel, false);
        _labelText = labelObj.AddComponent<Text>();
        _labelText.text = "VOLATILITY";
        _labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _labelText.fontSize = 15;
        _labelText.fontStyle = FontStyle.Bold;
        _labelText.color = _textDim;
        _labelText.alignment = TextAnchor.UpperLeft;
        _labelText.raycastTarget = false;
        RectTransform labelRect = _labelText.rectTransform;
        labelRect.anchorMin = new Vector2(0, 1);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.pivot = new Vector2(0, 1);
        labelRect.anchoredPosition = new Vector2(10, -8);
        labelRect.sizeDelta = new Vector2(-20, 16);

        // ── Percentage Text ──
        GameObject pctObj = new GameObject("Percent");
        pctObj.transform.SetParent(_rootPanel, false);
        _percentText = pctObj.AddComponent<Text>();
        _percentText.text = "0%";
        _percentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _percentText.fontSize = 17;
        _percentText.fontStyle = FontStyle.Bold;
        _percentText.color = _textColor;
        _percentText.alignment = TextAnchor.UpperRight;
        _percentText.raycastTarget = false;
        RectTransform pctRect = _percentText.rectTransform;
        pctRect.anchorMin = new Vector2(0, 1);
        pctRect.anchorMax = new Vector2(1, 1);
        pctRect.pivot = new Vector2(1, 1);
        pctRect.anchoredPosition = new Vector2(-10, -6);
        pctRect.sizeDelta = new Vector2(-20, 18);

        // ── Bar Background ──
        RectTransform barBgRect = CreateRect("BarBg", _rootPanel, barWidth, barHeight);
        barBgRect.anchorMin = new Vector2(0.5f, 1);
        barBgRect.anchorMax = new Vector2(0.5f, 1);
        barBgRect.pivot = new Vector2(0.5f, 1);
        barBgRect.anchoredPosition = new Vector2(0, -28);
        _barBackground = barBgRect.gameObject.AddComponent<Image>();
        _barBackground.color = _barBgColor;
        _barBackground.raycastTarget = false;

        // ── Bar Glow (behind fill, for danger pulse) ──
        RectTransform glowRect = CreateRect("BarGlow", barBgRect, barWidth + 8, barHeight + 8);
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.anchoredPosition = Vector2.zero;
        _barGlow = glowRect.gameObject.AddComponent<Image>();
        _barGlow.color = _glowBase;
        _barGlow.raycastTarget = false;

        // ── Bar Fill ──
        RectTransform fillRect = CreateRect("BarFill", barBgRect, barWidth - 4, barHeight - 4);
        fillRect.anchorMin = new Vector2(0, 0.5f);
        fillRect.anchorMax = new Vector2(0, 0.5f);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.anchoredPosition = new Vector2(2, 0);
        _barFill = fillRect.gameObject.AddComponent<Image>();
        _barFill.color = _colorSafe;
        _barFill.type = Image.Type.Filled;
        _barFill.fillMethod = Image.FillMethod.Horizontal;
        _barFill.fillOrigin = 0;
        _barFill.fillAmount = 0f;
        _barFill.raycastTarget = false;
        _barFillRect = fillRect;

        // ── Tick marks on the bar for thresholds ──
        CreateTickMark(barBgRect, 0.75f); // High threshold marker

        // ── Bug Warning Text ──
        GameObject warnObj = new GameObject("BugWarning");
        warnObj.transform.SetParent(_rootPanel, false);
        _bugWarningText = warnObj.AddComponent<Text>();
        _bugWarningText.text = "";
        _bugWarningText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _bugWarningText.fontSize = 14;
        _bugWarningText.fontStyle = FontStyle.Bold;
        _bugWarningText.color = _colorDanger;
        _bugWarningText.alignment = TextAnchor.LowerCenter;
        _bugWarningText.raycastTarget = false;
        RectTransform warnRect = _bugWarningText.rectTransform;
        warnRect.anchorMin = new Vector2(0, 0);
        warnRect.anchorMax = new Vector2(1, 0);
        warnRect.pivot = new Vector2(0.5f, 0);
        warnRect.anchoredPosition = new Vector2(0, 4);
        warnRect.sizeDelta = new Vector2(-20, 16);
        warnObj.SetActive(false);
    }

    private void CreateTickMark(RectTransform parent, float normalizedPos)
    {
        RectTransform tick = CreateRect("Tick", parent, 2, barHeight - 2);
        tick.anchorMin = new Vector2(normalizedPos, 0.5f);
        tick.anchorMax = new Vector2(normalizedPos, 0.5f);
        tick.anchoredPosition = Vector2.zero;
        Image tickImg = tick.gameObject.AddComponent<Image>();
        tickImg.color = new Color(1f, 1f, 1f, 0.2f);
        tickImg.raycastTarget = false;
    }

    private RectTransform CreateRect(string name, Transform parent, float w, float h)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    private void SetAnchorPosition(RectTransform panel)
    {
        switch (hudPosition)
        {
            case HUDPosition.TopRight:
                panel.anchorMin = new Vector2(1, 1);
                panel.anchorMax = new Vector2(1, 1);
                panel.pivot = new Vector2(1, 1);
                panel.anchoredPosition = new Vector2(-padding, -padding);
                break;
            case HUDPosition.TopLeft:
                panel.anchorMin = new Vector2(0, 1);
                panel.anchorMax = new Vector2(0, 1);
                panel.pivot = new Vector2(0, 1);
                panel.anchoredPosition = new Vector2(padding, -padding);
                break;
            case HUDPosition.BottomRight:
                panel.anchorMin = new Vector2(1, 0);
                panel.anchorMax = new Vector2(1, 0);
                panel.pivot = new Vector2(1, 0);
                panel.anchoredPosition = new Vector2(-padding, padding);
                break;
            case HUDPosition.BottomLeft:
                panel.anchorMin = new Vector2(0, 0);
                panel.anchorMax = new Vector2(0, 0);
                panel.pivot = new Vector2(0, 0);
                panel.anchoredPosition = new Vector2(padding, padding);
                break;
        }
    }

    private string FormatBugName(MechanicalBugType bug)
    {
        return bug switch
        {
            MechanicalBugType.InvertedControls => "CONTROLS INVERTED",
            MechanicalBugType.ReverseGravity => "GRAVITY REVERSED",
            MechanicalBugType.CameraShake => "CAMERA UNSTABLE",
            MechanicalBugType.InputLag => "INPUT DELAY",
            MechanicalBugType.MapDecay => "MAP DECAYING",
            MechanicalBugType.GravityToggleOnJump => "GRAVITY GLITCH",
            _ => bug.ToString().ToUpper()
        };
    }
}
