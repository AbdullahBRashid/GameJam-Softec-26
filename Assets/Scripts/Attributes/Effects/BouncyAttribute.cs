using UnityEngine;

/// <summary>
/// Bouncy attribute effect — swaps the target's collider PhysicMaterial
/// to a high-bounce material and adds a subtle upward impulse on contact.
/// </summary>
public class BouncyAttribute : IAttributeEffect
{
    // Cache the original material so we can restore it on Remove.
    private PhysicsMaterial _originalMaterial;
    private Color _originalColor;

    public void Apply(GameObject target, AttributeSO data)
    {
        // ── Physics Material Swap ──
        Collider col = target.GetComponent<Collider>();
        if (col != null)
        {
            _originalMaterial = col.material; // cache original

            if (data.physicsMaterial != null)
            {
                col.material = data.physicsMaterial;
            }
            else
            {
                // Fallback: create a bouncy material on the fly
                PhysicsMaterial bouncyMat = new PhysicsMaterial("Bouncy_Runtime");
                bouncyMat.bounciness = 0.95f;
                bouncyMat.bounceCombine = PhysicsMaterialCombine.Maximum;
                bouncyMat.dynamicFriction = 0.1f;
                bouncyMat.staticFriction = 0.1f;
                col.material = bouncyMat;
            }
        }

        // ── Visual Feedback: Tint the object ──
        Renderer rend = target.GetComponent<Renderer>();
        if (rend != null)
        {
            _originalColor = rend.material.color;
            rend.material.color = data.attributeColor;
        }

        // ── VFX spawn ──
        if (data.vfxPrefab != null)
        {
            Object.Instantiate(data.vfxPrefab, target.transform.position, Quaternion.identity);
        }

        // ── Audio ──
        if (data.applySound != null)
        {
            AudioSource.PlayClipAtPoint(data.applySound, target.transform.position);
        }

        Debug.Log($"[BouncyAttribute] Applied to {target.name}");
    }

    public void Remove(GameObject target, AttributeSO data)
    {
        // ── Restore Original Physics Material ──
        Collider col = target.GetComponent<Collider>();
        if (col != null && _originalMaterial != null)
        {
            col.material = _originalMaterial;
        }

        // ── Restore Original Color ──
        Renderer rend = target.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = _originalColor;
        }

        // ── Audio ──
        if (data.removeSound != null)
        {
            AudioSource.PlayClipAtPoint(data.removeSound, target.transform.position);
        }

        Debug.Log($"[BouncyAttribute] Removed from {target.name}");
    }
}
