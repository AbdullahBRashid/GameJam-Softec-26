using UnityEngine;

/// <summary>
/// A station that fully restores the player's health when they enter its trigger or collide with it.
/// </summary>
public class HealStation : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        HealPlayer(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HealPlayer(collision.gameObject);
    }

    public void HealPlayer(GameObject playerObj)
    {
        PlayerHealth health = playerObj.GetComponent<PlayerHealth>();
        if (health != null && health.CurrentHealth < health.MaxHealth)
        {
            health.Heal(health.MaxHealth);
            Debug.Log("[HealStation] Player fully healed.");
        }
    }
}
