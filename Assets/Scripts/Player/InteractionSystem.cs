using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Raycast-based interaction system.
/// Detects objects with an AttributeController and allows
/// the player to Take (E) or Apply (Q) attributes.
/// 
/// Setup: Add to the Player GameObject.
///        Assign the Camera transform in the Inspector.
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

    // ── Cached References ──
    private AttributeInventory _inventory;
    private AttributeController _currentTarget;

    private void Awake()
    {
        _inventory = GetComponent<AttributeInventory>();

        if (cameraTransform == null)
        {
            // Fallback: try to find the main camera
            Camera cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }
    }

    private void Update()
    {
        PerformRaycast();
    }

    // ═══ Raycast ════════════════════════════════════════════════════

    private void PerformRaycast()
    {
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

        // Nothing hit or no controller found
        _currentTarget = null;
        if (interactPromptUI != null) interactPromptUI.SetActive(false);
    }

    // ═══ Input Callbacks (New Input System) ═════════════════════════
    // Bind these in Player Actions:
    //   "Take"  → E key
    //   "Apply" → Q key

    /// <summary>
    /// TAKE (E): Remove the first attribute from the target object
    /// and add it to the player's inventory.
    /// </summary>
    public void OnTake(InputValue value)
    {
        if (!value.isPressed) return;
        if (_currentTarget == null)
        {
            Debug.Log("[InteractionSystem] No target in range.");
            return;
        }

        if (_currentTarget.AttributeCount == 0)
        {
            Debug.Log("[InteractionSystem] Target has no attributes to take.");
            return;
        }

        // Take the first attribute from the object
        AttributeSO taken = _currentTarget.RemoveFirst();
        if (taken != null)
        {
            bool added = _inventory.AddAttribute(taken);
            if (!added)
            {
                // Inventory full — put it back
                _currentTarget.ApplyAttribute(taken);
                Debug.Log("[InteractionSystem] Inventory full. Attribute returned to object.");
            }
        }
    }

    /// <summary>
    /// APPLY (Q): Take the first attribute from the player's inventory
    /// and apply it to the target object.
    /// </summary>
    public void OnApply(InputValue value)
    {
        if (!value.isPressed) return;
        if (_currentTarget == null)
        {
            Debug.Log("[InteractionSystem] No target in range.");
            return;
        }

        AttributeSO selected = _inventory.GetSelected();
        if (selected == null)
        {
            Debug.Log("[InteractionSystem] Inventory is empty.");
            return;
        }

        bool applied = _currentTarget.ApplyAttribute(selected);
        if (applied)
        {
            _inventory.RemoveAttribute(selected);
        }
    }
}
