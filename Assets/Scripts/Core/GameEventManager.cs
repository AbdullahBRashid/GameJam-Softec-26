using System;
using UnityEngine;

/// <summary>
/// Central event bus using the Observer Pattern.
/// Both VolatilityManager and AIDirector publish events here.
/// PlayerController, EnvironmentManager, and UI listen.
/// </summary>
public static class GameEventManager
{
    // ─── Attribute Removal Events (Taking from objects) ─────────────
    /// <summary>Fired when a DEFAULT attribute is removed from its home object. Args: (AttributeSO, GameObject source)</summary>
    public static event Action<AttributeSO, GameObject> OnDefaultAttributeRemoved;

    /// <summary>Fired when a FOREIGN (non-default) attribute is removed from an object. Args: (AttributeSO, GameObject source)</summary>
    public static event Action<AttributeSO, GameObject> OnForeignAttributeRemoved;

    // ─── Attribute Apply Events (Placing onto objects) ──────────────
    /// <summary>Fired when an attribute is RESTORED to an object where it's a default. Args: (AttributeSO, GameObject target)</summary>
    public static event Action<AttributeSO, GameObject> OnAttributeRestoredToDefault;

    /// <summary>Fired when an attribute is applied to an object where it's NOT a default. Args: (AttributeSO, GameObject target)</summary>
    public static event Action<AttributeSO, GameObject> OnAttributeAppliedToForeign;

    // ─── General Attribute Events (for UI/audio, not volatility) ────
    /// <summary>Fired on ANY attribute apply (for VFX/audio hooks). Args: (AttributeSO, GameObject target)</summary>
    public static event Action<AttributeSO, GameObject> OnAttributeApplied;

    /// <summary>Fired on ANY attribute remove (for VFX/audio hooks). Args: (AttributeSO, GameObject source)</summary>
    public static event Action<AttributeSO, GameObject> OnAttributeRemoved;

    /// <summary>Fired when the player picks up an attribute into inventory. Args: (AttributeSO)</summary>
    public static event Action<AttributeSO> OnAttributePickedUp;

    /// <summary>Fired when the player uses an attribute from inventory. Args: (AttributeSO)</summary>
    public static event Action<AttributeSO> OnAttributeDropped;

    // ─── Volatility Events ──────────────────────────────────────────
    /// <summary>Fired every time volatility changes. Args: (float currentVolatility, float delta)</summary>
    public static event Action<float, float> OnVolatilityChanged;

    /// <summary>Fired when a mechanical bug is triggered. Args: (MechanicalBugType)</summary>
    public static event Action<MechanicalBugType> OnMechanicalBugTriggered;

    /// <summary>Fired when a mechanical bug ends. Args: (MechanicalBugType)</summary>
    public static event Action<MechanicalBugType> OnMechanicalBugEnded;

    // ─── AI Director Events ─────────────────────────────────────────
    /// <summary>Fired when the AI Director triggers a sabotage. Args: (SabotageType, string narratorText)</summary>
    public static event Action<SabotageType, string> OnSabotageTriggered;

    /// <summary>Fired when a level milestone is reached. Args: (int levelIndex)</summary>
    public static event Action<int> OnLevelMilestoneReached;

    // ─── Narrative Events ───────────────────────────────────────────
    /// <summary>Fired to display narrator text and play audio. Args: (string text, AudioClip clip, float duration)</summary>
    public static event Action<string, AudioClip, float> OnNarratorSpeak;

    // ─── Player State Events ────────────────────────────────────────
    /// <summary>Fired when player controls should be inverted. Args: (bool isInverted)</summary>
    public static event Action<bool> OnControlsInverted;

    /// <summary>Fired when gravity should be reversed. Args: (bool isReversed)</summary>
    public static event Action<bool> OnGravityReversed;

    /// <summary>Fired when gravity drift is toggled. Args: (bool isActive)</summary>
    public static event Action<bool> OnGravityDrift;

    /// <summary>Fired when inertia corruption (ice rink) is toggled. Args: (bool isActive)</summary>
    public static event Action<bool> OnInertiaCorruption;

    // ─── Health Events ──────────────────────────────────────────────
    /// <summary>Fired when the player's health changes. Args: (int currentHealth, int maxHealth)</summary>
    public static event Action<int, int> OnPlayerHealthChanged;

    /// <summary>Fired when the player dies.</summary>
    public static event Action OnPlayerDied;

    // ─── Time Events ────────────────────────────────────────────────
    public static event Action<bool> OnTimeStateChanged;
    public static bool IsTimeRunning { get; private set; } = true;

    // ─── Stage Events ───────────────────────────────────────────────
    /// <summary>Fired when the player enters a stage zone. Args: (int stageIndex, string stageName)</summary>
    public static event Action<int, string> OnStageEntered;

    /// <summary>Fired when a stage is reset by the player.</summary>
    public static event Action OnStageReset;

    // ═══ Invoke Methods ═════════════════════════════════════════════

