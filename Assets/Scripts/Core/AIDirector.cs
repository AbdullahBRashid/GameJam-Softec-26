using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Progressive AI Director (System B).
/// Tracks level progress and triggers scripted sabotage events
/// tied to the narrative. The AI "grows jealous" as you advance.
/// 
/// Setup: Place on an empty GameObject named "AIDirector" in the scene.
/// </summary>
public class AIDirector : MonoBehaviour
{
    public static AIDirector Instance { get; private set; }

    [Header("Level Progress")]
    [SerializeField] private int currentLevel = 0;

    [Header("Sabotage Schedule")]
    [SerializeField] private List<SabotageEntry> sabotageSchedule = new List<SabotageEntry>();

    [Header("Light Sabotage")]
    [Tooltip("All scene lights that the AI Director can dim.")]
    [SerializeField] private Light[] sceneLights;
    [SerializeField] private float dimmedIntensity = 0.15f;
    private float[] _originalLightIntensities;

    [Header("Map Decay")]
    [Tooltip("Floor colliders that can be disabled for Map Decay sabotage.")]
    [SerializeField] private Collider[] decayableFloors;

    // Track which sabotages have already fired so they don't repeat
    private readonly HashSet<int> _firedSabotages = new HashSet<int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Cache original light intensities
        if (sceneLights != null && sceneLights.Length > 0)
        {
            _originalLightIntensities = new float[sceneLights.Length];
            for (int i = 0; i < sceneLights.Length; i++)
            {
                if (sceneLights[i] != null)
                    _originalLightIntensities[i] = sceneLights[i].intensity;
            }
        }
    }

    private void OnEnable()
    {
        GameEventManager.OnLevelMilestoneReached += HandleLevelMilestone;
    }

    private void OnDisable()
    {
        GameEventManager.OnLevelMilestoneReached -= HandleLevelMilestone;
    }

    // ═══ Public API ═════════════════════════════════════════════════

    /// <summary>Call this when the player completes a level / enters a new zone.</summary>
    public void AdvanceLevel()
    {
        currentLevel++;
        GameEventManager.LevelMilestoneReached(currentLevel);
    }

    /// <summary>Force a specific level (for testing).</summary>
    public void SetLevel(int level)
    {
        currentLevel = level;
        GameEventManager.LevelMilestoneReached(currentLevel);
    }

    // ═══ Event Handling ═════════════════════════════════════════════

    private void HandleLevelMilestone(int level)
    {
        foreach (var entry in sabotageSchedule)
        {
            if (entry.triggerLevel == level && !_firedSabotages.Contains(level))
            {
                _firedSabotages.Add(level);
                ExecuteSabotage(entry);
            }
        }
    }

    private void ExecuteSabotage(SabotageEntry entry)
    {
        Debug.Log($"[AIDirector] 🎭 Sabotage triggered at Level {entry.triggerLevel}: {entry.sabotageType} — \"{entry.narratorText}\"");

        // Narrator speaks
        if (!string.IsNullOrEmpty(entry.narratorText))
        {
            GameEventManager.NarratorSpeak(entry.narratorText, entry.narratorDuration);
        }

        // Fire global sabotage event
        GameEventManager.SabotageTriggered(entry.sabotageType, entry.narratorText);

        // Execute the specific sabotage
        switch (entry.sabotageType)
        {
            case SabotageType.LockAttribute:
                ExecuteLockAttribute(entry);
                break;

            case SabotageType.DimLights:
                ExecuteDimLights();
                break;

            case SabotageType.MapDecay:
                ExecuteMapDecay();
                break;

            case SabotageType.DisableFlashlight:
                ExecuteDimLights(); // reuses dim logic
                break;

            case SabotageType.SpawnObstacles:
                // Placeholder for spawning logic
                Debug.Log("[AIDirector] SpawnObstacles — implement per level.");
                break;

            case SabotageType.CorruptUI:
                // Placeholder — UI distortion shader trigger
                Debug.Log("[AIDirector] CorruptUI — implement with overlay shader.");
                break;
        }

        // Optionally spike volatility when sabotage fires
        if (VolatilityManager.Instance != null)
        {
            VolatilityManager.Instance.AddVolatility(entry.volatilitySpike);
        }
    }

    // ═══ Sabotage Implementations ═══════════════════════════════════

    private void ExecuteLockAttribute(SabotageEntry entry)
    {
        // Lock all AttributeControllers currently in the scene
        // (or target specific ones via entry.targetObjects)
        if (entry.targetObjects != null)
        {
            foreach (var obj in entry.targetObjects)
            {
                if (obj == null) continue;
                var ctrl = obj.GetComponent<AttributeController>();
                if (ctrl != null) ctrl.Lock();
            }
        }
    }

    private void ExecuteDimLights()
    {
        if (sceneLights == null) return;
        foreach (var light in sceneLights)
        {
            if (light != null)
                light.intensity = dimmedIntensity;
        }
    }

    /// <summary>Restore lights to original intensity.</summary>
    public void RestoreLights()
    {
        if (sceneLights == null || _originalLightIntensities == null) return;
        for (int i = 0; i < sceneLights.Length; i++)
        {
            if (sceneLights[i] != null)
                sceneLights[i].intensity = _originalLightIntensities[i];
        }
    }

    private void ExecuteMapDecay()
    {
        if (decayableFloors == null) return;
        foreach (var floor in decayableFloors)
        {
            if (floor != null)
            {
                floor.enabled = false;
                Debug.Log($"[AIDirector] Floor '{floor.gameObject.name}' disabled (Map Decay).");
            }
        }
    }
}

/// <summary>
/// Serializable entry for the sabotage schedule.
/// Configure these in the AIDirector Inspector.
/// </summary>
[Serializable]
public class SabotageEntry
{
    [Tooltip("Which level triggers this sabotage.")]
    public int triggerLevel;

    [Tooltip("Type of sabotage to execute.")]
    public SabotageType sabotageType;

    [Tooltip("What the AI narrator says when this fires.")]
    [TextArea(2, 4)]
    public string narratorText;

    [Tooltip("How long the narrator text stays on screen.")]
    public float narratorDuration = 5f;

    [Tooltip("Volatility spike when this sabotage fires.")]
    public float volatilitySpike = 10f;

    [Tooltip("Specific objects to target (for LockAttribute sabotage).")]
    public GameObject[] targetObjects;
}
