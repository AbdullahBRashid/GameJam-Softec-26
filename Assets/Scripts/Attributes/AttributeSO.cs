using UnityEngine;

/// <summary>
/// Data container for a single Attribute.
/// Create instances via: Assets → Create → AI Benchmark → Attribute.
/// </summary>
[CreateAssetMenu(fileName = "New Attribute", menuName = "AI Benchmark/Attribute")]
public class AttributeSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique ID for this attribute (e.g., 'bouncy', 'frictionless', 'locked').")]
    public string attributeID;

    [Tooltip("Display name shown in the UI.")]
    public string displayName;

    [Tooltip("Short description for the player.")]
    [TextArea(2, 4)]
    public string description;

    [Header("Compatibility")]
    [Tooltip("Which object categories this attribute can be applied to. Include 'Any' for universal.")]
    public ObjectCategory[] compatibleWith = { ObjectCategory.Any };

    [Tooltip("Is this a physics-based attribute (modifies PhysicMaterial/Rigidbody)?")]
    public bool isPhysicsAttribute = true;

    [Header("Visuals")]
    [Tooltip("Color tint applied to objects that have this attribute.")]
    public Color attributeColor = Color.white;

    [Tooltip("Icon for the inventory UI (optional).")]
    public Sprite icon;

    [Tooltip("Particle effect prefab spawned when this attribute is applied/removed (optional).")]
    public GameObject vfxPrefab;

    [Header("Audio")]
    [Tooltip("Sound played when this attribute is applied.")]
    public AudioClip applySound;

    [Tooltip("Sound played when this attribute is removed.")]
    public AudioClip removeSound;

    [Header("Physics")]
    [Tooltip("Physic Material to apply to the target's collider (for Bouncy, Frictionless, etc.).")]
    public PhysicsMaterial physicsMaterial;

    [Header("Volatility")]
    [Tooltip("Volatility cost when this attribute is displaced from its default object.")]
    [Range(0f, 30f)]
    public float volatilityCost = 5f;

    [Tooltip("Can this attribute be locked by the AI Director?")]
    public bool canBeLocked = true;

    // ═══ Helper Methods ═════════════════════════════════════════════

    /// <summary>
    /// Check if this attribute is compatible with a given object category.
    /// </summary>
    public bool IsCompatibleWith(ObjectCategory category)
    {
        if (compatibleWith == null || compatibleWith.Length == 0) return true;

        foreach (var cat in compatibleWith)
        {
            if (cat == ObjectCategory.Any || cat == category) return true;
        }
        return false;
    }
}
