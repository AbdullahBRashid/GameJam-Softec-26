using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-building Volatility Meter UI.
/// Listens to GameEventManager to update its visuals smoothly.
/// 
/// Setup: Create an empty GameObject, add this component. Done.
/// </summary>
public class VolatilityHUD : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color safeColor = new Color(0.18f, 0.85f, 0.45f);
    [SerializeField] private Color warningColor = new Color(1f, 0.75f, 0.1f);
    [SerializeField] private Color dangerColor = new Color(0.95f, 0.22f, 0.22f);

    [Header("Animation")]
    [Tooltip("Speed the bar catches up to the target value.")]
    [SerializeField] private float fillSpeed = 5f;

    private float _targetFill = 0f;
    private float _currentFill = 0f;

    // UI Elements
    private RectTransform _fillBarRT;
    private Image _fillBarImage;
    private Text _percentageText;
    private Text _bugWarningText;

    private void Awake()
    {
        BuildUI();
        if (_bugWarningText != null)
            _bugWarningText.gameObject.SetActive(false);
    }

    private void Start()
    {
        // Try to pull initial state correctly if the manager spawned before us
        if (VolatilityManager.Instance != null)
        {
            _targetFill = VolatilityManager.Instance.NormalizedVolatility;
            _currentFill = _targetFill;
        }
    }

    private void OnEnable()
    {
        GameEventManager.OnVolatilityChanged += HandleVolatilityChanged;
        GameEventManager.OnMechanicalBugTriggered += HandleBugTriggered;
        GameEventManager.OnMechanicalBugEnded += HandleBugEnded;
    }

    private void OnDisable()
    {
        GameEventManager.OnVolatilityChanged -= HandleVolatilityChanged;
        GameEventManager.OnMechanicalBugTriggered -= HandleBugTriggered;
        GameEventManager.OnMechanicalBugEnded -= HandleBugEnded;
    }

    private void Update()
    {
        // Smoothly animate the fill amount
        _currentFill = Mathf.Lerp(_currentFill, _targetFill, Time.deltaTime * fillSpeed);

        if (_fillBarRT != null && _fillBarImage != null)
        {
            // Instead of using Image.Type.Filled which requires a sprite, 
            // we animate the anchor point for a perfectly scalable filled bar!
            _fillBarRT.anchorMax = new Vector2(Mathf.Clamp01(_currentFill), 1f);
            
            _fillBarImage.color = GetColorForFill(_currentFill);
        }

        if (_percentageText != null)
        {
            _percentageText.text = $"{Mathf.RoundToInt(_currentFill * 100)}%";
        }
    }

    private void HandleVolatilityChanged(float current, float delta)
    {
        if (VolatilityManager.Instance != null)
        {
            _targetFill = VolatilityManager.Instance.NormalizedVolatility;
        }
    }

    private void HandleBugTriggered(MechanicalBugType bug)
    {
        if (_bugWarningText != null)
        {
            _bugWarningText.text = $"⚠ {FormatBugName(bug)}";
            _bugWarningText.gameObject.SetActive(true);
        }
    }

    private void HandleBugEnded(MechanicalBugType bug)
    {
        if (_bugWarningText != null)
        {
            _bugWarningText.gameObject.SetActive(false);
        }
    }

    private Color GetColorForFill(float fill)
    {
        if (fill < 0.5f)
            return Color.Lerp(safeColor, warningColor, fill / 0.5f);
        else
            return Color.Lerp(warningColor, dangerColor, (fill - 0.5f) / 0.5f);
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

    // ═══ UI Builder ═════════════════════════════════════════════════

    private void BuildUI()
    {
        // ── Canvas ──
        GameObject canvasObj = new GameObject("VolatilityHUD_Canvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95; // Keeps it right below the Inventory
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // ── Root Bar Panel (Top Center) ──
        GameObject panelObj = new GameObject("VolBarContainer");
        panelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rootPanel = panelObj.AddComponent<RectTransform>();
        rootPanel.anchorMin = new Vector2(0.5f, 1f);
        rootPanel.anchorMax = new Vector2(0.5f, 1f);
        rootPanel.pivot = new Vector2(0.5f, 1f);
        rootPanel.anchoredPosition = new Vector2(0, -30);
        rootPanel.sizeDelta = new Vector2(400, 24);
        Image bgImg = panelObj.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.12f, 0.15f, 0.9f);

        // ── Fill Bar Segment ──
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(panelObj.transform, false);
        _fillBarRT = fillObj.AddComponent<RectTransform>();
        _fillBarRT.anchorMin = new Vector2(0f, 0f); // Pin to left edge
        _fillBarRT.anchorMax = new Vector2(0f, 1f); // Anchors dictate width!
        _fillBarRT.pivot = new Vector2(0f, 0.5f);
        _fillBarRT.anchoredPosition = Vector2.zero;
        _fillBarRT.sizeDelta = Vector2.zero; // Stretches flush inside anchors
        _fillBarImage = fillObj.AddComponent<Image>();
        _fillBarImage.color = safeColor;

        // ── Name Label "VOLATILITY" ──
        GameObject nameObj = new GameObject("LabelText");
        nameObj.transform.SetParent(panelObj.transform, false);
        Text nameText = nameObj.AddComponent<Text>();
        nameText.text = "VOLATILITY";
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 14;
        nameText.fontStyle = FontStyle.Bold;
        nameText.color = new Color(0.8f, 0.8f, 0.85f, 1f);
        nameText.alignment = TextAnchor.MiddleLeft;
        RectTransform nameRT = nameText.rectTransform;
        nameRT.anchorMin = new Vector2(0, 1);
        nameRT.anchorMax = new Vector2(0, 1);
        nameRT.pivot = new Vector2(0, 1);
        nameRT.anchoredPosition = new Vector2(0, 24);
        nameRT.sizeDelta = new Vector2(200, 20);

        // ── Percentage Text ──
        GameObject percObj = new GameObject("PercText");
        percObj.transform.SetParent(panelObj.transform, false);
        _percentageText = percObj.AddComponent<Text>();
        _percentageText.text = "0%";
        _percentageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _percentageText.fontSize = 14;
        _percentageText.fontStyle = FontStyle.Bold;
        _percentageText.color = Color.white;
        _percentageText.alignment = TextAnchor.MiddleRight;
        RectTransform percRT = _percentageText.rectTransform;
        percRT.anchorMin = new Vector2(1, 1);
        percRT.anchorMax = new Vector2(1, 1);
        percRT.pivot = new Vector2(1, 1);
        percRT.anchoredPosition = new Vector2(0, 24);
        percRT.sizeDelta = new Vector2(100, 20);

        // ── Bug Warning Text (Red flashing text under the bar) ──
        GameObject warnObj = new GameObject("BugWarningText");
        warnObj.transform.SetParent(panelObj.transform, false);
        _bugWarningText = warnObj.AddComponent<Text>();
        _bugWarningText.text = "⚠ WARNING";
        _bugWarningText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _bugWarningText.fontSize = 16;
        _bugWarningText.fontStyle = FontStyle.Bold;
        _bugWarningText.color = dangerColor;
        _bugWarningText.alignment = TextAnchor.MiddleCenter;
        RectTransform warnRT = _bugWarningText.rectTransform;
        warnRT.anchorMin = new Vector2(0.5f, 0f);
        warnRT.anchorMax = new Vector2(0.5f, 0f);
        warnRT.pivot = new Vector2(0.5f, 1f);
        warnRT.anchoredPosition = new Vector2(0, -6);
        warnRT.sizeDelta = new Vector2(300, 24);
    }
}
