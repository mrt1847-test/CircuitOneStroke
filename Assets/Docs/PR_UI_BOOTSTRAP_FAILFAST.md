# PR: UI Bootstrap + Fail Fast (퍼즐 앱 형태 초기 화면 보장)

## 요약

- **Scene Load → UIRoot/Bootstrap → UIScreenRouter → Initial Screen** 실행 경로를 [UI_BOOTSTRAP_CALLGRAPH.md](UI_BOOTSTRAP_CALLGRAPH.md)에 콜그래프로 정리함.
- UIRoot는 에디터 메뉴로 씬 생성 시에만 만들어지며, 런타임 자동 생성은 없음. **UIBootstrap**을 추가해 씬에 UIScreenRouter가 없으면 **Fail Fast**(Debug.LogError + Debug.Assert)로 원인을 콘솔에 남김.
- **UIScreenRouter.Start()**에서 **ShowInitialScreen()**을 반드시 호출하고, **Show(initialScreen)**로 초기 화면(기본 **LevelSelect**)을 표시하도록 수정.
- 의존성 누락 시 조용히 return 하지 않고 **Debug.LogError / Debug.Assert**로 Fail Fast.

---

## 파일별 변경 요약

| 파일 | 변경 요약 |
|------|------------|
| **Assets/Docs/UI_BOOTSTRAP_CALLGRAPH.md** | 신규. Scene Load → UIRoot → UIScreenRouter → Initial Screen 콜그래프 및 Fail Fast 위치 정리. |
| **Assets/UI/Scripts/UIBootstrap.cs** | 신규. Awake에서 UIScreenRouter 존재 여부 검사, 없으면 LogError + Assert. |
| **Assets/UI/Scripts/UIScreenRouter.cs** | initialScreen(기본 LevelSelect) 추가, ShowInitialScreen()/Show(Screen)/ValidateBootstrapDependencies() 추가. Start()는 항상 ShowInitialScreen() 호출. GetOrInstantiate/SetScreenActive/StartLevelLegacy에서 null 시 LogError+Assert. ShowHome/DoShowLevelSelect/DoShowGameHUD를 Show(Screen) 호출로 통일. |
| **Assets/Scripts/Core/GameFlowController.cs** | Boot()에서 router null 시 LogError+Assert 후 return, 그 외 router.ShowHome() 호출. |
| **Assets/Scripts/Editor/CreateGameScene.cs** | UIRoot 생성 시 UIBootstrap 컴포넌트 추가. 라우터 SerializedObject에 initialScreen = LevelSelect 설정. 로그 문구 수정. |
| **Assets/Docs/PR_UI_BOOTSTRAP_FAILFAST.md** | 본 PR 요약(이 문서). |

---

## 코드 Diff 요약

### 1. Assets/UI/Scripts/UIBootstrap.cs (신규)

```csharp
using UnityEngine;

namespace CircuitOneStroke.UI
{
    public class UIBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            var router = Object.FindFirstObjectByType<UIScreenRouter>();
            if (router == null)
            {
                Debug.LogError(
                    "UIBootstrap: UIScreenRouter not found in scene. " +
                    "Add UIRoot (Canvas + UIScreenRouter) to the scene, or use menu 'Circuit One-Stroke > Create Game Scene' to create the full game scene.");
                Debug.Assert(false, "UIScreenRouter not found. Scene must contain UIRoot with UIScreenRouter.");
            }
        }
    }
}
```

---

### 2. Assets/UI/Scripts/UIScreenRouter.cs

**추가: Bootstrap + initialScreen**

```diff
+        [Header("Bootstrap")]
+        [Tooltip("First screen shown when the app starts. Puzzle-style: LevelSelect.")]
+        [SerializeField] private Screen initialScreen = Screen.LevelSelect;
+
         [Header("Overlay References")]
```

**Start() 변경: 항상 ShowInitialScreen() 호출**

```diff
         private void Start()
         {
-            var flow = UIServices.GetFlow();
-            if (flow != null)
-                flow.Boot();
-            else
-                ShowHome();
+            ShowInitialScreen();
         }
+
+        public void ShowInitialScreen()
+        {
+            ValidateBootstrapDependencies();
+            Show(initialScreen);
+        }
+
+        private void ValidateBootstrapDependencies()
+        {
+            if (screenContainer == null)
+            {
+                Debug.LogError("UIScreenRouter: screenContainer is not assigned. ...");
+                Debug.Assert(false, "UIScreenRouter.screenContainer is null.");
+            }
+            var prefab = GetPrefab(initialScreen);
+            if (prefab == null)
+            {
+                Debug.LogError($"UIScreenRouter: prefab for initial screen '{initialScreen}' is not assigned. ...");
+                Debug.Assert(false, $"UIScreenRouter: missing prefab for initial screen {initialScreen}.");
+            }
+        }
+
+        public void Show(Screen screen)
+        {
+            _history.Clear();
+            _outOfHeartsVisible = false;
+            _resultDialogVisible = false;
+            HideAllScreens();
+            SetScreenActive(screen, true);
+            CurrentScreen = screen;
+            OnScreenChanged?.Invoke(CurrentScreen);
+        }
```

