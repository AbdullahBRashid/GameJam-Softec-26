using UnityEngine;

/// <summary>
/// Locked attribute effect — prevents doors/gates from opening.
/// Non-physics attribute: doesn't modify PhysicMaterial, instead
/// enables/disables door-opening behavior.
/// </summary>
public class LockedAttribute : IAttributeEffect
{
    private Color _originalColor;

    public void Apply(GameObject target, AttributeSO data)
    {
        // ── Disable any door-opening component ──
        // Look for common door controller scripts
        var doorController = target.GetComponent<MonoBehaviour>();
        // We search for any component with "Door" or "Open" in its name
        foreach (var comp in target.GetComponents<MonoBehaviour>())
        {
            string typeName = comp.GetType().Name.ToLower();
            if (typeName.Contains("door") || typeName.Contains("open") || typeName.Contains("gate"))
            {
                if (comp != target.GetComponent<AttributeController>()) // Don't disable ourselves
                {
                    comp.enabled = false;
                    Debug.Log($"[LockedAttribute] Disabled '{comp.GetType().Name}' on {target.name}");
                }
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
            Object.Instantiate(data.vfxPrefab, target.transform.position, Quaternion.identity);

        // ── Audio ──
        if (data.applySound != null)
            AudioSource.PlayClipAtPoint(data.applySound, target.transform.position);

        Debug.Log($"[LockedAttribute] 🔒 Applied to {target.name} — door is locked");
    }

    public void Remove(GameObject target, AttributeSO data)
    {
        // ── Re-enable door components ──
        foreach (var comp in target.GetComponents<MonoBehaviour>())
        {
            string typeName = comp.GetType().Name.ToLower();
            if (typeName.Contains("door") || typeName.Contains("open") || typeName.Contains("gate"))
            {
                if (comp != target.GetComponent<AttributeController>())
                {
                    comp.enabled = true;
                    Debug.Log($"[LockedAttribute] Re-enabled '{comp.GetType().Name}' on {target.name}");
                }
            }
        }

        // ── Restore Visual ──
        Renderer rend = target.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = _originalColor;
        }

        // ── Audio ──
        if (data.removeSound != null)
            AudioSource.PlayClipAtPoint(data.removeSound, target.transform.position);

        Debug.Log($"[LockedAttribute] 🔓 Removed from {target.name} — door is unlocked");
    }
}
