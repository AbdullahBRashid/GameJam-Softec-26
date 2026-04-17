using UnityEngine;

/// <summary>
/// Door controller that uses animation triggers on a child pivot object.
/// When disabled by LockedAttribute, the door cannot be interacted with.
/// 
/// Setup:
///   1. Add this component to the Door parent GameObject.
///   2. Assign the child "door pivot" object that has the Animator.
///   3. Add an AttributeController to the same object.
///   4. Set the door's category to ObjectCategory.Door.
///   5. Add the "locked" AttributeSO to the door's defaultAttributes list.
///   6. When the player removes the "locked" attribute, LockedAttribute.Remove()
///      re-enables this component, allowing the door to be opened.
/// </summary>
public class DoorController : MonoBehaviour
{
    [Header("Door Pivot")]
    [Tooltip("The child object with the Animator (door pivot).")]
    [SerializeField] private Animator doorAnimator;

    [Header("Animation Triggers")]
    [Tooltip("Animator trigger parameter name to open the door.")]
    [SerializeField] private string openTrigger = "Open";

    [Tooltip("Animator trigger parameter name to close the door.")]
    [SerializeField] private string closeTrigger = "Close";

    [Header("Audio")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    private bool _isOpen = false;
    private AudioSource _audioSource;
    private Collider _collider;
    private float _lastInteractTime = -1f;
    private const float INTERACT_COOLDOWN = 0.5f;

    public bool IsOpen => _isOpen;
    public bool CanInteract => Time.time - _lastInteractTime > INTERACT_COOLDOWN;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _collider = GetComponent<Collider>();

        // Auto-find Animator on child if not assigned
        if (doorAnimator == null)
            doorAnimator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Open the door. Only works when enabled (locked attribute removed).
    /// </summary>
    public void OpenDoor()
    {
        if (!enabled || _isOpen || !CanInteract) return;

        if (!GameEventManager.IsTimeRunning)
        {
            GameEventManager.NarratorSpeak("It won't budge. Time is standing still.", 2f);
            return;
        }

        if (doorAnimator == null)
        {
            Debug.LogWarning($"[DoorController] {gameObject.name} — no Animator found on child pivot!");
            return;
        }

        _isOpen = true;
        _lastInteractTime = Time.time;
        doorAnimator.SetTrigger(openTrigger);

        // Make collider a trigger so player can walk through
        if (_collider != null)
            _collider.isTrigger = true;

        PlaySound(openSound);
        Debug.Log($"[DoorController] {gameObject.name} OPENED.");
    }

    /// <summary>
    /// Close the door.
    /// </summary>
    public void CloseDoor()
    {
        if (!_isOpen || !CanInteract) return;

        if (!GameEventManager.IsTimeRunning)
        {
            GameEventManager.NarratorSpeak("It won't budge. Time is standing still.", 2f);
            return;
        }

        if (doorAnimator == null) return;

        _isOpen = false;
        _lastInteractTime = Time.time;
        doorAnimator.SetTrigger(closeTrigger);

        // Restore solid collider
        if (_collider != null)
            _collider.isTrigger = false;

        PlaySound(closeSound);
        Debug.Log($"[DoorController] {gameObject.name} CLOSED.");
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        if (_audioSource != null)
            _audioSource.PlayOneShot(clip);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    /// <summary>
    /// Called when this component is re-enabled (e.g., after LockedAttribute is removed).
    /// </summary>
    private void OnEnable()
    {
        Debug.Log($"[DoorController] {gameObject.name} — door is now UNLOCKED and interactable.");
    }

    /// <summary>
    /// Called when this component is disabled (e.g., when LockedAttribute is re-applied).
    /// Force-closes the door to keep state in sync.
    /// </summary>
    private void OnDisable()
    {
        if (_isOpen)
        {
            CloseDoor();
        }
        Debug.Log($"[DoorController] {gameObject.name} — door is now LOCKED.");
    }
}
