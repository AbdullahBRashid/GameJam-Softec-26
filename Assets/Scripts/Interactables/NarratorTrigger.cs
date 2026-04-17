using UnityEngine;
using System.Collections;

/// <summary>
/// A trigger zone that fires GameEventManager.NarratorSpeak when the player enters it.
/// Supports a delay, one-time triggering, and an optional voice line (AudioClip).
/// </summary>
[RequireComponent(typeof(Collider))]
public class NarratorTrigger : MonoBehaviour
{
    [Header("Narrator Message")]
    [Tooltip("The query key for the narrator line in NarratorLinesSO.")]
    public string messageName = "IntroMessage";
    public float displayDuration = 4f;

    [Header("Timing & Setup")]
    public float delayInSeconds = 0f;
    [Tooltip("Should this dialogue only happen the first time the player enters?")]
    public bool triggerOnlyOnce = true;

    [Header("Optional Audio")]
    [Tooltip("Optional voice-over line to play alongside the text.")]
    public AudioClip voiceLine;
    
    private bool _hasTriggered = false;
    private AudioSource _audioSource;

    private void Awake()
    {
        // Ensure collider is a trigger
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // CharacterControllers do not trigger OnTriggerEnter unless ONE of the objects has a Rigidbody.
        // We add a kinematic rigidbody here purely to guarantee collision detection with the player.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Auto-setup AudioSource if a voice clip is supplied
        if (voiceLine != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D sound so the player hears it clearly anywhere
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[NarratorTrigger] Something entered the trigger: {other.name} (Tag: {other.tag})");

        if (triggerOnlyOnce && _hasTriggered) 
        {
            Debug.Log("[NarratorTrigger] Already triggered once. Ignoring.");
            return;
        }

        if (other.CompareTag("Player"))
        {
            Debug.Log("[NarratorTrigger] Player confirmed! Starting dialogue sequence.");
            _hasTriggered = true;
            StartCoroutine(TriggerDialogueSequence());
        }
        else
        {
            Debug.Log($"[NarratorTrigger] Object {other.name} is NOT tagged 'Player'. Tag is actually '{other.tag}'.");
        }
    }

    private IEnumerator TriggerDialogueSequence()
    {
        Debug.Log($"[NarratorTrigger] Waiting for {delayInSeconds} seconds...");
        if (delayInSeconds > 0)
        {
            yield return new WaitForSeconds(delayInSeconds);
        }

        Debug.Log($"[NarratorTrigger] Firing event for '{messageName}'");
        // Show UI text using the existing central event system
        GameEventManager.NarratorSpeak(messageName, displayDuration);

        // Play optional audio
        if (voiceLine != null && _audioSource != null)
        {
            _audioSource.clip = voiceLine;
            _audioSource.Play();
        }
    }
}
