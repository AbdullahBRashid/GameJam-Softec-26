using System.Collections.Generic;

/// <summary>
/// Factory that maps AttributeSO IDs to their IAttributeEffect implementations.
/// Add new attribute types here as you create them.
/// </summary>
public static class AttributeEffectFactory
{
    /// <summary>
    /// Returns a new IAttributeEffect implementation for the given AttributeSO.
    /// Returns null if the attribute ID has no registered effect.
    /// </summary>
    public static IAttributeEffect GetEffect(AttributeSO attribute)
    {
        if (attribute == null) return null;

        string id = attribute.attributeID.ToLower().Trim();

        // ── Register new attribute types here ──
        IAttributeEffect effect = id switch
        {
            "bouncy"        => new BouncyAttribute(),
            "frictionless"  => new FrictionlessAttribute(),
            "heavy"         => new HeavyAttribute(),
            "locked"        => new LockedAttribute(),
            "temporal"      => new TemporalAttribute(),
            "solid"         => new SolidAttribute(),
            "key"           => new KeyAttribute(),
            "glow"          => new GlowAttribute(),
            "shattered"     => new ShatteredAttribute(),
            "float"         => new FloatAttribute(),
            // Add more here as you create new effects:
            // "magnetic"   => new MagneticAttribute(),
            // "explosive"  => new ExplosiveAttribute(),
            // "dimmed"     => new DimmedAttribute(),
            _               => null
        };

        if (effect == null)
        {
            UnityEngine.Debug.LogWarning($"[AttributeEffectFactory] No effect registered for attribute ID: '{id}'");
        }

        return effect;
    }
}
