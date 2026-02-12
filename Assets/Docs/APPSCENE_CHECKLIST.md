# AppScene 탭 플로우 체크리스트

## Kenney UI 적용
- **한 번에 적용**: 메뉴 **Circuit One-Stroke > UI > Apply Kenney UI (Theme + Current Scene)** 실행.  
  → 테마에 Kenney Sci-Fi 스프라이트·폰트 할당 후, 열린 씬의 Canvas에 테마 적용.
- **테마만 할당**: **Circuit One-Stroke > UI > Assign Kenney Sci-Fi to Theme**  
  → 이후 Play 시 ThemeApplier가 Kenney 스타일 적용.
- AppScene 생성 시에도 테마에 Kenney를 자동 할당하고, Canvas에 ThemeApplier + ThemeRole(버튼/패널/하단바)이 연결되어 있음.

## 실행 방법
1. Unity 메뉴: **Circuit One-Stroke > Create AppScene (Tab Flow + Set First Build)** 실행.
2. `Assets/Scenes/AppScene.unity` 가 생성되고 **Build Settings** 0번으로 설정됨.
3. Play 시 **AppScene** 이 먼저 로드되어 **Home 탭(레벨 선택)** 이 보이고, 하단에 **Home / Shop / Settings** 탭 바가 보임.

## 계층 구조 (Hierarchy)
- **Canvas**
  - **SafeAreaPanel**
    - **MainShellRoot**
      - **HomeScreenRoot** — 레벨 선택 (Play Level 1 버튼)
      - **ShopScreenRoot** — 상점 (NoAds 플레이스홀더)
      - **SettingsScreenRoot** — 설정 (BGM/SFX/진동/언어)
      - **BottomNavBar** — 하단 3탭 (선택 시 하이라이트)
    - **GameScreenRoot** — 게임 HUD만 (상단/하단 바, 진입 시 표시)
      - **GameHUDRoot** (GameHUD)
    - **OverlayRoot** — 결과/OutOfHearts/확인 다이얼로그 (Create AppScene 시 생성)
      - **OverlayManager**
  - **Game** (LevelLoader + Nodes, Edges) — 씬 루트(Canvas와 형제). 카메라가 퍼즐을 그림.
  - **ScreenRouter** (Canvas에 컴포넌트)
  - **AppRouter**

## 확인 사항
- [ ] Play 시 **Home** 탭이 보이고 "Level Select", "Play Level 1" 버튼이 보인다.
- [ ] **Shop**, **Settings** 탭을 누르면 해당 화면으로 전환되고, 하단 바에서 선택된 탭이 하이라이트된다.
- [ ] **Play Level 1** 클릭 시 GameScreenRoot 로 전환되고 하단 바가 사라진다. (LevelLoader/프리팹이 없으면 빈 게임 화면)
- [ ] 게임 화면에서 **Home** 버튼(또는 GameHUD의 나가기) 시 다시 Home 탭으로 돌아오고 하단 바가 보인다.

---

## AppScene vs GameScene (게임플레이·레벨 진입 기준)

두 씬은 **같은 게임 로직**(LevelLoader, GameStateMachine, MoveValidator, TouchInputController, GameHUD)을 쓰지만, **네비게이션·진입 경로·오브젝트 활성화** 방식이 다릅니다.

### 1. 네비게이션 구조

| 구분 | GameScene | AppScene |
|------|-----------|----------|
| **라우터** | **UIScreenRouter** (단일) | **AppRouter** + **ScreenRouter** (역할 분리) |
| **화면 모델** | 스크린 스택: Home → LevelSelect → **GameHUD** / Settings / Shop / OutOfHearts | 탭(Home/Shop/Settings) + **게임 화면** (게임 중일 때만) |
| **초기 화면** | `initialScreen` (보통 LevelSelect) | Home 탭 = 레벨 선택 (MainShellRoot) |
| **레벨 선택 UI** | LevelSelect **프리팹** 인스턴스 (ScreenContainer에 로드) | HomeScreenRoot 안 **LevelSelectScreen** (인스턴스) |

