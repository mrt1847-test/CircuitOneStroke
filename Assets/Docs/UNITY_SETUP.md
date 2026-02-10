# Circuit One-Stroke — 유니티 설정 가이드

이 문서는 **유니티에서 이 프로젝트가 제대로 동작하도록** 하는 설정을 **하나부터 차근차근** 정리한 것입니다.  
처음 프로젝트를 열었거나, 씬/프리팹이 없어서 플레이 시 화면이 안 나올 때 이 순서대로 진행하면 됩니다.

---

## 1. 유니티 버전 및 프로젝트 열기

- **권장 유니티 버전**: **Unity 6** (현재 프로젝트: 6000.3.7f1)
- **열기**: Unity Hub에서 **Add** → 이 프로젝트 폴더(`CircuitOneStroke`) 선택 → **Open**
- **처음 열면**: 스크립트 컴파일과 에셋 임포트가 끝날 때까지 기다립니다.

---

## 2. 필수 폴더 구조 (자동 생성 여부)

런타임/에디터가 기대하는 경로는 아래와 같습니다. **메뉴를 실행하면 필요한 폴더는 자동으로 만들어집니다.**

| 경로 | 용도 | 만드는 방법 |
|------|------|-------------|
| `Assets/Resources/Levels/` | 레벨 데이터(`Level_1.asset` 등), `GeneratedLevelManifest.asset` | Create Default Level / Create Game Scene / Level Bake |
| `Assets/UI/Theme/` | `CircuitOneStrokeTheme.asset` | Create Default Theme / Create Screen Prefabs |
| `Assets/UI/Screens/` | HomeScreen, LevelSelectScreen 등 스크린 프리팹 | Create Screen Prefabs / Create Game Scene |
| `Assets/UI/Prefabs/` | HeartBar, LevelCell 등 | Create Screen Prefabs |
| `Assets/Scenes/` | 게임 씬 저장 | Create Game Scene |
| `Assets/Prefabs/` | NodeView, EdgeView 등 게임 프리팹 | Create Game Scene |

---

## 3. 설정 순서 (처음 한 번만)

**아래 순서대로 메뉴를 실행**하면, 플레이에 필요한 에셋과 씬이 모두 준비됩니다.

### 3-1. 테마 생성 (UI 색/폰트용)

1. 상단 메뉴 **Circuit One-Stroke** → **UI** → **Create Default Theme**
2. `Assets/UI/Theme/CircuitOneStrokeTheme.asset` 이 생성됩니다.
3. (선택) Kenney/Skymon 스프라이트를 쓰려면 [UI_IMPORT.md](UI_IMPORT.md) 참고 후, 해당 Theme 에셋에서 스프라이트/폰트를 할당합니다.

**현재 기본 테마(라이트):** 흰 배경, 그린 상·하단 바(primary), 블루 게임 노드·선(secondary). **Create Game Scene** 실행 시 HUD가 참고 스크린샷처럼 구성됩니다: 상단 그린 바(LEVEL 왼쪽, 설정 오른쪽), 하단 그린 바(Undo·Back·Hint 세 칸), 퍼즐은 카메라 `orthographicSize = 6.5`로 여백 확보. 기존 씬을 새 레이아웃으로 쓰려면 메뉴에서 **Create Game Scene**을 다시 실행하면 됩니다.

**세로 화면:** Project Settings에서 기본 방향이 Portrait이며, **AppBootstrap**이 플레이 시작 시 `Screen.orientation = Portrait`로 고정합니다. 캔버스는 Game 뷰 해상도에 맞춰지므로, **가로가 더 길면** UIRoot에 **ForcePortraitCanvas**가 자동으로 붙어 매 프레임 세로(1080×1920) + **Scale (1,1,1)** 로 되돌립니다. Canvas Scaler **Match**는 세로 우선으로 **1.0 (Height)** 로 두었습니다.

**캔버스/세로 해결 체크리스트 (우선순위 순):**
1. **Game 뷰를 9:16(세로)으로** — Game 탭 상단 Aspect/Resolution에서 **9:16** 또는 **1080×1920** 선택. 이걸 안 바꾸면 미리보기가 계속 가로로 나옵니다.
2. **UIRoot Scale (1,1,1)** — UIRoot 선택 → Rect Transform에서 Scale을 **(1, 1, 1)** 로 두세요. Canvas는 보통 Scale (1,1,1)이어야 정상입니다. ForcePortraitCanvas가 가로일 때 자동으로 1로 되돌립니다.
3. **Pos/Width/Height "driven by Canvas"** — 정상입니다. Canvas는 Scaler/해상도/앵커로 맞추는 대상이라, 해당 값은 무시해도 됩니다.

