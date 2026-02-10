using CircuitOneStroke.Data;

namespace CircuitOneStroke.Core
{
    public class MoveValidator
    {
        private readonly LevelRuntime _runtime;

        public MoveValidator(LevelRuntime runtime)
        {
            _runtime = runtime;
        }

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

            var node = GetNode(nextNodeId);
            if (node == null)
                return MoveResult.Reject;
            // Revisit any node (Bulb or Switch) = instant Fail with clear feedback.
            if (_runtime.StrokeNodes.Contains(nextNodeId))
                return MoveResult.Fail;
            if (node.nodeType == NodeType.Bulb && _runtime.VisitedBulbs.Contains(nextNodeId))
                return MoveResult.Fail;

            _runtime.CurrentNodeId = nextNodeId;
            _runtime.StrokeNodes.Add(nextNodeId);
            if (node.nodeType == NodeType.Bulb)
                _runtime.VisitedBulbs.Add(nextNodeId);
            if (node.nodeType == NodeType.Switch)
                _runtime.ToggleGateGroup(node.switchGroupId);

            return MoveResult.Ok;
        }

        private NodeData GetNode(int nodeId)
        {
            if (_runtime.LevelData?.nodes == null) return null;
            foreach (var n in _runtime.LevelData.nodes)
                if (n.id == nodeId) return n;
            return null;
        }
    }
}
