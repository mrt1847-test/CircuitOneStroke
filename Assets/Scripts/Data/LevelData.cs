using System.Collections.Generic;
using UnityEngine;

namespace CircuitOneStroke.Data
{
    /// <summary>
    /// 한 레벨의 그래프 정의. 노드(전구/스위치)와 엣지(선/다이오드/게이트) 배열로 구성.
    /// </summary>
    [CreateAssetMenu(fileName = "Level", menuName = "Circuit One-Stroke/Level Data", order = 0)]
    public class LevelData : ScriptableObject
    {
        /// <summary>레벨 식별자 (UI·기록용).</summary>
        public int levelId;
        /// <summary>노드 목록. 각 노드는 Bulb 또는 Switch.</summary>
        public NodeData[] nodes;
        /// <summary>엣지 목록. a-b 연결, 다이오드/게이트 그룹 포함.</summary>
        public EdgeData[] edges;
        public List<int> solutionPath = new List<int>();
        // Stored as (minNodeId, maxNodeId) using Vector2Int for Unity serialization.
        public List<Vector2Int> solutionEdgesUndirected = new List<Vector2Int>();

        public HashSet<(int a, int b)> BuildSolutionEdgeSetUndirected()
        {
            var set = new HashSet<(int a, int b)>();
            if (solutionEdgesUndirected == null)
                return set;
            for (int i = 0; i < solutionEdgesUndirected.Count; i++)
            {
                int a = solutionEdgesUndirected[i].x;
                int b = solutionEdgesUndirected[i].y;
                if (a == b)
                    continue;
                if (a > b)
                {
                    int t = a;
                    a = b;
                    b = t;
                }
                set.Add((a, b));
            }
            return set;
        }
    }
}
