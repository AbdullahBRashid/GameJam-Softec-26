using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Singleton manager for levels/stages.
/// Tracks current stage, furthest stage reached (checkpoint), and handles respawning/resetting.
/// </summary>
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Player References")]
    [Tooltip("The player GameObject")]
    [SerializeField] private GameObject player;

    [Header("State")]
    [SerializeField] private int currentStageIndex = -1;
    [SerializeField] private int furthestStageReached = -1;

    // Checkpoint Data
    private AttributeInventory.InventorySnapshot _inventoryCheckpoint;
    private Dictionary<string, List<AttributeSO>> _objectSnapshots = new Dictionary<string, List<AttributeSO>>();
    
    // Track all attribute controllers in the scene to snapshot them
    private AttributeController[] _allObjects;
    
    // Track the active stage zone for respawn points
    private Dictionary<int, StageZone> _stageZones = new Dictionary<int, StageZone>();

    public int CurrentStageIndex => currentStageIndex;
    public int FurthestStage => furthestStageReached;

    private Vector3 _initialSpawnPos;
    private Quaternion _initialSpawnRot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (player == null)
            player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            _initialSpawnPos = player.transform.position;
            _initialSpawnRot = player.transform.rotation;
        }
    }

    private void Start()
    {
        // Find all attribute controllers in the scene initially
        // We include inactive ones to ensure a complete world snapshot
        _allObjects = FindObjectsByType<AttributeController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        // Find all stage zones to register them early
        StageZone[] zones = FindObjectsByType<StageZone>(FindObjectsSortMode.None);
        foreach (var zone in zones)
        {
            _stageZones[zone.stageIndex] = zone;
        }

        // Wait to ensure all Start() methods naturally fire first, then save the world state
        StartCoroutine(SaveInitialCheckpointRoutine());
    }

    private System.Collections.IEnumerator SaveInitialCheckpointRoutine()
    {
        yield return new WaitForEndOfFrame();
        
        // Only save if we haven't already hit a trigger (just in case they spawned physically inside a trigger)
        if (_objectSnapshots.Count == 0)
        {
            SaveCheckpoint();
            Debug.Log("[StageManager] Initial world state snapshotted successfully.");
        }
    }

    private void OnEnable()
    {
        GameEventManager.OnPlayerDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        GameEventManager.OnPlayerDied -= HandlePlayerDied;
    }

    /// <summary>
    /// Called by StageZone when player enters.
    /// </summary>
    public void EnterStage(StageZone zone)
    {
        if (currentStageIndex == zone.stageIndex) return; // Already here

        currentStageIndex = zone.stageIndex;
        _stageZones[zone.stageIndex] = zone;

        Debug.Log($"[StageManager] Entered stage {currentStageIndex}: {zone.stageName}");

        if (currentStageIndex > furthestStageReached)
        {
            furthestStageReached = currentStageIndex;
            SaveCheckpoint();
        }

        GameEventManager.StageEntered(currentStageIndex);
    }

    /// <summary>
    /// Saves the player's inventory and all objects in the current stage as a checkpoint.
    /// </summary>
    private void SaveCheckpoint()
    {
        if (player == null) return;

        var inventory = player.GetComponent<AttributeInventory>();
        if (inventory != null)
        {
            _inventoryCheckpoint = inventory.GetSnapshot();
        }

        _objectSnapshots.Clear();

        // Save state of all objects
        foreach (var obj in _allObjects)
        {
            if (obj != null)
            {
                // We use InstanceID as key, or name if preferred. Using a unique identifier is better.
                // Assuming object names are unique or relying on instance IDs. Custom ID is safest, but we'll use name for simplicity if they are unique enough.
                _objectSnapshots[obj.gameObject.name + obj.GetInstanceID()] = obj.GetAttributeSnapshot();
            }
        }

        Debug.Log($"[StageManager] Checkpoint saved for stage {furthestStageReached}.");
    }

    /// <summary>
    /// Resets the current stage limits to default attributes, ignoring checkpoint.
    /// Useful for puzzle retries within the same stage.
    /// </summary>
    public void ResetCurrentStage()
    {
        // This resets ALL objects to their default state (initial puzzle state)
        foreach (var obj in _allObjects)
        {
            if (obj != null)
            {
                obj.ResetToDefaults();
            }
        }

        // Restore inventory to checkpoint
        if (player != null)
        {
            var inventory = player.GetComponent<AttributeInventory>();
            if (inventory != null)
            {
                inventory.RestoreFromSnapshot(_inventoryCheckpoint);
            }
        }

        Debug.Log($"[StageManager] Stage {currentStageIndex} reset to defaults.");
        GameEventManager.StageReset();
    }

    /// <summary>
    /// Teleports the player to the current stage's spawn point and restores checkpoint state.
    /// </summary>
    public void RespawnPlayer()
    {
        if (player == null) return;

        // 1. Move player to spawn
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        if (_stageZones.TryGetValue(currentStageIndex, out StageZone zone) && zone.spawnPoint != null)
        {
            player.transform.position = zone.spawnPoint.position;
            player.transform.rotation = zone.spawnPoint.rotation;
            Debug.Log($"[StageManager] Player respawned at {zone.stageName}.");
        }
        else
        {
            Debug.LogWarning($"[StageManager] No spawn point found for stage {currentStageIndex}! Using initial spawn point.");
            player.transform.position = _initialSpawnPos;
            player.transform.rotation = _initialSpawnRot;
        }

        if (cc != null) cc.enabled = true;
        Physics.SyncTransforms(); // Ensures the teleport registers with the physics engine immediately

        // 2. Restore all object states
        foreach (var obj in _allObjects)
        {
            if (obj != null)
            {
                string key = obj.gameObject.name + obj.GetInstanceID();
                if (_objectSnapshots.TryGetValue(key, out var snapshot))
                {
                    obj.RestoreFromSnapshot(snapshot);
                }
            }
        }

        // 3. Restore inventory
        var inventory = player.GetComponent<AttributeInventory>();
        if (inventory != null)
        {
            inventory.RestoreFromSnapshot(_inventoryCheckpoint);
        }

        // 4. Heal player
        var health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.ResetHealth();
        }
    }

    /// <summary>
    /// Instantly teleports the player to a given stage's spawn point bypassing checklists and resets.
    /// </summary>
    public void TeleportToStage(int stageIndex)
    {
        if (player == null) return;
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        if (_stageZones.TryGetValue(stageIndex, out StageZone zone) && zone.spawnPoint != null)
        {
            player.transform.position = zone.spawnPoint.position;
            player.transform.rotation = zone.spawnPoint.rotation;
            Debug.Log($"[StageManager] Teleported instantly to stage {stageIndex}.");
        }

        if (cc != null) cc.enabled = true;
        Physics.SyncTransforms();
    }

    private void HandlePlayerDied()
    {
        Debug.Log("[StageManager] Player died. Awaiting UI retry...");
        // RespawnPlayer() is now called from DeathScreenUI.cs when the player clicks Retry.
    }

    /// <summary>
    /// Checks if a given stage is allowed to be reset (e.g. current or previous).
    /// </summary>
    public bool CanResetStage(int stageIndex)
    {
        return stageIndex <= furthestStageReached;
    }
}
