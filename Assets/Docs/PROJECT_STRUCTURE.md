# Circuit One-Stroke — 프로젝트 구조 및 내용

Android 대상 **한붓 퍼즐** 게임. 한 번의 연속 드래그로 모든 전구를 정확히 1회 방문하고, 다이오드/게이트/스위치 제약을 만족하면 클리어.

---

## 1. 루트 구조

```
CircuitOneStroke/
├── Assets/
│   ├── Art/          # UI·아트 에셋
│   ├── Docs/         # 프로젝트 문서
│   ├── Scripts/      # 게임 로직·뷰·에디터
│   └── UI/           # UI 테마·프리팹·스크립트
├── Packages/
│   └── manifest.json
└── README.md
```

---

## 2. Assets 폴더 구조

### 2.1 Art

| 경로 | 용도 |
|------|------|
| `Art/UI/KenneySciFi/` | Kenney UI Pack Sci-Fi (CC0) — 패널/버튼/프로그레스. 임포트 가이드: `Docs/UI_IMPORT.md` |
| `Art/UI/SkymonIcons/` | Skymon Icon Pack Free — 흰색 PNG 아이콘. 임포트 가이드: `Docs/UI_IMPORT.md` |

### 2.2 Docs

| 파일 | 내용 |
|------|------|
| `UI_IMPORT.md` | Kenney/Skymon 에셋 임포트 방법, 테마·프리팹 사용법, 스타일 가이드 |
| `PROJECT_STRUCTURE.md` | 이 문서 — 프로젝트 구조 및 모듈 설명 |

### 2.3 Scripts (게임 로직·뷰)

| 폴더 | 역할 | 주요 타입 |
|------|------|------------|
| **Core** | 게임 상태·레벨 로드·검증·설정·기록 | `GameStateMachine`, `LevelRuntime`, `LevelLoader`, `MoveValidator`, `GameSettings`, `LevelRecords`, `GameFeedback`, `GraphModel`, `MoveResult` |
| **Data** | 레벨/노드/엣지 데이터 정의 | `LevelData`, `NodeData`, `EdgeData`, `LevelManifest` |
| **Input** | 터치·마우스 입력 | `TouchInputController` (snapRadius / commitRadius) |
| **Solver** | 레벨 풀기 가능 여부·메트릭 | `LevelSolver` (DFS, gate 그룹별 토글, `MaxNodesSupported`) |
| **Generation** | 시드 기반 레벨 자동 생성 | `LevelGenerator`, `LevelTemplates`, `LayoutTemplates`, `AestheticEvaluator` |
| **View** | 노드·엣지·한붓 라인 시각화 | `NodeView`, `EdgeView`, `StrokeRenderer`, `EdgeCrossingMarkers` |
| **Editor** | 에디터 전용 도구 | `CreateDefaultLevel`, `CreateGameScene`, `LevelBakeTool` |

### 2.4 UI (테마·프리팹 시스템)

| 경로 | 용도 |
|------|------|
| `UI/Theme/` | `CircuitOneStrokeTheme` 에셋 (메뉴로 생성: Circuit One-Stroke → UI → Create Default Theme) |
| `UI/Prefabs/` | Panel, Button, ProgressSlider 프리팹 (메뉴로 생성: Circuit One-Stroke → UI → Create UI Prefabs) |
| `UI/Screens/` | 화면 구성용 (빈 폴더 또는 씬별 스크린 프리팹) |
| `UI/Scripts/` | 게임 UI 로직 + 테마·스타일·적용 로직 |

**UI/Scripts 주요 타입**

- **게임 UI**: `GameHUD`, `LevelSelectUI`, `SettingsPanel` — HUD·레벨 선택·설정 패널.
- `CircuitOneStrokeTheme` — ScriptableObject: 색상, 폰트, Kenney/Skymon 스프라이트 슬롯.
- `UIStyleConstants` — 테마 없을 때 폴백 색상 (다크 네이비, 네온 틸, 블루, 앰버/레드).
- `ThemeApplier` — 하위 UI에 테마 적용; 스프라이트 없으면 색상 플레이스홀더.
- `ThemeRole` — Image 역할(Panel, Button, SliderFill 등)로 스프라이트/색 지정.
- `ScreenRoot` — 화면 루트에 테마 적용.
- Editor: `CreateDefaultTheme`, `CreateUIPrefabs` — 테마·프리팹 생성 메뉴.

