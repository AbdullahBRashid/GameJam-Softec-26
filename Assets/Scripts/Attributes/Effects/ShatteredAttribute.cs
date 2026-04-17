using UnityEngine;

/// <summary>
/// Toggles a glass wall from solid to shattered so the player can walk through it.
/// Requires the target to have the ShatteredWallState component attached.
/// </summary>
public class ShatteredAttribute : IAttributeEffect
{
    public void Apply(GameObject target, AttributeSO attribute)
    {
        ShatteredWallState state = target.GetComponent<ShatteredWallState>();
        if (state != null)
        {
            if (state.intactModel != null) state.intactModel.SetActive(false);
            if (state.shatteredModel != null) state.shatteredModel.SetActive(true);
            
            GameEventManager.NarratorSpeak("shatteredCritical", 2f);
            Debug.Log($"[ShatteredAttribute] Wall {target.name} shattered.");
        }
        else
        {
            Debug.LogWarning($"[ShatteredAttribute] Applied to {target.name}, but missing ShatteredWallState component!");
        }
    }

    public void Remove(GameObject target, AttributeSO attribute)
    {
        ShatteredWallState state = target.GetComponent<ShatteredWallState>();
        if (state != null)
        {
            if (state.intactModel != null) state.intactModel.SetActive(true);
            if (state.shatteredModel != null) state.shatteredModel.SetActive(false);
            
            Debug.Log($"[ShatteredAttribute] Wall {target.name} restored to solid.");
        }
    }
}
