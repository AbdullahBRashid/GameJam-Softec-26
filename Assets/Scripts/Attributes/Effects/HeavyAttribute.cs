using UnityEngine;

/// <summary>
/// Heavy attribute effect — increases the Rigidbody mass dramatically
/// and reduces drag, making the object sink / resist forces.
/// </summary>
public class HeavyAttribute : IAttributeEffect
{
    private float _originalMass;
    private float _originalDrag;
    private Color _originalColor;

    public void Apply(GameObject target, AttributeSO data)
    {
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null)
        {
            _originalMass = rb.mass;
            _originalDrag = rb.linearDamping;
            rb.mass *= 10f;
            rb.linearDamping = 0f;
        }

        // Physics material (optional — heavy objects might also have low bounce)
        Collider col = target.GetComponent<Collider>();
        if (col != null && data.physicsMaterial != null)
        {
            col.material = data.physicsMaterial;
        }

        // Visual tint
        Renderer rend = target.GetComponent<Renderer>();
        if (rend != null)
        {
            _originalColor = rend.material.color;
            rend.material.color = data.attributeColor;
        }

        if (data.vfxPrefab != null)
            Object.Instantiate(data.vfxPrefab, target.transform.position, Quaternion.identity);

        if (data.applySound != null)
            AudioSource.PlayClipAtPoint(data.applySound, target.transform.position);

        Debug.Log($"[HeavyAttribute] Applied to {target.name} — mass is now {rb?.mass}");
    }

    public void Remove(GameObject target, AttributeSO data)
    {
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = _originalMass;
            rb.linearDamping = _originalDrag;
        }

        Renderer rend = target.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = _originalColor;
        }

        if (data.removeSound != null)
            AudioSource.PlayClipAtPoint(data.removeSound, target.transform.position);

        Debug.Log($"[HeavyAttribute] Removed from {target.name}");
    }
}
