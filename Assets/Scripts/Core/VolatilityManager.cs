using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reactive Volatility Manager (System A).
/// Tracks a global volatility float (0–100).
/// Only APPLYING an attribute to a DIFFERENT object than its source increases volatility.
/// Taking an attribute off and placing it back on the same object = net zero.
/// Bugs only trigger when volatility exceeds the high threshold.
/// 
/// Setup: Place on an empty GameObject named "VolatilityManager" in the scene.
/// </summary>
public class VolatilityManager : MonoBehaviour
{
    public static VolatilityManager Instance { get; private set; }

    [Header("Volatility")]
    [SerializeField] private float volatility = 0f;
    [SerializeField] private float maxVolatility = 100f;

    [Tooltip("Volatility decays this much per second when idle.")]
    [SerializeField] private float decayRate = 0.5f;

    [Tooltip("Volatility gained per attribute swap on an object.")]
    [SerializeField] private float swapCost = 5f;

    [Tooltip("Volatility gained per attribute in inventory, per second.")]
    [SerializeField] private float inventoryPressure = 1f;

    [Header("Thresholds")]
    [Tooltip("Above this value, mechanical bugs start triggering.")]
    [SerializeField] private float highThreshold = 75f;

    [Tooltip("Seconds between bug checks.")]
    [SerializeField] private float bugCheckInterval = 8f;

    [Header("Active Bugs")]
    [SerializeField] private float bugDuration = 6f;

    // ── Internal State ──
    private float _bugCheckTimer;
    private int _currentInventoryCount;
    private readonly HashSet<MechanicalBugType> _activeBugs = new HashSet<MechanicalBugType>();

    // Read-only access
    public float Volatility => volatility;
    public float NormalizedVolatility => volatility / maxVolatility;
    public bool IsInDangerZone => volatility >= highThreshold;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Ensure we start at 0
        volatility = 0f;
    }

    private void OnEnable()
    {
        // Only listen to Apply events for volatility increase.
        // Remove events do NOT increase volatility (taking off is free).
        GameEventManager.OnAttributeApplied += HandleAttributeApplied;
        GameEventManager.OnAttributePickedUp += HandleAttributePickup;
        GameEventManager.OnAttributeDropped += HandleAttributeDrop;
    }

    private void OnDisable()
    {
        GameEventManager.OnAttributeApplied -= HandleAttributeApplied;
        GameEventManager.OnAttributePickedUp -= HandleAttributePickup;
        GameEventManager.OnAttributeDropped -= HandleAttributeDrop;
    }

    private void Update()
    {
        // ── Inventory Pressure: high inventory count pushes volatility up ──
        if (_currentInventoryCount > 0)
        {
            AddVolatility(_currentInventoryCount * inventoryPressure * Time.deltaTime);
        }

        // ── Natural Decay ──
        if (volatility > 0f)
        {
            float decayAmount = decayRate * Time.deltaTime;
            AddVolatility(-decayAmount);
        }

        // ── Bug Check Timer ──
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
        }
    }

    /// <summary>Force-trigger a specific mechanical bug (used by AIDirector).</summary>
    public void ForceBug(MechanicalBugType bugType, float duration = -1f)
    {
        if (duration < 0f) duration = bugDuration;
        StartCoroutine(RunBug(bugType, duration));
    }

    // ═══ Event Handlers ═════════════════════════════════════════════

    /// <summary>
    /// Only fires when an attribute is APPLIED to an object.
    /// The InteractionSystem passes isReturningToSource=true via the event
    /// when the player places an attribute back on its original object.
    /// In that case, we don't charge volatility.
    /// </summary>
    private void HandleAttributeApplied(AttributeSO attr, GameObject target)
    {
        // The InteractionSystem sets a flag on the VolatilityManager before applying
        // if the attribute is returning to its source. Check that flag.
        if (_skipNextApplyCost)
        {
            _skipNextApplyCost = false;
            Debug.Log($"[VolatilityManager] Attribute returned to source — no volatility cost.");
            return;
        }

        AddVolatility(attr != null ? attr.volatilityCost : swapCost);
    }

    private void HandleAttributePickup(AttributeSO attr)
    {
        _currentInventoryCount++;
        // Picking up does NOT add volatility — only inventory pressure over time.
    }

    private void HandleAttributeDrop(AttributeSO attr)
    {
        _currentInventoryCount = Mathf.Max(0, _currentInventoryCount - 1);
    }

    // ═══ Source Tracking ═════════════════════════════════════════════
    // The InteractionSystem calls MarkReturnToSource() before applying
    // an attribute back to its original object.

    private bool _skipNextApplyCost = false;

    /// <summary>
    /// Call this BEFORE applying an attribute to tell the VolatilityManager
    /// that the next apply is a return-to-source (no volatility cost).
    /// </summary>
    public void MarkReturnToSource()
    {
        _skipNextApplyCost = true;
    }

    // ═══ Bug Logic ══════════════════════════════════════════════════

    private void EvaluateBugTrigger()
    {
        // Only trigger bugs when volatility exceeds the high threshold
        if (volatility < highThreshold) return;

        // Pick a random bug from the library
        MechanicalBugType[] bugLibrary = (MechanicalBugType[])System.Enum.GetValues(typeof(MechanicalBugType));
        MechanicalBugType chosen = bugLibrary[Random.Range(0, bugLibrary.Length)];

        // Don't stack the same bug
        if (_activeBugs.Contains(chosen)) return;

        StartCoroutine(RunBug(chosen, bugDuration));
    }

    private System.Collections.IEnumerator RunBug(MechanicalBugType bug, float duration)
    {
        if (_activeBugs.Contains(bug)) yield break;

        _activeBugs.Add(bug);
        Debug.Log($"[VolatilityManager] ⚠ Mechanical Bug TRIGGERED: {bug} (duration: {duration}s)");

        // Fire the event — listeners (PlayerController, Environment) react
        GameEventManager.MechanicalBugTriggered(bug);

        // Apply specific effects
        switch (bug)
        {
            case MechanicalBugType.InvertedControls:
                GameEventManager.ControlsInverted(true);
                break;
            case MechanicalBugType.ReverseGravity:
                GameEventManager.GravityReversed(true);
                break;
        }

        yield return new WaitForSeconds(duration);

        // Undo effects
        switch (bug)
        {
            case MechanicalBugType.InvertedControls:
                GameEventManager.ControlsInverted(false);
                break;
            case MechanicalBugType.ReverseGravity:
                GameEventManager.GravityReversed(false);
                break;
        }

        _activeBugs.Remove(bug);
        GameEventManager.MechanicalBugEnded(bug);
        Debug.Log($"[VolatilityManager] ✔ Mechanical Bug ENDED: {bug}");
    }
}
