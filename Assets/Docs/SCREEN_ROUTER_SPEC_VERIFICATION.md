# ScreenRouter + MainShell/GamePlay 스펙 점검 결과

스펙(UX SPEC, Scene Hierarchy, Navigation Types, Main Components, Back, BuildLevelJob, Acceptance) 대비 구현 여부 점검 결과입니다.

---

## 0) UX SPEC (MUST FOLLOW)

| 항목 | 상태 | 비고 |
|------|------|------|
| **Screens** | ✅ | MainShell(Home/Shop/Settings), GamePlay 구현됨. |
| **MainShell** | ✅ | TAB_HOME(Level Select), TAB_SHOP, TAB_SETTINGS. `NavigationTypes.cs` + `AppRouter` + `MainShellNavBar`. |
| **GamePlay** | ✅ | GameRoot만 활성, 하단 탭 없음. `AppRouter.EnterGame` / `ExitGameToHomeTab`. |
| **Overlays** | ✅ | TRANSITION(TransitionManager), RESULT_WIN/LOSE(OverlayManager), OUT_OF_HEARTS, TOAST(GameFeedback), CONFIRM_EXIT_GAME. |
| **Exclusion** | ✅ | `OverlayManager`: ShowResultWin/ShowResultLose 시 `HideOutOfHearts()`, ShowOutOfHearts 시 `HideResult()`. |
| **TRANSITION blocks** | ✅ | `AppRouter.HandleBack`: `if (IsTransitioning) return;` |
| **Guards** | ✅ | RequestStartLevel/RequestRetry/RequestNext에서 `HeartsManager.CanStartAttempt()` 실패 시 ShowOutOfHearts, 진입/재시도/다음 차단. |
| **Heart consumption** | ✅ | `GameStateMachine.OnHardFail`: `_heartConsumedThisAttemptFail`로 1회만 `ConsumeHeart(1)`. Reject는 TryMoveTo에서만, 하트 소모 없음. |
| **Back policy** | ✅ | `AppRouter.HandleBack` 구현: Transition 무시 → Result 시 ExitGameToHomeTab → OutOfHearts(MainShell: Hide, GamePlay: Exit) → GamePlay: ConfirmExit → 탭 전환 → Home에서 Quit. |

---

## 1) SCENE HIERARCHY / PREFABS

| 항목 | 상태 | 비고 |
|------|------|------|
| **UIRoot (Canvas, SafeArea)** | ✅ | `CreateMainShellHierarchy`: Canvas 하위 SafeAreaPanel. |
| **MainShellRoot** | ✅ | MainShellContentRoot + BottomNavBar(3 tabs). |
| **GameRoot (disabled by default)** | ✅ | 생성 시 `gameRoot.gameObject.SetActive(false)`. GameHUDRoot 자식. |
| **OverlayRoot** | ✅ | CreateOverlayRoot → OverlayManager, ResultDialog, OutOfHeartsPanel, ConfirmExitDialog. |
| **TransitionOverlay** | ⚠️ | 스펙은 OverlayRoot 하위 기대. 실제는 `TransitionManager`가 런타임에 DontDestroyOnLoad 오브젝트로 자체 오버레이 생성. 동작은 입력 차단·페이드·스피너 모두 함. |
| **ToastUI** | ✅ | `OverlayManager.ShowToast` → `GameFeedback.RequestToast`. 별도 Toast UI 컴포넌트 존재 여부는 GameFeedback 구현에 따름. |
| **Single Scene** | ✅ | 메인 게임 씬 하나에서 처리. |

---

## 2) NAVIGATION TYPES (DATA)

| 항목 | 상태 | 비고 |
|------|------|------|
| **ScreenMode** | ✅ | `NavigationTypes.cs`: `MainShell`, `GamePlay`. |
| **MainTab** | ✅ | `NavigationTypes.cs`: `Home`, `Shop`, `Settings`. |
| **Router state** | ✅ | AppRouter: CurrentMode, CurrentTab, CurrentLevelId, LastIntent. IsTransitioning는 TransitionManager.Instance.IsTransitioning. |
| **LastIntent** | ✅ | `GameFlowController`: `IntentType { StartLevel, RetryLevel, NextLevel }`, `LastIntent { type, levelId }`. AppRouter에서 설정 후 GameFlowController.SetLastIntent. |
| **Resume after refill** | ✅ | `OnRefillThenResume` → HideResult/HideOutOfHearts → `ResumeLastIntent()` (StartLevel/RetryLevel/NextLevel에 따라 RequestStartLevel 또는 코루틴 재진입). |

