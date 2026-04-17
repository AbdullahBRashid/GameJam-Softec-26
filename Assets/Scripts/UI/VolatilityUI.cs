using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A self-building UI meter for the Volatility System.
/// Dynamically hooks into the GameEventManager to display a smooth sliding gauge and text.
/// </summary>
public class VolatilityUI : MonoBehaviour
{
    public enum ScreenPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    [Header("Position & Layout")]
    [Tooltip("Where on the screen should the meter dock?")]
    [SerializeField] private ScreenPosition anchorPosition = ScreenPosition.TopRight;
    
    [Tooltip("Nudge the meter from its anchor point (X, Y).")]
    [SerializeField] private Vector2 positionOffset = new Vector2(-30f, -30f);

    [Header("UI Scaling")]
    [Tooltip("Base font size. The entire meter scales proportionally with this value just like the NarratorUI.")]
    [SerializeField] private int baseFontSize = 24;

    [Header("Colors & Style")]
    [SerializeField] private Color stableColor = new Color(0.1f, 0.8f, 0.9f, 1f);
    [SerializeField] private Color criticalColor = new Color(0.9f, 0.1f, 0.1f, 1f);
    [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.9f);

    // ── Internal References ──
    private Canvas _canvas;
    private RectTransform _container;
    private Slider _slider;
    private Text _valueText;
    private Image _fillImage;
    private Image _sliderBgImage;

    private float _targetVolatility = 0f;
    private float _currentVolatility = 0f;
    private float _maxVolatility = 100f;

    private void Start()
    {
        BuildUI();

        // Try to get initial value if VolatilityManager already woke up
        if (VolatilityManager.Instance != null)
        {
            _targetVolatility = VolatilityManager.Instance.Volatility;
            _currentVolatility = _targetVolatility;
            _maxVolatility = VolatilityManager.Instance.MaxVolatility;
        }

        UpdateVisuals();
    }

    private void OnEnable()
    {
        GameEventManager.OnVolatilityChanged += HandleVolatilityChanged;
    }

    private void OnDisable()
    {
        GameEventManager.OnVolatilityChanged -= HandleVolatilityChanged;
    }

    private void Update()
    {
        // Smoothly interpolate the slider instead of snapping purely for aesthetic feel
        if (Mathf.Abs(_currentVolatility - _targetVolatility) > 0.01f)
        {
            _currentVolatility = Mathf.Lerp(_currentVolatility, _targetVolatility, Time.deltaTime * 5f);
            UpdateVisuals();
        }
    }

    private void HandleVolatilityChanged(float current, float delta)
    {
        _targetVolatility = current;
        if (VolatilityManager.Instance != null)
        {
            _maxVolatility = VolatilityManager.Instance.MaxVolatility;
        }
    }

    private void UpdateVisuals()
    {
        if (_slider == null || _valueText == null || _fillImage == null) return;

        float ratio = _maxVolatility > 0 ? _currentVolatility / _maxVolatility : 0;
        _slider.value = ratio;

        // Change color from cool blue to aggressive red as it nears 100%
        _fillImage.color = Color.Lerp(stableColor, criticalColor, ratio);
        
        // Glow/pulse text violently if high
        _valueText.text = $"VOLATILITY: {_currentVolatility:F0}%";
        if (ratio >= 0.75f)
        {
            _valueText.color = Color.Lerp(criticalColor, Color.white, Mathf.PingPong(Time.time * 4f, 1f));
        }
        else
        {
            _valueText.color = Color.Lerp(Color.white, new Color(0.8f, 0.8f, 0.8f), 0.5f);
        }
    }

    // ═══ Self-Building UI System ════════════════════════════════════