### 2. 레벨 클릭 → 게임 화면까지 흐름

**GameScene**

1. `LevelSelectScreen.OnLevelClicked(levelId)` → `_router?.StartLevel(levelId)` (UIScreenRouter)
2. `UIScreenRouter.StartLevel` → `GameFlowController.RequestStartLevel(levelId)` (또는 레거시 `StartLevelLegacy`)
3. `GameFlowController.RunBuildLevelAndShowGame(levelId)`  
   - `BuildLevelCoroutine(levelId)` → `levelLoader.LoadLevelCoroutine(data)` (레벨 로드)  
   - 이어서 `router?.ShowGame()` → **UIScreenRouter.ShowGameHUD()**
4. `ShowGameHUD()` → `Show(Screen.GameHUD)` → GameHUD 스크린 활성화 + **SetRootBackgroundForScreen(GameHUD)** 로 루트 배경 **Color.clear** (퍼즐이 카메라에 보이도록)
5. **Game** 오브젝트는 씬 루트에 **항상 활성**. 터치 입력도 항상 동작 가능.

**AppScene**

1. `LevelSelectScreen.OnLevelClicked(levelId)` → **AppRouter.Instance.RequestStartLevel(levelId)** (AppRouter 우선)
2. `AppRouter.RequestStartLevel` → 하트 체크 후 `RunTransitionThenEnterGame(levelId)` 코루틴
3. `RunTransitionThenEnterGame`:  
   - **BuildLevelJob(levelId)** (전환 연출 있으면 TransitionManager로 실행)  
     - `gameFlowController.BuildLevelCoroutine(levelId)` 또는 `levelLoader.LoadLevelCoroutine(data)` 로 **레벨만 로드**  
   - 그 다음 **EnterGame(levelId)** 호출
4. **EnterGame(levelId)** 에서:  
   - `mainShellRoot.SetActive(false)`  
   - **GameWorldRoot(Game).SetActive(true)**  
   - Game·자식 레이어 0 고정, GameScreenRoot Image clear·비활성  
   - **ScreenRouter.Instance?.SetGameInputEnabled(true)** 로 터치 입력 활성화
5. **Game** 오브젝트는 **메인 셸일 때는 비활성**, 레벨 진입 시에만 활성. 터치 입력도 **게임 화면일 때만** 켜짐.

### 3. 게임플레이 공통·차이

| 항목 | GameScene | AppScene |
|------|-----------|----------|
| **LevelLoader / 런타임** | 같은 씬 루트의 **Game** 하위, 한 번 로드 후 계속 활성 | 같은 **Game**(씬 루트) 하위, **레벨 진입 시에만 Game 활성** |
| **퍼즐이 보이는 이유** | GameHUD로 전환 시 **ScreenRoot 부모 Image**를 `Color.clear`로 변경 | **GameScreenRoot** Image를 clear + `enabled = false` 로 퍼즐 영역이 카메라에 보이게 함 |
| **TouchInputController** | Game이 항상 활성이라 **항상 enabled** | **EnterGame 시에만** `SetGameInputEnabled(true)`, **ShowMainShell 시** `false` |
| **결과(승/패) 표시** | UIScreenRouter.**ShowResultWin** / **ShowResultLose** → GameHUD에 결과 표시 플래그 + GameHUD 스크린 유지 | **AppRouter.OnLevelComplete / OnHardFail** → **OverlayManager** 있으면 오버레이, 없으면 **GameHUD** 자체 패널 (`UseOverlayForResult`) |
| **나가기(Back)** | UIScreenRouter.GoBack / ShowLevelSelect | **AppRouter.RequestExitGame** → 확인 시 **ExitGameToHomeTab** → **ShowMainShell** + **SetGameInputEnabled(false)** |

### 4. 요약

