using System.Collections.Generic;

/// <summary>
/// Factory that maps AttributeSO IDs to their IAttributeEffect implementations.
/// Add new attribute types here as you create them.
/// </summary>
public static class AttributeEffectFactory
{
    // Cache instances so we don't re-create every frame
    private static readonly Dictionary<string, IAttributeEffect> _effectCache
        = new Dictionary<string, IAttributeEffect>();

    /// <summary>
    /// Returns the IAttributeEffect implementation for the given AttributeSO.
    /// Returns null if the attribute ID has no registered effect.
    /// </summary>
    public static IAttributeEffect GetEffect(AttributeSO attribute)
    {
        if (attribute == null) return null;

        string id = attribute.attributeID.ToLower().Trim();

        // Return cached instance if we already created one
        if (_effectCache.TryGetValue(id, out IAttributeEffect cached))
            return cached;

        // ── Register new attribute types here ──
        IAttributeEffect effect = id switch
        {
            "bouncy"       => new BouncyAttribute(),
            "frictionless"  => new FrictionlessAttribute(),
            "heavy"        => new HeavyAttribute(),
            // Add more here as you create new effects:
            // "magnetic"  => new MagneticAttribute(),
            // "explosive" => new ExplosiveAttribute(),
            _              => null
        };

        if (effect != null)
        {
            _effectCache[id] = effect;
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[AttributeEffectFactory] No effect registered for attribute ID: '{id}'");
        }

        return effect;
    }

    /// <summary>Clear the cache (e.g., on scene reload).</summary>
    public static void ClearCache() => _effectCache.Clear();
}
