using UnityEngine;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.View
{
    /// <summary>
    /// LevelRuntime.StrokeNodes 순서대로 현재 한 붓 경로를 LineRenderer로 그림.
    /// 2~3겹 라인(Outer/Core/Track) 구조로 흰 배경 대비 보장. 전류 텍스처 스크롤 + 간헐적 스파크 버스트만(점/비드 없음).
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class StrokeRenderer : MonoBehaviour
    {
        [Header("Line")]
        [SerializeField] private float lineWidthBase = 0.08f;
        [SerializeField] private bool singleLineStyle = true;

        [Header("Electric Flow (texture scroll)")]
        [SerializeField] private float flowSpeed = 2f;
        [SerializeField] private float textureScale = 4f;

        [Header("Spark bursts (at nodes, no continuous beads)")]
        [SerializeField, Range(2f, 8f)] private float sparkBurstsPerSecond = 3f;
        [SerializeField, Range(0.1f, 0.25f)] private float sparkLifetimeMin = 0.12f;
        [SerializeField, Range(0.1f, 0.25f)] private float sparkLifetimeMax = 0.22f;

        [Header("Flicker (optional)")]
        [SerializeField] private bool flickerEnabled = true;

        private LineRenderer _lrCore;
        private LineRenderer _lrOuter;
        private LineRenderer _lrTrack;
        private LevelRuntime _runtime;
        private Material _coreMaterial;
        private Material _outerMaterial;
        private Material _trackMaterial;
        private ParticleSystem _sparks;
        private Transform _sparkEmitter;
        private float _sparkBurstAccum;
        private Vector3[] _pathCache = new Vector3[64];
        private int _coreSortOrder;

        private const float CoreAlphaBase = 0.95f;
        private const float OuterAlphaBase = 0.55f;
        private float _flickerPhase;

        private void Awake()
        {
            _lrCore = GetComponent<LineRenderer>();
            _coreSortOrder = ViewRenderingConstants.OrderStroke;

            if (!singleLineStyle)
                CreateOuterAndTrack();
            ApplyElectricStyle();
            SetupElectricTexture();
            SetupSparkParticles();
        }

        private void CreateOuterAndTrack()
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");

            var outerGo = new GameObject("StrokeOuter");
            outerGo.transform.SetParent(transform, false);
            outerGo.transform.localPosition = Vector3.zero;
            _lrOuter = outerGo.AddComponent<LineRenderer>();

            var trackGo = new GameObject("StrokeTrack");
            trackGo.transform.SetParent(transform, false);
            trackGo.transform.localPosition = Vector3.zero;
            _lrTrack = trackGo.AddComponent<LineRenderer>();

            _outerMaterial = new Material(shader);
            _trackMaterial = new Material(shader);
        }

        /// <summary>색/두께/정렬/캡 설정을 한 곳에서 관리.</summary>
        private void ApplyElectricStyle()
        {
            float coreW = GetCoreWidth();

            _lrCore.useWorldSpace = true;
            _lrCore.startColor = new Color(0.32f, 0.80f, 0.90f, 0.96f);
            _lrCore.endColor = new Color(0.32f, 0.80f, 0.90f, 0.96f);
            _lrCore.startWidth = _lrCore.endWidth = coreW;
            _lrCore.numCapVertices = 5;
            _lrCore.numCornerVertices = 4;
            var coreR = _lrCore.GetComponent<Renderer>();
            if (coreR != null) coreR.sortingOrder = _coreSortOrder;

            if (singleLineStyle)
            {
                if (_lrOuter != null) _lrOuter.gameObject.SetActive(false);
                if (_lrTrack != null) _lrTrack.gameObject.SetActive(false);
                return;
            }

            float outerW = coreW * 2f;
            _lrOuter.useWorldSpace = true;
            _outerMaterial.color = new Color(0.10f, 0.55f, 1f, OuterAlphaBase);
            _lrOuter.material = _outerMaterial;
            _lrOuter.startColor = _lrOuter.endColor = Color.white;
            _lrOuter.startWidth = _lrOuter.endWidth = outerW;
            _lrOuter.numCapVertices = 6;
            _lrOuter.numCornerVertices = 6;
            var outerR = _lrOuter.GetComponent<Renderer>();
            if (outerR != null) outerR.sortingOrder = _coreSortOrder - 1;

            float trackW = coreW * 1.2f;
            _trackMaterial.color = new Color(0.12f, 0.14f, 0.20f, 0.25f);
            _lrTrack.material = _trackMaterial;
            _lrTrack.useWorldSpace = true;
            _lrTrack.startColor = _lrTrack.endColor = Color.white;
            _lrTrack.startWidth = _lrTrack.endWidth = trackW;
            _lrTrack.numCapVertices = 6;
            _lrTrack.numCornerVertices = 6;
            var trackR = _lrTrack.GetComponent<Renderer>();
            if (trackR != null) trackR.sortingOrder = _coreSortOrder - 2;
        }

        private float GetCoreWidth()
        {
            return GameSettings.Instance?.Data != null
                ? GameSettings.Instance.LineThicknessValue switch
                {
                    LineThickness.Thin => lineWidthBase * 0.7f,
                    LineThickness.Thick => lineWidthBase * 1.4f,
                    _ => lineWidthBase
                }
                : lineWidthBase;
        }

        private void SetupElectricTexture()
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            _coreMaterial = new Material(shader);
            _coreMaterial.mainTexture = ProceduralSprites.ElectricFlowTexture;
            _coreMaterial.mainTextureScale = new Vector2(textureScale, 1f);
            _coreMaterial.color = Color.white;
            _lrCore.material = _coreMaterial;
            _lrCore.textureMode = LineTextureMode.Tile;
            _lrCore.alignment = LineAlignment.View;
        }

        /// <summary>스파크는 버스트만 사용. rateOverTime=0으로 연속 점/비드 없음.</summary>
        private void SetupSparkParticles()
        {
            var go = new GameObject("StrokeSparks");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            _sparkEmitter = go.transform;

            _sparks = go.AddComponent<ParticleSystem>();
            var main = _sparks.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = sparkLifetimeMax;
            main.startSpeed = 0.12f;
            main.startSize = 0.09f;
            main.startColor = new Color(0.70f, 0.92f, 1f, 0.75f);
            main.maxParticles = 16;
            main.playOnAwake = false;

            var emission = _sparks.emission;
            emission.rateOverTime = 0f;
            emission.enabled = false;

            var shape = _sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.001f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = _coreSortOrder + 1;
            var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Particles/Standard Unlit"));
            mat.mainTexture = ProceduralSprites.Circle.texture;
            mat.color = Color.white;
            renderer.material = mat;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (GameSettings.Instance != null)
                GameSettings.Instance.OnChanged += OnSettingsChanged;
        }

        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            if (GameSettings.Instance != null)
                GameSettings.Instance.OnChanged -= OnSettingsChanged;
            if (_sparks != null && _sparks.isPlaying)
                _sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnDestroy()
        {
            if (_coreMaterial != null)
            {
                if (Application.isPlaying) Destroy(_coreMaterial);
                else DestroyImmediate(_coreMaterial);
            }
            if (_outerMaterial != null)
            {
                if (Application.isPlaying) Destroy(_outerMaterial);
                else DestroyImmediate(_outerMaterial);
            }
            if (_trackMaterial != null)
            {
                if (Application.isPlaying) Destroy(_trackMaterial);
                else DestroyImmediate(_trackMaterial);
            }
        }

        private void OnSettingsChanged(GameSettingsData _)
        {
            ApplyElectricStyle();
        }

        public void Bind(LevelRuntime runtime)
        {
            _runtime = runtime;
        }

        private void Update()
        {
            if (_runtime == null || _runtime.StrokeNodes == null || _runtime.StrokeNodes.Count == 0)
            {
                SetPositionCountAll(0);
                if (_sparks != null && _sparks.isPlaying)
                    _sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                return;
            }

            var nodes = _runtime.LevelData?.nodes;
            if (nodes == null) return;

            int n = _runtime.StrokeNodes.Count;
            if (_pathCache.Length < n)
            {
                int newLen = Mathf.Max(n, _pathCache.Length * 2);
                _pathCache = new Vector3[newLen];
            }

            for (int i = 0; i < n; i++)
            {
                int id = _runtime.StrokeNodes[i];
                Vector2 pos = _runtime.GetNodePosition(id);
                _pathCache[i] = new Vector3(pos.x, pos.y, -0.1f);
            }

            SetPositionCountAll(n);
            for (int i = 0; i < n; i++)
            {
                var p = _pathCache[i];
                _lrCore.SetPosition(i, p);
                if (!singleLineStyle)
                {
                    _lrOuter.SetPosition(i, p);
                    _lrTrack.SetPosition(i, p);
                }
            }

            float totalLen = 0f;
            for (int i = 1; i < n; i++)
                totalLen += Vector3.Distance(_pathCache[i - 1], _pathCache[i]);

            if (_coreMaterial != null)
            {
                float ts = totalLen > 0.01f ? totalLen * textureScale * 0.5f : textureScale;
                _coreMaterial.mainTextureScale = new Vector2(ts, 1f);
                float offset = Mathf.Repeat(Time.time * flowSpeed, 1f);
                _coreMaterial.mainTextureOffset = new Vector2(offset, 0f);
            }

            if (flickerEnabled)
            {
                _flickerPhase += Time.deltaTime * 9f;
                float coreAlpha = 0.88f + Mathf.PerlinNoise(_flickerPhase, 0f) * 0.08f;
                float outerAlpha = 0.45f + Mathf.PerlinNoise(_flickerPhase + 10f, 0f) * 0.2f;
                _lrCore.startColor = _lrCore.endColor = new Color(0.32f, 0.80f, 0.90f, coreAlpha);
                if (!singleLineStyle && _outerMaterial != null)
                    _outerMaterial.color = new Color(0.10f, 0.55f, 1f, outerAlpha);
            }

            float burstInterval = 1f / Mathf.Clamp(sparkBurstsPerSecond, 1f, 20f);
            _sparkBurstAccum += Time.deltaTime;
            while (_sparkBurstAccum >= burstInterval && n >= 1)
            {
                _sparkBurstAccum -= burstInterval;
                Vector3 sparkPos = GetSparkSpawnPosition(n);
                _sparkEmitter.position = sparkPos;
                var ep = new ParticleSystem.EmitParams();
                ep.startLifetime = Random.Range(sparkLifetimeMin, sparkLifetimeMax);
                _sparks.Emit(ep, 1);
            }
            if (n >= 1 && !_sparks.isPlaying)
                _sparks.Play();
        }

        private void SetPositionCountAll(int count)
        {
            _lrCore.positionCount = count;
            if (!singleLineStyle)
            {
                _lrOuter.positionCount = count;
                _lrTrack.positionCount = count;
            }
        }

        /// <summary>스파크 생성 위치: 접점(노드) 위주, 라인 중간은 최소화.</summary>
        private Vector3 GetSparkSpawnPosition(int n)
        {
            if (n <= 0) return Vector3.zero;
            if (n == 1) return _pathCache[0];
            bool atNode = Random.value < 0.7f;
            if (atNode)
            {
                int i;
                if (n == 2) i = Random.Range(0, 2);
                else if (n == 3) i = Random.Range(0, 3);
                else { int[] ends = new[] { 0, 1, n - 1, n - 2 }; i = ends[Random.Range(0, 4)]; }
                return _pathCache[i];
            }
            float t = Random.Range(0.1f, 0.9f);
            return GetPointAlongPath(t);
        }

        private Vector3 GetPointAlongPath(float t)
        {
            int n = _runtime?.StrokeNodes?.Count ?? 0;
            if (n == 0) return Vector3.zero;
            if (n == 1) return _pathCache[0];
            float totalLen = 0f;
            for (int i = 1; i < n; i++)
                totalLen += Vector3.Distance(_pathCache[i - 1], _pathCache[i]);
            if (totalLen < 0.001f) return _pathCache[0];
            float target = Mathf.Clamp01(t) * totalLen;
            float acc = 0f;
            for (int i = 1; i < n; i++)
            {
                float seg = Vector3.Distance(_pathCache[i - 1], _pathCache[i]);
                if (acc + seg >= target)
                {
                    float u = (target - acc) / seg;
                    return Vector3.Lerp(_pathCache[i - 1], _pathCache[i], u);
                }
                acc += seg;
            }
            return _pathCache[n - 1];
        }
    }
}
