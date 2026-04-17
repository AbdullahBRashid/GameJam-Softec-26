#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[InitializeOnLoad]
public class NarratorLinesGenerator
{
    static NarratorLinesGenerator()
    {
        EditorApplication.delayCall += CheckAndGenerateAsset;
    }

    private static void CheckAndGenerateAsset()
    {
        if (!System.IO.Directory.Exists("Assets/Resources"))
        {
            System.IO.Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.Refresh();
        }

        NarratorLinesSO existing = Resources.Load<NarratorLinesSO>("NarratorLines");
        if (existing == null)
        {
            GenerateAsset();
        }
    }

    [MenuItem("Tools/Generate Stub Narrator Lines (thank pomas for this)")]
    public static void GenerateStubAsset()
    {
        GenerateAsset();
    }

    private static void GenerateAsset()
    {
        NarratorLinesSO so = Resources.Load<NarratorLinesSO>("NarratorLines");
        if (so != null)
        {
            Debug.Log("[NarratorLinesGenerator] NarratorLines asset already exists. Skipping stub generation to prevent overwrite.");
            return;
        }

        so = ScriptableObject.CreateInstance<NarratorLinesSO>();

        // Pre-populate all the default narrator lines
        so.lines = new List<NarratorLine>
        {
             new NarratorLine { messageName = "Test", message = "if you see this message then this means the narrator lines asset was missing. this is an auto generated stub file, replace this with the actual asset." }
        };

        AssetDatabase.CreateAsset(so, "Assets/Resources/NarratorLines.asset");
        
        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        Debug.Log("[NarratorLinesGenerator] NarratorLines asset populated with default lines.");
    }
}
#endif