---

### 3-2. 테스트용 레벨 데이터 생성 (Resources/Levels)

1. **Circuit One-Stroke** → **Create Default Test Level (Level_1)**
2. `Assets/Resources/Levels/Level_1.asset` 이 생성됩니다.
3. (선택) **Create Diode Test Level (Level_2)**, **Create Switch/Gate Test Level (Level_3)** 으로 추가 테스트 레벨도 만들 수 있습니다.

**레벨이 하나도 없으면** Create Game Scene 실행 시 Level_1을 자동으로 만들려고 시도합니다. 그래도 **한 번은 3-2를 먼저 해 두는 것을 권장**합니다.

---

### 3-3. 스크린 프리팹 생성 (홈/레벨선택/설정 등)

1. **Circuit One-Stroke** → **UI** → **Create Screen Prefabs**
2. 다음이 생성됩니다.
   - `Assets/UI/Prefabs/HeartBar.prefab`, `LevelCell.prefab`
   - `Assets/UI/Screens/HomeScreen.prefab`, `LevelSelectScreen.prefab`, `SettingsScreen.prefab`, `ShopScreen.prefab`, `OutOfHeartsScreen.prefab`
3. 콘솔에 `Screen prefabs created.` 로그가 나오면 성공입니다.

**이 단계를 건너뛰면** Create Game Scene에서 스크린 프리팹을 찾지 못해 UIScreenRouter에 할당이 비어 있고, **플레이 시 NullReferenceException** 이 납니다. 반드시 **Create Game Scene 전에** 실행하세요.

---

### 3-4. 게임 씬 생성 (UIRoot + 게임 + HUD)

1. **Circuit One-Stroke** → **Create Game Scene**
2. 다음이 한 번에 만들어집니다.
   - **새 씬**: Main Camera, Game(LevelLoader·노드·엣지·입력·GameFlowController 등), UIRoot(Canvas·SafeArea·ScreenRoot·ScreenContainer·UIScreenRouter), GameHUD, Toast 등
   - `Assets/Scenes/GameScene.unity` 로 저장
   - UIScreenRouter에 HomeScreen/LevelSelectScreen 등 프리팹 및 screenContainer, levelLoader, levelManifest 참조 할당
3. 콘솔에 `Created Assets/Scenes/GameScene.unity - app starts on LevelSelect (puzzle-style).` 로그가 나오면 성공입니다.

**주의**: Create Game Scene은 **현재 열린 씬을 새 씬으로 덮어씁니다.** 이미 작업 중인 씬이 있으면 먼저 저장하거나 다른 이름으로 백업하세요.

---

### 3-5. 빌드 설정에 씬 넣기 (플레이/빌드 시 로드)

1. **File** → **Build Settings** (Ctrl+Shift+B)
2. **Scenes In Build** 에서 **Add Open Scenes** 를 누르거나, **Assets/Scenes/GameScene** 을 Project에서 드래그해서 목록에 넣습니다.
3. **GameScene** 이 목록의 **0번**이면 플레이/빌드 시 첫 씬으로 로드됩니다.
4. **Build Settings** 창을 닫아도 됩니다.

**Scenes In Build 가 비어 있으면** 플레이 버튼을 눌러도 아무 씬도 로드되지 않거나, 에디터 기본 씬만 열릴 수 있습니다. 반드시 **GameScene을 추가**하세요.

---

## 4. 플레이 전 확인

- **GameScene** 이 열려 있는지 확인합니다. (Project에서 `Assets/Scenes/GameScene` 더블클릭)
- **Hierarchy** 에 대략 다음이 있어야 합니다.
  - **Main Camera**
  - **Game** (LevelLoader, GameFlowController 등)
  - **UIRoot** (Canvas, SafeAreaPanel, ScreenRoot, ScreenContainer, UIScreenRouter)
- **Play** 버튼을 누르면 **LevelSelect 화면**(그리드 레벨 선택)이 먼저 뜨고, 셀을 누르면 레벨이 로드되어 퍼즐이 동작해야 합니다.

---

## 5. 문제가 생겼을 때

