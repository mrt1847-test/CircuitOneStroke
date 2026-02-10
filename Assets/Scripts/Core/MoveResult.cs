namespace CircuitOneStroke.Core
{
    /// <summary>한 노드로의 이동 시도 결과. Reject=하트 소모 없음, HardFail=하트 소모.</summary>
    public enum MoveResult
    {
        /// <summary>이동 성공. 현재 노드 갱신·방문 처리·스위치 시 게이트 토글 완료.</summary>
        Ok,
        /// <summary>이동 불가(엣지 없음/게이트 닫힘/다이오드 역방향 등). 하트 소모 없음, 스트로크 유지.</summary>
        Reject,
        /// <summary>재방문 등 규칙 위반으로 Hard Fail. 하트 소모, 시도 종료.</summary>
        HardFail
    }
}
