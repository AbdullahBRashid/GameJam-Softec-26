using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Triggers a fade-in / fade-out UI text when the player enters a new stage.
/// </summary>
public class StageAnnouncerUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The Text component displaying the stage name/number.")]
    [SerializeField] private TextMeshProUGUI stageText;
    
    [Tooltip("CanvasGroup to control transparency for fading.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Timing Settings")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float displayDuration = 2.5f;

    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void OnEnable()
    {
        GameEventManager.OnStageEntered += HandleStageEntered;
    }

    private void OnDisable()
    {
        GameEventManager.OnStageEntered -= HandleStageEntered;
    }

    private void HandleStageEntered(int stageIndex, string stageName)
    {
        if (stageText == null || canvasGroup == null) return;

        // Use the descriptive stage name provided by the zone
        stageText.text = stageName.ToUpper();

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        
        _fadeCoroutine = StartCoroutine(FadeSequence());
    }

    private IEnumerator FadeSequence()
    {
        // Fade In
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Hold
        yield return new WaitForSeconds(displayDuration);

        // Fade Out
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }
}