    private void BuildUI()
    {
        float scale = baseFontSize / 18f;

        // ── Canvas ──
        GameObject canvasObj = new GameObject("VolatilityUI_Canvas");
        canvasObj.transform.SetParent(transform);
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // ── Main Container ──
        GameObject containerObj = new GameObject("Volatility_Container");
        containerObj.transform.SetParent(canvasObj.transform, false);
        _container = containerObj.AddComponent<RectTransform>();
        _container.sizeDelta = new Vector2(300 * scale, 60 * scale);
        ApplyAnchorPreset(_container, anchorPosition, positionOffset * scale);

        // Add a sleek background plate
        Image bgImg = containerObj.AddComponent<Image>();
        bgImg.color = backgroundColor;

        // Accent border
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(containerObj.transform, false);
        RectTransform borderRT = borderObj.AddComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = Vector2.one;
        borderRT.sizeDelta = Vector2.zero;
        Image borderOutline = borderObj.AddComponent<Image>();
        borderOutline.color = new Color(0.8f, 0.8f, 0.9f, 0.15f);

        // ── Text Label ──
        GameObject textObj = new GameObject("LabelText");
        textObj.transform.SetParent(containerObj.transform, false);
        _valueText = textObj.AddComponent<Text>();
        _valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _valueText.fontSize = baseFontSize;
        _valueText.fontStyle = FontStyle.Bold;
        _valueText.alignment = TextAnchor.UpperLeft;
        _valueText.raycastTarget = false;
        
        RectTransform textRT = _valueText.rectTransform;
        textRT.anchorMin = new Vector2(0, 0.5f);
        textRT.anchorMax = new Vector2(1, 1);
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.sizeDelta = new Vector2(-20 * scale, 0); // Padding left/right
        textRT.anchoredPosition = new Vector2(0, -5 * scale); // Nudge down

        // ── Slider Background ──
        GameObject sliderBgObj = new GameObject("SliderBG");
        sliderBgObj.transform.SetParent(containerObj.transform, false);
        _sliderBgImage = sliderBgObj.AddComponent<Image>();
        _sliderBgImage.color = new Color(0, 0, 0, 0.6f);
        
        RectTransform sliderBgRT = _sliderBgImage.rectTransform;
        sliderBgRT.anchorMin = new Vector2(0, 0);
        sliderBgRT.anchorMax = new Vector2(1, 0.5f);
        sliderBgRT.pivot = new Vector2(0.5f, 0.0f);
        sliderBgRT.sizeDelta = new Vector2(-20 * scale, -15 * scale); // Margin
        sliderBgRT.anchoredPosition = new Vector2(0, 5 * scale); // Nudge up

        // ── Slider Fill Area ──
        GameObject fillAreaObj = new GameObject("FillArea");
        fillAreaObj.transform.SetParent(sliderBgObj.transform, false);
        RectTransform fillAreaRT = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.sizeDelta = new Vector2(-4 * scale, -4 * scale); // Padding inside the background

        // ── Slider Fill Image ──
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        _fillImage = fillObj.AddComponent<Image>();
        _fillImage.type = Image.Type.Simple;
        
        RectTransform fillRT = _fillImage.rectTransform;
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = Vector2.zero;

        // ── Setup Slider Component ──
        _slider = containerObj.AddComponent<Slider>();
        _slider.interactable = false;
        _slider.transition = Selectable.Transition.None;
        _slider.fillRect = fillRT;
        _slider.minValue = 0f;
        _slider.maxValue = 1f;
    }

    private void ApplyAnchorPreset(RectTransform rt, ScreenPosition pos, Vector2 offset)
    {
        Vector2 anchorMin = Vector2.zero;
        Vector2 anchorMax = Vector2.zero;
        Vector2 pivot = Vector2.zero;

        switch (pos)
        {
            case ScreenPosition.TopLeft:
                anchorMin = anchorMax = new Vector2(0, 1);
                pivot = new Vector2(0, 1);
                break;
            case ScreenPosition.TopCenter:
                anchorMin = anchorMax = new Vector2(0.5f, 1);
                pivot = new Vector2(0.5f, 1);
                break;
            case ScreenPosition.TopRight:
                anchorMin = anchorMax = new Vector2(1, 1);
                pivot = new Vector2(1, 1);
                break;
            case ScreenPosition.BottomLeft:
                anchorMin = anchorMax = new Vector2(0, 0);
                pivot = new Vector2(0, 0);
                break;
            case ScreenPosition.BottomCenter:
                anchorMin = anchorMax = new Vector2(0.5f, 0);
                pivot = new Vector2(0.5f, 0);
                break;
            case ScreenPosition.BottomRight:
                anchorMin = anchorMax = new Vector2(1, 0);
                pivot = new Vector2(1, 0);
                break;
        }

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        
        // Invert X/Y offsets based on corner so positive numbers always push inward perfectly
        float xModify = (pivot.x == 1) ? -1 : 1;
        float yModify = (pivot.y == 1) ? -1 : 1;
        
        // If center anchored, X offset applies directly
        if (pivot.x == 0.5f) xModify = 1f; 

        rt.anchoredPosition = new Vector2(offset.x * xModify, offset.y * yModify);
    }
}
