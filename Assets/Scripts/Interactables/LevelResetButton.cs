using UnityEngine;

/// <summary>
/// A physical button in the world that resets the current stage.
/// Disables itself if the player moves to a later stage.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LevelResetButton : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The stage this button is associated with.")]
    public int stageIndex;

    [Header("Visuals (Optional)")]
    [Tooltip("Renderer to change color when enabled/disabled.")]
    public Renderer buttonRenderer;
    public Color enabledColor = Color.green;
    public Color disabledColor = Color.gray;

    private bool _isActive = true;

    private void Start()
    {
        UpdateVisuals();
    }

    private void OnEnable()
    {
        GameEventManager.OnStageEntered += HandleStageEntered;
    }

    private void OnDisable()
    {
        GameEventManager.OnStageEntered -= HandleStageEntered;
    }

    private void HandleStageEntered(int newStageIndex)
    {
        // Disable the button if player has proceeded to a further stage.
        // Re-enable if they come back to this stage (or if they are currently here).
        if (StageManager.Instance != null)
        {
            _isActive = StageManager.Instance.CanResetStage(stageIndex) && newStageIndex == stageIndex;
        }
        else
        {
            _isActive = newStageIndex == stageIndex;
        }

        UpdateVisuals();
    }

    /// <summary>
    /// Call this from the InteractionSystem (raycast -> Take button)
    /// or explicitly check for it. Alternatively, we can let InteractionSystem
    /// call this if we add a shortcut for buttons (similar to how doors work).
    /// </summary>
    public void Press()
    {
        if (!_isActive)
        {
            Debug.Log($"[LevelResetButton] Button for stage {stageIndex} is disabled.");
            return;
        }

        Debug.Log($"[LevelResetButton] Reset pressed for stage {stageIndex}.");
        if (StageManager.Instance != null && StageManager.Instance.CurrentStageIndex == stageIndex)
        {
            StageManager.Instance.ResetCurrentStage();
            // A reset restores objects to default states, but doesn't teleport the player.
            // If we want a full reset that also respawns, we could call RespawnPlayer() after ResetCurrentStage().
        }
    }

    private void UpdateVisuals()
    {
        if (buttonRenderer != null)
        {
            buttonRenderer.material.color = _isActive ? enabledColor : disabledColor;
        }
    }

    // Optional: Let player press it by walking into it
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // You can choose whether simply touching it resets, 
            // or if they need to press 'E'. We'll put it here for convenience, 
            // but you can remove it and rely on InteractionSystem.
            Press();
        }
    }
}
