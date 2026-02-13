using System.Collections.Generic;
using CircuitOneStroke.Core;
using CircuitOneStroke.View;
using UnityEngine;

namespace CircuitOneStroke.Input
{
    public enum DrawState
    {
        Idle,
        Selecting,
        Completed,
        Failed
    }

    /// <summary>
    /// Tap-based one-stroke input:
    /// 1) tap a start node,
    /// 2) legal next nodes are highlighted,
    /// 3) tap a highlighted node to commit one edge.
    /// </summary>
    public class TouchInputController : MonoBehaviour
    {
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask nodeLayer = -1;

        private GameStateMachine _stateMachine;
        private DrawState _drawState = DrawState.Idle;

        private void Start()
        {
            if (levelLoader != null)
            {
                levelLoader.OnStateMachineChanged += HandleStateMachineChanged;
                HandleStateMachineChanged(levelLoader.StateMachine);
            }
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (levelLoader != null && levelLoader.StateMachine != _stateMachine)
                HandleStateMachineChanged(levelLoader.StateMachine);
        }

        private void OnDestroy()
        {
            if (levelLoader != null)
                levelLoader.OnStateMachineChanged -= HandleStateMachineChanged;
            if (_stateMachine != null)
                _stateMachine.OnStateChanged -= OnGameStateChanged;
        }

        private void HandleStateMachineChanged(GameStateMachine stateMachine)
        {
            if (_stateMachine != null)
                _stateMachine.OnStateChanged -= OnGameStateChanged;

            _stateMachine = stateMachine;

            if (_stateMachine != null)
            {
                _stateMachine.OnStateChanged += OnGameStateChanged;
                OnGameStateChanged(_stateMachine.State);
            }
            else
            {
                _drawState = DrawState.Idle;
                levelLoader?.SetMoveHints(-1, null, null);
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Idle)
            {
                _drawState = DrawState.Idle;
                levelLoader?.SetMoveHints(-1, null, null);
                return;
            }

            if (state == GameState.Drawing)
            {
                _drawState = DrawState.Selecting;
                RefreshHintsFromCurrent();
                return;
            }

            if (state == GameState.LevelComplete)
            {
                _drawState = DrawState.Completed;
                levelLoader?.SetMoveHints(-1, null, null);
                return;
            }

            if (state == GameState.LevelFailed || state == GameState.OutOfHearts)
            {
                _drawState = DrawState.Failed;
                levelLoader?.SetMoveHints(-1, null, null);
            }
        }

        private void Update()
        {
            if (_stateMachine == null || levelLoader?.Runtime == null) return;
            if (Core.TransitionManager.Instance != null && Core.TransitionManager.Instance.IsTransitioning) return;

            if (UnityEngine.Input.touchCount > 0)
            {
                var touch = UnityEngine.Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    Vector2 worldPos = ScreenToWorld(touch.position);
                    HandleTap(worldPos);
                }
                return;
            }

            if (Application.isEditor && UnityEngine.Input.GetMouseButtonDown(0))
            {
                Vector2 worldPos = ScreenToWorld(UnityEngine.Input.mousePosition);
                HandleTap(worldPos);
            }
        }

        private Vector2 ScreenToWorld(Vector3 screenPos)
        {
            var cam = mainCamera != null ? mainCamera : Camera.main;
            if (cam == null) return screenPos;
            screenPos.z = -cam.transform.position.z;
            var world = cam.ScreenToWorldPoint(screenPos);
            return new Vector2(world.x, world.y);
        }

        private void HandleTap(Vector2 worldPos)
        {
            int nodeId = HitNode(worldPos);
            if (nodeId < 0) return;

            if (_drawState == DrawState.Completed || _drawState == DrawState.Failed)
                return;

            if (_stateMachine.State == GameState.Idle)
            {
                TryStartAt(nodeId);
                return;
            }

            if (_stateMachine.State == GameState.Drawing && _drawState == DrawState.Selecting)
            {
                TryCommitTappedNode(nodeId);
            }
        }

        private int HitNode(Vector2 worldPos)
        {
            var hit = Physics2D.OverlapPoint(worldPos, nodeLayer);
            if (hit == null) return -1;
            var nv = hit.GetComponent<NodeView>();
            return nv != null ? nv.NodeId : -1;
        }

