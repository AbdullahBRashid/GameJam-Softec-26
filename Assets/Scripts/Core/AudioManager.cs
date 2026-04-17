using UnityEngine;
using System.Collections;

/// <summary>
/// Singleton Audio Manager handling continuous background music and smooth crossfading.
/// Attach this to your core Game_Systems_Manager object.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("How long it takes to crossfade from one track to another.")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Tooltip("Maximum volume for music tracks.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxMusicVolume = 0.5f;

    private AudioSource _sourceA;
    private AudioSource _sourceB;
    private bool _isUsingSourceA = true;
    private Coroutine _activeCrossfade;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-generate our two AudioSources required for crossfading math
        _sourceA = gameObject.AddComponent<AudioSource>();
        _sourceA.loop = true;
        _sourceA.playOnAwake = false;
        _sourceA.volume = 0f;

        _sourceB = gameObject.AddComponent<AudioSource>();
        _sourceB.loop = true;
        _sourceB.playOnAwake = false;
        _sourceB.volume = 0f;
    }

    /// <summary>
    /// Smoothly transitions the background music to the provided track. 
    /// If the track is already actively playing, it safely ignores the command.
    /// </summary>
    public void PlayMusic(AudioClip newTrack)
    {
        if (newTrack == null) return;

        AudioSource activeSource = _isUsingSourceA ? _sourceA : _sourceB;

        // Prevent restarting identically overlapping trigger collisions
        if (activeSource.clip == newTrack && activeSource.isPlaying)
        {
            return;
        }

        if (_activeCrossfade != null)
        {
            StopCoroutine(_activeCrossfade);
        }

        _activeCrossfade = StartCoroutine(CrossfadeRoutine(newTrack));
    }

    private IEnumerator CrossfadeRoutine(AudioClip newTrack)
    {
        AudioSource activeSource = _isUsingSourceA ? _sourceA : _sourceB;
        AudioSource nextSource = _isUsingSourceA ? _sourceB : _sourceA;

        // Setup the new track on the silent source
        nextSource.clip = newTrack;
        nextSource.volume = 0f;
        nextSource.Play();

        float t = 0;
        float startVolumeA = activeSource.volume;
        
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float percent = t / fadeDuration;

            // Fade out the old track
            activeSource.volume = Mathf.Lerp(startVolumeA, 0f, percent);
            
            // Fade in the new track
            nextSource.volume = Mathf.Lerp(0f, maxMusicVolume, percent);

            yield return null;
        }

        // Finalize cleanup
        activeSource.volume = 0f;
        activeSource.Stop();
        
        nextSource.volume = maxMusicVolume;

        // Swap the internal pointer so the next collision targets the freshly emptied source
        _isUsingSourceA = !_isUsingSourceA;
        _activeCrossfade = null;
    }
}
