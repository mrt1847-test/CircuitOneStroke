using System;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.Core
{
    /// <summary>게임 흐름 상태. Idle→Drawing→Success/Fail.</summary>
    public enum GameState
    {
        Idle,
        Drawing,
        Success,
        Fail
    }

    /// <summary>
    /// 한 레벨 플레이의 상태 기계. 스트로크 시작/이동/종료와 클리어·실패·기록 저장 처리.
    /// </summary>
    public class GameStateMachine
    {
        public GameState State { get; private set; } = GameState.Idle;
        /// <summary>현재 스트로크 시작 시각. 클리어 시 최단 시간 기록용.</summary>
        public float StrokeStartTime { get; private set; }

        public event Action<GameState> OnStateChanged;

        public LevelRuntime Runtime { get; }
        public MoveValidator Validator { get; }

        public GameStateMachine(LevelRuntime runtime)
        {
            Runtime = runtime;
            Validator = new MoveValidator(runtime);
        }

        /// <summary>상태 전환 및 OnStateChanged 발동.</summary>
        public void SetState(GameState newState)
        {
            if (State == newState) return;
            State = newState;
            OnStateChanged?.Invoke(State);
        }

        /// <summary>Idle일 때만. nodeId에서 스트로크 시작 → Drawing.</summary>
        public void StartStroke(int nodeId)
        {
            if (State != GameState.Idle) return;
            var node = Runtime.GetNode(nodeId);
            if (node == null) return;

            Runtime.CurrentNodeId = nodeId;
            Runtime.ClearStrokeNodes();
            Runtime.AddStrokeNode(nodeId);
            Runtime.VisitedBulbs.Clear();
            if (node.nodeType == NodeType.Bulb)
                Runtime.VisitedBulbs.Add(nodeId);
            if (node.nodeType == NodeType.Switch)
                Runtime.ToggleGateGroup(node.switchGroupId);

            StrokeStartTime = UnityEngine.Time.time;
            SetState(GameState.Drawing);
        }

        /// <summary>Drawing일 때만. nextNodeId로 이동 시도. 결과에 따라 Reject/Ok/Fail.</summary>
        public MoveResult TryMoveTo(int nextNodeId)
        {
            if (State != GameState.Drawing) return MoveResult.Reject;
            return Validator.TryMoveTo(nextNodeId);
        }

        /// <summary>Drawing일 때만. 스트로크 종료. 전구 모두 방문 시 Success+기록, 아니면 Fail.</summary>
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

        /// <summary>클리어 시 레벨 클리어 플래그·최단 시간 저장.</summary>
        private void SaveClearRecord()
        {
            if (Runtime.LevelData == null) return;
            int id = Runtime.LevelData.levelId;
            LevelRecords.SetCleared(id);
            float elapsed = UnityEngine.Time.time - StrokeStartTime;
            LevelRecords.SetBestTime(id, elapsed);
        }

        /// <summary>Idle로 되돌림. 재시도 등에서 사용.</summary>
        public void ResetToIdle()
        {
            SetState(GameState.Idle);
        }
    }
}
