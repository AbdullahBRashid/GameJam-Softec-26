using UnityEngine;

/// <summary>
/// A trigger area marking the boundaries of a stage.
/// When the player enters, it registers as the current stage.
/// </summary>
[RequireComponent(typeof(Collider))]
public class StageZone : MonoBehaviour
{
    [Header("Stage Settings")]
    [Tooltip("Order of this stage (0 = first level, 1 = second, etc.)")]
    public int stageIndex;

    [Tooltip("Name of the stage for UI/logging.")]
    public string stageName = "New Stage";

    [Tooltip("Where the player respawns if they die in this stage.")]
    public Transform spawnPoint;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Register with StageManager
            if (StageManager.Instance != null)
            {
                StageManager.Instance.EnterStage(this);
            }
            else
            {
                Debug.LogWarning($"[StageZone] Reached stage {stageIndex} but no StageManager found in scene!");
            }
        }
    }
}
