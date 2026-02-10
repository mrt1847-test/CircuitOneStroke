#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.Editor
{
    public static class CreateDefaultLevel
    {
        [MenuItem("Circuit One-Stroke/Create Default Test Level (Level_1)")]
        public static void CreateLevel1()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelId = 1;
            level.nodes = new NodeData[]
            {
                new NodeData { id = 0, pos = new Vector2(-2f, 0f), nodeType = NodeType.Bulb },
                new NodeData { id = 1, pos = new Vector2(0f, 1.5f), nodeType = NodeType.Bulb },
                new NodeData { id = 2, pos = new Vector2(2f, 0f), nodeType = NodeType.Bulb },
                new NodeData { id = 3, pos = new Vector2(0f, -1.5f), nodeType = NodeType.Bulb }
            };
            level.edges = new EdgeData[]
            {
                new EdgeData { id = 0, a = 0, b = 1 },
                new EdgeData { id = 1, a = 0, b = 3 },
                new EdgeData { id = 2, a = 1, b = 2 },
                new EdgeData { id = 3, a = 2, b = 3 },
                new EdgeData { id = 4, a = 1, b = 3 },
                new EdgeData { id = 5, a = 0, b = 2 }
            };

            const string path = "Assets/Resources/Levels";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Levels"))
                AssetDatabase.CreateFolder("Assets/Resources", "Levels");

            AssetDatabase.CreateAsset(level, $"{path}/Level_1.asset");
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {path}/Level_1.asset");
        }

        [MenuItem("Circuit One-Stroke/Create Diode Test Level (Level_2)")]
        public static void CreateLevel2()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelId = 2;
            level.nodes = new NodeData[]
            {
                new NodeData { id = 0, pos = new Vector2(-2f, 0f), nodeType = NodeType.Bulb },
                new NodeData { id = 1, pos = new Vector2(0f, 1.2f), nodeType = NodeType.Bulb },
                new NodeData { id = 2, pos = new Vector2(2f, 0f), nodeType = NodeType.Bulb },
                new NodeData { id = 3, pos = new Vector2(0f, -1.2f), nodeType = NodeType.Bulb }
            };
            level.edges = new EdgeData[]
            {
                new EdgeData { id = 0, a = 0, b = 1, diode = DiodeMode.AtoB },
                new EdgeData { id = 1, a = 0, b = 3 },
                new EdgeData { id = 2, a = 1, b = 2 },
                new EdgeData { id = 3, a = 2, b = 3, diode = DiodeMode.BtoA },
                new EdgeData { id = 4, a = 1, b = 3 }
            };

            const string path = "Assets/Resources/Levels";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Levels"))
                AssetDatabase.CreateFolder("Assets/Resources", "Levels");

            AssetDatabase.CreateAsset(level, $"{path}/Level_2.asset");
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {path}/Level_2.asset");
        }

        [MenuItem("Circuit One-Stroke/Create Switch/Gate Test Level (Level_3)")]
        public static void CreateLevel3()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelId = 3;
            level.nodes = new NodeData[]
            {
                new NodeData { id = 0, pos = new Vector2(-2f, 0f), nodeType = NodeType.Bulb },
                new NodeData { id = 1, pos = new Vector2(0f, 1.2f), nodeType = NodeType.Switch, switchGroupId = 0 },
                new NodeData { id = 2, pos = new Vector2(2f, 0f), nodeType = NodeType.Bulb },
                new NodeData { id = 3, pos = new Vector2(0f, -1.2f), nodeType = NodeType.Bulb }
            };
            level.edges = new EdgeData[]
            {
                new EdgeData { id = 0, a = 0, b = 1 },
                new EdgeData { id = 1, a = 0, b = 3 },
                new EdgeData { id = 2, a = 1, b = 2, gateGroupId = 0, initialGateOpen = false },
                new EdgeData { id = 3, a = 2, b = 3 },
                new EdgeData { id = 4, a = 1, b = 3, gateGroupId = 0, initialGateOpen = true }
            };

            const string path = "Assets/Resources/Levels";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Levels"))
                AssetDatabase.CreateFolder("Assets/Resources", "Levels");

            AssetDatabase.CreateAsset(level, $"{path}/Level_3.asset");
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {path}/Level_3.asset");
        }
    }
}
#endif
