using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Raycast-based interaction system with UI selection panel.
/// Press Interact (E) to open the panel when looking at an interactable.
/// Press E again to close it. Camera and movement freeze while panel is open.
///
/// Setup: Add to the Player GameObject.
///        Assign the Camera transform in the Inspector.
///        Assign the InteractionPanel UI prefab.
/// </summary>
[RequireComponent(typeof(AttributeInventory))]
public class InteractionSystem : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Tooltip("The camera used for raycasting (assign your main/Cinemachine camera).")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("Max distance for interaction raycast.")]
    [SerializeField] private float interactRange = 4f;

    [Tooltip("Layer mask for interactable objects.")]
    [SerializeField] private LayerMask interactableLayer;

    [Header("UI Feedback")]
    [Tooltip("Crosshair or prompt text that appears when looking at an interactable.")]
    [SerializeField] private GameObject interactPromptUI;

    [Header("Interaction Panel")]
    [Tooltip("The interaction panel Canvas (disable by default in editor).")]
    [SerializeField] private GameObject interactionPanel;

    [Tooltip("Parent transform for TAKE buttons (object's attributes).")]
    [SerializeField] private Transform takeButtonContainer;

    [Tooltip("Parent transform for APPLY buttons (player's inventory).")]
    [SerializeField] private Transform applyButtonContainer;

    [Tooltip("Prefab for each attribute button in the panel.")]
    [SerializeField] private GameObject attributeButtonPrefab;

    // ── Cached References ──
    private AttributeInventory _inventory;
    private AttributeController _currentTarget;
    public bool IsPanelOpen => _panelOpen;
    private bool _panelOpen = false;

    // ── Discard Menu ──
    private GameObject _discardMenuRoot;
    private RectTransform _discardMenuRT;
    private AttributeSO _currentDiscardAttr;

    // ── Components to disable when panel is open ──
    private PlayerMovement _playerMovement;
    private PlayerInput _playerInput;
    private MonoBehaviour _cinemachineInputController;

    private void Awake()
    {
        _inventory = GetComponent<AttributeInventory>();
        _playerMovement = GetComponent<PlayerMovement>();
        _playerInput = GetComponent<PlayerInput>();

        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }

        if (interactionPanel != null)
            interactionPanel.SetActive(false);
    }

    private void Start()
    {
        // Find the Cinemachine input controller in the scene to disable it during interaction
        // CinemachineInputAxisController is the component that reads mouse input for camera look
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb.GetType().Name == "CinemachineInputAxisController")
            {
                _cinemachineInputController = mb;
                break;
            }
        }

        if (interactionPanel != null)
        {
            BuildDiscardMenu(interactionPanel.transform);
        }
    }

    private void Update()
    {
        if (!_panelOpen)
        {
            PerformRaycast();

            // Left mouse click toggles unlocked doors
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryToggleDoor();
            }
        }
    }

    /// <summary>
    /// If looking at a door, open it (only if unlocked) or close it with left click.
    /// </summary>
    private void TryToggleDoor()
    {
        if (_currentTarget == null) return;

        DoorController door = _currentTarget.GetComponent<DoorController>();
        if (door == null || !door.CanInteract) return;

        if (door.IsOpen)
        {
            // Always allow closing
            door.CloseDoor();
        }
        else if (door.enabled && !HasLockedAttribute(_currentTarget))
        {
            // Only allow opening if unlocked
            door.OpenDoor();
        }
    }

    // ═══ Raycast ════════════════════════════════════════════════════

    private void PerformRaycast()
    {
        if (cameraTransform == null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // We explicitly cast through everything (except IgnoreRaycast) to guarantee we can see Mirrors, 
        // which might just be on the 'Default' layer rather than the dedicated 'interactable' layer!
        RaycastHit[] hits = Physics.RaycastAll(ray, interactRange);
        
        // Sort hits by distance so we process the closest physical object first
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            // Ignore the ray colliding with the inside of the player's own head when looking forward!
            if (hit.collider.transform.root == transform.root) continue;

            AttributeController controller = null;

            // 1. Did we hit a Mirror?
            if (hit.collider.CompareTag("Mirror"))
            {
                // BOUNCE THE RAY!
                Vector3 reflectDir = Vector3.Reflect(ray.direction, hit.normal);
                float remainingDist = interactRange - hit.distance;
                
                // Nudge the start point slightly forward along the normal to prevent immediately hitting the mirror again
                Ray reflectRay = new Ray(hit.point + hit.normal * 0.01f, reflectDir);
                
                // Now, specifically query the reflect ray. 
                // We test against EVERYTHING again because we need to see the Player's body!
                RaycastHit[] reflectedHits = Physics.RaycastAll(reflectRay, remainingDist);
                System.Array.Sort(reflectedHits, (a, b) => a.distance.CompareTo(b.distance));
                
                foreach (var refHit in reflectedHits)
                {
                    if (refHit.collider.CompareTag("Mirror")) continue; // Don't infinite loop mirrors
                    
                    controller = refHit.collider.GetComponentInParent<AttributeController>();
                    if (controller != null) break;
                }
                
                // A mirror is solid glass; we cannot physically interact 'through' it directly 
                // without reflection, so we stop evaluating forward hits.
                if (controller != null)
                {
                    _currentTarget = controller;
                    if (interactPromptUI != null) interactPromptUI.SetActive(true);
                    return;
                }
                break;
            }
            // 2. Did we directly look at an interactable object?
            else if (((1 << hit.collider.gameObject.layer) & interactableLayer.value) != 0)
            {
                controller = hit.collider.GetComponentInParent<AttributeController>();
                if (controller != null)
                {
                    _currentTarget = controller;
                    if (interactPromptUI != null) interactPromptUI.SetActive(true);
                    return;
                }
            }
            // 3. Did we hit a solid, standard wall that blocks our vision?
            else if (!hit.collider.isTrigger)
            {
                break;
            }
        }

        _currentTarget = null;
        if (interactPromptUI != null) interactPromptUI.SetActive(false);
    }

    // ═══ Input Callback (New Input System — Send Messages) ══════════

    /// <summary>
    /// INTERACT (E): Opens/closes the interaction panel for the current target.
    /// </summary>
    public void OnTake(InputValue value)
    {
        if (!value.isPressed) return;

        // If panel is open, close it
        if (_panelOpen)
        {
            ClosePanel();
            return;
        }

        if (_currentTarget == null)
        {
            Debug.Log("[InteractionSystem] No target in range.");
            return;
        }

        if (interactionPanel == null || attributeButtonPrefab == null)
        {
            Debug.LogWarning("[InteractionSystem] Interaction Panel or Button Prefab not assigned!");
            return;
        }

        OpenPanel();
    }

    /// <summary>Check if the target currently has a "locked" attribute active.</summary>
    private bool HasLockedAttribute(AttributeController controller)
    {
        foreach (var attr in controller.ActiveAttributes)
        {
            if (attr.attributeID.ToLower() == "locked") return true;
        }
        return false;
    }

    // ═══ Panel UI ═══════════════════════════════════════════════════

    private void OpenPanel()
    {
        _panelOpen = true;
        interactionPanel.SetActive(true);

        // Unlock cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Freeze player movement
        if (_playerMovement != null)
        {
            _playerMovement.enabled = false;
        }

        // Freeze camera panning
        if (_cinemachineInputController != null)
            _cinemachineInputController.enabled = false;

        PopulatePanel();
    }

    public void ClosePanel()
    {
        _panelOpen = false;
        if (interactionPanel != null)
            interactionPanel.SetActive(false);

        HideDiscardMenu();

        // Re-lock cursor for FPS controls
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Re-enable player movement
        if (_playerMovement != null)
            _playerMovement.enabled = true;

        // Re-enable camera panning
        if (_cinemachineInputController != null)
            _cinemachineInputController.enabled = true;
    }

    private void PopulatePanel()
    {
        // Clear existing buttons
        ClearContainer(takeButtonContainer);
        ClearContainer(applyButtonContainer);

        // ── TAKE section: show object's attributes ──
        // Note: we always show take buttons. The "locked" attribute should be
        // removable by the player. The AI Director lock (IsLocked) only blocks
        // inside RemoveAttribute() and shows a narrator message there.
        if (_currentTarget != null)
        {
            foreach (var attr in _currentTarget.ActiveAttributes)
            {
                CreateTakeButton(attr);
            }
        }

        // ── APPLY section: show inventory attributes ──
        if (_currentTarget != null)
        {
            foreach (var attr in _inventory.Items)
            {
                CreateApplyButton(attr);
            }
        }
    }

    private void CreateTakeButton(AttributeSO attr)
    {
        if (takeButtonContainer == null || attributeButtonPrefab == null) return;

        GameObject btn = Object.Instantiate(attributeButtonPrefab, takeButtonContainer);
        btn.name = $"Take_{attr.displayName}";

        // Calculate cost info for taking this attribute
        bool isDefault = _currentTarget.IsDefaultAttribute(attr);
        string costStr = isDefault ? $"  <color=#F05545>+{attr.volatilityCost:F0}</color>" : "  <color=#888888>+0</color>";
        string label = $"TAKE{costStr}";

        var buttonUI = btn.GetComponent<AttributeButtonUI>();
        if (buttonUI != null)
        {
            buttonUI.Setup(attr, label, attr.attributeColor, () => OnTakeButtonClicked(attr, btn));
        }
        else
        {
            var uiButton = btn.GetComponent<UnityEngine.UI.Button>();
            var uiText = btn.GetComponentInChildren<UnityEngine.UI.Text>();
            if (uiText != null) { uiText.text = $"Take: {attr.displayName} ({(isDefault ? "+" + attr.volatilityCost : "+0")})"; uiText.supportRichText = true; }
            if (uiButton != null) uiButton.onClick.AddListener(() => OnTakeButtonClicked(attr, btn));

            var tmpText = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null) { tmpText.text = $"Take: {attr.displayName} ({(isDefault ? "+" + attr.volatilityCost : "+0")})"; tmpText.richText = true; }
        }
    }

    private void CreateApplyButton(AttributeSO attr)
    {
        if (applyButtonContainer == null || attributeButtonPrefab == null) return;

        GameObject btn = Object.Instantiate(attributeButtonPrefab, applyButtonContainer);
        btn.name = $"Apply_{attr.displayName}";

        bool isCompatible = _currentTarget.CanAccept(attr);
        string label;
        Color tintColor;
        float reduction = 0f;

        if (isCompatible)
        {
            // Calculate cost info for applying this attribute
            bool isRestoring = _currentTarget.IsDefaultAttribute(attr) && _currentTarget.IsMissingDefault(attr);
            reduction = isRestoring ? attr.volatilityCost : attr.volatilityCost * 0.5f;
            string costStr = isRestoring
                ? $"  <color=#2ECC71>-{reduction:F0}</color>"
                : $"  <color=#F1C40F>-{reduction:F0}</color>";
            label = $"APPLY{costStr}";
            tintColor = attr.attributeColor;
        }
        else
        {
            label = "<color=#E74C3C>INCOMPATIBLE</color>";
            tintColor = Color.gray;
        }

        var buttonUI = btn.GetComponent<AttributeButtonUI>();
        if (buttonUI != null)
        {
            buttonUI.Setup(attr, label, tintColor, () => OnApplyButtonClicked(attr, isCompatible, btn));
        }
        else
        {
            var uiButton = btn.GetComponent<UnityEngine.UI.Button>();
            var uiText = btn.GetComponentInChildren<UnityEngine.UI.Text>();
            if (uiText != null) { uiText.text = isCompatible ? $"Apply: {attr.displayName} (-{reduction:F0})" : $"Incompatible: {attr.displayName}"; uiText.supportRichText = true; }
            if (uiButton != null) uiButton.onClick.AddListener(() => OnApplyButtonClicked(attr, isCompatible, btn));

            var tmpText = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null) { tmpText.text = isCompatible ? $"Apply: {attr.displayName} (-{reduction:F0})" : $"<color=#E74C3C>Incompatible: {attr.displayName}</color>"; tmpText.richText = true; }
        }

        // Add Right-Click to discard
        EventTrigger trigger = btn.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener((data) => {
            PointerEventData pData = (PointerEventData)data;
            if (pData.button == PointerEventData.InputButton.Right)
            {
                ShowDiscardMenu(attr, pData.position);
            }
        });
        trigger.triggers.Add(entry);
    }

    // ═══ Button Click Handlers ══════════════════════════════════════

    private void OnTakeButtonClicked(AttributeSO attr, GameObject btn)
    {
        if (_currentTarget == null) return;

        // Check if taking this would exceed max volatility
        if (VolatilityManager.Instance != null && attr.attributeID.ToLower() != "key")
        {
            float costToAdd = _currentTarget.IsDefaultAttribute(attr) ? attr.volatilityCost : (attr.volatilityCost * 0.5f);
            if (VolatilityManager.Instance.Volatility + costToAdd > VolatilityManager.Instance.MaxVolatility)
            {
                Debug.Log($"[InteractionSystem] Blocked. Taking '{attr.displayName}' would exceed max volatility limit.");
                StartCoroutine(ShakeButtonCoroutine(btn));
                GameEventManager.NarratorSpeak("SYSTEM LIMIT REACHED: Interaction would critically destabilize runtime.", 3f);
                return;
            }
        }

        // --- NEW: Special Key Teleport Sequence ---
        if (attr.attributeID.ToLower() == "key")
        {
            StartCoroutine(KeyTeleportSequence(attr, _currentTarget));
            return;
        }

        bool wasDefault = _currentTarget.IsDefaultAttribute(attr);
        GameObject sourceObj = _currentTarget.gameObject;

        AttributeSO taken = _currentTarget.RemoveAttribute(attr);
        if (taken != null)
        {
            bool added = _inventory.AddAttribute(taken, sourceObj, wasDefault);
            if (!added)
            {
                _currentTarget.ApplyAttribute(taken);
                Debug.Log("[InteractionSystem] Inventory full.");
            }
        }

        // Refresh panel or close if nothing left to interact with
        if (_currentTarget.AttributeCount == 0 && _inventory.Count == 0)
            ClosePanel();
        else
            PopulatePanel();
    }

    private void OnApplyButtonClicked(AttributeSO attr, bool isCompatible, GameObject btn)
    {
        if (_currentTarget == null) return;

        if (!isCompatible || !_currentTarget.CanAccept(attr))
        {
            Debug.Log($"[InteractionSystem] Cannot apply '{attr.displayName}' here.");
            StartCoroutine(ShakeButtonCoroutine(btn));
            GameEventManager.NarratorSpeak("cannotApply", 2f);
            return;
        }

        bool applied = _currentTarget.ApplyAttribute(attr);
        if (applied)
        {
            _inventory.RemoveAttribute(attr);
            _inventory.ClearTracking(attr);
        }

        PopulatePanel();
    }

    private System.Collections.IEnumerator ShakeButtonCoroutine(GameObject btn)
    {
        if (btn == null) yield break;
        
        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt == null) yield break;
        
        Vector3 originalPos = rt.anchoredPosition3D;
        float elapsed = 0f;
        float duration = 0.3f;
        float magnitude = 10f;
        
        while (elapsed < duration)
        {
            if (btn == null || rt == null) yield break;
            float damping = 1f - (elapsed / duration);
            float xOffset = Mathf.Sin(elapsed * 60f) * magnitude * damping;
            rt.anchoredPosition3D = originalPos + new Vector3(xOffset, 0, 0);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        if (btn != null && rt != null)
            rt.anchoredPosition3D = originalPos;
    }

    private System.Collections.IEnumerator KeyTeleportSequence(AttributeSO keyAttr, AttributeController target)
    {
        // 1. Delete the key from the scene permanently and place it in the player's inventory
        bool wasDefault = target.IsDefaultAttribute(keyAttr);
        GameObject sourceObj = target.gameObject;
        
        AttributeSO taken = target.RemoveAttribute(keyAttr);
        if (taken != null)
        {
            _inventory.AddAttribute(taken, sourceObj, wasDefault);
        }

        ClosePanel(); // Note: ClosePanel re-enables input, so we immediately disable it again below.

        // --- DISABLE INPUT FOR CUTSCENE ---
        if (_playerInput != null) _playerInput.DeactivateInput();
        if (_playerMovement != null) _playerMovement.enabled = false;
        if (_cinemachineInputController != null) _cinemachineInputController.enabled = false;

        // 2. Camera Shake via Volatility system
        GameEventManager.MechanicalBugTriggered(MechanicalBugType.CameraShake);

        // 3. Fade in Black
        GameObject canvasObj = new GameObject("FadeCanvas");
        Canvas c = canvasObj.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 999;
        
        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image fadeImage = imgObj.AddComponent<UnityEngine.UI.Image>();
        fadeImage.color = new Color(0, 0, 0, 0);
        RectTransform rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        float t = 0;
        float fadeTime = 2.0f;
        while(t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            fadeImage.color = new Color(0, 0, 0, t / fadeTime);
            yield return null;
        }

        // 4. Teleport to stage 0
        if (StageManager.Instance != null)
        {
            StageManager.Instance.TeleportToStage(0);
        }

        // 5. Fade out Black
        t = 0;
        while(t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            fadeImage.color = new Color(0, 0, 0, 1f - (t / fadeTime));
            yield return null;
        }

        GameEventManager.MechanicalBugEnded(MechanicalBugType.CameraShake);
        Destroy(canvasObj);

        // --- RE-ENABLE INPUT AFTER CUTSCENE ---
        if (_playerInput != null) _playerInput.ActivateInput();
        if (_playerMovement != null) _playerMovement.enabled = true;
        if (_cinemachineInputController != null) _cinemachineInputController.enabled = true;
    }

    // ═══ Discard Menu ═══════════════════════════════════════════════

    private void BuildDiscardMenu(Transform canvasTransform)
    {
        GameObject blockerObj = new GameObject("DiscardBlocker");
        blockerObj.transform.SetParent(canvasTransform, false);
        RectTransform blockerRT = blockerObj.AddComponent<RectTransform>();
        blockerRT.anchorMin = Vector2.zero;
        blockerRT.anchorMax = Vector2.one;
        blockerRT.sizeDelta = Vector2.zero;
        UnityEngine.UI.Image blockerImg = blockerObj.AddComponent<UnityEngine.UI.Image>();
        blockerImg.color = new Color(0, 0, 0, 0);
        blockerImg.raycastTarget = true;
        
        EventTrigger blockerTrigger = blockerObj.AddComponent<EventTrigger>();
        EventTrigger.Entry blockerEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        blockerEntry.callback.AddListener((data) => HideDiscardMenu());
        blockerTrigger.triggers.Add(blockerEntry);

        GameObject menuObj = new GameObject("DiscardMenu");
        menuObj.transform.SetParent(blockerObj.transform, false);
        _discardMenuRT = menuObj.AddComponent<RectTransform>();
        _discardMenuRT.sizeDelta = new Vector2(100, 36);
        _discardMenuRT.pivot = new Vector2(0, 1);
        UnityEngine.UI.Image menuBg = menuObj.AddComponent<UnityEngine.UI.Image>();
        menuBg.color = new Color(0.12f, 0.12f, 0.14f, 0.98f);
        menuBg.raycastTarget = true;
        UnityEngine.UI.Button discardBtn = menuObj.AddComponent<UnityEngine.UI.Button>();
        discardBtn.onClick.AddListener(OnDiscardClicked);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(menuObj.transform, false);
        UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = "Discard";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 14;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(0.95f, 0.3f, 0.25f, 1f);
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        RectTransform textRT = text.rectTransform;
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        _discardMenuRoot = blockerObj;
        _discardMenuRoot.SetActive(false);
    }

    private void ShowDiscardMenu(AttributeSO attr, Vector2 screenPos)
    {
        _currentDiscardAttr = attr;
        _discardMenuRoot.SetActive(true);
        _discardMenuRoot.transform.SetAsLastSibling();
        
        RectTransform canvasRT = interactionPanel.GetComponent<RectTransform>();
        if (canvasRT != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPos, null, out Vector2 localPoint);
            _discardMenuRT.anchoredPosition = localPoint;
        }
    }

    private void HideDiscardMenu()
    {
        if (_discardMenuRoot != null) _discardMenuRoot.SetActive(false);
        _currentDiscardAttr = null;
    }

    private void OnDiscardClicked()
    {
        if (_currentDiscardAttr != null && _inventory != null)
        {
            _inventory.RemoveAttribute(_currentDiscardAttr);
            PopulatePanel();
        }
        HideDiscardMenu();
    }

    // ═══ Helpers ════════════════════════════════════════════════════

    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }
}
