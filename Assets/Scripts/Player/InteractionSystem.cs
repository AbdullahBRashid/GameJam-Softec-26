using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Raycast-based interaction system with UI selection support.
/// Detects objects with an AttributeController and opens a selection panel
/// for Taking or Applying attributes.
/// 
/// Pressing Interact (E) opens the interaction panel for the targeted object.
/// The panel shows available attributes to take and compatible attributes to apply.
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

    private void Awake()
    {
        _inventory = GetComponent<AttributeInventory>();

        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }

        if (interactionPanel != null)
            interactionPanel.SetActive(false);
    }

    private void Update()
    {
        if (!_panelOpen)
        {
            PerformRaycast();
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

    // ═══ Input Callbacks (New Input System — Send Messages) ═════════

    /// <summary>
    /// INTERACT (E): Opens the interaction panel for the current target.
    /// If no panel is assigned, falls back to quick-take behavior.
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

        // If we have a UI panel, open it
        if (interactionPanel != null && attributeButtonPrefab != null)
        {
            OpenPanel();
        }
        else
        {
            // Fallback: quick-take first attribute
            QuickTake();
        }
    }

    /// <summary>
    /// APPLY (Q): Quick-apply selected inventory attribute to target.
    /// Used as a shortcut when the panel isn't open.
    /// </summary>
    public void OnApply(InputValue value)
    {
        if (!value.isPressed) return;
        if (_panelOpen) return; // Use panel buttons instead

        if (_currentTarget == null)
        {
            Debug.Log("[InteractionSystem] No target in range.");
            return;
        }

        QuickApply();
    }

    // ═══ Quick Actions (No Panel) ═══════════════════════════════════

    private void QuickTake()
    {
        if (_currentTarget.AttributeCount == 0)
        {
            Debug.Log("[InteractionSystem] Target has no attributes to take.");
            return;
        }

        // Take the first attribute
        AttributeSO first = _currentTarget.ActiveAttributes[0];
        bool wasDefault = _currentTarget.IsDefaultAttribute(first);
        GameObject sourceObj = _currentTarget.gameObject;

        AttributeSO taken = _currentTarget.RemoveAttribute(first);
        if (taken != null)
        {
            bool added = _inventory.AddAttribute(taken, sourceObj, wasDefault);
            if (!added)
            {
                // Inventory full — put it back
                _currentTarget.ApplyAttribute(taken);
                Debug.Log("[InteractionSystem] Inventory full. Attribute returned to object.");
            }
        }
    }

    private void QuickApply()
    {
        AttributeSO selected = _inventory.GetSelected();
        if (selected == null)
        {
            Debug.Log("[InteractionSystem] Inventory is empty.");
            return;
        }

        if (!_currentTarget.CanAccept(selected))
        {
            Debug.Log($"[InteractionSystem] '{selected.displayName}' cannot be applied to {_currentTarget.gameObject.name}.");
            GameEventManager.NarratorSpeak($"That doesn't belong there.", 3f);
            return;
        }

        bool applied = _currentTarget.ApplyAttribute(selected);
        if (applied)
        {
            _inventory.RemoveAttribute(selected);
            _inventory.ClearTracking(selected);
        }
    }

    // ═══ Panel UI ═══════════════════════════════════════════════════

    private void OpenPanel()
    {
        _panelOpen = true;
        interactionPanel.SetActive(true);

        // Unlock cursor so the player can click buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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
    }

    private void PopulatePanel()
    {
        // Clear existing buttons
        ClearContainer(takeButtonContainer);
        ClearContainer(applyButtonContainer);

        // ── TAKE section: show object's attributes ──
        if (_currentTarget != null && !_currentTarget.IsLocked)
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

        // Set up the button visuals
        var buttonUI = btn.GetComponent<AttributeButtonUI>();
        if (buttonUI != null)
        {
            buttonUI.Setup(attr, "TAKE", attr.attributeColor, () => OnTakeButtonClicked(attr));
        }
        else
        {
            // Fallback: use Unity Button + Text
            var uiButton = btn.GetComponent<UnityEngine.UI.Button>();
            var uiText = btn.GetComponentInChildren<UnityEngine.UI.Text>();
            if (uiText != null) uiText.text = $"Take: {attr.displayName}";
            if (uiButton != null) uiButton.onClick.AddListener(() => OnTakeButtonClicked(attr));

            // Also try TextMeshPro
            var tmpText = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null) tmpText.text = $"Take: {attr.displayName}";
        }
    }

    private void CreateApplyButton(AttributeSO attr)
    {
        if (applyButtonContainer == null || attributeButtonPrefab == null) return;

        GameObject btn = Object.Instantiate(attributeButtonPrefab, applyButtonContainer);
        btn.name = $"Apply_{attr.displayName}";

        var buttonUI = btn.GetComponent<AttributeButtonUI>();
        if (buttonUI != null)
        {
            buttonUI.Setup(attr, "APPLY", attr.attributeColor, () => OnApplyButtonClicked(attr));
        }
        else
        {
            var uiButton = btn.GetComponent<UnityEngine.UI.Button>();
            var uiText = btn.GetComponentInChildren<UnityEngine.UI.Text>();
            if (uiText != null) uiText.text = $"Apply: {attr.displayName}";
            if (uiButton != null) uiButton.onClick.AddListener(() => OnApplyButtonClicked(attr));

            var tmpText = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null) tmpText.text = $"Apply: {attr.displayName}";
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

        // Refresh panel or close if nothing left
        if (_currentTarget.AttributeCount == 0 && _inventory.Count == 0)
            ClosePanel();
        else
            PopulatePanel(); // Refresh
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

        // Refresh panel
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
