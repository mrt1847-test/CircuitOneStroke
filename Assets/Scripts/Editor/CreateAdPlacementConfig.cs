#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace CircuitOneStroke.Editor
{
    public static class CreateAdPlacementConfig
    {
        [MenuItem("Circuit One-Stroke/Create Ad Placement Config")]
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            var path = "Assets/Resources/AdPlacementConfig.asset";
            if (AssetDatabase.LoadAssetAtPath<CircuitOneStroke.Services.AdPlacementConfig>(path) != null)
            {
                Debug.Log("AdPlacementConfig already exists at " + path);
                return;
            }
            var config = ScriptableObject.CreateInstance<CircuitOneStroke.Services.AdPlacementConfig>();
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            Debug.Log("Created " + path);
        }
    }
}
#endif
