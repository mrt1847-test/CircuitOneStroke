using UnityEngine;
using CircuitOneStroke.Core;
using CircuitOneStroke.View;

namespace CircuitOneStroke.Input
{
    public enum DrawState
    {
        Idle,
        Dragging,
        Paused,
        Completed,
        Failed
    }

    /// <summary>
    /// Touch/mouse input for one-stroke path drawing.
    /// Selection is previewed during drag and committed on release.
    /// </summary>
    public class TouchInputController : MonoBehaviour
    {
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private Camera mainCamera;
        [Tooltip("Base radius for snap/commit tuning.")]
        [SerializeField] private float snapRadiusBase = 1.5f;
        [Tooltip("Hysteresis value kept for compatibility with existing tuning.")]
        [SerializeField] private float snapHysteresisBase = 0.2f;
        [SerializeField] private LayerMask nodeLayer = -1;
        [Tooltip("When paused, touch near the tail node to resume drawing.")]
        [SerializeField] private float resumeRadius = 0.6f;

        [Header("Release Commit")]
        [Tooltip("Minimum drag distance from current node before a preview target appears.")]
        [SerializeField] private float minPreviewDragDistance = 0.32f;
        [SerializeField] private float previewLineWidth = 0.12f;
        [SerializeField] private float previewLockedLineWidth = 0.24f;
        [SerializeField] private Color previewLineColor = new Color(0.90f, 0.95f, 1f, 0.70f);
        [SerializeField] private Color previewLockedLineColor = new Color(1f, 0.98f, 0.75f, 0.98f);

        private GameStateMachine _stateMachine;
        private int _lastCommittedNodeId = -1;
        private int _snapCandidateId = -1;
        private Vector2 _lastPointerWorld;
        private DrawState _drawState = DrawState.Idle;

        private LineRenderer _previewLine;
        private Material _previewMaterial;

        private void Start()
        {
            EnsurePreviewLine();
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

            if (_previewMaterial != null)
            {
                if (Application.isPlaying) Destroy(_previewMaterial);
                else DestroyImmediate(_previewMaterial);
            }
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
                ClearPreviewLine();
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
                levelLoader?.SetMoveHints(-1, null, null);
                ClearPreviewLine();
            }
            else if (state == GameState.Drawing)
            {
                if (_drawState != DrawState.Paused)
                    _drawState = DrawState.Dragging;
            }
            else if (state == GameState.LevelComplete)
            {
                _drawState = DrawState.Completed;
                HighlightTail(false);
                levelLoader?.SetMoveHints(-1, null, null);
                ClearPreviewLine();
            }
            else if (state == GameState.LevelFailed || state == GameState.OutOfHearts)
            {
                _drawState = DrawState.Failed;
                HighlightTail(false);
                levelLoader?.SetMoveHints(-1, null, null);
                ClearPreviewLine();
            }
        }

        private void Update()
        {
            if (_stateMachine == null || levelLoader?.Runtime == null) return;
            if (Core.TransitionManager.Instance != null && Core.TransitionManager.Instance.IsTransitioning) return;

            if (UnityEngine.Input.touchCount > 0)
            {
                var touch = UnityEngine.Input.GetTouch(0);
                var worldPos = mainCamera != null
                    ? mainCamera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, -mainCamera.transform.position.z))
                    : (Vector3)touch.position;
                worldPos.z = 0f;

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        HandleTouchStart(worldPos);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
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
            if (_drawState == DrawState.Dragging)
                return;

            if (_drawState == DrawState.Paused)
            {
                if (IsTouchNearTail(worldPos))
                    ResumeFromTail();
                else
                    Core.GameFeedback.RequestToast(Core.Localization.Get("toast_continue_from_tail"));
                return;
            }

            if (_drawState == DrawState.Completed || _drawState == DrawState.Failed)
                return;

            if (_stateMachine.State != GameState.Idle)
                return;

            int nodeId = HitNode(worldPos);
            if (nodeId < 0) return;

            _stateMachine.StartStroke(nodeId);
            if (_stateMachine.State != GameState.Drawing) return;

