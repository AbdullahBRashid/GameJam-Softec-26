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
        CheckForPlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        CheckForPlayer(other);
    }

    private void CheckForPlayer(Collider other)
    {
        // Try getting PlayerMovement to bypass tag dependency problems
        if (other.GetComponent<PlayerMovement>() != null)
        {
            if (StageManager.Instance != null && StageManager.Instance.CurrentStageIndex != stageIndex)
            {
                StageManager.Instance.EnterStage(this);
            }
        }
    }
}
