using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private bool _panelOpen = false;

    // ── Components to disable when panel is open ──
    private PlayerMovement _playerMovement;
    private MonoBehaviour _cinemachineInputController;

    private void Awake()
    {
        _inventory = GetComponent<AttributeInventory>();
        _playerMovement = GetComponent<PlayerMovement>();

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

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            AttributeController controller = hit.collider.GetComponent<AttributeController>();

            if (controller != null)
            {
                _currentTarget = controller;
                if (interactPromptUI != null) interactPromptUI.SetActive(true);
                return;
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
            _playerMovement.enabled = false;

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

        // ── APPLY section: show compatible inventory attributes ──
        if (_currentTarget != null)
        {
            foreach (var attr in _inventory.Items)
            {
                if (_currentTarget.CanAccept(attr))
                {
                    CreateApplyButton(attr);
                }
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
            buttonUI.Setup(attr, label, attr.attributeColor, () => OnTakeButtonClicked(attr));
        }
        else
        {
            var uiButton = btn.GetComponent<UnityEngine.UI.Button>();
            var uiText = btn.GetComponentInChildren<UnityEngine.UI.Text>();
            if (uiText != null) { uiText.text = $"Take: {attr.displayName} ({(isDefault ? "+" + attr.volatilityCost : "+0")})"; uiText.supportRichText = true; }
            if (uiButton != null) uiButton.onClick.AddListener(() => OnTakeButtonClicked(attr));

            var tmpText = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null) { tmpText.text = $"Take: {attr.displayName} ({(isDefault ? "+" + attr.volatilityCost : "+0")})"; tmpText.richText = true; }
        }
    }

    private void CreateApplyButton(AttributeSO attr)
    {
        if (applyButtonContainer == null || attributeButtonPrefab == null) return;

        GameObject btn = Object.Instantiate(attributeButtonPrefab, applyButtonContainer);
        btn.name = $"Apply_{attr.displayName}";

        // Calculate cost info for applying this attribute
        bool isRestoring = _currentTarget.IsDefaultAttribute(attr) && _currentTarget.IsMissingDefault(attr);
        float reduction = isRestoring ? attr.volatilityCost : attr.volatilityCost * 0.5f;
        string costStr = isRestoring
            ? $"  <color=#2ECC71>-{reduction:F0}</color>"
            : $"  <color=#F1C40F>-{reduction:F0}</color>";
        string label = $"APPLY{costStr}";

        var buttonUI = btn.GetComponent<AttributeButtonUI>();
        if (buttonUI != null)
        {
            buttonUI.Setup(attr, label, attr.attributeColor, () => OnApplyButtonClicked(attr));
        }
        else
        {
            var uiButton = btn.GetComponent<UnityEngine.UI.Button>();
            var uiText = btn.GetComponentInChildren<UnityEngine.UI.Text>();
            if (uiText != null) { uiText.text = $"Apply: {attr.displayName} (-{reduction:F0})"; uiText.supportRichText = true; }
            if (uiButton != null) uiButton.onClick.AddListener(() => OnApplyButtonClicked(attr));

            var tmpText = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null) { tmpText.text = $"Apply: {attr.displayName} (-{reduction:F0})"; tmpText.richText = true; }
        }
    }

    // ═══ Button Click Handlers ══════════════════════════════════════

    private void OnTakeButtonClicked(AttributeSO attr)
    {
        if (_currentTarget == null) return;

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

    private void OnApplyButtonClicked(AttributeSO attr)
    {
        if (_currentTarget == null) return;

        if (!_currentTarget.CanAccept(attr))
        {
            Debug.Log($"[InteractionSystem] Cannot apply '{attr.displayName}' here.");
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