- **게임 로직(레벨 로드, 스트로크, 재방문 실패, 클리어)** 은 두 씬 모두 **LevelLoader + GameStateMachine + GameFlowController** 로 동일.
- **GameScene**: “스크린 전환”만 하고, **Game은 항상 켜져 있어** 터치·렌더가 자연스럽게 동작.
- **AppScene**: “탭 + 게임 화면” 구조라 **Game을 껐다 켰다** 하고, **터치 입력도 게임 화면일 때만** 켜줘야 해서, **EnterGame/ShowMainShell** 에서 `SetGameInputEnabled`·`GameWorldRoot.SetActive` 를 명시적으로 호출해야 함.  
- **레벨 진입** 시 공통으로 **GameFlowController.BuildLevelCoroutine** (또는 동등한 레벨 로드)가 호출되며, **GameScene** 은 그 다음 `router.ShowGame()` 로 화면만 전환하고, **AppScene** 은 그 다음 `EnterGame()` 에서 Game 활성화 + 터치 활성화까지 한 번에 처리함.

---

## 풀 게임플레이 연동
- **Create AppScene** 실행 시 **Game**(씬 루트) 안에 LevelLoader, Nodes, Edges, StrokeRenderer, GameFlowController, TouchInputController, GameFeedback, AudioManager, HapticsManager, AdServiceMock 이 채워지고, **GameScreenRoot** 안에는 **GameHUDRoot**(GameHUD)만 둠. (FillGameScreenRoot에서 Game을 만든 뒤 CreateAppScene에서 Game을 씬 루트로 옮김.)
- **AppRouter**에 levelLoader·gameFlowController·overlayManager가 연결되어 있어야 레벨 로드·퍼즐·결과 오버레이가 동작함. (Create AppScene 시 자동 연결.)
- **TransitionManager**는 런타임에 BeforeSceneLoad로 생성되므로 씬에 넣지 않아도 됨. **HeartsManager**은 static 싱글톤이라 씬 오브젝트가 아님.
- 이미 만든 AppScene에만 게임플레이를 채우려면: **Circuit One-Stroke > Fill GameScreenRoot with Gameplay** 메뉴 실행 (GameScreenRoot가 있는 씬에서).
- **게임에 들어갔는데 퍼즐이 안 보이거나 UI만 보일 때**: **Circuit One-Stroke > Repair AppScene (Wire AppRouter + Show Puzzle)** 실행 → AppRouter 참조 연결 + GameScreenRoot 배경 투명 + **Game(퍼즐)을 씬 루트로 이동**(Canvas와 형제로 두어 카메라가 그리도록 함). (씬 저장 후 다시 Play.)
- **GameScene과의 차이**: GameScene은 UIScreenRouter가 게임 HUD 표시 시 **ScreenRoot 배경을 Color.clear**로 바꿔 퍼즐이 보이게 함. AppScene은 **EnterGame 시 GameScreenRoot의 Image를 clear + 비활성화(img.enabled=false)** 로 같은 효과를 냄.
- **게임 하단에 Back만 있고 Undo/Hint가 없을 때**: **Circuit One-Stroke > Fill GameScreenRoot with Gameplay** 를 한 번 실행하면 하단 바가 **Undo | Back | Hint** 로 채워짐. (기존 AppScene은 한 번 Fill 하면 적용.)
- NodeView/EdgeView 프리팹이 없으면 Create AppScene 실행 시 자동 생성됨.

