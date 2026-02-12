# Level Bake 사용법

레벨을 자동 생성해 에셋으로 저장하고 매니페스트를 갱신하는 에디터 도구가 **두 가지** 있습니다. 용도와 메뉴가 다릅니다.

---

## 1. Level Bake Tool (템플릿 기반, 4~25노드)

**메뉴**: `Circuit One-Stroke > Level Bake Tool`

LevelTemplates에 정의된 **고정 노드 수 템플릿**(8, 9노드 등)을 사용해 레벨을 만들고, Solver로 난이도 필터를 거친 뒤 저장합니다.

### 1.1 창 열기

- 상단 메뉴에서 **Circuit One-Stroke > Level Bake Tool** 선택.

### 1.2 항목 설명

| 항목 | 의미 | 비고 |
|------|------|------|
| **Tier** | 난이도 구간. Easy / Medium / Hard. | 다이오드·게이트·스위치 개수와 Solver 필터(해 수, early branching, dead-end depth) 기준이 tier별로 다름. |
| **Target Count** | **생성할 레벨 개수.** | 이 수만큼 “통과한” 레벨이 나올 때까지 seed를 올려가며 생성. |
| **Seed Start** | 생성에 쓰는 **시드 시작값.** | 레벨마다 seed, seed+1, seed+2… 로 증가. 같은 Seed Start면 같은 순서로 레벨 생성. |
| **Use Grid Range (16–25 nodes)** | 켜면 **Grid Range 생성기** 사용(16~25노드). 끄면 **LevelGenerator.GenerateWithMetadata** 사용(템플릿 8,9노드 등). | 켜면 아래 Min/Max Nodes, Solver 예산 등이 활성화되고, Output Folder는 tier별 폴더로 고정됨. |
| **Min Nodes** | (Grid Range 사용 시) 노드 수 **하한.** | 16~25. |
| **Max Nodes** | (Grid Range 사용 시) 노드 수 **상한.** | 16~25, Min 이상. |
| **Max States Expanded** | (Grid Range 사용 시) Solver가 확장할 **상태 수 상한.** | 너무 크면 한 레벨 풀이가 오래 걸림. |
| **Max Millis (per level)** | (Grid Range 사용 시) 레벨당 Solver **시간 예산(ms).** | | 
| **Max Solutions (budget)** | (Grid Range 사용 시) 찾을 **해 개수 상한.** | 이 수만큼 찾으면 Solver 중단. |
| **Output Folder** | (Grid Range **미**사용 시) 레벨 에셋을 저장할 **폴더 경로.** | 예: `Assets/Levels/Generated`. |
| **Override Include Switch** | 스위치 포함 여부를 **Tier 기본값이 아닌 수동 값**으로 고정할지. | 체크 시 아래 “Include Switch”로 on/off 지정. |
| **Include Switch** | (Override 시) 레벨에 **스위치 노드 포함 여부.** | Easy는 기본 false, Medium/Hard는 기본 true. |

### 1.3 Advanced filter parameters (펼침)

Tier 기본 필터 대신 **직접 숫자**를 쓰고 싶을 때 사용합니다.

| 항목 | 의미 |
|------|------|
| **Solution count min** | 통과 조건: 해 개수 ≥ 이 값. |
| **Solution count max (Easy/Medium/Hard)** | Tier별 “해 개수 상한”. 이하면 통과. (Easy 80, Medium 120, Hard 200 등) |
| **Early branching min (Easy/Medium/Hard)** | Tier별 “초기 분기 지표” 하한. (Easy 1.4, Medium 1.7, Hard 2.0 등) |
| **Dead-end depth min/max (Easy/Medium/Hard)** | Tier별 “막다른 길 깊이 평균” 구간. 이 구간 안이어야 통과. |

- **Advanced**를 펼치지 않으면 **LevelGenerator.PassesFilter(tier, result)** 의 기본값(Easy/Medium/Hard 구간)이 사용됩니다.

### 1.4 동작 요약

- **Use Grid Range = false**:  
  `LevelGenerator.GenerateWithMetadata(tier, seed, …)` 로 레벨 생성 → `LevelSolver.Solve` → PassesFilter(또는 Advanced 커스텀) 통과 시 `Output Folder`에 `Level_1.asset`, `Level_2.asset` … 저장 + 매니페스트 갱신.
- **Use Grid Range = true**:  
  `GridRangeGenerator.Generate(minNodes, maxNodes, tier, seed)` 로 생성 → Solver 예산 내에서 풀리면 `Assets/Resources/Levels/Generated/{Tier}/` 에 저장 + tier별 매니페스트 갱신.

---

## 2. Level Bake (Factory v2, 16~25노드 + SolverV2)

**메뉴**: `Tools > Circuit One-Stroke > Level Bake`

