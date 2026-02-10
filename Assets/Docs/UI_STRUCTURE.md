# UI 레이어 구조 (Circuit One-Stroke)

문서·코드만이 아니라 **구조적 검토** 기준으로 정리한 UI 아키텍처와 패턴입니다.

---

## 1. 아키텍처 개요

```
UIScreenRouter (씬에 1개)
  ├── Screen prefab 인스턴스화 / 활성화
  ├── LevelLoader, LevelManifest 참조 → StartLevel / StartContinue
  └── 각 Screen은 BindRouter(router) 로 router 받음

Screen (Home, LevelSelect, GameHUD, Settings, Shop, OutOfHearts)
  ├── IUIScreen.BindRouter(UIScreenRouter) 로 라우터 저장
  ├── 네비게이션: router.ShowHome(), router.GoBack() 등
  └── Core/Services 접근: UIServices.GetFlow(), UIServices.GetAdService(), HeartsRefillAdFlow
```

- **라우터가 화면 소유:** 어떤 화면을 켤지는 Router가 결정하고, 화면은 `_router`로 뒤로가기/다른 화면으로 이동만 요청합니다.
- **Core/Services 접근 집중:** `GameFlowController`, `IAdService` 조회는 **UIServices** 한 곳에서만 합니다. `FindFirstObjectByType` + `Instance` 패턴을 화면마다 반복하지 않습니다.
- **공통 플로우 단일화:** "Watch Ad to Refill Hearts"는 **HeartsRefillAdFlow.Run()** 하나로만 처리합니다. GameHUD와 OutOfHeartsScreen은 인자(levelIndex, adComponent, onSuccess, onClosedOrFailed)만 넘깁니다.

---

## 2. 의존성 맵

| 스크립트 | 역할 | 의존 (Core/Services) | 비고 |
|----------|------|------------------------|------|
| **UIScreenRouter** | 화면 전환, 레벨 시작 | GameFlowController(UIServices), TransitionManager, GameSettings, LevelLoader, LevelManifest | Flow는 UIServices.GetFlow() |
| **GameHUD** | 게임 중 HUD, 성공/실패/OutOfHearts 패널 | LevelLoader, HeartsManager, TransitionManager, AdDecisionService, InterstitialTracker, AdPlacementConfig, UIServices, HeartsRefillAdFlow | 리필은 HeartsRefillAdFlow |
| **OutOfHeartsScreen** | 하트 0 전용 화면 | LevelRecords, UIServices(HeartsRefillAdFlow 내부 사용) | 리필은 HeartsRefillAdFlow.Run만 호출 |
| **HomeScreen** | 홈 (Continue, Level Select, Settings, Shop) | HeartsManager, LevelRecords, _router | |
| **LevelSelectScreen** | 그리드 레벨 선택 | LevelManifest, LevelLoader, HeartsManager, _router | BindRouter에서 _manifest, _loader 설정 |
| **SettingsPanel** | 설정 UI | GameSettings, _router | |
| **ShopPanel** | NoAds 등 상점 | PurchaseEntitlements, _router | |
| **ThemeApplier / ScreenRoot** | 테마 적용 | CircuitOneStrokeTheme, GameSettings(옵션) | |
| **UIServices** | Flow / AdService 조회 | GameFlowController, AdServiceRegistry, AdServiceMock | UI 전용 접근층 |
| **HeartsRefillAdFlow** | 리워드 광고 → 하트 리필 | AdDecisionService, HeartsManager, GameFeedback, GameSettings, UIServices | 한 곳에서만 CanShow/ShowRewarded/콜백 처리 |

---

## 3. 라이프사이클·이벤트

- **BindRouter:** Router가 화면을 인스턴스화한 직후 `IUIScreen.BindRouter(this)` 호출. 화면은 `_router`를 저장해 이후 네비게이션에 사용.
- **Start:** 버튼 리스너 등록, 필요 시 HeartsManager 등 구독. **OnDestroy**에서 같은 채널 구독 해제 (HeartsManager, LevelLoader.OnStateMachineChanged 등). 구독/해제 시 해당 Instance null 체크 권장.
- **ThemeApplier / HeartBar / AccessibilityTextScaler:** **OnEnable**에서 구독, **OnDisable**에서 해제. 비활성화 시에도 해제되도록 동일 패턴 유지.

---

## 4. 사용 패턴

- **Flow 조회:** `var flow = UIServices.GetFlow();` 만 사용. `GameFlowController.Instance ?? FindFirstObjectByType<GameFlowController>()` 반복 금지.
- **광고 서비스 조회:** `UIServices.GetAdService(adServiceComponent)`. 씬에 배치된 MonoBehaviour는 인자로 넘김.
- **하트 리필 광고:** `HeartsRefillAdFlow.Run(levelIndex, adServiceComponent, onSuccess, onClosedOrFailed)`. 화면별로 onSuccess/onClosedOrFailed만 다르게 넘김.
- **네비게이션:** 화면은 `_router?.ShowHome()`, `_router?.GoBack()` 등만 호출. Router가 실제로 어떤 화면을 켤지 결정.

---

## 5. 네이밍·역할 구분

- **LevelSelectScreen:** 그리드 형태 레벨 선택 (LevelSelectCell 리스트). Router가 LevelSelect 화면으로 켤 때 사용.
- **LevelSelectUI:** Prev/Next 버튼으로 한 레벨씩 넘기며 로드하는 UI. 레벨 플레이 씬 등 다른 맥락에서 사용될 수 있음. 둘 다 “레벨 선택”이지만 화면 구성·용도가 다름.

---

## 6. 개선 요약 (구조적 검토 결과)

1. **UIServices 도입** — Flow/AdService 조회를 한 곳으로 모아 의존성·중복 제거.
2. **HeartsRefillAdFlow 도입** — GameHUD와 OutOfHeartsScreen에 나뉘어 있던 "리워드 광고로 하트 리필" 로직을 단일 진입점으로 통합.
3. **GetFlow() 호출 통일** — UIScreenRouter, GameHUD 등에서 모두 UIServices.GetFlow() 사용.
4. **이벤트 구독/해제** — Instance null 체크 후 구독·해제해 에디터/테스트 씬에서도 안전하게 동작하도록 유지.

추가로, 향후 **설정/광고/플로우**를 인터페이스로 빼고 주입하는 방식으로 바꾸면 테스트와 교체가 더 쉬워집니다.
