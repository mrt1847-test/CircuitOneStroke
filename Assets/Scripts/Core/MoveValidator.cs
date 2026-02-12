using CircuitOneStroke.Data;

namespace CircuitOneStroke.Core
{
    /// <summary>
    /// 현재 노드에서 nextNodeId로의 이동 가능 여부를 검사하고, 허용 시 런타임 상태를 갱신.
    /// GameStateMachine이 Drawing 상태에서 TryMoveTo 시 호출.
    /// </summary>
    public class MoveValidator
    {
        private readonly LevelRuntime _runtime;

        public MoveValidator(LevelRuntime runtime)
        {
            _runtime = runtime;
        }

        /// <summary>
        /// nextNodeId로 이동 시도. Reject=이동 불가(하트 소모 없음), HardFail=재방문 등 규칙 위반(하트 소모), Ok=이동 처리 완료.
        /// </summary>
        public MoveResult TryMoveTo(int nextNodeId)
        {
            int current = _runtime.CurrentNodeId;
            if (current < 0)
                return MoveResult.Reject;

            if (!_runtime.Graph.TryGetEdge(current, nextNodeId, out var edge))
                return MoveResult.Reject;

            if (edge.gateGroupId >= 0 && !_runtime.IsGateOpen(edge.id))
                return MoveResult.Reject;

            bool fromAtoB = (current == edge.a && nextNodeId == edge.b);
            if (edge.diode == DiodeMode.AtoB && !fromAtoB)
                return MoveResult.Reject;
            if (edge.diode == DiodeMode.BtoA && fromAtoB)
                return MoveResult.Reject;

            var node = _runtime.GetNode(nextNodeId);
            if (node == null)
                return MoveResult.Reject;
            if (node.nodeType == NodeType.Blocked)
                return MoveResult.Reject;
            // 한 붓 안에서 이미 지나간 노드 재방문 = HardFail (클리어 불가, 하트 소모)
            if (_runtime.StrokeContains(nextNodeId))
                return MoveResult.HardFail;
            if (node.nodeType == NodeType.Bulb && _runtime.VisitedBulbs.Contains(nextNodeId))
                return MoveResult.HardFail;

            _runtime.CurrentNodeId = nextNodeId;
            _runtime.AddStrokeNode(nextNodeId);
            if (node.nodeType == NodeType.Bulb)
                _runtime.VisitedBulbs.Add(nextNodeId);
            if (node.nodeType == NodeType.Switch)
                _runtime.ToggleGateGroup(node.switchGroupId);

            return MoveResult.Ok;
        }
    }
}