        private void TryStartAt(int nodeId)
        {
            _stateMachine.StartStroke(nodeId);
            if (_stateMachine.State != GameState.Drawing) return;
            _drawState = DrawState.Selecting;
            UpdateNodeVisitedStates();
            RefreshHintsFromCurrent();
        }

        private void TryCommitTappedNode(int targetNodeId)
        {
            var runtime = _stateMachine.Runtime;
            if (runtime == null) return;
            int fromNodeId = runtime.CurrentNodeId;
            if (fromNodeId < 0) return;
            if (targetNodeId == fromNodeId)
            {
                RefreshHintsFromCurrent();
                return;
            }

            var result = _stateMachine.TryMoveTo(targetNodeId);
            if (result == MoveResult.Ok)
            {
                UpdateNodeVisitedStates();
                Core.GameFeedback.Instance?.PlayMoveOk();

                if (runtime.VisitedBulbs.Count == runtime.TotalBulbCount)
                    _stateMachine.EndStroke();
                else
                    RefreshHintsFromCurrent();
                return;
            }

            if (result == MoveResult.Reject)
            {
                bool showRejectFeedback = Core.GameSettings.Instance?.Data?.rejectFeedbackEnabled ?? true;
                if (showRejectFeedback && levelLoader != null && runtime.Graph.TryGetEdge(fromNodeId, targetNodeId, out var edge))
                    levelLoader.GetEdgeView(edge.id)?.SetRejectFlash(true);
                Core.GameFeedback.Instance?.PlayReject();
                Core.GameFeedback.RequestToast("Invalid move");
                RefreshHintsFromCurrent();
                return;
            }

            if (result == MoveResult.HardFail)
            {
                Core.GameFeedback.Instance?.PlayFail();
                _stateMachine.OnHardFail("revisit_node");
            }
        }

        private void RefreshHintsFromCurrent()
        {
            if (_stateMachine?.Runtime == null || levelLoader == null)
                return;
            if (_stateMachine.State != GameState.Drawing)
            {
                levelLoader.SetMoveHints(-1, null, null);
                return;
            }

            int current = _stateMachine.Runtime.CurrentNodeId;
            if (current < 0)
            {
                levelLoader.SetMoveHints(-1, null, null);
                return;
            }

            var neighbors = _stateMachine.Runtime.Graph.GetNeighbors(current);
            var legalNodes = new List<int>(neighbors != null ? neighbors.Count : 0);
            var legalEdgeIds = new List<int>(neighbors != null ? neighbors.Count : 0);
            CollectLegalMoves(current, neighbors, legalNodes, legalEdgeIds);
            levelLoader.SetMoveHints(current, legalNodes, legalEdgeIds);

            if (legalNodes.Count == 0 && _stateMachine.Runtime.VisitedBulbs.Count < _stateMachine.Runtime.TotalBulbCount)
                _stateMachine.EndStroke();
        }

        private void CollectLegalMoves(
            int current,
            IReadOnlyList<(int neighborId, CircuitOneStroke.Data.EdgeData edge)> neighbors,
            List<int> legalNodes,
            List<int> legalEdgeIds)
        {
            legalNodes.Clear();
            legalEdgeIds.Clear();
            if (_stateMachine?.Runtime == null || neighbors == null) return;

            for (int i = 0; i < neighbors.Count; i++)
            {
                int neighbor = neighbors[i].neighborId;
                var edge = neighbors[i].edge;
                var node = _stateMachine.Runtime.GetNode(neighbor);
                if (node == null || node.nodeType == CircuitOneStroke.Data.NodeType.Blocked) continue;
                if (_stateMachine.Runtime.StrokeContains(neighbor)) continue;
                if (edge.gateGroupId >= 0 && !_stateMachine.Runtime.IsGateOpen(edge.id)) continue;

                bool fromAtoB = (current == edge.a && neighbor == edge.b);
                if (edge.diode == CircuitOneStroke.Data.DiodeMode.AtoB && !fromAtoB) continue;
                if (edge.diode == CircuitOneStroke.Data.DiodeMode.BtoA && fromAtoB) continue;

                legalNodes.Add(neighbor);
                legalEdgeIds.Add(edge.id);
            }
        }

        private void UpdateNodeVisitedStates()
        {
            levelLoader?.RefreshNodeViews();
        }
    }
}
