using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Dynamically scales post-processing screen effects (Vignette, Chromatic Aberration, Lens Distortion)
/// based on the current Volatility level in the game.
/// 
/// Setup: Simply attach this script to any empty GameObject in the scene (or the VolatilityManager itself).
/// It will dynamically generate a Global Volume and override the screen effects automatically.
/// </summary>
public class VolatilityVFX : MonoBehaviour
{
    private Volume _volume;
    private Vignette _vignette;
    private ChromaticAberration _ca;
    private LensDistortion _ld;

    private void Start()
    {
        // Programmatically generate a post-processing volume
        _volume = gameObject.AddComponent<Volume>();
        _volume.isGlobal = true;
        _volume.weight = 1f;
        _volume.priority = 100; // High priority so it overrides the standard environment look

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        _volume.profile = profile;

        // 1. Vignette (Darkening / Reddening the edges of the screen)
        _vignette = profile.Add<Vignette>();
        _vignette.active = true;
        _vignette.intensity.overrideState = true;
        _vignette.intensity.value = 0f;
        _vignette.color.overrideState = true;
        _vignette.color.value = new Color(0.1f, 0f, 0f); // Bleeding dark red

        // 2. Chromatic Aberration (Color splitting on edges to simulate a broken camera lens)
        _ca = profile.Add<ChromaticAberration>();
        _ca.active = true;
        _ca.intensity.overrideState = true;
        _ca.intensity.value = 0f;

        // 3. Lens Distortion (Slight fisheye warping of the screen)
        _ld = profile.Add<LensDistortion>();
        _ld.active = true;
        _ld.intensity.overrideState = true;
        _ld.intensity.value = 0f;

        // Sync initially
        if (VolatilityManager.Instance != null)
        {
            HandleVolatilityChanged(VolatilityManager.Instance.Volatility, 0f);
        }

        GameEventManager.OnVolatilityChanged += HandleVolatilityChanged;
    }

    private void HandleVolatilityChanged(float current, float delta)
    {
        if (VolatilityManager.Instance == null) return;

        float maxV = VolatilityManager.Instance.MaxVolatility;
        float normalized = Mathf.Clamp01(current / maxV);
        
        // Non-linear scaling: The effects aggressively kick in only after reaching 50% Volatility
        float factor = Mathf.Clamp01((normalized - 0.4f) / 0.6f); 

        // Apply visual sickness
        if (_vignette != null) 
            _vignette.intensity.value = Mathf.Lerp(0f, 0.5f, factor);
            
        if (_ca != null) 
            _ca.intensity.value = Mathf.Lerp(0f, 1f, factor);
            
        if (_ld != null) 
            _ld.intensity.value = Mathf.Lerp(0f, -0.4f, factor); // Negative pulls the edges inward
    }

    private void OnDestroy()
    {
        GameEventManager.OnVolatilityChanged -= HandleVolatilityChanged;
    }
}
