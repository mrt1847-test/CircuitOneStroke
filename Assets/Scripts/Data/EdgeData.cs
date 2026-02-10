using UnityEngine;

namespace CircuitOneStroke.Data
{
    /// <summary>엣지 통과 방향. None=양방향, AtoB/BtoA=한 방향만.</summary>
    public enum DiodeMode
    {
        None,
        AtoB,
        BtoA
    }

    /// <summary>
    /// 두 노드를 잇는 엣지. 다이오드 방향·게이트 그룹·초기 열림 상태 포함.
    /// </summary>
    [System.Serializable]
    public class EdgeData
    {
        /// <summary>엣지 고유 id.</summary>
        public int id;
        /// <summary>연결 노드 id (a → b 방향 기준).</summary>
        public int a;
        public int b;
        /// <summary>양방향(None) 또는 한 방향만 통과 가능.</summary>
        public DiodeMode diode = DiodeMode.None;
        /// <summary>-1 = 일반 선, >= 0 = 이 id의 게이트 그룹에 속함(열림/닫힘 공유).</summary>
        [Tooltip("-1 = normal wire, >= 0 = gate group id")]
        public int gateGroupId = -1;
        /// <summary>게이트일 때 초기 상태. false면 닫혀 있어 통과 불가.</summary>
        [Tooltip("Used when gateGroupId >= 0")]
        public bool initialGateOpen = true;
    }
}
