using UnityEngine;
using CircuitOneStroke.Core;
using CircuitOneStroke.View;

namespace CircuitOneStroke.Input
{
    /// <summary>입력 측의 그리기 상태. PointerUp 시 Fail 대신 Paused로 전환, 꼬리 노드에서만 재개.</summary>
    public enum DrawState
    {
        Idle,
        Dragging,
        Paused,
        Completed,
        Failed
    }

    /// <summary>
    /// 터치/에디터 마우스 입력을 받아 스트로크 시작·이동·종료를 GameStateMachine에 전달.
    /// 스냅/커밋 반경으로 인접 노드만 후보로 두고, commitRadius 안에서만 TryMoveTo 호출.
    /// PointerUp 시 즉시 Fail 하지 않고 Paused; 꼬리 노드 근처에서만 재개 가능.
    /// </summary>
    /// <remarks>입력 경로: 레거시 Input (GetTouch/GetMouseButton). UI는 EventSystem+StandaloneInputModule. Input System Only 전환 시 새 Input API 또는 PlayerInput으로 이전 필요. See Assets/Docs/INPUT_COMPATIBILITY.md</remarks>
    public class TouchInputController : MonoBehaviour
    {
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private Camera mainCamera;
        [Tooltip("이 거리 안의 이웃만 스냅 후보로 고려함. 이 범위 밖이면 커밋하지 않음.")]
        [SerializeField] private float snapRadiusBase = 1.5f;
        [Tooltip("스냅 후보 전환 시 요구되는 거리 차이(미터). 커질수록 깜빡임 감소.")]
        [SerializeField] private float snapHysteresisBase = 0.2f;
        [SerializeField] private LayerMask nodeLayer = -1;
        [Tooltip("Paused 상태에서 이 거리 안에서 터치해야 꼬리 노드에서 재개 가능 (월드 단위).")]
        [SerializeField] private float resumeRadius = 0.6f;

