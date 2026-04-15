using UnityEngine;

/// <summary>
/// Simple health system for the player.
/// Handles damage, invincibility frames, and death events.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Maximum health of the player.")]
    [SerializeField] private int maxHealth = 5;
    
    [Tooltip("Time in seconds the player is invincible after taking damage.")]
    [SerializeField] private float damageCooldown = 0.5f;

    private int _currentHealth;
    private float _lastDamageTime = -1f;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => maxHealth;

    private void Start()
    {
        ResetHealth();
    }

    /// <summary>
    /// Reduces player health and triggers death if health reaches 0.
    /// Respects the damage cooldown.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (amount <= 0 || _currentHealth <= 0) return;
        if (Time.time - _lastDamageTime < damageCooldown) return;

        _currentHealth -= amount;
        _lastDamageTime = Time.time;

        GameEventManager.PlayerHealthChanged(_currentHealth, maxHealth);
        Debug.Log($"[PlayerHealth] Took {amount} damage. Health: {_currentHealth}/{maxHealth}");

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Heals the player up to maxHealth.
    /// </summary>
    public void Heal(int amount)
    {
        if (amount <= 0 || _currentHealth <= 0) return;

        _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);
        GameEventManager.PlayerHealthChanged(_currentHealth, maxHealth);
        Debug.Log($"[PlayerHealth] Healed {amount}. Health: {_currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Resets health to maximum. Used when respawning.
    /// </summary>
    public void ResetHealth()
    {
        _currentHealth = maxHealth;
        GameEventManager.PlayerHealthChanged(_currentHealth, maxHealth);
    }

    private void Die()
    {
        _currentHealth = 0;
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if(pm != null)
        {
            // optional: stop movement during death
        }

        Debug.Log("[PlayerHealth] Player has died.");
        GameEventManager.PlayerDied();
    }
}
