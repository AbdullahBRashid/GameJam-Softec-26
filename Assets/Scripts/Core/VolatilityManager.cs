using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reactive Volatility Manager (System A).
/// Purely event-driven — NO time-based decay or inventory pressure.
/// 
/// Volatility Rules:
///   Take default attribute from object     → +full cost
///   Take foreign attribute from object     → +0
///   Apply to object where it IS default    → −full cost (restore)
///   Apply to object where it's NOT default → −half cost
///
/// When volatility exceeds the high threshold, mechanical bugs trigger.
/// 
/// Setup: Place on an empty GameObject named "VolatilityManager" in the scene.
/// </summary>
public class VolatilityManager : MonoBehaviour
{
    public static VolatilityManager Instance { get; private set; }

    [Header("Volatility")]
    [SerializeField] private float volatility = 0f;
    [SerializeField] private float maxVolatility = 100f;

    [Header("Thresholds")]
    [Tooltip("Above this value, mechanical bugs start triggering.")]
    [SerializeField] private float highThreshold = 75f;

    [Tooltip("Seconds between bug checks (only checked when in danger zone).")]
    [SerializeField] private float bugCheckInterval = 8f;

    [Header("Active Bugs")]
    [SerializeField] private float bugDuration = 12f;

    // ── Internal State ──
    private float _bugCheckTimer;
    private readonly HashSet<MechanicalBugType> _activeBugs = new HashSet<MechanicalBugType>();