        private GameStateMachine _stateMachine;
        private int _lastCommittedNodeId = -1;
        /// <summary>현재 스냅 후보. 히스테리시스로 전환 시 깜빡임 방지.</summary>
        private int _snapCandidateId = -1;
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
                _snapCandidateId = -1;
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Idle)
            {
                _lastCommittedNodeId = -1;
                _snapCandidateId = -1;
                _drawState = DrawState.Idle;
                HighlightTail(false);
            }
            else if (state == GameState.Drawing)
            {
                // Dragging은 HandleTouchStart/ResumeFromTail에서 설정. 여기서는 Paused였을 수 있으므로 유지.
                if (_drawState != DrawState.Paused)
                    _drawState = DrawState.Dragging;
            }
            else if (state == GameState.LevelComplete)
            {
                _drawState = DrawState.Completed;
                HighlightTail(false);
            }
            else if (state == GameState.LevelFailed || state == GameState.OutOfHearts)
            {
                _drawState = DrawState.Failed;
                HighlightTail(false);
            }
        }

        /// <summary>터치 시 터치 입력, 에디터에서는 마우스로 터치 시뮬레이션.</summary>
        private void Update()
        {
            if (_stateMachine == null || levelLoader?.Runtime == null) return;
            if (Core.TransitionManager.Instance != null && Core.TransitionManager.Instance.IsTransitioning) return;

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

        /// <summary>Idle: 시작 노드에서만 스트로크 시작. Paused: 꼬리 노드 근처에서만 재개, 아니면 토스트.</summary>
        private void HandleTouchStart(Vector3 worldPos)
        {
            if (_drawState == DrawState.Dragging)
                return;

            if (_drawState == DrawState.Paused)
            {
                if (IsTouchNearTail(worldPos))
                    ResumeFromTail();
                else
                {
                    Core.GameFeedback.RequestToast(Core.Localization.Get("toast_continue_from_tail"));
                }
                return;
            }

            if (_drawState == DrawState.Completed || _drawState == DrawState.Failed)
                return;

            if (_stateMachine.State != GameState.Idle) return;

            int nodeId = HitNode(worldPos);
            if (nodeId >= 0)
            {
                _stateMachine.StartStroke(nodeId);
                if (_stateMachine.State == GameState.Drawing)
                {
                    _drawState = DrawState.Dragging;
                    _lastCommittedNodeId = nodeId;
                    UpdateNodeVisitedStates();
                }
            }
        }

        /// <summary>Dragging일 때만. 현재 노드 이웃 중 가장 가까운 노드를 스냅 후보로 두고, commitRadius 내면 이동 시도.</summary>
        private void HandleTouchMove(Vector3 worldPos)
        {
            if (_stateMachine.State != GameState.Drawing || _drawState != DrawState.Dragging) return;

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

            float snapA = Core.GameSettings.Instance?.Data?.snapAssist ?? 0.7f;
            float snapR = snapRadiusBase * (0.8f + 0.4f * snapA);
            float commitR = snapR * 0.65f;
            float hyst = snapHysteresisBase * (0.5f + snapA);

            if (bestNeighbor >= 0 && bestDist > snapR)
                bestNeighbor = -1;

            // 히스테리시스: 기존 스냅 후보가 유효하면 새 후보가 확실히 더 가까울 때만 전환 (깜빡임 방지)
            if (_snapCandidateId >= 0 && bestNeighbor >= 0 && _snapCandidateId != bestNeighbor)
            {
                float distToCurrent = Vector2.Distance(worldPos, _stateMachine.Runtime.GetNodePosition(_snapCandidateId));
                if (distToCurrent <= snapR && bestDist >= distToCurrent - hyst)
                    bestNeighbor = _snapCandidateId;
            }
            if (bestNeighbor >= 0)
                _snapCandidateId = bestNeighbor;

            if (bestNeighbor >= 0 && bestDist <= commitR && bestNeighbor != _lastCommittedNodeId)
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
                    bool showRejectFeedback = Core.GameSettings.Instance?.Data?.rejectFeedbackEnabled ?? true;
                    if (showRejectFeedback && levelLoader != null && _stateMachine.Runtime.Graph.TryGetEdge(fromNode, bestNeighbor, out var edge))
                        levelLoader.GetEdgeView(edge.id)?.SetRejectFlash(true);
                    Core.GameFeedback.Instance?.PlayReject();
                    Core.GameFeedback.RequestToast("Invalid move");
                }
                else if (result == MoveResult.HardFail)
                {
                    Core.GameFeedback.Instance?.PlayFail();
                    _stateMachine.OnHardFail("revisit_node");
                }
            }
        }

        /// <summary>손가락 떼면: 클리어 조건이면 EndStroke(성공), 아니면 Paused. 재개는 꼬리 노드에서만 가능.</summary>
        private void HandleTouchEnd()
        {
            if (_drawState != DrawState.Dragging) return;
            var rt = _stateMachine.Runtime;
            if (rt != null && rt.VisitedBulbs.Count == rt.TotalBulbCount)
                _stateMachine.EndStroke();
            else
                SetPaused();
            // TODO: optional subtle toast when lifted mid-edge: "Release on a node to lock the connection"
            // TODO: optional "Undo 1 step" button or debug shortcut to remove last visited node/edge when Paused
        }

        /// <summary>월드 좌표에서 nodeLayer로 OverlapPoint. NodeView가 있으면 해당 NodeId 반환.</summary>
        private int HitNode(Vector3 worldPos)
        {
            var hit = Physics2D.OverlapPoint(new Vector2(worldPos.x, worldPos.y), nodeLayer);
            if (hit == null) return -1;
            var nv = hit.GetComponent<NodeView>();
            return nv != null ? nv.NodeId : -1;
        }

        /// <summary>방문 전구 반영해 노드 뷰 색/상태 갱신.</summary>
        private void UpdateNodeVisitedStates()
        {
            if (levelLoader != null)
                levelLoader.RefreshNodeViews();
        }

        private int GetTailNodeId()
        {
            return _stateMachine != null && _stateMachine.Runtime != null ? _stateMachine.Runtime.CurrentNodeId : -1;
        }

        private bool IsTouchNearTail(Vector2 worldPos)
        {
            int tailId = GetTailNodeId();
            if (tailId < 0) return false;
            Vector2 tailPos = _stateMachine.Runtime.GetNodePosition(tailId);
            return Vector2.Distance(worldPos, tailPos) <= resumeRadius;
        }

        private void SetPaused()
        {
            _drawState = DrawState.Paused;
            HighlightTail(true);
        }

        private void ResumeFromTail()
        {
            _drawState = DrawState.Dragging;
            HighlightTail(false);
        }

        private void HighlightTail(bool on)
        {
            if (levelLoader == null) return;
            int tailId = GetTailNodeId();
            if (tailId < 0) return;
            var nv = levelLoader.GetNodeView(tailId);
            if (nv != null)
                nv.SetResumeHighlight(on);
        }
    }
}
