using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controls the "solid" physical nature of an object.
/// If an object is "solid" by default, removing this attribute makes its colliders into triggers
/// (ethereal), allowing the player to walk right through it, and suspends gravity so it doesn't
/// infinitely fall. 
/// 
/// If this is applied to a foreign object, it has no effect based on your specific requirements:
/// "If something doesnt have solid in its default attributes, its behavior remains unchanged."
/// </summary>
public class SolidAttribute : IAttributeEffect
{
    private List<Collider> _modifiedColliders = new List<Collider>();
    private bool _suspendedPhysics = false;

    public void Apply(GameObject target, AttributeSO attribute)
    {
        AttributeController controller = target.GetComponent<AttributeController>();
        if (controller != null && controller.IsDefaultAttribute(attribute))
        {
            // Restore normal physical solidity
            foreach (Collider col in _modifiedColliders)
            {
                if (col != null) col.isTrigger = false;
            }
            _modifiedColliders.Clear();

            // Unfreeze physics if we paused it to prevent falling through the world
            if (_suspendedPhysics)
            {
                Rigidbody rb = target.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = false;
                _suspendedPhysics = false;
            }
            
            Debug.Log($"[SolidAttribute] RESTORED solidity on {target.name}.");
        }
    }

    public void Remove(GameObject target, AttributeSO attribute)
    {
        AttributeController controller = target.GetComponent<AttributeController>();
        if (controller != null && controller.IsDefaultAttribute(attribute))
        {
            // Convert physical colliders to triggers so the player can pass through
            // but the interaction raycast can still hit it.
            Collider[] colliders = target.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                if (!col.isTrigger)
                {
                    col.isTrigger = true;
                    _modifiedColliders.Add(col);
                }
            }

            // Freeze the rigidbody if present, because turning colliders to triggers 
            // makes gravity pull it through the floor to infinity!
            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.isKinematic = true;
                _suspendedPhysics = true;
            }
            
            Debug.Log($"[SolidAttribute] REMOVED solidity from {target.name}. Object is now ethereal.");
        }
    }
}