---

## 3. 데이터 모델 요약

### 3.1 LevelData (ScriptableObject)

- `levelId`, `nodes[]`, `edges[]`.
- Create Asset Menu: **Circuit One-Stroke → Level Data**.

### 3.2 NodeData

- `id`, `pos` (Vector2), `nodeType` (Bulb / Switch), `switchGroupId` (Switch일 때).

### 3.3 EdgeData

- `id`, `a`, `b` (노드 id), `diode` (None / AtoB / BtoA), `gateGroupId` (-1이면 일반 선, ≥0이면 게이트 그룹), `initialGateOpen`.

### 3.4 게임 규칙 (요약)

- **전구(Bulb)**: 한 붓 안에서 정확히 1회만 방문해야 함.
- **스위치(Switch)**: 방문 시 `switchGroupId`에 해당하는 게이트 그룹만 토글.
- **게이트**: 열림/닫힘 상태는 **엣지별**(`initialGateOpen`·런타임 `GateOpenByEdgeId`). 같은 `gateGroupId`의 엣지들은 **스위치 방문 시 함께 토글**되며, 닫힌 엣지는 통과 불가.
- **다이오드**: AtoB / BtoA 방향 제한.

---

## 4. 런타임 흐름 (Core)

1. **LevelLoader** — LevelData 로드, Node/Edge 뷰 생성, LevelRuntime에 데이터 전달.
2. **LevelRuntime** — 현재 노드, 방문 전구, 스트로크 경로, 게이트 상태, 노드 캐시(`GetNode`/`GetNodePosition`), 스트로크 집합(`StrokeContains`/`AddStrokeNode`/`ClearStrokeNodes`) 관리.
3. **GameStateMachine** — Idle → Drawing → Success/Fail; `StartStroke`, `TryMoveTo`, `EndStroke`.
4. **MoveValidator** — 엣지 존재, 게이트 열림, 다이오드, 노드 재방문(실패), 전구 중복 방문(실패) 검사.
5. **TouchInputController** — 터치/마우스 → 월드 좌표, 노드 히트, 인접 노드로 이동(snapRadius 내 후보, commitRadius 내 커밋).

---

## 5. Solver / Generation 연동

- **LevelSolver**: DFS로 풀기 가능 여부·해 개수·메트릭 계산. `MaxNodesSupported`(기본 25, N>12는 예산 평가) 초과 레벨은 미검증.
- **LevelGenerator**: `MaxNodesAllowed = LevelSolver.MaxNodesSupported`로 동일 상한 유지. 시드·난이도·스위치 포함 여부로 레벨 생성.

---

## 6. 리소스·씬·빌드

- **Resources**: 생성 레벨은 `Levels/GeneratedLevelManifest.asset`(LevelBakeTool이 저장)을 로드해 사용. 수동 레벨은 `Levels/Level_N.asset` 또는 씬에서 할당한 LevelManifest 사용.
- **Scenes**: GameScene 등은 README의 “씬 구성” 참고.
- **프리팹**: NodeView(SpriteRenderer + Collider2D + NodeView), EdgeView(LineRenderer + EdgeView), (선택) UI/Prefabs의 Panel/Button/ProgressSlider.

---

## 7. 설정·기록

- **GameSettings** (PlayerPrefs): Sound, Vibrate, Fail 모드(RejectOnly / ImmediateFail).
- **LevelRecords**: 레벨별 클리어 여부, 최단 시간.

---

이 문서는 프로젝트 구조와 각 모듈의 역할을 한곳에서 참고하기 위한 요약입니다. 씬 구성·Android 빌드 등은 루트 `README.md`를 참고하세요.
