using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player's attribute inventory. Holds attributes the player has "taken"
/// from objects and can "apply" to other objects.
/// 
/// Tracks the source object and whether the attribute was a default
/// on that source, for correct volatility cost calculation.
/// 
/// Setup: Add to the Player GameObject alongside PlayerMovement.
/// </summary>
public class AttributeInventory : MonoBehaviour
{
    [Header("Inventory")]
    [Tooltip("Max number of attributes the player can carry.")]
    [SerializeField] private int maxCapacity = 5;

    [SerializeField] private List<AttributeSO> inventory = new List<AttributeSO>();

    // Track source info for each held attribute
    private readonly Dictionary<AttributeSO, GameObject> _sourceMap
        = new Dictionary<AttributeSO, GameObject>();
    private readonly Dictionary<AttributeSO, bool> _wasDefaultMap
        = new Dictionary<AttributeSO, bool>();

    private bool firstAttribute = true;

    // ── Public Properties ───────────────────────────────────────────
    public IReadOnlyList<AttributeSO> Items => inventory;
    public int Count => inventory.Count;
    public int MaxCapacity => maxCapacity;
    public bool IsFull => inventory.Count >= maxCapacity;

    // ═══ Public API ═════════════════════════════════════════════════

    /// <summary>
    /// Add an attribute to the player's inventory.
    /// Tracks source object and whether it was a default.
    /// Returns true if successful.
    /// </summary>
    public bool AddAttribute(AttributeSO attribute, GameObject source = null, bool wasDefault = false)
    {
        if (attribute == null) return false;

        if (IsFull)
        {
            Debug.LogWarning($"[AttributeInventory] Inventory full! ({maxCapacity} max)");
            GameEventManager.NarratorSpeak(NarratorLinesSO.Instance.GetLine("inventoryFull"), 3f);
            return false;
        }

        inventory.Add(attribute);

        if (source != null)
            _sourceMap[attribute] = source;

        _wasDefaultMap[attribute] = wasDefault;

        GameEventManager.AttributePickedUp(attribute);

        Debug.Log($"[AttributeInventory] ✔ Picked up '{attribute.displayName}' (wasDefault: {wasDefault}). Inventory: {Count}/{maxCapacity}");
        

        if (firstAttribute && inventory.Count == 1)
        {
            string actualText = NarratorLinesSO.Instance.GetLine("solidRemoved");
            GameEventManager.NarratorSpeak(actualText, 3f);
        }
        
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

        Debug.Log($"[AttributeInventory] ✔ Used '{attribute.displayName}'. Inventory: {Count}/{maxCapacity}");
        return attribute;
    }

    /// <summary>Remove attribute at a specific index.</summary>
    public AttributeSO RemoveAt(int index)
    {
        if (index < 0 || index >= inventory.Count) return null;
        return RemoveAttribute(inventory[index]);
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

    /// <summary>Get the source GameObject this attribute was taken from.</summary>
    public GameObject GetSource(AttributeSO attribute)
    {
        _sourceMap.TryGetValue(attribute, out GameObject source);
        return source;
    }

    /// <summary>Was this attribute a default on its source object?</summary>
    public bool WasDefault(AttributeSO attribute)
    {
        return _wasDefaultMap.TryGetValue(attribute, out bool v) && v;
    }

    /// <summary>Clear source/default tracking for an attribute (call after applying).</summary>
    public void ClearTracking(AttributeSO attribute)
    {
        _sourceMap.Remove(attribute);
        _wasDefaultMap.Remove(attribute);
    }

    // ═══ Checkpoints / Save State ═══════════════════════════════════

    /// <summary>
    /// Represents a saved state of the inventory.
    /// </summary>
    public class InventorySnapshot
    {
        public List<AttributeSO> Items = new List<AttributeSO>();
        public Dictionary<AttributeSO, GameObject> SourceMap = new Dictionary<AttributeSO, GameObject>();
        public Dictionary<AttributeSO, bool> WasDefaultMap = new Dictionary<AttributeSO, bool>();
    }

    /// <summary>
    /// Returns a deep copy of the current inventory state.
    /// </summary>
    public InventorySnapshot GetSnapshot()
    {
        return new InventorySnapshot
        {
            Items = new List<AttributeSO>(inventory),
            SourceMap = new Dictionary<AttributeSO, GameObject>(_sourceMap),
            WasDefaultMap = new Dictionary<AttributeSO, bool>(_wasDefaultMap)
        };
    }

    /// <summary>
    /// Restores the inventory to a previously saved snapshot.
    /// </summary>
    public void RestoreFromSnapshot(InventorySnapshot snapshot)
    {
        inventory.Clear();
        _sourceMap.Clear();
        _wasDefaultMap.Clear();

        if (snapshot != null)
        {
            inventory.AddRange(snapshot.Items);
            foreach (var kvp in snapshot.SourceMap) _sourceMap[kvp.Key] = kvp.Value;
            foreach (var kvp in snapshot.WasDefaultMap) _wasDefaultMap[kvp.Key] = kvp.Value;
        }

        Debug.Log($"[AttributeInventory] Inventory restored to checkpoint ({inventory.Count}/{maxCapacity}).");
    }

    /// <summary>
    /// Clears the entire inventory.
    /// </summary>
    public void ClearAll()
    {
        inventory.Clear();
        _sourceMap.Clear();
        _wasDefaultMap.Clear();
        Debug.Log("[AttributeInventory] Inventory cleared.");
    }
}