    // -- Volatility-specific removal events --
    public static void DefaultAttributeRemoved(AttributeSO attribute, GameObject source)
    {
        OnDefaultAttributeRemoved?.Invoke(attribute, source);
    }

    public static void ForeignAttributeRemoved(AttributeSO attribute, GameObject source)
    {
        OnForeignAttributeRemoved?.Invoke(attribute, source);
    }

    // -- Volatility-specific apply events --
    public static void AttributeRestoredToDefault(AttributeSO attribute, GameObject target)
    {
        OnAttributeRestoredToDefault?.Invoke(attribute, target);
    }

    public static void AttributeAppliedToForeign(AttributeSO attribute, GameObject target)
    {
        OnAttributeAppliedToForeign?.Invoke(attribute, target);
    }

    // -- General (for VFX/audio, not volatility) --
    public static void AttributeApplied(AttributeSO attribute, GameObject target)
    {
        OnAttributeApplied?.Invoke(attribute, target);
    }

    public static void AttributeRemoved(AttributeSO attribute, GameObject target)
    {
        OnAttributeRemoved?.Invoke(attribute, target);
    }

    public static void AttributePickedUp(AttributeSO attribute)
    {
        OnAttributePickedUp?.Invoke(attribute);
    }

    public static void AttributeDropped(AttributeSO attribute)
    {
        OnAttributeDropped?.Invoke(attribute);
    }

    public static void VolatilityChanged(float current, float delta)
    {
        OnVolatilityChanged?.Invoke(current, delta);
    }

    public static void MechanicalBugTriggered(MechanicalBugType bugType)
    {
        OnMechanicalBugTriggered?.Invoke(bugType);
    }

    public static void MechanicalBugEnded(MechanicalBugType bugType)
    {
        OnMechanicalBugEnded?.Invoke(bugType);
    }

    public static void SabotageTriggered(SabotageType sabotageType, string narratorText)
    {
        OnSabotageTriggered?.Invoke(sabotageType, narratorText);
    }

    public static void LevelMilestoneReached(int levelIndex)
    {
        OnLevelMilestoneReached?.Invoke(levelIndex);
    }

    public static void NarratorSpeak(string messageName, float duration = 4f)
    {
        string actualText = NarratorLinesSO.Instance.GetLine(messageName);
        if (string.IsNullOrEmpty(actualText) || actualText.StartsWith("[Missing Line"))
            actualText = messageName; // Fallback in case they pass raw text or missing

        AudioClip clip = Resources.Load<AudioClip>($"Audio/Narrator Voicelines/{messageName}");
#if UNITY_EDITOR
        if (clip == null) clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Narrator Voicelines/{messageName}.wav");
        if (clip == null) clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Narrator Voicelines/{messageName}.mp3");
#endif

        OnNarratorSpeak?.Invoke(actualText, clip, duration);
    }

    public static void ControlsInverted(bool isInverted)
    {
        OnControlsInverted?.Invoke(isInverted);
    }

    public static void GravityReversed(bool isReversed)
    {
        OnGravityReversed?.Invoke(isReversed);
    }

    public static void GravityDrift(bool isActive)
    {
        OnGravityDrift?.Invoke(isActive);
    }

    public static void InertiaCorruption(bool isActive)
    {
        OnInertiaCorruption?.Invoke(isActive);
    }

    public static void PlayerHealthChanged(int current, int max)
    {
        OnPlayerHealthChanged?.Invoke(current, max);
    }

    public static void PlayerDied()
    {
        OnPlayerDied?.Invoke();
    }

    public static void SetTimeState(bool isRunning)
    {
        IsTimeRunning = isRunning;
        OnTimeStateChanged?.Invoke(isRunning);
    }

    public static void StageEntered(int stageIndex, string stageName)
    {
        OnStageEntered?.Invoke(stageIndex, stageName);
    }

    public static void StageReset()
    {
        OnStageReset?.Invoke();
    }
}

// ─── Enums ──────────────────────────────────────────────────────────

/// <summary>What kind of interactable object this is. Used for attribute compatibility.</summary>
public enum ObjectCategory
{
    Any,            // Works on everything
    PhysicsObject,  // Cubes, balls, movable objects
    Door,           // Doors, gates
    Light,          // Light fixtures, lamps
    Platform,       // Moving platforms, floors
    Player          // The player (future use)
}

/// <summary>Types of mechanical bugs the Volatility system can trigger.</summary>
public enum MechanicalBugType
{
    InvertedControls,
    ReverseGravity,
    GravityDrift,
    InertiaCorruption,
    GravityToggleOnJump,
    MapDecay,
    CameraShake,
    InputLag
}

/// <summary>Types of scripted sabotage the AI Director can trigger.</summary>
public enum SabotageType
{
    LockAttribute,
    DimLights,
    MapDecay,
    DisableFlashlight,
    SpawnObstacles,
    CorruptUI
}
