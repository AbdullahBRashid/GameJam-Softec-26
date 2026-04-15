using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player's attribute inventory. Holds attributes the player has "taken"
/// from objects and can "apply" to other objects.
/// 
/// High inventory count increases the Volatility Meter via GameEventManager.
/// 
/// Setup: Add to the Player GameObject alongside PlayerMovement.
/// </summary>
public class AttributeInventory : MonoBehaviour
{
    [Header("Inventory")]
    [Tooltip("Max number of attributes the player can carry.")]
    [SerializeField] private int maxCapacity = 3;

    [SerializeField] private List<AttributeSO> inventory = new List<AttributeSO>();

    // ── Public Properties ───────────────────────────────────────────
    public IReadOnlyList<AttributeSO> Items => inventory;
    public int Count => inventory.Count;
    public int MaxCapacity => maxCapacity;
    public bool IsFull => inventory.Count >= maxCapacity;

    // ═══ Public API ═════════════════════════════════════════════════

    /// <summary>
    /// Add an attribute to the player's inventory.
    /// Returns true if successful.
    /// </summary>
    public bool AddAttribute(AttributeSO attribute)
    {
        if (attribute == null) return false;

        if (IsFull)
        {
            Debug.LogWarning($"[AttributeInventory] Inventory full! ({maxCapacity} max)");
            GameEventManager.NarratorSpeak("Your pockets are full. Drop something first.", 3f);
            return false;
        }

        inventory.Add(attribute);
        GameEventManager.AttributePickedUp(attribute);

        Debug.Log($"[AttributeInventory] ✔ Picked up '{attribute.displayName}'. Inventory: {Count}/{maxCapacity}");
        return true;
    }

    /// <summary>
    /// Remove a specific attribute from inventory.
    /// Returns the removed AttributeSO, or null if not found.
    /// </summary>
    public AttributeSO RemoveAttribute(AttributeSO attribute)
    {
        if (attribute == null || !inventory.Contains(attribute)) return null;

        inventory.Remove(attribute);
        GameEventManager.AttributeDropped(attribute);

        Debug.Log($"[AttributeInventory] ✔ Dropped '{attribute.displayName}'. Inventory: {Count}/{maxCapacity}");
        return attribute;
    }

    /// <summary>
    /// Remove attribute at a specific index (used by UI slot selection).
    /// </summary>
    public AttributeSO RemoveAt(int index)
    {
        if (index < 0 || index >= inventory.Count) return null;
        AttributeSO attr = inventory[index];
        return RemoveAttribute(attr);
    }

    /// <summary>Get the currently selected attribute (first in list).</summary>
    public AttributeSO GetSelected()
    {
        return inventory.Count > 0 ? inventory[0] : null;
    }

    /// <summary>Get attribute at a specific slot index.</summary>
    public AttributeSO GetAt(int index)
    {
        if (index < 0 || index >= inventory.Count) return null;
        return inventory[index];
    }

    /// <summary>Check if the player has a specific attribute.</summary>
    public bool HasAttribute(AttributeSO attribute)
    {
        foreach (var a in inventory)
        {
            if (a.attributeID == attribute.attributeID) return true;
        }
        return false;
    }
}