            _drawState = DrawState.Dragging;
            _lastCommittedNodeId = nodeId;
            _snapCandidateId = -1;
            _lastPointerWorld = worldPos;
            ClearPreviewLine();
            UpdateNodeVisitedStates();
        }

        private void HandleTouchMove(Vector3 worldPos)
        {
            if (_stateMachine.State != GameState.Drawing || _drawState != DrawState.Dragging) return;

            _lastPointerWorld = worldPos;

            int current = _stateMachine.Runtime.CurrentNodeId;
            var neighbors = _stateMachine.Runtime.Graph.GetNeighbors(current);
            if (neighbors == null)
            {
                ClearPreviewLine();
                return;
            }

            var legalNodes = new System.Collections.Generic.List<int>(neighbors.Count);
            var legalEdgeIds = new System.Collections.Generic.List<int>(neighbors.Count);
            CollectLegalMoves(current, neighbors, legalNodes, legalEdgeIds);
            levelLoader?.SetMoveHints(current, legalNodes, legalEdgeIds);

            _snapCandidateId = SelectPreviewTargetNode(current, legalNodes, worldPos);
            UpdatePreviewLine(current, worldPos, _snapCandidateId);
        }

        private void HandleTouchEnd()
        {
            if (_drawState != DrawState.Dragging) return;

            TryCommitPreviewSelection();

            var rt = _stateMachine.Runtime;
            if (rt != null && rt.VisitedBulbs.Count == rt.TotalBulbCount)
                _stateMachine.EndStroke();
            else
                SetPaused();

            ClearPreviewLine();
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
            levelLoader?.SetMoveHints(-1, null, null);
            _snapCandidateId = -1;
            ClearPreviewLine();
        }

        private void ResumeFromTail()
        {
            _drawState = DrawState.Dragging;
            HighlightTail(false);
            levelLoader?.SetMoveHints(-1, null, null);
            _snapCandidateId = -1;
            ClearPreviewLine();
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

        private void CollectLegalMoves(
            int current,
            System.Collections.Generic.IReadOnlyList<(int neighborId, CircuitOneStroke.Data.EdgeData edge)> neighbors,
            System.Collections.Generic.List<int> legalNodes,
            System.Collections.Generic.List<int> legalEdgeIds)
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

        private void TryCommitPreviewSelection()
        {
            if (_stateMachine == null || _stateMachine.Runtime == null) return;
            if (_stateMachine.State != GameState.Drawing) return;

            int fromNode = _stateMachine.Runtime.CurrentNodeId;
            if (fromNode < 0) return;

            var neighbors = _stateMachine.Runtime.Graph.GetNeighbors(fromNode);
            if (neighbors == null || neighbors.Count == 0) return;

            var legalNodes = new System.Collections.Generic.List<int>(neighbors.Count);
            var legalEdgeIds = new System.Collections.Generic.List<int>(neighbors.Count);
            CollectLegalMoves(fromNode, neighbors, legalNodes, legalEdgeIds);

            int targetNode = SelectPreviewTargetNode(fromNode, legalNodes, _lastPointerWorld);
            if (targetNode < 0) return;
            if (!CanCommitOnRelease(fromNode, targetNode, _lastPointerWorld)) return;
            if (targetNode == _lastCommittedNodeId || _stateMachine.Runtime.StrokeContains(targetNode)) return;

            var result = _stateMachine.TryMoveTo(targetNode);
            if (result == MoveResult.Ok)
            {
                _lastCommittedNodeId = targetNode;
                UpdateNodeVisitedStates();
                Core.GameFeedback.Instance?.PlayMoveOk();
                return;
            }

            if (result == MoveResult.Reject)
            {
                bool showRejectFeedback = Core.GameSettings.Instance?.Data?.rejectFeedbackEnabled ?? true;
                if (showRejectFeedback && levelLoader != null && _stateMachine.Runtime.Graph.TryGetEdge(fromNode, targetNode, out var edge))
                    levelLoader.GetEdgeView(edge.id)?.SetRejectFlash(true);
                Core.GameFeedback.Instance?.PlayReject();
                Core.GameFeedback.RequestToast("Invalid move");
                return;
            }

            if (result == MoveResult.HardFail)
            {
                Core.GameFeedback.Instance?.PlayFail();
                _stateMachine.OnHardFail("revisit_node");
            }
        }

        private int SelectPreviewTargetNode(int currentNodeId, System.Collections.Generic.IReadOnlyList<int> legalNodes, Vector2 worldPos)
        {
            if (_stateMachine?.Runtime == null || legalNodes == null || legalNodes.Count == 0) return -1;

            float snapA = Core.GameSettings.Instance?.Data?.snapAssist ?? 0.7f;
            Vector2 currentPos = _stateMachine.Runtime.GetNodePosition(currentNodeId);
            Vector2 drag = worldPos - currentPos;
            float dragLen = drag.magnitude;
            if (dragLen < minPreviewDragDistance) return -1;

            Vector2 dragDir = drag / Mathf.Max(0.0001f, dragLen);
            float maxAngleDeg = Mathf.Lerp(36f, 20f, snapA);
            float maxPerpRatio = Mathf.Lerp(0.60f, 0.34f, snapA);

            int bestNode = -1;
            float bestScore = float.MaxValue;
            for (int i = 0; i < legalNodes.Count; i++)
            {
                int neighborId = legalNodes[i];
                Vector2 neighborPos = _stateMachine.Runtime.GetNodePosition(neighborId);
                Vector2 toNeighbor = neighborPos - currentPos;
                float edgeLen = toNeighbor.magnitude;
                if (edgeLen <= 0.001f) continue;
                Vector2 edgeDir = toNeighbor / edgeLen;

                float angle = Vector2.Angle(dragDir, edgeDir);
                if (angle > maxAngleDeg) continue;

                float along = Vector2.Dot(drag, edgeDir);
                if (along <= edgeLen * 0.10f) continue;

                float perp = Mathf.Abs(drag.x * edgeDir.y - drag.y * edgeDir.x);
                float perpRatio = perp / edgeLen;
                if (perpRatio > maxPerpRatio) continue;

                float distToNeighbor = Vector2.Distance(worldPos, neighborPos);
                float score = angle * 2.2f + perpRatio * 90f + distToNeighbor * 0.45f;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestNode = neighborId;
                }
            }

            return bestNode;
        }

        private bool CanCommitOnRelease(int fromNodeId, int toNodeId, Vector2 releaseWorldPos)
        {
            Vector2 from = _stateMachine.Runtime.GetNodePosition(fromNodeId);
            Vector2 to = _stateMachine.Runtime.GetNodePosition(toNodeId);
            Vector2 edge = to - from;
            float edgeLen = edge.magnitude;
            if (edgeLen <= 0.001f) return false;

            float snapA = Core.GameSettings.Instance?.Data?.snapAssist ?? 0.7f;
            float snapR = snapRadiusBase * (0.75f + 0.35f * snapA);
            float commitRadius = snapR * 0.60f;

            float distToTarget = Vector2.Distance(releaseWorldPos, to);
            if (distToTarget <= commitRadius) return true;

            Vector2 drag = releaseWorldPos - from;
            Vector2 edgeDir = edge / edgeLen;
            float along = Vector2.Dot(drag, edgeDir);
            if (along <= edgeLen * 0.55f) return false;

            float perp = Mathf.Abs(drag.x * edgeDir.y - drag.y * edgeDir.x);
            return perp <= edgeLen * 0.16f;
        }

        private void EnsurePreviewLine()
        {
            if (_previewLine != null) return;

            var go = new GameObject("DragPreviewLine");
            go.transform.SetParent(transform, false);
            _previewLine = go.AddComponent<LineRenderer>();
            _previewLine.useWorldSpace = true;
            _previewLine.positionCount = 2;
            _previewLine.numCapVertices = 6;
            _previewLine.numCornerVertices = 4;
            _previewLine.alignment = LineAlignment.View;
            _previewLine.textureMode = LineTextureMode.Stretch;

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            _previewMaterial = new Material(shader);
            _previewLine.material = _previewMaterial;

            var r = _previewLine.GetComponent<Renderer>();
            if (r != null)
                r.sortingOrder = ViewRenderingConstants.OrderNodeIcon + 1;

            ClearPreviewLine();
        }

        private void UpdatePreviewLine(int fromNodeId, Vector2 pointerWorld, int targetNodeId)
        {
            if (_stateMachine?.Runtime == null) return;
            EnsurePreviewLine();

            Vector2 from = _stateMachine.Runtime.GetNodePosition(fromNodeId);
            Vector2 to = targetNodeId >= 0 ? _stateMachine.Runtime.GetNodePosition(targetNodeId) : pointerWorld;

            _previewLine.enabled = true;
            _previewLine.startWidth = _previewLine.endWidth = targetNodeId >= 0 ? previewLockedLineWidth : previewLineWidth;
            Color c = targetNodeId >= 0 ? previewLockedLineColor : previewLineColor;
            _previewLine.startColor = c;
            _previewLine.endColor = c;
            _previewLine.SetPosition(0, new Vector3(from.x, from.y, -0.08f));
            _previewLine.SetPosition(1, new Vector3(to.x, to.y, -0.08f));
        }

        private void ClearPreviewLine()
        {
            if (_previewLine == null) return;
            _previewLine.enabled = false;
            _previewLine.positionCount = 2;
            _previewLine.SetPosition(0, Vector3.zero);
            _previewLine.SetPosition(1, Vector3.zero);
        }
    }
}