**BackboneFirstGenerator**와 **SolverV2**를 사용해 16~25노드 레벨을 만들고, “해 개수·결정점·분기·함정 깊이” 등 조건을 만족하는 것만 저장합니다.

### 2.1 창 열기

- 상단 메뉴에서 **Tools > Circuit One-Stroke > Level Bake** 선택.

### 2.2 항목 설명

| 항목 | 의미 | 비고 |
|------|------|------|
| **Pack Name** | 이번에 만드는 **팩 이름.** | 출력 폴더가 `Assets/Resources/Levels/Generated/{Pack Name}` 으로 잡힐 수 있음. |
| **Count to Generate** | **만들 레벨 개수.** | Target Count와 동일한 개념. |
| **Seed** | 생성 시드. | “Randomize”로 랜덤 시드 생성 가능. 내부적으로 레벨·attempt별로 시드 오프셋 적용. |
| **Node Count Min** | 노드 수 **하한.** | 16~25. |
| **Node Count Max** | 노드 수 **상한.** | 16~25, Min 이상. |
| **Difficulty** | **Tier.** Easy / Medium / Hard. | BackboneFirstGenerator의 다이오드/게이트 등 난이도 파라미터에 반영. |
| **Target Solutions Min** | 통과 조건: SolverV2가 찾은 **해 개수 ≥ 이 값.** | |
| **Target Solutions Max** | 통과 조건: 해 개수 **≤ 이 값.** | 이 범위 안에 들어와야 통과. |
| **Solver Time Budget (ms)** | 레벨당 SolverV2 **시간 예산(ms).** | |
| **Output Folder** | 저장할 **폴더 경로.** | 비어 있으면 `Assets/Resources/Levels/Generated/{Pack Name}` 사용. |
| **Max Attempts Per Level** | **레벨 하나당** 시도 횟수 상한. | 이 횟수 안에 조건 만족하는 레벨이 안 나오면 해당 슬롯은 실패로 건너뜀. |
| **Decision Points Min / Max** | 통과 조건: SolverV2 메트릭 “결정점 수”가 이 구간 안. | |
| **Avg Branching Max** | 통과 조건: **평균 분기 수**가 이 값 이하. | |
| **Require Avg Trap Depth** | 켜면 “평균 함정 깊이” 조건 사용. | |
| **Avg Trap Depth Min** | (Require 시) 평균 함정 깊이 **하한.** | |
| **Max Crossings (aesthetics)** | 레이아웃 허용 **엣지 교차 수 상한.** | AestheticEvaluator.Accept(level, maxCrossings, …) 에 전달. |

### 2.3 버튼

- **Bake**: 위 조건으로 레벨 생성 후 **에셋 저장** + GeneratedLevelManifest 갱신.
- **Dry Run (no save)**: 같은 조건으로 생성·필터만 수행하고 **저장하지 않음.** 통과/거절 통계만 확인.
- **Open Output Folder**: 출력 폴더를 Finder/탐색기로 연다.

### 2.4 Last run stats

- **Attempted**: 시도한 레벨 수(attempt 단위).
- **Passed**: 조건 통과해 저장된 레벨 수.
- **Rejected**: Unsat(풀 수 없음), Timeout(시간 초과), TooFew/TooMany(해 개수), Metrics(결정점/분기/함정 깊이 등), Aesthetics(교차 등 레이아웃)로 거절된 횟수.

---

## 3. Tier(난이도) 의미 정리

| Tier | 일반적 의미 | Level Bake Tool (템플릿) | Level Bake (Factory v2) |
|------|-------------|---------------------------|--------------------------|
| **Easy** | 쉬운 퍼즐, 해 많고 분기 적음 | 스위치 없음, 다이오드 0~1, 게이트 0. 해 1~80, early branching ≥1.4, dead-end depth 2~6. | BackboneFirstGenerator Easy 설정. Target Solutions 범위·Decision Points 등으로 조정. |
| **Medium** | 중간, 스위치/게이트 등장 | 스위치 있음, 다이오드 1~2, 게이트 2~3. 해 1~120, early branching ≥1.7, dead-end depth 3~7. | Medium 설정. |
| **Hard** | 어려운 퍼즐, 해 적거나 분기 많음 | 스위치 있음, 다이오드 2~3, 게이트 3~5. 해 1~200, early branching ≥2.0, dead-end depth 4~8. | Hard 설정. |

---

## 4. 출력 위치·매니페스트

- **Level Bake Tool**  
  - Grid 미사용: `Output Folder`에 `Level_1.asset`, `Level_2.asset` … + `Level_*_meta.json`.  
  - Grid 사용: `Assets/Resources/Levels/Generated/{Tier}/` + tier별 매니페스트 `GeneratedLevelManifest_{Tier}.asset`.
- **Level Bake (Factory v2)**  
  - `Output Folder`(또는 `Generated/{Pack Name}`)에 `Level_{PackName}_{levelIndex}_{seed}.asset`.  
  - `Assets/Resources/Levels/GeneratedLevelManifest.asset` 에 레벨 목록 추가.

