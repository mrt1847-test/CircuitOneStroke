using UnityEngine;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.View
{
    /// <summary>
    /// LevelRuntime.StrokeNodes 순서대로 현재 한 붓 경로를 LineRenderer로 그림.
    /// LevelLoader가 로드 시 Bind(runtime) 호출.
    /// GameSettings.LineThickness 반영.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class StrokeRenderer : MonoBehaviour
    {
        [SerializeField] private Color strokeColor = new Color(1f, 0.9f, 0.2f);
        [SerializeField] private float lineWidthBase = 0.15f;

        private LineRenderer _lr;
        private LevelRuntime _runtime;

        private void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            _lr.useWorldSpace = true;
            _lr.startColor = _lr.endColor = strokeColor;
            ApplyLineWidth();
        }

        private void OnEnable()
        {
            GameSettings.Instance.OnChanged += OnSettingsChanged;
        }

        private void OnDisable()
        {
            GameSettings.Instance.OnChanged -= OnSettingsChanged;
        }

        private void OnSettingsChanged(GameSettingsData _) => ApplyLineWidth();

        private void ApplyLineWidth()
        {
            float w = GameSettings.Instance?.Data != null
                ? GameSettings.Instance.LineThicknessValue switch
                {
                    LineThickness.Thin => lineWidthBase * 0.7f,
                    LineThickness.Thick => lineWidthBase * 1.4f,
                    _ => lineWidthBase
                }
                : lineWidthBase;
            _lr.startWidth = _lr.endWidth = w;
        }

        /// <summary>LevelLoader.LoadCurrent에서 호출. 이후 Update에서 이 런타임의 StrokeNodes로 라인 갱신.</summary>
        public void Bind(LevelRuntime runtime)
        {
            _runtime = runtime;
        }

        /// <summary>매 프레임 StrokeNodes 경로로 라인 포지션 갱신. 노드 위치는 LevelData에서 조회.</summary>
        private void Update()
        {
            if (_runtime == null || _runtime.StrokeNodes == null || _runtime.StrokeNodes.Count == 0)
            {
                _lr.positionCount = 0;
                return;
            }

            var nodes = _runtime.LevelData?.nodes;
            if (nodes == null) return;

            _lr.positionCount = _runtime.StrokeNodes.Count;
            for (int i = 0; i < _runtime.StrokeNodes.Count; i++)
            {
                int id = _runtime.StrokeNodes[i];
                Vector2 pos = _runtime.GetNodePosition(id);
                _lr.SetPosition(i, new Vector3(pos.x, pos.y, -0.1f));
            }
        }
    }
}