---

## 3) MAIN COMPONENTS

### A) AppRouter

| API/역할 | 상태 | 비고 |
|----------|------|------|
| Boot() | ✅ | ShowMainShell(Home), bottomNavBar.Bind(ShowTab). |
| ShowTab(MainTab) | ✅ | CurrentTab 갱신, 탭 뷰 활성화, bottomNavBar.SetSelectedTab. |
| RequestStartLevel(levelId) | ✅ | LastIntent 설정, CanStartAttempt 실패 시 ShowOutOfHearts, 성공 시 RunTransitionThenEnterGame. |
| RequestRetry() | ✅ | LastIntent(RetryLevel), CanStartAttempt 가드, RunTransitionThenEnterGame(currentId). |
| RequestNext() | ✅ | LastIntent(NextLevel), CanStartAttempt 가드, TryInterstitialThenEnterGame(nextId). |
| OnLevelComplete() | ✅ | ShowResultWin(levelId, onNext, onLevelSelect). onNext에서 hearts==0이면 ShowOutOfHearts. |
| OnHardFail(FailReason) | ✅ | ShowResultLose(hearts, onRetry, onLevelSelect, onWatchAd, showWatchAdButton). |
| ShowOutOfHearts(ctx) | ✅ | OverlayManager.ShowOutOfHearts, onBack는 ctx에 따라 HideOutOfHearts 또는 ExitGameToHomeTab. |
| ShowToast(msg) | ✅ | OverlayManager.ShowToast. |
| ShowMainShell(tab) | ✅ | CurrentMode=MainShell, mainShell 활성, gameRoot 비활성, ShowTab. |
| EnterGame(levelId) | ✅ | CurrentLevelId, CurrentMode=GamePlay, mainShell 비활성, gameRoot 활성. |
| ExitGameToHomeTab() | ✅ | HideAllExceptToast, ShowMainShell(Home). |
| HandleBack() | ✅ | 위 Back policy 대로 처리. |

### B) OverlayManager

| API/규칙 | 상태 | 비고 |
|----------|------|------|
| ShowResultWin(levelId, onNext, onLevelSelect) | ✅ | HideOutOfHearts 후 Win 컨텐츠 표시. |
| ShowResultLose(hearts, ...) | ✅ | HideOutOfHearts 후 Lose 컨텐츠, Retry 활성/비활성, WatchAd 버튼 표시. |
| ShowOutOfHearts(ctx, onWatchAd, onBack) | ✅ | HideResult 후 패널 표시. |
| ShowConfirmExit(onConfirmExit) | ✅ | confirmExitDialog 표시, Confirm 시 onConfirmExit 호출. |
| ShowToast(msg) | ✅ | GameFeedback.RequestToast. |
| HideResult / HideOutOfHearts / HideAllExceptToast | ✅ | 구현됨. RESULT_*와 OUT_OF_HEARTS 상호 숨김 규칙 준수. |

### C) TransitionManager

| 항목 | 상태 | 비고 |
|------|------|------|
| Run(Action) / Run(IEnumerator) | ✅ | RunTransition(work, options). Default 옵션: fade 0.2s, spinnerDelay 0.3s. |
| Fade 0.2s in/out | ✅ | TransitionOptions.Default fadeOutDuration/fadeInDuration = 0.2f. |
| Spinner after 0.3s | ✅ | spinnerDelay = 0.30f, RunWorkWithSpinner에서 초과 시 SetSpinnerVisible(true). |
| Input blocker | ✅ | SetBlocker(true) during transition. |

### D) HeartsManager

