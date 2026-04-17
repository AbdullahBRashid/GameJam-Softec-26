using UnityEngine;
using System.Collections;

/// <summary>
/// A one-time use System Override Key.
/// Applies solely to an object that has been forcibly locked by the AI Director (IsLocked = true).
/// It shatters the AI override, permanently unsealing the object, and destroys itself.
/// </summary>
public class KeyAttribute : IAttributeEffect
{
    public void Apply(GameObject target, AttributeSO attribute)
    {
        AttributeController controller = target.GetComponent<AttributeController>();
        if (controller != null && controller.IsLocked)
        {
            // 1. Shatter the lock
            controller.Unlock();
            
            // 2. Play thematic feedback
            GameEventManager.NarratorSpeak("keyUsed", 4f);
            Debug.Log($"[KeyAttribute] System override applied to {target.name}. Object is now completely unlocked.");
            
            // 3. Consume the key permanently (one-time use)
            // We delay this marginally to the end of the frame to ensure Unity's 
            // active lists are done processing the 'Apply' additions before we violently rip it out.
            controller.StartCoroutine(ConsumeKeyRoutine(controller, attribute));
        }
    }

    private IEnumerator ConsumeKeyRoutine(AttributeController controller, AttributeSO attribute)
    {
        yield return new WaitForEndOfFrame();
        
        // Erase it from the object entirely. Because we just called Unlock(), this will succeed!
        controller.RemoveAttribute(attribute);
    }

    public void Remove(GameObject target, AttributeSO attribute)
    {
        // Fired when the key consumes itself and vanishes. 
        // No specific mechanical cleanup is needed since it's a one-and-done item!
    }
}
