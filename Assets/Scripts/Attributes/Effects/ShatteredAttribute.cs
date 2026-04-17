using UnityEngine;

/// <summary>
/// Toggles a glass wall from solid to shattered so the player can walk through it.
/// It toggles between the children of the target object.
/// Assumes Child 0 is the fixed wall and Child 1 is the shattered wall.
/// </summary>
public class ShatteredAttribute : IAttributeEffect
{
    public void Apply(GameObject target, AttributeSO attribute)
    {
        if (target.transform.childCount >= 2)
        {
            target.transform.GetChild(0).gameObject.SetActive(false);
            target.transform.GetChild(1).gameObject.SetActive(true);
            
            GameEventManager.NarratorSpeak("shatteredCritical", 2f);
            Debug.Log($"[ShatteredAttribute] Wall {target.name} shattered.");
        }
        else
        {
            Debug.LogWarning($"[ShatteredAttribute] Applied to {target.name}, but it does not have at least 2 children!");
        }
    }

    public void Remove(GameObject target, AttributeSO attribute)
    {
        if (target.transform.childCount >= 2)
        {
            target.transform.GetChild(0).gameObject.SetActive(true);
            target.transform.GetChild(1).gameObject.SetActive(false);
            
            Debug.Log($"[ShatteredAttribute] Wall {target.name} restored to solid.");
        }
    }
}
