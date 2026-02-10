using UnityEngine;
using CircuitOneStroke.Core;
using CircuitOneStroke.View;

namespace CircuitOneStroke.Input
{
    public class TouchInputController : MonoBehaviour
    {
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private Camera mainCamera;
        [Tooltip("이 거리 안의 이웃만 스냅 후보로 고려함. 이 범위 밖이면 커밋하지 않음.")]
        [SerializeField] private float snapRadius = 1.5f;
        [Tooltip("스냅 후보 중 이 거리 안에 들어와야 실제로 이동(커밋)함. snapRadius 이하여야 함.")]
        [SerializeField] private float commitRadius = 1f;
        [SerializeField] private LayerMask nodeLayer = -1;

        private GameStateMachine _stateMachine;
        private int _lastCommittedNodeId = -1;

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
                _lastCommittedNodeId = -1;
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Idle)
                _lastCommittedNodeId = -1;
        }

        private void Update()
        {
            if (_stateMachine == null || levelLoader?.Runtime == null) return;

            if (UnityEngine.Input.touchCount > 0)
            {
                var touch = UnityEngine.Input.GetTouch(0);
                var worldPos = mainCamera != null ? mainCamera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, -mainCamera.transform.position.z)) : (Vector3)touch.position;
                worldPos.z = 0f;

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        HandleTouchStart(worldPos);
                        break;
                    case TouchPhase.Moved:
                        HandleTouchMove(worldPos);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        HandleTouchEnd();
                        break;
                }
            }
            else
            {
                if (Application.isEditor)
                    HandleEditorInput();
            }
        }

        private void HandleEditorInput()
        {
            var cam = mainCamera != null ? mainCamera : Camera.main;
            if (cam == null) return;
            var mouse = UnityEngine.Input.mousePosition;
            mouse.z = -cam.transform.position.z;
            var worldPos = cam.ScreenToWorldPoint(mouse);
            worldPos.z = 0f;

            if (UnityEngine.Input.GetMouseButtonDown(0))
                HandleTouchStart(worldPos);
            else if (UnityEngine.Input.GetMouseButton(0))
                HandleTouchMove(worldPos);
            else if (UnityEngine.Input.GetMouseButtonUp(0))
                HandleTouchEnd();
        }

        private void HandleTouchStart(Vector3 worldPos)
        {
            if (_stateMachine.State != GameState.Idle) return;

            int nodeId = HitNode(worldPos);
            if (nodeId >= 0)
            {
                _stateMachine.StartStroke(nodeId);
                _lastCommittedNodeId = nodeId;
                UpdateNodeVisitedStates();
            }
        }

        private void HandleTouchMove(Vector3 worldPos)
        {
            if (_stateMachine.State != GameState.Drawing) return;

            int current = _stateMachine.Runtime.CurrentNodeId;
            var neighbors = _stateMachine.Runtime.Graph.GetNeighbors(current);
            if (neighbors == null) return;

            int bestNeighbor = -1;
            float bestDist = float.MaxValue;
            foreach (var (neighborId, _) in neighbors)
            {
                var nodePos = _stateMachine.Runtime.GetNodePosition(neighborId);
                float d = Vector2.Distance(worldPos, nodePos);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestNeighbor = neighborId;
                }
            }

            // 스냅 영역 밖의 이웃은 후보에서 제외 (먼 노드로 오인 커밋 방지)
            if (bestNeighbor >= 0 && bestDist > snapRadius)
                bestNeighbor = -1;

            if (bestNeighbor >= 0 && bestDist <= commitRadius && bestNeighbor != _lastCommittedNodeId)
            {
                int fromNode = _stateMachine.Runtime.CurrentNodeId;
                var result = _stateMachine.TryMoveTo(bestNeighbor);
                if (result == MoveResult.Ok)
                {
                    _lastCommittedNodeId = bestNeighbor;
                    UpdateNodeVisitedStates();
                    Core.GameFeedback.Instance?.PlayMoveOk();
                }
                else if (result == MoveResult.Reject)
                {
                    if (levelLoader != null && _stateMachine.Runtime.Graph.TryGetEdge(fromNode, bestNeighbor, out var edge))
                        levelLoader.GetEdgeView(edge.id)?.SetRejectFlash(true);
                    Core.GameFeedback.Instance?.PlayReject();
                    if (Core.GameSettings.FailMode == Core.FailFeedbackMode.ImmediateFail)
                        _stateMachine.EndStroke();
                }
                else if (result == MoveResult.Fail)
                {
                    Core.GameFeedback.Instance?.PlayFail();
                    _stateMachine.EndStroke();
                }
            }
        }

        private void HandleTouchEnd()
        {
            if (_stateMachine.State == GameState.Drawing)
                _stateMachine.EndStroke();
        }

        private int HitNode(Vector3 worldPos)
        {
            var hit = Physics2D.OverlapPoint(new Vector2(worldPos.x, worldPos.y), nodeLayer);
            if (hit == null) return -1;
            var nv = hit.GetComponent<NodeView>();
            return nv != null ? nv.NodeId : -1;
        }

        private void UpdateNodeVisitedStates()
        {
            if (levelLoader != null)
                levelLoader.RefreshNodeViews();
        }
    }
}
