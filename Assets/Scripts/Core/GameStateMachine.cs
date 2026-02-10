using System;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.Core
{
    public enum GameState
    {
        Idle,
        Drawing,
        Success,
        Fail
    }

    public class GameStateMachine
    {
        public GameState State { get; private set; } = GameState.Idle;
        public float StrokeStartTime { get; private set; }

        public event Action<GameState> OnStateChanged;

        public LevelRuntime Runtime { get; }
        public MoveValidator Validator { get; }

        public GameStateMachine(LevelRuntime runtime)
        {
            Runtime = runtime;
            Validator = new MoveValidator(runtime);
        }

        public void SetState(GameState newState)
        {
            if (State == newState) return;
            State = newState;
            OnStateChanged?.Invoke(State);
        }

        public void StartStroke(int nodeId)
        {
            if (State != GameState.Idle) return;
            var node = GetNode(nodeId);
            if (node == null) return;

            Runtime.CurrentNodeId = nodeId;
            Runtime.StrokeNodes.Clear();
            Runtime.StrokeNodes.Add(nodeId);
            Runtime.VisitedBulbs.Clear();
            if (node.nodeType == NodeType.Bulb)
                Runtime.VisitedBulbs.Add(nodeId);
            if (node.nodeType == NodeType.Switch)
                Runtime.ToggleGateGroup(node.switchGroupId);

            StrokeStartTime = UnityEngine.Time.time;
            SetState(GameState.Drawing);
        }

        public MoveResult TryMoveTo(int nextNodeId)
        {
            if (State != GameState.Drawing) return MoveResult.Reject;
            return Validator.TryMoveTo(nextNodeId);
        }

        public void EndStroke()
        {
            if (State != GameState.Drawing) return;
            if (Runtime.VisitedBulbs.Count == Runtime.TotalBulbCount)
            {
                SetState(GameState.Success);
                SaveClearRecord();
            }
            else
                SetState(GameState.Fail);
        }

        private void SaveClearRecord()
        {
            if (Runtime.LevelData == null) return;
            int id = Runtime.LevelData.levelId;
            LevelRecords.SetCleared(id);
            float elapsed = UnityEngine.Time.time - StrokeStartTime;
            LevelRecords.SetBestTime(id, elapsed);
        }

        public void ResetToIdle()
        {
            SetState(GameState.Idle);
        }

        private NodeData GetNode(int nodeId)
        {
            if (Runtime.LevelData?.nodes == null) return null;
            foreach (var n in Runtime.LevelData.nodes)
                if (n.id == nodeId) return n;
            return null;
        }
    }
}
