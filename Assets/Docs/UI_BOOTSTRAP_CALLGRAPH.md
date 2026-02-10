# Scene Load → UIRoot/Bootstrap → UIScreenRouter → Initial Screen (Call Graph)

실행 경로를 코드 기준으로 추적한 콜그래프입니다.

---

## 1. Unity 런타임 진입점

```
Unity Engine
  └─ Scene Load (빌드/에디터 플레이 시 첫 씬 로드)
       └─ [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] (선택)
            └─ TransitionManager.EnsureInstance()  [TransitionManager.cs]
                 └─ new GameObject("TransitionManager") + AddComponent<TransitionManager>()
       └─ 씬 내 모든 GameObject Awake() (순서 비보장)
            └─ GameFlowController.Awake()
            └─ UIScreenRouter.Awake()
            └─ ... (Canvas, ScreenRoot, LevelLoader 등)
       └─ 씬 내 모든 GameObject Start() (순서 비보장)
            └─ UIScreenRouter.Start()  ← 여기서 초기 화면 결정
```

- **UIRoot**: 에디터 메뉴 `Circuit One-Stroke > Create Game Scene`으로 씬을 만들 때 `CreateUIRoot()`가 `GameObject("UIRoot")` + Canvas + SafeAreaPanel + ScreenRoot + ScreenContainer + UIScreenRouter를 생성합니다. **런타임 자동 생성은 없습니다.** 씬에 UIRoot(또는 동일 계층)가 없으면 UI가 없습니다.
- **Bootstrap**: 기존 코드에는 "UIBootstrap"이 없었습니다. 이 PR에서 **UIBootstrap**을 추가해, 씬에 UIScreenRouter가 있는지 검사하고 없으면 **Fail Fast**(Debug.LogError + Debug.Assert)로 알립니다.

---

## 2. UIScreenRouter → Initial Screen 경로 (수정 후)

```
UIScreenRouter.Start()  [UIScreenRouter.cs]
  ├─ (선택) GameFlowController 존재 시 flow.Boot() 호출 가능
  └─ 반드시 실행: ShowInitialScreen()
       └─ ValidateBootstrapDependencies()  // screenContainer, initialScreen prefab 검사
       │    └─ 실패 시: Debug.LogError + Debug.Assert (Fail Fast)
       └─ Show(initialScreen)
            └─ HideAllScreens()
            └─ SetScreenActive(initialScreen, true)
                 └─ GetOrInstantiate(initialScreen)
                      ├─ GetPrefab(initialScreen) → prefab
                      │    └─ prefab == null → Debug.LogError + Debug.Assert
                      └─ Instantiate(prefab, screenContainer)
                 └─ go.SetActive(true)
            └─ CurrentScreen = initialScreen
            └─ OnScreenChanged?.Invoke(CurrentScreen)
```

- **initialScreen**: UIScreenRouter 인스펙터에서 설정. 기본값 **LevelSelect** (퍼즐 앱 형태로 레벨 선택 화면이 먼저 뜨도록).
- **Show(Screen)**: 기존 ShowHome() / ShowLevelSelect() 등은 내부적으로 Show(Screen.XXX)를 호출하도록 통일 가능. 초기 화면은 **Show(initialScreen)** 한 번으로만 보장.

---

## 3. GameFlowController.Boot() 경로 (수정 후)

```
(다른 코드에서) flow.Boot() 호출 시
  └─ GameFlowController.Boot()  [GameFlowController.cs]
       └─ router == null → Debug.LogError + Debug.Assert (Fail Fast)
       └─ router.ShowHome()  // Boot()는 홈 표시용. 초기 화면은 UIScreenRouter.ShowInitialScreen()이 담당.
```

- Boot()는 "플로우 부팅 시 홈으로 보낸다"는 의미로 유지. **첫 프레임의 초기 화면**은 UIScreenRouter.Start() → ShowInitialScreen()이 담당합니다.

---

## 4. 의존성 검사 (Fail Fast)

| 위치 | 검사 대상 | 실패 시 동작 |
|------|-----------|--------------|
| UIBootstrap | UIScreenRouter가 씬에 존재하는지 | Debug.LogError + Debug.Assert |
| UIScreenRouter.Start() | screenContainer, GetPrefab(initialScreen) | Debug.LogError + Debug.Assert |
| UIScreenRouter.GetOrInstantiate() | prefab == null | Debug.LogError + Debug.Assert |
| UIScreenRouter.SetScreenActive() | GetOrInstantiate() 반환 null | Debug.LogError |
| GameFlowController.Boot() | router == null | Debug.LogError + Debug.Assert |

---

## 5. 요약

- **Scene Load** 후 씬에 **UIRoot**(Canvas + UIScreenRouter 등)가 있어야 UI가 뜹니다. 런타임 자동 생성은 없습니다.
- **UIScreenRouter.Start()**에서 **ShowInitialScreen()**이 반드시 호출되며, **Show(initialScreen)**로 초기 화면(기본 LevelSelect)을 표시합니다.
- **UIBootstrap**은 씬에 UIScreenRouter가 있는지만 검사하고, 없으면 콘솔에 원인을 남기고 Assert로 중단합니다.
- 널/누락 시 **조용히 return 하지 않고** Debug.LogError / Debug.Assert로 **Fail Fast** 합니다.
