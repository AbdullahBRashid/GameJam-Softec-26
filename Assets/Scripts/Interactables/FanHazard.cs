using UnityEngine;

/// <summary>
/// A spinning fan/propeller that damages the player while running.
/// Stops spinning and becomes harmless when GameEventManager.IsTimeRunning is false.
/// </summary>
[RequireComponent(typeof(Collider))]
public class FanHazard : MonoBehaviour
{
    [Header("Fan Movement")]
    public float spinSpeed = 360f;
    [Tooltip("The local axis the fan spins around (e.g. forward, up).")]
    public Vector3 spinAxis = Vector3.forward;

    [Header("Damage Settings")]
    [Tooltip("Amount of damage to deal per tick/bump.")]
    public int damagePerTick = 1;
    
    private void Update()
    {
        if (GameEventManager.IsTimeRunning)
        {
            // Rotate the fan blades
            transform.Rotate(spinAxis * spinSpeed * Time.deltaTime, Space.Self);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Fan is completely harmless when time is stopped
        if (!GameEventManager.IsTimeRunning) return;

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
        // Support physically bumped fans too
        if (!GameEventManager.IsTimeRunning) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth health = collision.gameObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damagePerTick);
            }
        }
    }

    /// <summary>
    /// Called manually by PlayerMovement.OnControllerColliderHit 
    /// to guarantee detection with CharacterControllers.
    /// </summary>
    public void HitByPlayer(PlayerHealth health)
    {
        if (!GameEventManager.IsTimeRunning) return;
        if (health != null)
        {
            health.TakeDamage(damagePerTick);
        }
    }
}