| 증상 | 확인할 것 |
|------|------------|
| 플레이해도 화면이 안 나옴 / 검은 화면 | 3-3 Create Screen Prefabs 실행했는지, 3-4 Create Game Scene 실행했는지 확인. UIRoot와 UIScreenRouter가 씬에 있고, UIScreenRouter Inspector에서 **Screen Prefabs**와 **screenContainer**가 할당되어 있는지 확인. |
| **버튼을 눌러도 반응 없음** (Back, Settings, 레벨 셀 등) | 씬에 **EventSystem**이 있어야 UI 클릭이 동작합니다. Create Game Scene으로 만든 씬에는 포함됩니다. 기존 씬이라면 Hierarchy에서 **EventSystem**이 있는지 확인하고, 없으면 **GameObject → UI → Event System**으로 추가하세요. (플레이 시 없으면 자동 생성됩니다.) |
| NullReferenceException (prefab, screenContainer 등) | 3-3, 3-4 순서대로 다시 실행. 스크린 프리팹이 `Assets/UI/Screens/` 에 있고, GameScene이 Create Game Scene으로 만든 최신 상태인지 확인. |
| 레벨을 눌러도 로드 안 됨 / 에러 | 3-2로 Level_1이 `Assets/Resources/Levels/Level_1.asset` 에 있는지 확인. GameFlowController / UIScreenRouter에서 **levelManifest** 가 할당되어 있거나, Resources에 `Levels/GeneratedLevelManifest` 가 있는지 확인. |
| 레벨 목록이 비어 있음 | LevelManifest가 없으면 기본 개수(예: 20) 또는 Resources 레벨만 사용. 레벨을 많이 쓰려면 6번 Level Bake로 `GeneratedLevelManifest` 생성. |
| **"The referenced script on this Behaviour is missing!"** (LevelSelect/LevelCell) | 스크립트 참조가 깨진 프리팹. **Circuit One-Stroke** → **UI** → **Create Screen Prefabs** 를 다시 실행해 스크린/LevelCell 프리팹을 덮어쓰면 해결됩니다. 코드에서는 런타임에 참조를 복구하므로 그리드는 동작할 수 있습니다. |
| **캔버스가 가로로 보임 / UIRoot Scale이 0.5…** | ① Game 뷰를 **9:16** 또는 **1080×1920**으로 변경. ② UIRoot 선택 → Rect Transform **Scale (1, 1, 1)** 로 수동 복구. ③ Canvas Scaler **Match**를 **1 (Height)** 로 두면 세로 우선. ForcePortraitCanvas가 가로일 때 자동으로 세로+Scale 1로 되돌립니다. |
| 빌드 후 실행해도 아무 씬도 안 뜸 | Build Settings에 GameScene이 들어가 있고, Index 0인지 확인 (4번). |

---

## 6. 선택 사항 (나중에 해도 됨)

- **Kenney / Skymon UI 에셋**: [UI_IMPORT.md](UI_IMPORT.md) 참고. 없어도 테마 색상으로 UI는 동작합니다.
- **레벨 대량 생성**: **Tools** → **Circuit One-Stroke** → **Level Bake** 로 16~25 노드 퍼즐을 생성하고 `Resources/Levels/Generated/` 와 `GeneratedLevelManifest.asset` 을 만듭니다.
- **전환 오버레이**: **Circuit One-Stroke** → **Create Transition Overlay Prefab** 으로 로딩/전환용 오버레이 프리팹 생성.
- **광고 설정**: **Circuit One-Stroke** → **Create Ad Placement Config** 로 `Resources/AdPlacementConfig.asset` 생성 (광고 연동 시 사용).
- **Canvas를 Screen Space - Overlay로 통일**: Canvas 선택 후 **Circuit One-Stroke** → **UI** → **Fix Canvas to Screen Space Overlay**.

---

## 7. 요약 체크리스트

처음 프로젝트를 연 뒤, **한 번만** 아래 순서대로 실행하면 됩니다.

1. **Circuit One-Stroke** → **UI** → **Create Default Theme**
2. **Circuit One-Stroke** → **Create Default Test Level (Level_1)**
3. **Circuit One-Stroke** → **UI** → **Create Screen Prefabs**
4. **Circuit One-Stroke** → **Create Game Scene**
5. **File** → **Build Settings** → **Scenes In Build** 에 **GameScene** 추가
6. **GameScene** 열고 **Play** 로 동작 확인

이후에는 **GameScene** 을 열고 플레이만 하면 됩니다.
