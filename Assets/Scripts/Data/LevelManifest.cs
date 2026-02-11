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
        /// <summary>레벨 에셋 참조 배열. 순서대로 1번째, 2번째 레벨.</summary>
        public LevelData[] levels;

        /// <summary>등록된 레벨 개수.</summary>
        public int Count => levels != null ? levels.Length : 0;

        /// <summary>인덱스(0-based)에 해당하는 레벨 에셋 반환. 범위 밖이면 null.</summary>
        public LevelData GetLevel(int index)
        {
            if (levels == null || index < 0 || index >= levels.Length)
                return null;
            return levels[index];
        }

        /// <summary>다음 레벨 ID (1-based). 다음이 없으면 currentLevelId 반환.</summary>
        public int GetNextLevelId(int currentLevelId)
        {
            if (levels == null || currentLevelId < 1) return currentLevelId;
            int nextId = currentLevelId + 1;
            return nextId <= levels.Length ? nextId : currentLevelId;
        }
    }
}