## 디버깅 (버튼 무응답 / 퍼즐 미노출)
- **Console에서 [AppScene] 로그 확인**: Play 후 Unity Console에 `[AppScene]` 으로 필터 걸면 다음 로그가 나옵니다.
  - **AppRouter.Awake**: `levelLoader`, `gameFlowController`, `gameWorldRoot`, `mainShellRoot`, `gameRoot` 참조 여부. 하나라도 false면 Inspector에서 해당 참조 확인.
  - **ScreenRouter.Awake**: `home`, `shop`, `settings`, `gameScreenRoot`, `bottomNavBar`, `safe`, `main` — 전부 true여야 함. false면 계층이 다르거나 Repair 필요.
  - **ScreenRouter.Start**: `worldRoot`(퍼즐용 Game), `bottomNavBar` Bind 여부.
  - **BottomNavBar.Bind**: 탭 버튼( homeBtn, shopBtn, settingsBtn )이 null이면 탭 클릭이 동작하지 않음.
  - **레벨 셀 클릭 시** `LevelSelectScreen.OnLevelClicked` → `RequestStartLevel` → `BuildLevelJob` → `EnterGame` 순으로 로그가 나와야 함. `OnLevelClicked`가 안 나오면 레벨 버튼/레이캐스트 문제.
  - **EnterGame**: `GameWorldRoot`가 null이면 퍼즐이 안 보임. `BuildLevelJob`에서 `levelLoader is null` 경고가 나오면 레벨이 로드되지 않음.
  - **EnterGame 직후 로그** "Nodes 자식 수=N": N이 **0**이면 레벨이 로드되지 않은 것(LevelLoader가 다른 Game을 쓰는지 확인). N이 **1 이상**이면 노드는 있는데 화면에 안 그려지는 것 → 카메라/레이어/정렬 순서 확인. Play 중 Hierarchy에서 **Game > Nodes** 자식 개수와 로그가 일치하는지 확인.
- **로그 끄기**: Play 전에 Hierarchy에서 **AppRouter** 선택 → Inspector에서 **Enable App Scene Debug Log** 체크 해제. 또는 코드에서 `enableAppSceneDebugLog` 기본값을 false로 변경.

## 왜 기존 로직이 연동되지 않았는지 (근본 원인 정리)

AppScene에서 “켜진 전구 재방문 = 게임오버” 등 기존 게임 로직이 동작하지 않았던 이유는 **연결 누락**이었습니다. 코드는 그대로 있는데, 씬/참조만 맞지 않았습니다.

| 원인 | 설명 | 수정 내용 |
|------|------|-----------|
| **TouchInputController 비활성** | 레벨 클릭 시 `AppRouter.RequestStartLevel` → `EnterGame`만 호출되고 `ScreenRouter.EnterGame`은 호출되지 않아, 터치 입력이 한 번도 켜지지 않음. | `AppRouter.EnterGame`에서 `ScreenRouter.Instance?.SetGameInputEnabled(true)` 호출. `ShowMainShell`에서 `SetGameInputEnabled(false)` 호출. |
| **OverlayManager 없음** | AppScene에 OverlayManager가 생성·연결되지 않아 `overlayManager`가 null. 실패/성공 시 `ShowResultLose` 등이 호출돼도 아무 UI가 안 나옴. | (1) **Create AppScene** 시 OverlayRoot + OverlayManager 생성 후 AppRouter에 연결. (2) **Repair** 시 씬에 있는 OverlayManager를 찾아 AppRouter에 연결. (3) OverlayManager가 없을 때를 대비해 **GameHUD**가 자체 fail/success 패널을 쓰도록 `UseOverlayForResult`로 분기. |
| **상태 머신 바인딩 타이밍** | Game이 처음에 비활성이라 TouchInputController.Start()가 레벨 로드 전에 실행되거나, 나중에 컨트롤만 켜지는 경우 상태 머신과 어긋날 수 있음. | TouchInputController **OnEnable**에서 `levelLoader.StateMachine`과 현재 바인딩이 다르면 다시 `HandleStateMachineChanged` 호출해 재바인딩. |
| **엣지 선이 안 보임** | EdgeView 프리팹 LineRenderer에 머티리얼이 없음(`m_Materials: {fileID: 0}`). | EdgeView **Awake**에서 `_lr.material == null`이면 `Sprites/Default` 머티리얼 생성해 할당. |

이 수정들로 **기존 GameStateMachine / MoveValidator / GameFlowController / AppRouter 흐름**은 그대로 두고, **AppScene만** 위 참조와 활성화를 맞춰 연동됩니다. 새로 AppScene을 만들 때는 **Create AppScene** 한 번으로 OverlayManager까지 연결되며, 이미 만든 씬은 **Repair AppScene**으로 OverlayManager·TouchInput·worldRoot 등을 다시 연결할 수 있습니다.
