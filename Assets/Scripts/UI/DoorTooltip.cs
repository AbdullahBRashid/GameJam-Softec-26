using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A world-space tooltip that appears above/near doors.
/// It billboards to face the player, shows/hides based on distance,
/// and only activates when the door is UNLOCKED (DoorController is enabled).
/// </summary>
public class DoorTooltip : MonoBehaviour
{
    [Header("Settings")]
    public float activationDistance = 5f;
    public Vector3 localOffset = new Vector3(0, 2f, 0);
    public float fadeSpeed = 5f;

    [Header("Visuals")]
    [Tooltip("If null, it will automatically create a generic mouse icon.")]
    public Sprite tooltipSprite;
    public Vector2 tooltipSize = new Vector2(0.5f, 0.5f);

    private Canvas _canvas;
    private Image _iconImage;
    private DoorController _door;
    private Transform _cameraTransform;
    private CanvasGroup _canvasGroup;

    private void Start()
    {
        _door = GetComponent<DoorController>();
        _cameraTransform = Camera.main?.transform;

        BuildTooltipUI();
    }

    private void Update()
    {
        if (_cameraTransform == null || _door == null) return;

        // 1. Check if the door is unlocked (DoorController is enabled when unlocked)
        bool isUnlocked = _door.enabled;

        // 2. Check distance
        float distance = Vector3.Distance(transform.position, _cameraTransform.position);
        bool inRange = distance <= activationDistance;

        // 3. Determine target visibility
        // Tooltip is visible only if: Unlocked AND (InRange OR Open)
        // Actually, user said: "Only active when the door is not locked. Appears at a certain distance."
        float targetAlpha = (isUnlocked && inRange) ? 1f : 0f;

        // 4. Smooth Fade
        _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

        // 5. Billboard logic
        if (_canvasGroup.alpha > 0)
        {
            _canvas.transform.LookAt(_canvas.transform.position + _cameraTransform.forward);
        }
    }

    private void BuildTooltipUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("DoorTooltip_Canvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = localOffset;
        
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        
        // Size of canvas in world units
        RectTransform canvasRT = _canvas.GetComponent<RectTransform>();
        canvasRT.sizeDelta = tooltipSize;

        _canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;

        // Create Image
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(canvasObj.transform, false);
        _iconImage = iconObj.AddComponent<Image>();
        
        if (tooltipSprite != null)
        {
            _iconImage.sprite = tooltipSprite;
        }
        else
        {
            // Generic mouse-click icon look (Circle with a divider)
            _iconImage.color = new Color(1, 1, 1, 0.8f);
        }

        RectTransform iconRT = _iconImage.GetComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.sizeDelta = Vector2.zero;
    }
}
