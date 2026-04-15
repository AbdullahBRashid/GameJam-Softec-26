using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Place this component on any GameObject that can hold attributes.
/// Manages the list of active attributes and delegates Apply/Remove
/// to the corresponding IAttributeEffect implementations.
/// 
/// Setup: Add to any interactable physics object alongside a Collider.
///        Tag the GameObject as "AttributeReady".
/// </summary>
public class AttributeController : MonoBehaviour
{
    [Header("Current Attributes")]
    [Tooltip("Attributes currently active on this object.")]
    [SerializeField] private List<AttributeSO> activeAttributes = new List<AttributeSO>();

    [Header("Limits")]
    [Tooltip("Max attributes this object can hold at once.")]
    [SerializeField] private int maxAttributes = 3;

    [Header("Lock State (AI Director)")]
    [Tooltip("If true, attributes cannot be removed from this object.")]
    [SerializeField] private bool isLocked = false;

    // Track the live effect instances so we can call Remove on the exact same instance
    private readonly Dictionary<string, IAttributeEffect> _liveEffects
        = new Dictionary<string, IAttributeEffect>();

    // ── Public Properties ───────────────────────────────────────────
    public IReadOnlyList<AttributeSO> ActiveAttributes => activeAttributes;
    public int AttributeCount => activeAttributes.Count;
    public bool IsFull => activeAttributes.Count >= maxAttributes;
    public bool IsLocked => isLocked;

    // ═══ Public API ═════════════════════════════════════════════════

    /// <summary>
    /// Apply an attribute to this object.
    /// Returns true if successful.
    /// </summary>
    public bool ApplyAttribute(AttributeSO attribute)
    {
        if (attribute == null) return false;

        // Check capacity
        if (IsFull)
        {
            Debug.LogWarning($"[AttributeController] {gameObject.name} is full ({maxAttributes} max).");
            return false;
        }

        // Don't duplicate the same attribute
        if (HasAttribute(attribute))
        {
            Debug.LogWarning($"[AttributeController] {gameObject.name} already has '{attribute.displayName}'.");
            return false;
        }

        // Get the effect implementation
        IAttributeEffect effect = AttributeEffectFactory.GetEffect(attribute);
        if (effect == null)
        {
            Debug.LogError($"[AttributeController] No effect found for '{attribute.attributeID}'.");
            return false;
        }

        // Apply
        effect.Apply(gameObject, attribute);
        activeAttributes.Add(attribute);
        _liveEffects[attribute.attributeID] = effect;

        // Fire global event (VolatilityManager listens to this)
        GameEventManager.AttributeApplied(attribute, gameObject);

        Debug.Log($"[AttributeController] ✔ '{attribute.displayName}' applied to {gameObject.name}. Count: {AttributeCount}/{maxAttributes}");
        return true;
    }

    /// <summary>
    /// Remove an attribute from this object.
    /// Returns the removed AttributeSO (for adding to inventory), or null if failed.
    /// </summary>
    public AttributeSO RemoveAttribute(AttributeSO attribute)
    {
        if (attribute == null) return null;

        if (isLocked)
        {
            Debug.LogWarning($"[AttributeController] {gameObject.name} is LOCKED by the AI Director. Cannot remove '{attribute.displayName}'.");
            GameEventManager.NarratorSpeak("Nice try. That one stays.", 3f);
            return null;
        }

        if (!HasAttribute(attribute))
        {
            Debug.LogWarning($"[AttributeController] {gameObject.name} doesn't have '{attribute.displayName}'.");
            return null;
        }

        // Find and run the Remove logic
        if (_liveEffects.TryGetValue(attribute.attributeID, out IAttributeEffect effect))
        {
            effect.Remove(gameObject, attribute);
            _liveEffects.Remove(attribute.attributeID);
        }

        activeAttributes.Remove(attribute);

        // Fire global event
        GameEventManager.AttributeRemoved(attribute, gameObject);

        Debug.Log($"[AttributeController] ✔ '{attribute.displayName}' removed from {gameObject.name}. Count: {AttributeCount}/{maxAttributes}");
        return attribute;
    }

    /// <summary>Remove and return the first attribute (used for quick "Take").</summary>
    public AttributeSO RemoveFirst()
    {
        if (activeAttributes.Count == 0) return null;
        return RemoveAttribute(activeAttributes[0]);
    }

    /// <summary>Check if this object has a specific attribute.</summary>
    public bool HasAttribute(AttributeSO attribute)
    {
        foreach (var a in activeAttributes)
        {
            if (a.attributeID == attribute.attributeID) return true;
        }
        return false;
    }

    // ═══ AI Director Controls ═══════════════════════════════════════

    /// <summary>Lock this object so attributes can't be removed.</summary>
    public void Lock()
    {
        isLocked = true;
        Debug.Log($"[AttributeController] 🔒 {gameObject.name} LOCKED by AI Director.");
    }

    /// <summary>Unlock this object.</summary>
    public void Unlock()
    {
        isLocked = false;
        Debug.Log($"[AttributeController] 🔓 {gameObject.name} UNLOCKED.");
    }
}
