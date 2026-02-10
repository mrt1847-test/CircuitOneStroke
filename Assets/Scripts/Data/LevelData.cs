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
    }
}
