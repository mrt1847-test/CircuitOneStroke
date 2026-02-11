# 입력 호환성 및 유지보수 리스크

## 현재 구조 요약

| 용도 | 입력 경로 | 비고 |
|------|-----------|------|
| **게임 플레이** (경로 그리기) | `UnityEngine.Input` — `GetTouch()` / `GetMouseButton*()` | `TouchInputController.Update()` |
| **UI** (버튼, 스크롤 등) | EventSystem + **InputSystemUIInputModule** (Input System 패키지 있음) 또는 StandaloneInputModule | `ENABLE_INPUT_SYSTEM` 분기로 자동 선택 |

**Input System 패키지** (`com.unity.inputsystem`) 가 이미 설치되어 있으므로, EventSystem 생성 시 **InputSystemUIInputModule** 이 사용됩니다.  
Project Settings **Active Input Handler** 가 **Both (2)** 인 한 UI/게임 입력이 함께 동작합니다.

---

## 1. 입력 경로 이원화 리스크

- **게임 입력**: 레거시 `Input` API (터치/마우스 시뮬레이션).
- **UI 입력**: `StandaloneInputModule` (레거시 입력 기반).

**Input System Only (Active Input Handler = 1)** 로 전환하면:

- UI는 **Input System UI Input Module** 으로 넘겨야 클릭/드래그가 동작합니다.
- 게임 플레이 입력은 **새 Input System API** 로 옮기지 않으면 터치/마우스가 동작하지 않습니다.

**권장 마이그레이션 방향:**

1. **UI**: EventSystem 생성 시 `StandaloneInputModule` 대신 `InputSystemUIInputModule` 사용 (Input System 패키지 설치 후).
2. **게임**: `TouchInputController` 를 새 Input System (예: `PlayerInput`, `Touchscreen`, `Mouse`) 기반으로 재구현하거나, `InputSystemUIInputModule` 과 이벤트 우선순위/블로킹을 정리.

---

## 2. UI 모듈 — 분기 적용됨

다음 위치에서 EventSystem 생성 시 **Input System 패키지 유무에 따라** 모듈을 선택합니다.

| 파일 | 용도 |
|------|------|
| `Assets/UI/Scripts/UIScreenRouter.cs` | `EnsureEventSystem()` — 런타임 자동 생성 |
| `Assets/Scripts/Editor/CreateGameScene.cs` | 게임 씬 에디터 메뉴 |
| `Assets/UI/Scripts/Editor/CreateUIScene.cs` | UI 씬 에디터 메뉴 |

- **ENABLE_INPUT_SYSTEM** 정의 시(패키지 설치 시): `InputSystemUIInputModule` 사용.
- 그 외: `StandaloneInputModule` 사용.

패키지가 이미 포함되어 있으므로, 별도 설치 없이 UI는 새 Input System 경로를 사용할 수 있습니다.

---

## 체크리스트 (Input System Only 전환 시)

- [ ] Project Settings → Active Input Handler → **Input System Package (New)** 로 변경
- [x] EventSystem 생성 코드 3곳에 `InputSystemUIInputModule` 분기 적용 (완료)
- [ ] `TouchInputController` 를 새 Input API 또는 PlayerInput 기반으로 이전 (게임 플레이 터치/마우스)
- [ ] UI와 게임 입력이 동시에 사용되는 씬에서 포인터/터치 우선순위·블로킹 검증
