using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Place this component on any GameObject that can hold attributes.
/// Manages active attributes, default attributes, compatibility checks,
/// and fires the correct volatility events.
/// 
/// Setup: Add to any interactable object. Set the category and default attributes.
/// </summary>
public class AttributeController : MonoBehaviour
{
    [Header("Object Identity")]
    [Tooltip("What type of object this is (for attribute compatibility).")]
    [SerializeField] private ObjectCategory category = ObjectCategory.PhysicsObject;

    [Header("Default Attributes")]
    [Tooltip("Attributes this object spawns with. These are its 'home' attributes for volatility tracking.")]
    [SerializeField] private List<AttributeSO> defaultAttributes = new List<AttributeSO>();

    [Header("Current Attributes")]
    [Tooltip("Attributes currently active on this object (auto-populated from defaults at Start).")]
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

    // Track which default attributes have been removed (for volatility cost logic)
    private readonly HashSet<string> _missingDefaults = new HashSet<string>();

    // ── Public Properties ───────────────────────────────────────────
    public ObjectCategory Category => category;
    public IReadOnlyList<AttributeSO> ActiveAttributes => activeAttributes;
    public IReadOnlyList<AttributeSO> DefaultAttributes => defaultAttributes;
    public int AttributeCount => activeAttributes.Count;
    public bool IsFull => activeAttributes.Count >= maxAttributes;
    public bool IsLocked => isLocked;

    // ═══ Lifecycle ══════════════════════════════════════════════════

    private void Start()
    {
        InitializeDefaults();
    }

    /// <summary>
    /// Apply all default attribute effects at scene start.
    /// This makes objects start with their attributes visually/physically active.
    /// Does NOT fire volatility events (these are the natural state).
    /// </summary>
    private void InitializeDefaults()
    {
        // Copy the default list into active (they should already be there from Inspector,
        // but ensure consistency)
        activeAttributes = new List<AttributeSO>(defaultAttributes);
        _missingDefaults.Clear();

        foreach (var attr in defaultAttributes)
        {
            if (attr == null) continue;

            IAttributeEffect effect = AttributeEffectFactory.GetEffect(attr);
            if (effect != null)
            {
                effect.Apply(gameObject, attr);
                _liveEffects[attr.attributeID] = effect;
            }
        }

        Debug.Log($"[AttributeController] {gameObject.name} initialized with {defaultAttributes.Count} default attributes.");
    }

    // ═══ Query Methods ══════════════════════════════════════════════

    /// <summary>Is this attribute one of this object's defaults?</summary>
    public bool IsDefaultAttribute(AttributeSO attribute)
    {
        if (attribute == null) return false;
        foreach (var d in defaultAttributes)
        {
            if (d != null && d.attributeID == attribute.attributeID) return true;
        }
        return false;
    }

    /// <summary>Has a default attribute been removed from this object?</summary>
    public bool IsMissingDefault(AttributeSO attribute)
    {
        return attribute != null && _missingDefaults.Contains(attribute.attributeID);
    }

    /// <summary>Check if this object has a specific attribute currently active.</summary>
    public bool HasAttribute(AttributeSO attribute)
    {
        foreach (var a in activeAttributes)
        {
            if (a.attributeID == attribute.attributeID) return true;
        }
        return false;
    }

    /// <summary>
    /// Check if this object can accept a given attribute.
    /// Checks: compatibility, capacity, no duplicates.
    /// </summary>
    public bool CanAccept(AttributeSO attribute)
    {
        if (attribute == null) return false;
        if (IsFull) return false;
        if (HasAttribute(attribute)) return false;
        if (!attribute.IsCompatibleWith(category)) return false;
        return true;
    }

    // ═══ Apply / Remove API ═════════════════════════════════════════

    /// <summary>
    /// Apply an attribute to this object.
    /// Fires the correct volatility event based on whether it's a default restoration or foreign apply.
    /// Returns true if successful.
    /// </summary>
    public bool ApplyAttribute(AttributeSO attribute)
    {
        if (attribute == null) return false;

        // Check compatibility
        if (!attribute.IsCompatibleWith(category))
        {
            Debug.LogWarning($"[AttributeController] '{attribute.displayName}' is not compatible with {category} object '{gameObject.name}'.");
            return false;
        }

        // Check capacity
        if (IsFull)
        {
            Debug.LogWarning($"[AttributeController] {gameObject.name} is full ({maxAttributes} max).");
            return false;
        }

        // No duplicates
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

        // Apply the effect
        effect.Apply(gameObject, attribute);
        activeAttributes.Add(attribute);
        _liveEffects[attribute.attributeID] = effect;

        // ── Fire the correct volatility event ──
        bool isRestoringDefault = IsDefaultAttribute(attribute) && _missingDefaults.Contains(attribute.attributeID);

        if (isRestoringDefault)
        {
            _missingDefaults.Remove(attribute.attributeID);
            GameEventManager.AttributeRestoredToDefault(attribute, gameObject);
            Debug.Log($"[AttributeController] ✔ '{attribute.displayName}' RESTORED to default on {gameObject.name}. (−full cost)");
        }
        else
        {
            GameEventManager.AttributeAppliedToForeign(attribute, gameObject);
            Debug.Log($"[AttributeController] ✔ '{attribute.displayName}' applied as FOREIGN to {gameObject.name}. (−half cost)");
        }

        // General event (for VFX/audio hooks)
        GameEventManager.AttributeApplied(attribute, gameObject);

        return true;
    }

    /// <summary>
    /// Remove an attribute from this object.
    /// Fires the correct volatility event based on whether it was a default attribute.
    /// Returns the removed AttributeSO, or null if failed.
    /// </summary>
    public AttributeSO RemoveAttribute(AttributeSO attribute)
    {
        if (attribute == null) return null;

        if (isLocked)
        {
            Debug.LogWarning($"[AttributeController] {gameObject.name} is LOCKED. Cannot remove '{attribute.displayName}'.");
            GameEventManager.NarratorSpeak("Nice try. That one stays.", 3f);
            return null;
        }

        if (!HasAttribute(attribute))
        {
            Debug.LogWarning($"[AttributeController] {gameObject.name} doesn't have '{attribute.displayName}'.");
            return null;
        }

        // Run the Remove effect
        if (_liveEffects.TryGetValue(attribute.attributeID, out IAttributeEffect effect))
        {
            effect.Remove(gameObject, attribute);
            _liveEffects.Remove(attribute.attributeID);
        }

        activeAttributes.Remove(attribute);

        // ── Fire the correct volatility event ──
        bool wasDefault = IsDefaultAttribute(attribute);

        if (wasDefault)
        {
            _missingDefaults.Add(attribute.attributeID);
            GameEventManager.DefaultAttributeRemoved(attribute, gameObject);
            Debug.Log($"[AttributeController] ✔ DEFAULT '{attribute.displayName}' removed from {gameObject.name}. (+full cost)");
        }
        else
        {
            GameEventManager.ForeignAttributeRemoved(attribute, gameObject);
            Debug.Log($"[AttributeController] ✔ FOREIGN '{attribute.displayName}' removed from {gameObject.name}. (+0 cost)");
        }

        // General event (for VFX/audio hooks)
        GameEventManager.AttributeRemoved(attribute, gameObject);

        return attribute;
    }

    /// <summary>Remove and return the first attribute (used for quick "Take").</summary>
    public AttributeSO RemoveFirst()
    {
        if (activeAttributes.Count == 0) return null;
        return RemoveAttribute(activeAttributes[0]);
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