**GetOrInstantiate: prefab/screenContainer null 시 Fail Fast**

```diff
             var prefab = GetPrefab(screen);
-            if (prefab == null) return null;
+            if (prefab == null)
+            {
+                Debug.LogError($"UIScreenRouter: prefab for screen '{screen}' is not assigned. ...");
+                Debug.Assert(false, $"UIScreenRouter: missing prefab for screen {screen}.");
+                return null;
+            }
+            if (screenContainer == null)
+            {
+                Debug.LogError("UIScreenRouter: screenContainer is null. ...");
+                Debug.Assert(false, "UIScreenRouter.screenContainer is null.");
+                return null;
+            }
```

**SetScreenActive: go null 시 LogError**

```diff
         private void SetScreenActive(Screen screen, bool active)
         {
             var go = GetOrInstantiate(screen);
-            if (go != null)
-                go.SetActive(active);
+            if (go == null)
+            {
+                Debug.LogError($"UIScreenRouter: failed to get or instantiate screen '{screen}'. ...");
+                return;
+            }
+            go.SetActive(active);
         }
```

**ShowHome / DoShowLevelSelect / DoShowGameHUD → Show(Screen) 호출로 통일**

```diff
         public void ShowHome()
         {
-            _history.Clear();
-            _outOfHeartsVisible = false;
-            _resultDialogVisible = false;
-            HideAllScreens();
-            SetScreenActive(Screen.Home, true);
-            CurrentScreen = Screen.Home;
-            OnScreenChanged?.Invoke(CurrentScreen);
+            Show(Screen.Home);
         }
```

```diff
         private void DoShowLevelSelect()
         {
-            _history.Clear();
-            _outOfHeartsVisible = false;
-            _resultDialogVisible = false;
-            HideAllScreens();
-            SetScreenActive(Screen.LevelSelect, true);
-            CurrentScreen = Screen.LevelSelect;
-            OnScreenChanged?.Invoke(CurrentScreen);
+            Show(Screen.LevelSelect);
         }
```

```diff
         private void DoShowGameHUD()
         {
-            _history.Clear();
-            _outOfHeartsVisible = false;
-            HideAllScreens();
-            SetScreenActive(Screen.GameHUD, true);
-            CurrentScreen = Screen.GameHUD;
-            OnScreenChanged?.Invoke(CurrentScreen);
+            Show(Screen.GameHUD);
         }
```

**StartLevelLegacy: levelLoader null 시 Fail Fast**

```diff
         private void StartLevelLegacy(int levelId)
         {
-            if (levelLoader == null) return;
+            if (levelLoader == null)
+            {
+                Debug.LogError("UIScreenRouter.StartLevelLegacy: LevelLoader is not assigned. ...");
+                Debug.Assert(false, "UIScreenRouter.levelLoader is null.");
+                return;
+            }
             LevelRecords.LastPlayedLevelId = levelId;
```

---

### 3. Assets/Scripts/Core/GameFlowController.cs

**Boot(): router null 시 Fail Fast**

```diff
         public void Boot()
         {
-            router?.ShowHome();
+            if (router == null)
+            {
+                Debug.LogError("GameFlowController.Boot: UIScreenRouter is not assigned. ...");
+                Debug.Assert(false, "GameFlowController.router is null.");
+                return;
+            }
+            router.ShowHome();
         }
```

---

### 4. Assets/Scripts/Editor/CreateGameScene.cs

**UIRoot에 UIBootstrap 추가**

```diff
             var canvas = new GameObject("UIRoot");
+            canvas.AddComponent<CircuitOneStroke.UI.UIBootstrap>();
             var c = canvas.AddComponent<Canvas>();
```

**라우터에 initialScreen = LevelSelect 설정**

```diff
                 routerSo.FindProperty("gameHUDRef").objectReferenceValue = gameHud;
+                routerSo.FindProperty("initialScreen").enumValueIndex = (int)CircuitOneStroke.UI.UIScreenRouter.Screen.LevelSelect;
                 routerSo.ApplyModifiedPropertiesWithoutUndo();
```

**로그 문구**

```diff
-            Debug.Log("Created Assets/Scenes/GameScene.unity - app starts on HomeScreen. Run level from Continue/LevelSelect.");
+            Debug.Log("Created Assets/Scenes/GameScene.unity - app starts on LevelSelect (puzzle-style). Run level from grid.");
```

---

## 검증

- 씬에 UIRoot(UIBootstrap + Canvas + UIScreenRouter)와 스크린 프리팹이 할당되어 있으면, 플레이 시 **LevelSelect**가 초기 화면으로 표시됨.
- UIScreenRouter가 씬에 없으면: UIBootstrap.Awake()에서 **LogError + Assert**.
- screenContainer 또는 initialScreen 프리팹이 없으면: UIScreenRouter.ShowInitialScreen() → ValidateBootstrapDependencies()에서 **LogError + Assert**.
- prefab/container가 GetOrInstantiate에서 null이면: **LogError + Assert** 후 SetScreenActive에서 **LogError**.
- GameFlowController.Boot() 호출 시 router가 null이면: **LogError + Assert**.
