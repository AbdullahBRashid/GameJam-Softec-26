using UnityEngine;

/// <summary>
/// A physical trigger space in the world that commands the AudioManager to flawlessly play a specific track.
/// Place on an empty GameObject with a BoxCollider/SphereCollider completely flagged as "IsTrigger = true".
/// </summary>
[RequireComponent(typeof(Collider))]
public class AudioZone : MonoBehaviour
{
    [Tooltip("The actual audio file (.mp3, .wav) that should play when the player walks into this zone.")]
    [SerializeField] private AudioClip zoneMusic;

    private void OnTriggerEnter(Collider other)
    {
        // Only respond to the physical player body walking in!
        if (other.CompareTag("Player"))
        {
            if (AudioManager.Instance != null && zoneMusic != null)
            {
                AudioManager.Instance.PlayMusic(zoneMusic);
            }
            else if (AudioManager.Instance == null)
            {
                Debug.LogWarning($"[AudioZone] Player entered {gameObject.name}, but no AudioManager object was found anywhere in the scene!");
            }
        }
    }
}