| 항목 | 상태 | 비고 |
|------|------|------|
| CanStartAttempt() | ✅ | Hearts > 0. |
| Current / Max | ✅ | Hearts, MaxHearts. (스펙의 Current/Max와 동일 의미.) |
| ConsumeHeart(amount) | ✅ | 구현. |
| RefillFull() | ✅ | 구현. |
| OnHeartsChanged | ⚠️ | 스펙: `Action<int,int>`. 실제: `Action<int>` (현재 값만). 사용처는 현재 값만 쓰면 됨. |

### E) GameFlow hooks

| 항목 | 상태 | 비고 |
|------|------|------|
| OnLevelComplete / OnHardFail | ✅ | GameStateMachine → GameFlowController.OnLevelComplete/OnHardFail(string) → AppRouter.OnLevelComplete/OnHardFail(FailReason). |
| heartConsumedThisAttemptFail | ✅ | GameStateMachine._heartConsumedThisAttemptFail, OnHardFail에서 1회만 ConsumeHeart(1). |

---

## 4) MAIN SHELL UI (TABS)

| 항목 | 상태 | 비고 |
|------|------|------|
| BottomNavBar 3 buttons | ✅ | Home / Shop / Settings, MainShellNavBar.Bind(onTabSelected), SetSelectedTab(tab). |
| Highlight | ✅ | homeHighlight/shopHighlight/settingsHighlight SetActive(tab==...). |
| HomeTabView | ✅ | Level selection + Continue. Continue → RequestStartLevel(LastPlayed/Unlocked). |
| Level select → RequestStartLevel | ✅ | LevelSelectScreen.OnLevelClicked → AppRouter.Instance.RequestStartLevel(levelId) (AppRouter 있을 때). |
| ShopTabView / SettingsTabView | ✅ | ShopPanel, SettingsPanel 존재. 탭 전환 시 해당 뷰 활성화. |
| Not separate scenes | ✅ | 한 씬 내 GameObject 활성/비활성으로 탭 전환. |
| App starts MainShell Home | ✅ | Boot() → ShowMainShell(MainTab.Home). |

---

## 5) GAMEPLAY SCREEN

| 항목 | 상태 | 비고 |
|------|------|------|
| GameRoot only in GamePlay | ✅ | EnterGame 시 gameRoot.SetActive(true), mainShellRoot.SetActive(false). |
| GameHUD hearts, level label | ✅ | GameHUD: heartsDisplay, heartsText/heartBar, levelLabel. |
| Settings button | ✅ | settingsButton → OnSettingsClicked (AppRouter 있으면 ShowTab(Settings)). |
| Exit/Home button | ✅ | backButton/homeButton → OnHomeClicked → AppRouter.RequestExitGame() → ConfirmExit 또는 ExitGameToHomeTab. |

---

## 6) ATTEMPT START/RETRY/NEXT GUARDS + LAST INTENT

| 항목 | 상태 | 비고 |
|------|------|------|
| RequestStartLevel: lastIntent, CanStartAttempt, ShowOutOfHearts, Transition, EnterGame | ✅ | 구현됨. |
| RequestRetry: lastIntent(RetryLevel), guard, RunTransitionThenEnterGame | ✅ | 구현됨. |
| RequestNext: GetNextLevelId, lastIntent(NextLevel), guard, TryInterstitialThenEnterGame | ✅ | levelManifest.GetNextLevelId(currentId). |
| Refill success → RefillFull → Resume lastIntent | ✅ | HeartsRefillAdFlow에서 RefillFull 후 OnRefillThenResume → ResumeLastIntent. |
| Overlays close before transition | ✅ | BuildLevelJob 시작 시 HideAllExceptToast; OnRefillThenResume에서 HideResult, HideOutOfHearts 후 ResumeLastIntent. |

---

## 7) RESULT DIALOG LOGIC

| 항목 | 상태 | 비고 |
|------|------|------|
| Win: Next / Level Select | ✅ | onNext: hearts==0면 ShowOutOfHearts, else RequestNext. onLevelSelect: ExitGameToHomeTab. |
| Lose: ConsumeHeart once on HardFail | ✅ | GameStateMachine.OnHardFail에서 1회만 ConsumeHeart(1). |
| Lose: Retry (hearts>0), Level Select, Watch Ad (hearts==0) | ✅ | ShowResultLose(hearts, ..., showWatchAdButton: hearts==0). Retry interactable = hearts>0. |
| Rewarded refill → resume lastIntent | ✅ | RunHeartsRefillAdFlow → OnRefillThenResume → ResumeLastIntent. |
| Exclusion Result vs OutOfHearts | ✅ | ShowResult* 전 HideOutOfHearts, ShowOutOfHearts 전 HideResult. |

