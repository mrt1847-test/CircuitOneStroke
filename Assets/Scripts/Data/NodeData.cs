using UnityEngine;

namespace CircuitOneStroke.Data
{
    /// <summary>노드 종류. Bulb=방문 목표, Switch=방문 시 해당 게이트 그룹 토글.</summary>
    public enum NodeType
    {
        Bulb,
        Switch
    }

    /// <summary>
    /// 그래프의 한 노드. 위치·타입·스위치일 때 연동할 게이트 그룹 id.
    /// </summary>
    [System.Serializable]
    public class NodeData
    {
        /// <summary>노드 고유 id. 엣지의 a, b에서 참조.</summary>
        public int id;
        /// <summary>월드(또는 레벨) 좌표상 위치.</summary>
        public Vector2 pos;
        /// <summary>Bulb = 클리어 조건(1회 방문), Switch = 방문 시 게이트 토글.</summary>
        public NodeType nodeType;
        /// <summary>Switch일 때만 사용. 이 id에 해당하는 gateGroupId의 게이트들을 토글.</summary>
        [Tooltip("Used when nodeType is Switch")]
        public int switchGroupId;
    }
}
