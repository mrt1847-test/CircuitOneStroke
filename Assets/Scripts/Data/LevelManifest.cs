using UnityEngine;

namespace CircuitOneStroke.Data
{
    /// <summary>
    /// ScriptableObject holding references to LevelData assets (e.g. generated levels).
    /// LevelBakeTool updates this; LevelSelectUI uses it to enumerate and load levels.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelManifest", menuName = "Circuit One-Stroke/Level Manifest", order = 1)]
    public class LevelManifest : ScriptableObject
    {
        public LevelData[] levels;

        public int Count => levels != null ? levels.Length : 0;

        public LevelData GetLevel(int index)
        {
            if (levels == null || index < 0 || index >= levels.Length)
                return null;
            return levels[index];
        }
    }
}