---

## 8) BACK BUTTON HANDLING

| 조건 | 동작 | 상태 |
|------|------|------|
| TRANSITION active | return (ignore) | ✅ |
| RESULT visible | ExitGameToHomeTab | ✅ |
| OUT_OF_HEARTS + MainShell | HideOutOfHearts | ✅ |
| OUT_OF_HEARTS + GamePlay | ExitGameToHomeTab | ✅ |
| CONFIRM_EXIT visible | HideAllExceptToast (cancel) | ✅ |
| GamePlay | ShowConfirmExit or ExitGameToHomeTab | ✅ |
| TAB_SHOP / TAB_SETTINGS | ShowTab(Home) | ✅ |
| TAB_HOME | Quit (Editor: stop play) | ✅ |

스펙의 "show exit confirm (dev can quit directly)"는 TAB_HOME에서 곧바로 Quit으로 해석되어 적용됨. 별도 “앱 종료 확인” 다이얼로그는 없음 (TODO 가능).

---

## 9) BUILD LEVEL JOB

| 항목 | 상태 | 비고 |
|------|------|------|
| Clear old / Instantiate nodes·edges / Finalize / currentLevelId | ✅ | BuildLevelJob에서 overlayManager.HideAllExceptToast, gameFlowController.BuildLevelCoroutine(levelId) 또는 levelLoader.LoadLevelCoroutine(data), CurrentLevelId 설정. |
| Yields | ⚠️ | 상세 단계별 yield는 LevelLoader.LoadCurrentCoroutine / GameFlowController.BuildLevelCoroutine 내부에 위임. AppRouter 레벨에서는 한 번에 코루틴 실행. |

---

## 10) ACCEPTANCE CHECKLIST

| 체크 | 상태 |
|------|------|
| App starts in MainShell, Home tab selected and highlighted | ✅ Boot → ShowMainShell(Home), SetSelectedTab(Home). |
| Switching tabs updates highlight and content | ✅ ShowTab → SetSelectedTab + 뷰 활성화. |
| Selecting a level enters GamePlay, bottom nav hidden | ✅ RequestStartLevel → Transition → EnterGame → mainShell 비활성, gameRoot 활성. |
| Exiting game returns to MainShell Home tab | ✅ ExitGameToHomeTab → ShowMainShell(Home). |
| Hearts==0 blocks start/retry/next, shows OutOfHearts | ✅ CanStartAttempt() 가드 + ShowOutOfHearts. |
| Result and OutOfHearts never overlap | ✅ OverlayManager 상호 Hide. |
| Back button (Escape) as specified | ✅ HandleBack 위와 동일. |
| Transitions fade and block input | ✅ TransitionManager 페이드 + SetBlocker. |

---

## 요약

- **구현 완료**: 스펙의 Screen(MainShell/GamePlay), Overlay, Exclusion, Guards, Heart 소모, Back, LastIntent/Resume, 탭/GameHUD, Accept 체크리스트가 모두 충족됩니다.
- **참고/선택 개선**:
  - **TransitionOverlay**: 스펙은 OverlayRoot 하위를 가정하지만, 현재는 TransitionManager가 별도 DontDestroyOnLoad 오브젝트로 오버레이를 갖습니다. 동작은 동일.
  - **HeartsManager.OnHeartsChanged**: 스펙은 `(int,int)` (current, max), 실제는 `(int)`. 필요 시 시그니처 확장 가능.
  - **TAB_HOME Back**: 현재는 바로 Quit. “앱 종료 확인” 다이얼로그는 필요 시 추가(TODO).
  - **BuildLevelJob**: 단계별 yield는 LevelLoader/GameFlowController 내부에 위임되어 있음.

이 문서는 스펙 대비 구현 점검용이며, 이후 스펙 변경 시 함께 갱신하는 것을 권장합니다.
