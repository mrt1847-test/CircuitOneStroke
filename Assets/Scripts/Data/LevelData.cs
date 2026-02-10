using UnityEngine;

namespace CircuitOneStroke.Data
{
    [CreateAssetMenu(fileName = "Level", menuName = "Circuit One-Stroke/Level Data", order = 0)]
    public class LevelData : ScriptableObject
    {
        public int levelId;
        public NodeData[] nodes;
        public EdgeData[] edges;
    }
}
