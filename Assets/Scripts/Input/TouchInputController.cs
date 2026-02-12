using UnityEngine;
using CircuitOneStroke.Core;
using CircuitOneStroke.View;

namespace CircuitOneStroke.Input
{
    /// <summary>?낅젰 痢≪쓽 洹몃━湲??곹깭. PointerUp ??Fail ???Paused濡??꾪솚, 瑗щ━ ?몃뱶?먯꽌留??ш컻.</summary>
    public enum DrawState
    {
        Idle,
        Dragging,
        Paused,
        Completed,
        Failed
    }

    /// <summary>
    /// ?곗튂/?먮뵒??留덉슦???낅젰??諛쏆븘 ?ㅽ듃濡쒗겕 ?쒖옉쨌?대룞쨌醫낅즺瑜?GameStateMachine???꾨떖.
    /// ?ㅻ깄/而ㅻ컠 諛섍꼍?쇰줈 ?몄젒 ?몃뱶留??꾨낫濡??먭퀬, commitRadius ?덉뿉?쒕쭔 TryMoveTo ?몄텧.
    /// PointerUp ??利됱떆 Fail ?섏? ?딄퀬 Paused; 瑗щ━ ?몃뱶 洹쇱쿂?먯꽌留??ш컻 媛??
    /// </summary>
    /// <remarks>?낅젰 寃쎈줈: ?덇굅??Input (GetTouch/GetMouseButton). UI??EventSystem+StandaloneInputModule. Input System Only ?꾪솚 ????Input API ?먮뒗 PlayerInput?쇰줈 ?댁쟾 ?꾩슂. See Assets/Docs/INPUT_COMPATIBILITY.md</remarks>
    public class TouchInputController : MonoBehaviour
    {
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private Camera mainCamera;
        [Tooltip("??嫄곕━ ?덉쓽 ?댁썐留??ㅻ깄 ?꾨낫濡?怨좊젮?? ??踰붿쐞 諛뽰씠硫?而ㅻ컠?섏? ?딆쓬.")]
        [SerializeField] private float snapRadiusBase = 1.5f;
        [Tooltip("?ㅻ깄 ?꾨낫 ?꾪솚 ???붽뎄?섎뒗 嫄곕━ 李⑥씠(誘명꽣). 而ㅼ쭏?섎줉 源쒕묀??媛먯냼.")]
        [SerializeField] private float snapHysteresisBase = 0.2f;
        [SerializeField] private LayerMask nodeLayer = -1;
        [Tooltip("Paused ?곹깭?먯꽌 ??嫄곕━ ?덉뿉???곗튂?댁빞 瑗щ━ ?몃뱶?먯꽌 ?ш컻 媛??(?붾뱶 ?⑥쐞).")]
        [SerializeField] private float resumeRadius = 0.6f;

