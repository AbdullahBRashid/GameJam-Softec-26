using UnityEngine;

/// <summary>
/// Helper component for the Shattered Attribute.
/// Place this alongside the AttributeController on the specific glass wall,
/// and assign the two visual models in the inspector.
/// </summary>
public class ShatteredWallState : MonoBehaviour
{
    [Tooltip("The normal, solid glass wall model.")]
    public GameObject intactModel;

    [Tooltip("The broken glass wall model with a hole to walk through.")]
    public GameObject shatteredModel;

    private void Awake()
    {
        // Ensure initial state makes sense before any attributes apply
        if (intactModel != null) intactModel.SetActive(true);
        if (shatteredModel != null) shatteredModel.SetActive(false);
    }
}
