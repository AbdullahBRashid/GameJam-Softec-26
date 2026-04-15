using System;
using UnityEngine;

/// <summary>
/// Central event bus using the Observer Pattern.
/// Both VolatilityManager and AIDirector publish events here.
/// PlayerController, EnvironmentManager, and UI listen.
/// </summary>
public static class GameEventManager
{
    // ─── Attribute Events ───────────────────────────────────────────
    /// <summary>Fired when an attribute is applied to an object. Args: (AttributeSO, GameObject target)</summary>
    public static event Action<AttributeSO, GameObject> OnAttributeApplied;

    /// <summary>Fired when an attribute is removed from an object. Args: (AttributeSO, GameObject target)</summary>
    public static event Action<AttributeSO, GameObject> OnAttributeRemoved;

    /// <summary>Fired when the player picks up an attribute into inventory. Args: (AttributeSO)</summary>
    public static event Action<AttributeSO> OnAttributePickedUp;

    /// <summary>Fired when the player drops an attribute from inventory. Args: (AttributeSO)</summary>
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
    /// <summary>Fired to display narrator text. Args: (string message, float duration)</summary>
    public static event Action<string, float> OnNarratorSpeak;

    // ─── Player State Events ────────────────────────────────────────
    /// <summary>Fired when player controls should be inverted. Args: (bool isInverted)</summary>
    public static event Action<bool> OnControlsInverted;

    /// <summary>Fired when gravity should be reversed. Args: (bool isReversed)</summary>
    public static event Action<bool> OnGravityReversed;

    // ═══ Invoke Methods ═════════════════════════════════════════════
    // Each event gets a safe-invoke wrapper to prevent null-ref issues.

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

    public static void NarratorSpeak(string message, float duration = 4f)
    {
        OnNarratorSpeak?.Invoke(message, duration);
    }

    public static void ControlsInverted(bool isInverted)
    {
        OnControlsInverted?.Invoke(isInverted);
    }

    public static void GravityReversed(bool isReversed)
    {
        OnGravityReversed?.Invoke(isReversed);
    }
}

// ─── Enums ──────────────────────────────────────────────────────────
/// <summary>Types of mechanical bugs the Volatility system can trigger.</summary>
public enum MechanicalBugType
{
    InvertedControls,
    ReverseGravity,
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
