using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct NarratorLine
{
    public string messageName;
    [TextArea(2, 5)] public string message;
}

[CreateAssetMenu(fileName = "NarratorLines", menuName = "Game/Narrator Lines")]
public class NarratorLinesSO : ScriptableObject
{
    private static NarratorLinesSO _instance;
    public static NarratorLinesSO Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<NarratorLinesSO>("NarratorLines");
                if (_instance == null)
                {
                    Debug.LogWarning("[NarratorLinesSO] Could not load NarratorLines from Resources! Make sure it exists at Assets/Resources/NarratorLines.asset");
                }
            }
            return _instance;
        }
    }

    [Header("All Narrator Lines")]
    public List<NarratorLine> lines = new List<NarratorLine>();

    public string GetLine(string messageName)
    {
        if (string.IsNullOrEmpty(messageName)) return "";

        foreach (var line in lines)
        {
            if (line.messageName == messageName)
                return line.message;
        }
        return $"[Missing Line: {messageName}]";
    }
}
