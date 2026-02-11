# View 렌더링·정렬 규칙 및 가시성 체크리스트

## 1. 렌더 규칙 (ViewRenderingConstants)

모든 EdgeView, NodeView, StrokeRenderer, 마커는 `ViewRenderingConstants`에 정의된 Order 값을 사용합니다.

| 컴포넌트 | Order | 설명 |
|----------|-------|------|
| Edge 선 (LineRenderer) | 0 | 엣지 기본 선 |
| Stroke (플레이어 경로) | 0 | z=-0.1로 엣지 뒤 |
| Edge crossing markers | 0 | 교차점 표시 |
| 다이오드 마커 | 1 | 선 위에 표시 |
| 게이트 마커 | 2 | 선 위에 표시 |
| 노드 (전구/스위치) | 3 | 엣지 위에 표시 |
| 노드 아이콘 | 4 | 전구/스위치 실루엣 |
| 디버그 오버레이 | 100 | 최상단 |

## 2. 레이어·스케일

- **SortingLayer**: 모두 `Default` (ID 0)
- **z 깊이**: Node z=0, Edge line z=0, Stroke z=-0.1, 마커 z=-0.05 ~ -0.08
- **마커 최소 스케일**: 다이오드 0.4, 게이트 0.35 (월드 단위, orthographicSize 6.5 기준)

## 3. 카메라·스케일링

- Orthographic 카메라 사용 (orthographicSize 6.5)
- 선 두께·마커 크기는 월드 단위 기준. 줌 변경 시 `ViewRenderingConstants.MinMarkerWorldScale` 등으로 최소 크기 클램프 권장

## 4. 변경된 파일 목록

| 파일 | 변경 내용 |
|------|-----------|
| `Scripts/View/ViewRenderingConstants.cs` | **신규**. Order 상수, 마커 최소 스케일 정의 |
| `Scripts/View/ProceduralSprites.cs` | **신규**. Circle, BulbShape, SwitchLever, DiodeTriangleBar, GateLock procedural 스프라이트 |
| `Scripts/View/NodeView.cs` | 스프라이트 null 시 fallback, 전구/스위치 아이콘 분리, Order 통일 |
| `Scripts/View/EdgeView.cs` | 다이오드 큰 삼각형+바 마커, 게이트 끊김 선+락 마커, Order 통일 |
| `Scripts/View/StrokeRenderer.cs` | Order 통일 |
| `Scripts/View/EdgeCrossingMarkers.cs` | Order 통일 |
| `Scripts/View/DebugVisibilityOverlay.cs` | **신규**. F3 토글로 노드/엣지 타입·상태 표시 |
| `Prefabs/NodeView.prefab` | switchColor 계열 색 변경 (형태 차별화 보조) |

## 5. 가시성 체크리스트

### 전구 (Bulb)

- [ ] 씬/캔버스 어디서든 전구가 **항상 보인다** (스프라이트 null fallback 적용)
- [ ] 전구 ON/OFF가 **즉시 구분**된다 (색 + 아이콘 밝기)
- [ ] 전구는 **원형 노드와 동일 형태가 아니다** (BulbShape 아이콘 사용)

### 스위치 (Switch)

- [ ] 스위치와 전구가 **형태로 구분**된다 (SwitchLever vs BulbShape)
- [ ] 스위치와 전구가 **색으로도 보조 구분**된다 (스위치: 보라 계열)

### 다이오드 (Diode)

- [ ] 그레이스케일로 봐도 **형태만으로 1초 내 인지** 가능 (삼각형+바 마커)
- [ ] 마커가 **작은 점이 아니다** (화면에서 20~28px급)

### 게이트 (Gate)

- [ ] 게이트 닫힘은 **"X 하나"가 아니다** (끊긴 선 + 락 마커)
- [ ] **끊김/잠금 느낌**이 체감된다

### 디버그 오버레이

- [ ] **F3** 토글로 활성화/비활성화
- [ ] 노드: Bulb(On/Off), Switch
- [ ] 엣지: Wire, Diode(A→B/B→A), Gate(Open/Closed)

## 6. 디버그 오버레이 사용법

1. 메뉴 **Circuit One-Stroke → Create Debug Visibility Overlay** 실행 또는
2. 씬에 빈 GameObject 생성 후 `DebugVisibilityOverlay` 컴포넌트 추가
3. (선택) LevelLoader, Camera 수동 할당. 미할당 시 자동 탐색
4. 플레이 후 **F3** 키로 토글
5. Editor/Development Build에서만 컴파일됨

## 7. 금지사항 (체크용)

- ❌ 선 두께만 조정해서 해결 시도
- ❌ 색상만 변경해서 구분
- ❌ 선 안의 작은 화살표 점
- ❌ 전구가 안 보이는 상태에서 On/Off 색상만 수정