게임에서 레벨 목록을 쓸 때는 **LevelManifest**를 참조합니다. (LevelBakeTool/LevelBakeWindow가 이 매니페스트를 갱신함.)

**중요**: AppScene/GameScene은 **`GeneratedLevelManifest.asset`** 한 개만 참조합니다. 이 에셋의 **levels** 배열에 들어 있는 레벨만 게임에 표시됩니다.  
Level Bake Tool에서 Grid 사용 시 tier별로 **GeneratedLevelManifest_Medium.asset**, **GeneratedLevelManifest_Hard.asset** 등에만 저장하므로, 게임에 Bake한 레벨을 쓰려면 **GeneratedLevelManifest.asset**의 levels를 해당 tier 매니페스트와 동일하게 맞춰 두거나, 씬에서 매니페스트 참조를 `GeneratedLevelManifest_Medium`(또는 원하는 tier)으로 바꿔야 합니다.

---

## 5. 빠른 참조

- **템플릿 8·9노드, Easy/Medium/Hard 필터로 20개 만들기**  
  → **Level Bake Tool** 열기 → Tier 선택, Target Count 20, Use Grid Range 끔, Output Folder 지정 → **Generate & Save**.

- **16~25노드, 해 개수 2~5개, 결정점·분기 제한해서 50개 만들기**  
  → **Level Bake (Factory v2)** 열기 → Node Count 16~25, Target Solutions 2~5, Count to Generate 50, Difficulty 선택 → **Bake**.

- **저장 없이 조건만 테스트**  
  → Factory v2에서 **Dry Run (no save)**.

---

## 6. Level 1을 새로 만드는 방법

"Level 1"은 **게임에서 첫 번째로 플레이하는 레벨**을 말합니다. 아래 세 가지 중 목적에 맞는 방법을 쓰면 됩니다.

### 6.1 테스트용 기본 Level_1 한 개만 만들기 (가장 간단)

- **메뉴**: `Circuit One-Stroke > Create Default Test Level (Level_1)`
- **결과**: `Assets/Resources/Levels/Level_1.asset` 이 생성됩니다.
  - 4개 전구 + 6개 엣지의 단순 그래프, 다이오드/스위치 없음.
- **용도**: CreateGameScene·테스트 씬에서 `Resources.Load("Levels/Level_1")` 로 바로 로드할 때 사용. **LevelManifest와는 무관**합니다.
- **매니페스트 사용 시**: 이 에셋을 `GeneratedLevelManifest.asset` 의 **levels** 배열 첫 번째(인덱스 0)에 넣어 주면 게임의 "레벨 1"로 플레이됩니다.

### 6.2 Level Bake Tool로 "레벨 1개" 생성 후 매니페스트에 등록

- **Level Bake Tool** 열기 → **Target Count = 1** 설정.
- Tier, Use Grid Range(끄면 템플릿 8·9노드, 켜면 16~25노드), Output Folder 등 필요한 값 설정 후 **Generate & Save**.
- 생성된 `Level_1.asset` 이 지정한 Output(또는 Grid 사용 시 `Assets/Resources/Levels/Generated/{Tier}/`)에 저장되고, **매니페스트에 1개만** 등록됩니다. → 게임에서 그 매니페스트를 쓰면 그 레벨이 "Level 1"이 됩니다.

### 6.3 Level Bake (Factory v2)로 "레벨 1개" 생성

- **Level Bake** (Tools 메뉴) 열기 → **Count to Generate = 1**, Pack Name·Node Count·Difficulty·Target Solutions 등 설정 후 **Bake**.
- 한 개 레벨이 통과하면 Output Folder(또는 `Assets/Resources/Levels/Generated/{Pack Name}`)에 저장되고 `GeneratedLevelManifest.asset` 에 추가됩니다. 매니페스트에 레벨이 1개만 있으면 그 레벨이 "Level 1"입니다.

### 6.4 기존 팩을 비우고 Level 1만 새로 넣고 싶을 때

- **방법 A**: Level Bake Tool 또는 Factory v2에서 **Target Count / Count to Generate = 1** 로 새로 Bake한 뒤, 사용할 매니페스트를 **그 Bake가 갱신한 매니페스트**만 쓰도록 하면 됩니다. (기존 매니페스트를 덮어쓴다면 기존 레벨 목록은 새 1개로 대체될 수 있음.)
- **방법 B**: `Assets/Resources/Levels/GeneratedLevelManifest.asset` 을 Inspector에서 열고 **levels** 배열을 비운 다음, 원하는 LevelData 에셋 하나(예: 방금 만든 `Level_1.asset`)만 요소로 넣어 저장합니다. 그러면 게임에서 "Level 1"은 그 에셋 하나뿐입니다.
