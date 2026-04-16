using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Volatility Meter UI handled via Editor-assigned components for clean, robust setup.
/// Listens to GameEventManager to update its visuals.
/// </summary>
public class VolatilityHUD : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The Image component used for the fill bar (Set Image Type to Filled).")]
    [SerializeField] private Image fillBarImage;

    [Tooltip("Text to display the current percentage (e.g. 50%).")]
    [SerializeField] private TextMeshProUGUI percentageText;

    [Tooltip("Optional text to display the current active bug warning.")]
    [SerializeField] private TextMeshProUGUI bugWarningText;

    [Header("Colors")]
    [SerializeField] private Color safeColor = new Color(0.18f, 0.85f, 0.45f);
    [SerializeField] private Color warningColor = new Color(1f, 0.75f, 0.1f);
    [SerializeField] private Color dangerColor = new Color(0.95f, 0.22f, 0.22f);

    [Header("Animation")]
    [Tooltip("Speed the bar catches up to the target value.")]
    [SerializeField] private float fillSpeed = 5f;

    private float _targetFill = 0f;
    private float _currentFill = 0f;

    private void Awake()
    {
        if (bugWarningText != null)
            bugWarningText.gameObject.SetActive(false);
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

        if (fillBarImage != null)
        {
            fillBarImage.fillAmount = _currentFill;
            fillBarImage.color = GetColorForFill(_currentFill);
        }

        if (percentageText != null)
        {
            percentageText.text = $"{Mathf.RoundToInt(_currentFill * 100)}%";
        }
    }

    private void HandleVolatilityChanged(float current, float delta)
    {
        // Volatility is fetched from the manager to get its normalized 0..1 scale
        if (VolatilityManager.Instance != null)
        {
            _targetFill = VolatilityManager.Instance.NormalizedVolatility;
        }
    }

    private void HandleBugTriggered(MechanicalBugType bug)
    {
        if (bugWarningText != null)
        {
            bugWarningText.text = $"⚠ {FormatBugName(bug)}";
            bugWarningText.gameObject.SetActive(true);
        }
    }

    private void HandleBugEnded(MechanicalBugType bug)
    {
        if (bugWarningText != null)
        {
            bugWarningText.gameObject.SetActive(false);
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
}