        private GameStateMachine _stateMachine;
        private int _lastCommittedNodeId = -1;
        /// <summary>?꾩옱 ?ㅻ깄 ?꾨낫. ?덉뒪?뚮━?쒖뒪濡??꾪솚 ??源쒕묀??諛⑹?.</summary>
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
                levelLoader?.SetMoveHints(-1, null, null);
            }
            else if (state == GameState.Drawing)
            {
                // Dragging? HandleTouchStart/ResumeFromTail?먯꽌 ?ㅼ젙. ?ш린?쒕뒗 Paused??????덉쑝誘濡??좎?.
                if (_drawState != DrawState.Paused)
                    _drawState = DrawState.Dragging;
            }
            else if (state == GameState.LevelComplete)
            {
                _drawState = DrawState.Completed;
                HighlightTail(false);
                levelLoader?.SetMoveHints(-1, null, null);
            }
            else if (state == GameState.LevelFailed || state == GameState.OutOfHearts)
            {
                _drawState = DrawState.Failed;
                HighlightTail(false);
                levelLoader?.SetMoveHints(-1, null, null);
            }
        }

        /// <summary>?곗튂 ???곗튂 ?낅젰, ?먮뵒?곗뿉?쒕뒗 留덉슦?ㅻ줈 ?곗튂 ?쒕??덉씠??</summary>
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

        /// <summary>Idle: ?쒖옉 ?몃뱶?먯꽌留??ㅽ듃濡쒗겕 ?쒖옉. Paused: 瑗щ━ ?몃뱶 洹쇱쿂?먯꽌留??ш컻, ?꾨땲硫??좎뒪??</summary>
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

        /// <summary>Dragging???뚮쭔. ?꾩옱 ?몃뱶 ?댁썐 以?媛??媛源뚯슫 ?몃뱶瑜??ㅻ깄 ?꾨낫濡??먭퀬, commitRadius ?대㈃ ?대룞 ?쒕룄.</summary>
        private void HandleTouchMove(Vector3 worldPos)
        {
            if (_stateMachine.State != GameState.Drawing || _drawState != DrawState.Dragging) return;

            int current = _stateMachine.Runtime.CurrentNodeId;
            var neighbors = _stateMachine.Runtime.Graph.GetNeighbors(current);
            if (neighbors == null) return;

            var legalNodes = new System.Collections.Generic.List<int>(neighbors.Count);
            var legalEdgeIds = new System.Collections.Generic.List<int>(neighbors.Count);
            CollectLegalMoves(current, neighbors, legalNodes, legalEdgeIds);
            levelLoader?.SetMoveHints(current, legalNodes, legalEdgeIds);

            int bestNeighbor = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < legalNodes.Count; i++)
            {
                int neighborId = legalNodes[i];
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

            // ?덉뒪?뚮━?쒖뒪: 湲곗〈 ?ㅻ깄 ?꾨낫媛 ?좏슚?섎㈃ ???꾨낫媛 ?뺤떎????媛源뚯슱 ?뚮쭔 ?꾪솚 (源쒕묀??諛⑹?)
            if (_snapCandidateId >= 0 && bestNeighbor >= 0 && _snapCandidateId != bestNeighbor)
            {
                float distToCurrent = Vector2.Distance(worldPos, _stateMachine.Runtime.GetNodePosition(_snapCandidateId));
                if (distToCurrent <= snapR && bestDist >= distToCurrent - hyst)
                    bestNeighbor = _snapCandidateId;
            }
            if (bestNeighbor >= 0 && legalNodes.Contains(bestNeighbor))
                _snapCandidateId = bestNeighbor;
            else if (bestNeighbor < 0)
                _snapCandidateId = -1;

            // ?대? ?ㅽ듃濡쒗겕???ы븿???몃뱶濡쒕뒗 ?대룞 ?쒕룄?섏? ?딆쓬 (?먭??쎌씠 ?쒖옉 ?몃뱶 履쎌쑝濡??뚯븘????利됱떆 寃뚯엫?ㅻ쾭 諛⑹?)
            if (bestNeighbor >= 0 && bestDist <= commitR && bestNeighbor != _lastCommittedNodeId
                && !_stateMachine.Runtime.StrokeContains(bestNeighbor))
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

        /// <summary>?먭????쇰㈃: ?대━??議곌굔?대㈃ EndStroke(?깃났), ?꾨땲硫?Paused. ?ш컻??瑗щ━ ?몃뱶?먯꽌留?媛??</summary>
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

        /// <summary>?붾뱶 醫뚰몴?먯꽌 nodeLayer濡?OverlapPoint. NodeView媛 ?덉쑝硫??대떦 NodeId 諛섑솚.</summary>
        private int HitNode(Vector3 worldPos)
        {
            var hit = Physics2D.OverlapPoint(new Vector2(worldPos.x, worldPos.y), nodeLayer);
            if (hit == null) return -1;
            var nv = hit.GetComponent<NodeView>();
            return nv != null ? nv.NodeId : -1;
        }

        /// <summary>諛⑸Ц ?꾧뎄 諛섏쁺???몃뱶 酉????곹깭 媛깆떊.</summary>
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
        }

        private void ResumeFromTail()
        {
            _drawState = DrawState.Dragging;
            HighlightTail(false);
            levelLoader?.SetMoveHints(-1, null, null);
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
    }
}
