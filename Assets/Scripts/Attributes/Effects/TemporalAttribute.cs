using UnityEngine;

/// <summary>
/// Temporal/RunningTime attribute.
/// When applied, time runs normally. When removed, it stops specific time-based objects (like FanHazard).
/// </summary>
public class TemporalAttribute : IAttributeEffect
{
    public void Apply(GameObject target, AttributeSO attribute)
    {
        GameEventManager.SetTimeState(true);
        Debug.Log($"[TemporalAttribute] Time Flow RESTORED from {target.name}.");
    }

    public void Remove(GameObject target, AttributeSO attribute)
    {
        GameEventManager.SetTimeState(false);
        Debug.Log($"[TemporalAttribute] Time Flow STOPPED by removing from {target.name}.");
    }
}
