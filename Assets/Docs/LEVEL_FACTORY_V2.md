# Level Factory v2 사용 가이드

Circuit One-Stroke의 16~25 노드 레벨을 에디터에서 생성·검증·저장하는 방법을 설명합니다.  
**무거운 연산(솔버/생성)은 모두 에디터 Bake에서만 수행되며, 런타임에서는 저장된 레벨만 로드합니다.**

---

## 목차

1. [개요](#1-개요)
2. [Level Bake 창 사용법](#2-level-bake-창-사용법)
3. [UI 필드 설명](#3-ui-필드-설명)
4. [워크플로우 (Bake / Dry Run)](#4-워크플로우-bake--dry-run)
5. [저장 위치와 매니페스트](#5-저장-위치와-매니페스트)
6. [런타임에서 레벨 로드](#6-런타임에서-레벨-로드)
7. [에디터 테스트 실행](#7-에디터-테스트-실행)
8. [트러블슈팅](#8-트러블슈팅)

---

## 1. 개요

- **Solver V2**: 16~25 노드 레벨의 풀기 가능 여부, 해 개수(상한 캡), 메트릭(분기점·트랩 깊이 등)을 시간 예산 내에서 계산합니다.
- **Backbone-first Generator**: “해가 되는 경로(백본)”를 먼저 만들고, 그 위에 디코이 엣지를 더해 선택지를 만듭니다. 즉시 막다른 길이 아닌, “몇 걸음 가다 실패”하는 구조를 지향합니다.
- **Level Bake 창**: 위 생성기와 Solver V2를 사용해 많은 후보를 만들고, 조건을 통과한 것만 에셋으로 저장한 뒤 `LevelManifest`에 등록합니다.

---

## 2. Level Bake 창 사용법

### 창 열기

Unity 메뉴에서 다음을 선택합니다.

- **Tools → Circuit One-Stroke → Level Bake**

창이 열리면 Pack 이름, 생성 개수, 시드, 노드 수 범위, 난이도, 해 개수 대역, 솔버 시간 예산, 출력 폴더 등을 설정할 수 있습니다.

### 기본 사용 순서

1. **Pack Name**에 팩 식별 이름 입력 (예: `V2Pack`, `Chapter2`).
2. **Count to Generate**에 만들 레벨 개수 입력 (예: `50`).
3. **Seed**를 지정하거나 **Randomize**로 새 시드 생성.
4. 필요 시 **Dry Run**으로 먼저 한 번 돌려서 통과/거절 통계 확인.
5. **Bake** 버튼으로 실제 생성·저장 실행.
6. **Open Output Folder**로 저장된 에셋 위치 확인.

---

## 3. UI 필드 설명

| 필드 | 설명 | 권장/기본 |
|------|------|-----------|
| **Pack Name** | 이번 Bake로 만드는 레벨 팩 이름. 에셋 파일명·폴더에 사용됨. | 예: `V2Pack` |
| **Count to Generate** | 저장할 레벨 개수. 이 수만큼 조건을 만족하는 레벨을 만들 때까지 시도합니다. | 20~100 |
| **Seed** | 난수 시드. 같은 시드면 같은 순서로 후보 생성. | 정수, Randomize로 변경 가능 |
| **Node Count Min / Max** | 생성 레벨의 노드 수 범위. | 16 ~ 25 |
| **Difficulty** | Easy / Medium / Hard. 스위치·게이트·다이오드 개수·강도에 반영. | Medium |
| **Target Solutions Min / Max** | 허용하는 해 개수 범위(캡 기준). 이 구간에 들어와야 통과. | 2 ~ 5 |
| **Solver Time Budget (ms)** | 레벨 하나당 Solver V2 최대 실행 시간(밀리초). 초과 시 Timeout 처리. | 150 |
| **Output Folder** | 에셋을 저장할 폴더 경로. 비워두면 `Assets/Resources/Levels/Generated/<PackName>/` 사용. | 기본값 사용 권장 |
| **Max Attempts Per Level** | 레벨 하나당 최대 시도 횟수. 이 안에 조건 통과 못 하면 해당 슬롯은 실패로 카운트. | 150~200 |
| **Decision Points Min / Max** | 통과 조건: “선택지가 2개 이상인 지점” 개수 범위. | 2 ~ 6 |
| **Avg Branching Max** | 통과 조건: 분기점에서의 평균 분기 수 상한. 너무 높으면 “추측” 느낌. | 2.3 |
| **Require Avg Trap Depth** | 켜면 “트랩 깊이” 조건 적용. 디코이가 너무 빨리 막히지 않게 함. | 필요 시만 켜기 |
| **Avg Trap Depth Min** | Require가 켜져 있을 때, 평균 트랩 깊이 하한. | 3.0 |
| **Max Crossings (aesthetics)** | 레이아웃 허용 최대 엣지 교차 수. | 1~2 |

### 버튼

- **Bake**: 설정대로 레벨 생성 → Solver V2 평가 → 조건 통과 시 에셋 저장 및 매니페스트 갱신.
- **Dry Run (no save)**: 동일 조건으로 생성·평가만 하고 저장하지 않음. 통과/거절 통계만 확인.
- **Open Output Folder**: 현재 **Output Folder** 경로를 탐색기에서 연다.

---

## 4. 워크플로우 (Bake / Dry Run)

### Dry Run으로 먼저 확인

1. Pack Name, Count(작게, 예: 5), Node 16~25, Difficulty, Target Solutions 2~5, Time Budget(150 ms) 설정.
2. **Dry Run** 클릭.
3. 하단 **Last run stats** 확인:
   - **Attempted**: 시도한 후보 수.
   - **Passed**: 조건 통과해 “저장될” 레벨 수.
   - **Rejected**: Unsat / Timeout / TooFew / TooMany / Metrics / Aesthetics 별 거절 횟수.

통과가 너무 적으면 **Max Attempts Per Level**를 늘리거나, **Target Solutions** 범위를 넓히거나, **Decision Points / Avg Branching** 조건을 완화해 보세요.

### Bake로 실제 저장

1. **Output Folder** 확인(또는 비워두어 기본 경로 사용).
2. **Bake** 클릭.
3. 진행 중에는 Console에 저장된 레벨 로그가 찍힙니다.
4. 완료 시 `Level Bake complete: N levels saved to ...` 로그와 함께 매니페스트가 갱신됩니다.

저장되는 에셋 이름 형식: `Level_<PackName>_<index>_<seed>.asset`  
예: `Level_V2Pack_1_1000.asset`, `Level_V2Pack_2_1001.asset`, …

---

## 5. 저장 위치와 매니페스트

### 저장 경로

- **기본**: `Assets/Resources/Levels/Generated/<PackName>/`
- **직접 지정**: **Output Folder**에 입력한 경로 (예: `Assets/Resources/Levels/MyPack`)

Resources 아래에 두면 런타임에 `Resources.Load`로 불러올 수 있습니다.

### LevelManifest

- Bake가 완료되면 **Assets/Resources/Levels/GeneratedLevelManifest.asset** 에 있는 `LevelManifest`에 이번 Bake로 저장된 레벨들이 **추가**됩니다.
- 기존 매니페스트가 없으면 자동 생성됩니다.
- 레벨 선택 화면에서 이 매니페스트를 참조하면, Bake로 만든 레벨들이 순서대로 나열됩니다.

---

## 6. 런타임에서 레벨 로드

- **레벨 생성/솔버 연산은 런타임에서 하지 않습니다.**  
  에디터 Bake로 저장된 `LevelData` 에셋만 로드합니다.

### 사용 방법

1. 씬의 **LevelLoader** 또는 **UIScreenRouter** 등에서 **LevelManifest** 참조를 다음 중 하나로 설정:
   - `Resources.Load<LevelManifest>("Levels/GeneratedLevelManifest")`
   - 또는 인스펙터에서 `Assets/Resources/Levels/GeneratedLevelManifest.asset` 할당
2. 레벨 선택 UI는 매니페스트의 `levels` 배열 순서대로 1번, 2번, … 레벨로 표시합니다.
3. `LevelLoader.LoadLevel(LevelData)` 또는 기존 플로우(레벨 인덱스로 매니페스트에서 가져오기)대로 로드하면 됩니다.

기존 `Level_1`, `Level_2` 같은 단일 에셋 로드 방식과 매니페스트 방식이 공존할 수 있으며, “어떤 매니페스트를 쓸지”만 지정해 주면 됩니다.

---

## 7. 에디터 테스트 실행

Level Factory v2 동작을 빠르게 검증하려면:

- 메뉴: **Tools → Circuit One-Stroke → Run Level Factory V2 Tests**

동작 요약:

- 10개 레벨 슬롯에 대해 BackboneFirstGenerator + Solver V2로 후보 생성·평가.
- Timeout이 과하게 나오지 않는지, 일정 비율 이상이 조건을 통과하는지 확인.
- 결과는 Console에 로그로 출력됩니다 (통과 개수, Timeout 횟수 등).

테스트가 실패하면 솔버 시간 예산(실제 Bake의 **Solver Time Budget**)이나 생성기 파라미터를 조정할 때 참고할 수 있습니다.

---

## 8. 트러블슈팅

| 현상 | 가능한 원인 | 대응 |
|------|-------------|------|
| **Passed가 0에 가깝다** | 조건이 너무 엄격하거나, Timeout이 많음. | **Max Attempts Per Level** 증가, **Target Solutions** 범위 확대, **Decision Points / Avg Branching** 완화, **Solver Time Budget** 200 ms 등으로 증가. |
| **Timeout이 많이 찍힌다** | 노드 수·그래프 복잡도에 비해 시간 예산이 짧음. | **Solver Time Budget (ms)** 를 200~300 정도로 올리기. |
| **저장은 되는데 플레이에서 안 보인다** | 매니페스트 미연결 또는 잘못된 매니페스트 참조. | GameHUD / LevelSelectScreen 등에서 사용하는 **LevelManifest**가 `GeneratedLevelManifest`를 가리키는지 확인. |
| **Bake가 오래 걸린다** | Count가 크고, 조건이 빡세서 시도 횟수가 많음. | Count를 줄이거나, **Dry Run**으로 먼저 통과율을 보고, 조건을 조금 완화한 뒤 Bake. |
| **같은 레벨이 반복되는 것 같다** | 시드가 고정되어 있고 Count가 시드 범위와 겹침. | **Seed**를 **Randomize**로 바꾸거나, Pack을 바꿔서 새 시드 구간 사용. |

---

## 관련 스크립트 위치

| 역할 | 경로 |
|------|------|
| Solver V2 | `Assets/Scripts/Solver/LevelSolverV2.cs` |
| Backbone-first Generator | `Assets/Scripts/Generation/BackboneFirstGenerator.cs` |
| Layout 템플릿 (16~25 노드) | `Assets/Scripts/Generation/LayoutTemplates.cs` (GetLayoutsForNodeCountV2) |
| Level Bake 창 | `Assets/Scripts/Editor/LevelBakeWindow.cs` |
| 에디터 테스트 | `Assets/Scripts/Editor/LevelFactoryV2Tests.cs` |
| 매니페스트·레벨 데이터 | `Assets/Scripts/Data/LevelManifest.cs`, `LevelData.cs` |

이 문서는 **Level Factory v2** 사용 방법을 위한 것이며, 기존 템플릿 기반 생성(LevelGenerator) 및 기존 Level Bake Tool은 그대로 사용할 수 있습니다.
