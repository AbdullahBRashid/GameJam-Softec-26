using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Makes the applied object act as a dynamic light source.
/// Turns all child renderers emissive and spawns a point light representing the glow.
/// </summary>
public class GlowAttribute : IAttributeEffect
{
    private Light _bodyLight;

    public void Apply(GameObject target, AttributeSO attribute)
    {
        Color glowColor = attribute.attributeColor != default && attribute.attributeColor.a > 0.1f 
                            ? attribute.attributeColor : new Color(0.1f, 0.8f, 1f); 

        // 1. Spawn a soft, atmospheric organic light ONLY if it is the Player
        if (target.CompareTag("Player"))
        {
            _bodyLight = target.AddComponent<Light>();
            _bodyLight.type = LightType.Point;         
            _bodyLight.color = glowColor;
            
            // Smoother, softer values for a lantern-like body glow
            _bodyLight.range = 25f; 
            _bodyLight.intensity = 10f;
            _bodyLight.shadows = LightShadows.Soft;
            _bodyLight.shadowStrength = 0.5f; // Softer shadows 
        }

        // 2. Force Enable Emission on ALL renderers (Player or Generic Objects)
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            foreach (var mat in rend.materials)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", glowColor * 2.5f); // Pop!
                }
            }
        }
        
        Debug.Log($"[GlowAttribute] Glow applied to {target.name}.");
    }

    public void Remove(GameObject target, AttributeSO attribute)
    {
        // 1. Destroy the light source
        if (_bodyLight != null)
        {
            Object.Destroy(_bodyLight);
        }

        // 2. Force Disable all emission! 
        // We purposely wipe it to black so that objects that start perfectly glowing in the Editor
        // don't awkwardly retain their editor value when the player explicitly removes the attribute!
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            foreach (var mat in rend.materials)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", Color.black);
                    mat.DisableKeyword("_EMISSION");
                }
            }
        }

        Debug.Log($"[GlowAttribute] Glow removed from {target.name}.");
    }
}
