using UnityEngine;

/// <summary>
/// Interface for attribute effects.
/// Any script that implements this can define what happens
/// when an attribute is applied to or removed from a GameObject.
/// </summary>
public interface IAttributeEffect
{
    /// <summary>
    /// Called when the attribute is applied to a target object.
    /// </summary>
    /// <param name="target">The GameObject receiving the attribute.</param>
    /// <param name="data">The ScriptableObject data for this attribute.</param>
    void Apply(GameObject target, AttributeSO data);

    /// <summary>
    /// Called when the attribute is removed from a target object.
    /// </summary>
    /// <param name="target">The GameObject losing the attribute.</param>
    /// <param name="data">The ScriptableObject data for this attribute.</param>
    void Remove(GameObject target, AttributeSO data);
}
