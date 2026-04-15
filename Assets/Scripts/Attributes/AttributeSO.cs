using UnityEngine;

/// <summary>
/// Data container for a single Attribute.
/// Create instances via: Assets → Create → AI Benchmark → Attribute.
/// </summary>
[CreateAssetMenu(fileName = "New Attribute", menuName = "AI Benchmark/Attribute")]
public class AttributeSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique ID for this attribute (e.g., 'bouncy', 'frictionless', 'heavy').")]
    public string attributeID;

    [Tooltip("Display name shown in the UI.")]
    public string displayName;

    [Tooltip("Short description for the player.")]
    [TextArea(2, 4)]
    public string description;

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
    [Tooltip("How much volatility this attribute adds when applied.")]
    [Range(0f, 20f)]
    public float volatilityCost = 5f;

    [Tooltip("Can this attribute be locked by the AI Director?")]
    public bool canBeLocked = true;
}
