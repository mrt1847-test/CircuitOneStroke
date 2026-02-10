# Circuit One-Stroke (회로 한붓 퍼즐)

Android 대상 한붓 퍼즐 게임. 한 번의 연속 드래그로 모든 전구를 정확히 1회 방문하며 다이오드/스위치 제약을 만족하면 클리어.

## Unity 설정

1. **프로젝트 열기**: Unity Hub에서 이 폴더를 2D 프로젝트로 열기 (또는 기존 Unity 2021+ 2D 프로젝트에 Assets 복사).

2. **테스트 레벨 생성**: 메뉴 **Circuit One-Stroke > Create Default Test Level (Level_1)** 실행. `Assets/Resources/Levels/Level_1.asset` 이 생성됩니다.

3. **씬 구성** (GameScene):
   - **Main Camera**: Orthographic, 2D.
   - **Game** (빈 GameObject):
     - **LevelLoader** 컴포넌트: Nodes Root, Edges Root, Node View Prefab, Edge View Prefab, Stroke Renderer, Level Data(Level_1 할당 또는 비워두면 Level_1 자동 로드) 할당.
     - **TouchInputController**: Level Loader, Main Camera 할당.
   - **Nodes** (빈 GameObject, Game 자식): LevelLoader의 Nodes Root로 지정.
   - **Edges** (빈 GameObject, Game 자식): LevelLoader의 Edges Root로 지정.
   - **StrokeRenderer** (Game 자식): LineRenderer + StrokeRenderer 스크립트. LevelLoader의 Stroke Renderer로 지정.
   - **Canvas** (Screen Space - Overlay):
     - **GameHUD**: Level Loader, Retry Button, Success Panel, Fail Panel, Level Label(선택) 할당.

4. **프리팹**:
   - **NodeView**: SpriteRenderer(원형 스프라이트) + CircleCollider2D(Is Trigger 가능) + NodeView 스크립트. Layer를 Node로 두고 TouchInputController의 Node Layer에 설정.
   - **EdgeView**: LineRenderer + EdgeView 스크립트.

5. **Build Settings**: GameScene을 Scenes In Build에 추가.

## Android 빌드

- **File > Build Settings**: Platform에서 Android 선택 후 Switch Platform.
- **Player Settings**: Company Name, Product Name, Default Orientation (Portrait 권장).
- **Other Settings**: Minimum API Level 24 이상 권장.
- **해상도**: Canvas Scaler (UI)는 Scale With Screen Size, Reference Resolution 1080x1920.
- **터치**: snapRadius / commitRadius는 TouchInputController 인스펙터에서 기기별로 조정 가능.

## 설정 및 기록

- **설정**: `GameSettings` (PlayerPrefs) — Sound, Vibrate, Fail 모드(진입 불가 vs 즉시 실패). `SettingsPanel`을 Canvas에 두고 토글로 연동 가능.
- **기록**: `LevelRecords` — 레벨별 클리어 여부, 최단 시간 저장.

## 플레이

- 터치(또는 에디터에서는 마우스)로 전구에서 시작해 인접 노드로 드래그. 모든 전구를 한 붓으로 1회씩 방문하면 클리어.
