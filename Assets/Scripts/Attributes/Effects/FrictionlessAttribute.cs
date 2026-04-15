using UnityEngine;

/// <summary>
/// Frictionless attribute effect — makes the target's surface
/// have zero friction, causing objects to slide on it.
/// </summary>
public class FrictionlessAttribute : IAttributeEffect
{
    private PhysicsMaterial _originalMaterial;
    private Color _originalColor;

    public void Apply(GameObject target, AttributeSO data)
    {
        // ── Physics Material Swap ──
        Collider col = target.GetComponent<Collider>();
        if (col != null)
        {
            _originalMaterial = col.material;

            if (data.physicsMaterial != null)
            {
                col.material = data.physicsMaterial;
            }
            else
            {
                // Fallback: create a frictionless material on the fly
                PhysicsMaterial slickMat = new PhysicsMaterial("Frictionless_Runtime");
                slickMat.dynamicFriction = 0f;
                slickMat.staticFriction = 0f;
                slickMat.frictionCombine = PhysicsMaterialCombine.Minimum;
                slickMat.bounciness = 0f;
                col.material = slickMat;
            }
        }

        // ── Visual Feedback ──
        Renderer rend = target.GetComponent<Renderer>();
        if (rend != null)
        {
            _originalColor = rend.material.color;
            rend.material.color = data.attributeColor;
        }

        // ── VFX ──
        if (data.vfxPrefab != null)
        {
            Object.Instantiate(data.vfxPrefab, target.transform.position, Quaternion.identity);
        }

        // ── Audio ──
        if (data.applySound != null)
        {
            AudioSource.PlayClipAtPoint(data.applySound, target.transform.position);
        }

        Debug.Log($"[FrictionlessAttribute] Applied to {target.name}");
    }

    public void Remove(GameObject target, AttributeSO data)
    {
        Collider col = target.GetComponent<Collider>();
        if (col != null && _originalMaterial != null)
        {
            col.material = _originalMaterial;
        }

        Renderer rend = target.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = _originalColor;
        }

        if (data.removeSound != null)
        {
            AudioSource.PlayClipAtPoint(data.removeSound, target.transform.position);
        }

        Debug.Log($"[FrictionlessAttribute] Removed from {target.name}");
    }
}
