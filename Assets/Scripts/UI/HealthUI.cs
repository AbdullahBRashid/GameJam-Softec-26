using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI script to visualize the player's health.
/// Listens to GameEventManager for health changes.
/// </summary>
public class HealthUI : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("Slider to represent the player's health graphically.")]
    [SerializeField] private Slider healthSlider;

    [Tooltip("Text element to display health numerically.")]
    [SerializeField] private TextMeshProUGUI healthText;

    private void OnEnable()
    {
        // Subscribe to the health changed event
        GameEventManager.OnPlayerHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        GameEventManager.OnPlayerHealthChanged -= HandleHealthChanged;
    }

    /// <summary>
    /// Updates the UI when the player's health changes.
    /// </summary>
    /// <param name="currentHealth">Current player health</param>
    /// <param name="maxHealth">Maximum player health</param>
    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"Health: {currentHealth} / {maxHealth}";
        }
    }
}