    // Read-only access
    public float Volatility => volatility;
    public float MaxVolatility => maxVolatility;
    public float NormalizedVolatility => volatility / maxVolatility;
    public bool IsInDangerZone => volatility >= highThreshold;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        volatility = 0f;
    }

    private void OnEnable()
    {
        // ── Subscribe to the 4 specific volatility events ──
        GameEventManager.OnDefaultAttributeRemoved += HandleDefaultRemoved;
        GameEventManager.OnForeignAttributeRemoved += HandleForeignRemoved;
        GameEventManager.OnAttributeRestoredToDefault += HandleRestoredToDefault;
        GameEventManager.OnAttributeAppliedToForeign += HandleAppliedToForeign;
        GameEventManager.OnAttributeDiscarded += HandleDiscarded;
    }

    private void OnDisable()
    {
        GameEventManager.OnDefaultAttributeRemoved -= HandleDefaultRemoved;
        GameEventManager.OnForeignAttributeRemoved -= HandleForeignRemoved;
        GameEventManager.OnAttributeRestoredToDefault -= HandleRestoredToDefault;
        GameEventManager.OnAttributeAppliedToForeign -= HandleAppliedToForeign;
        GameEventManager.OnAttributeDiscarded -= HandleDiscarded;
    }

    private void Update()
    {
        // Only check for bugs periodically when in danger zone
        if (!IsInDangerZone) return;

        _bugCheckTimer -= Time.deltaTime;
        if (_bugCheckTimer <= 0f)
        {
            _bugCheckTimer = bugCheckInterval;
            EvaluateBugTrigger();
        }
    }

    // ═══ Public API ═════════════════════════════════════════════════

    /// <summary>Add (or subtract) volatility. Clamps to [0, max].</summary>
    public void AddVolatility(float amount)
    {
        float previous = volatility;
        volatility = Mathf.Clamp(volatility + amount, 0f, maxVolatility);
        float delta = volatility - previous;

        if (Mathf.Abs(delta) > 0.001f)
        {
            GameEventManager.VolatilityChanged(volatility, delta);
            Debug.Log($"[VolatilityManager] Volatility: {previous:F1} → {volatility:F1} (Δ{delta:+0.0;-0.0})");
        }
    }

    /// <summary>Hard-sets volatility to a specific value. Used for checkpoint restoration.</summary>
    public void SetVolatility(float value)
    {
        float previous = volatility;
        volatility = Mathf.Clamp(value, 0f, maxVolatility);
        float delta = volatility - previous;
        GameEventManager.VolatilityChanged(volatility, delta);
    }

    /// <summary>Force-trigger a specific mechanical bug (used by AIDirector).</summary>
    public void ForceBug(MechanicalBugType bugType, float duration = -1f)
    {
        if (duration < 0f) duration = bugDuration;
        StartCoroutine(RunBug(bugType, duration));
    }

    // ═══ Event Handlers ═════════════════════════════════════════════

    /// <summary>Default attribute removed from its home object → +full cost</summary>
    private void HandleDefaultRemoved(AttributeSO attr, GameObject source)
    {
        float cost = attr != null ? attr.volatilityCost : 5f;
        AddVolatility(cost);
        Debug.Log($"[VolatilityManager] Default '{attr?.displayName}' removed from {source.name} → +{cost}");
    }

    /// <summary>Foreign attribute removed from an object → +half cost (reverting the discount!)</summary>
    private void HandleForeignRemoved(AttributeSO attr, GameObject source)
    {
        float cost = attr != null ? attr.volatilityCost * 0.5f : 2.5f;
        AddVolatility(cost);
        Debug.Log($"[VolatilityManager] Foreign '{attr?.displayName}' removed from {source.name} → +{cost} (reverted)");
    }

    /// <summary>Attribute restored to its default object → −full cost</summary>
    private void HandleRestoredToDefault(AttributeSO attr, GameObject target)
    {
        float cost = attr != null ? attr.volatilityCost : 5f;
        AddVolatility(-cost);
        Debug.Log($"[VolatilityManager] '{attr?.displayName}' restored to default on {target.name} → −{cost}");
    }

    /// <summary>Attribute applied to a non-default object → −half cost</summary>
    private void HandleAppliedToForeign(AttributeSO attr, GameObject target)
    {
        float cost = attr != null ? attr.volatilityCost * 0.5f : 2.5f;
        AddVolatility(-cost);
        Debug.Log($"[VolatilityManager] '{attr?.displayName}' applied as foreign to {target.name} → −{cost}");
    }

    /// <summary>Attribute discarded mapping reverse of take penalty</summary>
    private void HandleDiscarded(AttributeSO attr, bool wasDefault)
    {
        float cost = attr != null ? attr.volatilityCost : 5f;
        if (!wasDefault) cost *= 0.5f;
        AddVolatility(-cost);
        Debug.Log($"[VolatilityManager] '{attr?.displayName}' DISCARDED (wasDefault: {wasDefault}) → −{cost}");
    }

    // ═══ Bug Logic ══════════════════════════════════════════════════

    private void EvaluateBugTrigger()
    {
        MechanicalBugType[] bugLibrary = (MechanicalBugType[])System.Enum.GetValues(typeof(MechanicalBugType));
        MechanicalBugType chosen = bugLibrary[Random.Range(0, bugLibrary.Length)];

        if (_activeBugs.Contains(chosen)) return;

        StartCoroutine(RunBug(chosen, bugDuration));
    }

    private System.Collections.IEnumerator RunBug(MechanicalBugType bug, float duration)
    {
        if (_activeBugs.Contains(bug)) yield break;

        _activeBugs.Add(bug);
        Debug.Log($"[VolatilityManager] ⚠ Mechanical Bug TRIGGERED: {bug} (duration: {duration}s)");

        GameEventManager.MechanicalBugTriggered(bug);

        switch (bug)
        {
            case MechanicalBugType.InvertedControls:
                GameEventManager.NarratorSpeak("WARNING: Neural link inverted.", 4f);
                GameEventManager.ControlsInverted(true);
                break;
            case MechanicalBugType.ReverseGravity:
                GameEventManager.NarratorSpeak("WARNING: Gravity polarity reversed.", 4f);
                GameEventManager.GravityReversed(true);
                break;
            case MechanicalBugType.GravityDrift:
                GameEventManager.NarratorSpeak("WARNING: Local gravity anchor destabilizing.", 4f);
                GameEventManager.GravityDrift(true);
                break;
            case MechanicalBugType.InertiaCorruption:
                GameEventManager.NarratorSpeak("WARNING: Friction coefficient corrupted.", 4f);
                GameEventManager.InertiaCorruption(true);
                break;
        }

        // Yield loop instead of strict WaitForSeconds so we can instantly abort if the player stabilizes Volatility!
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (!IsInDangerZone)
            {
                Debug.Log($"[VolatilityManager] Volatility stabilized! Aborting bug: {bug}");
                GameEventManager.NarratorSpeak("SYSTEM STABILIZED. Purging mechanical side effects.", 3f);
                break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        switch (bug)
        {
            case MechanicalBugType.InvertedControls:
                GameEventManager.ControlsInverted(false);
                break;
            case MechanicalBugType.ReverseGravity:
                GameEventManager.GravityReversed(false);
                break;
            case MechanicalBugType.GravityDrift:
                GameEventManager.GravityDrift(false);
                break;
            case MechanicalBugType.InertiaCorruption:
                GameEventManager.InertiaCorruption(false);
                break;
        }

        _activeBugs.Remove(bug);
        GameEventManager.MechanicalBugEnded(bug);
        Debug.Log($"[VolatilityManager] ✔ Mechanical Bug ENDED: {bug}");
    }
}
