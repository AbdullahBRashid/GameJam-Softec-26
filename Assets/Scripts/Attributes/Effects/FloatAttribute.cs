using UnityEngine;

public class FloatAttribute : IAttributeEffect
{
    private const float MaxDistance = 100f;
    private const float Speed = 5f;

    public void Apply(GameObject target, AttributeSO data)
    {
        // 1. Setup Physics
        Rigidbody rb = target.GetComponent<Rigidbody>();
        Collider col = target.GetComponent<Collider>();

        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true; // Use kinematic so it doesn't get blocked by walls
        }

        if (col != null)
        {
            col.isTrigger = true; // Become a trigger to "ghost" through everything
        }

        // 2. Start Execution Immediately
        // If it's a balloon, we don't move or destroy it
        if (!target.name.ToLower().Contains("balloon"))
        {
            var driver = target.AddComponent<FloatMoveDriver>();
            driver.maxDistance = MaxDistance;
            driver.speed = Speed;
        }

        // 3. Feedback
        if (data.applySound != null) AudioSource.PlayClipAtPoint(data.applySound, target.transform.position);

        Debug.Log($"[FloatAttribute] Started floating {target.name} (Trigger Mode)");
    }

    public void Remove(GameObject target, AttributeSO data)
    {
        var driver = target.GetComponent<FloatMoveDriver>();
        if (driver != null) Object.Destroy(driver);

        Rigidbody rb = target.GetComponent<Rigidbody>();
        Collider col = target.GetComponent<Collider>();

        if (rb != null) { rb.useGravity = true; rb.isKinematic = false; }
        if (col != null) col.isTrigger = false;
    }
}

/// <summary>
/// Driver component to handle movement and player detection.
/// </summary>
public class FloatMoveDriver : MonoBehaviour
{
    public float speed;
    public float maxDistance;
    private float _traveled = 0f;

    private void Update()
    {
        float step = speed * Time.deltaTime;
        transform.Translate(Vector3.up * step, Space.World);
        _traveled += step;

        if (_traveled >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Since we are a Trigger, we ignore walls automatically.
        // We only care if we hit the player.
        if (other.CompareTag("Player"))
        {
            Debug.Log($"{gameObject.name} bumped into the Player while floating!");
            // Optional: Destroy(gameObject); // if you want it to pop on hit
        }
    }
}