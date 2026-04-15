using UnityEngine;

/// <summary>
/// A deadly hazard that continually damages the player while they stay inside it.
/// If the "bouncy" attribute is applied, the player can bounce off it, but still takes damage 
/// (to maintain tension and require good timing/execution).
/// </summary>
[RequireComponent(typeof(Collider))]
public class LavaHazard : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Amount of damage to deal per tick.")]
    public int damagePerTick = 1;

    // We don't need a tickInterval here since PlayerHealth has a damageCooldown (invincibility frame).
    
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damagePerTick);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth health = collision.gameObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damagePerTick);
            }
        }
    }
}
