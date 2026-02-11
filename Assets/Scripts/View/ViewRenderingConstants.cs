namespace CircuitOneStroke.View
{
    /// <summary>
    /// EdgeView / NodeView / StrokeRenderer / 마커의 SortingLayer 및 OrderInLayer 규칙.
    /// 모든 게임 오브젝트가 동일한 규칙을 따르도록 통일.
    /// </summary>
    public static class ViewRenderingConstants
    {
        /// <summary>엣지 선(LineRenderer).</summary>
        public const int OrderEdges = 0;

        /// <summary>플레이어 경로(StrokeRenderer). z=-0.1로 엣지 뒤에 위치.</summary>
        public const int OrderStroke = 0;

        /// <summary>다이오드 방향 마커. 선 위에 표시.</summary>
        public const int OrderDiodeMarker = 1;

        /// <summary>게이트 닫힘 마커. 선 위에 표시.</summary>
        public const int OrderGateMarker = 2;

        /// <summary>노드(전구/스위치). 엣지·마커 위에 표시.</summary>
        public const int OrderNodes = 3;

        /// <summary>노드 아이콘(전구/스위치 실루엣). 노드 위.</summary>
        public const int OrderNodeIcon = 4;

        /// <summary>디버그 오버레이. 최상단.</summary>
        public const int OrderDebugOverlay = 100;

        /// <summary>orthographicSize 6.5 기준. 화면에서 ~24px로 보이게 하는 월드 스케일.</summary>
        public const float MinMarkerWorldScale = 0.35f;

        /// <summary>다이오드 마커 최소 월드 스케일. 형태로 1초 내 인지 가능하도록.</summary>
        public const float DiodeMarkerMinScale = 0.4f;

        /// <summary>게이트 마커 최소 월드 스케일.</summary>
        public const float GateMarkerMinScale = 0.35f;
    }
}
