# Circuit One-Stroke — UI 에셋 가이드

이 문서는 **Kenney UI Pack - Sci-Fi**와 **Skymon Icon Pack Free** 에셋을 프로젝트에 넣는 방법을 안내합니다.  
에셋이 없어도 UI 테마와 프리팹은 동작하며, 스프라이트는 플레이스홀더(단색)로 표시됩니다.  
**Unity 6** 기준으로 스크립트 및 메뉴가 맞춰져 있습니다 (`FindFirstObjectByType`, Canvas Render Mode 등).

---

## 1. Kenney — UI Pack Sci-Fi (CC0)

- **출처**: [Kenney.nl](https://kenney.nl/assets/ui-pack-space-expansion) 또는 "UI Pack - Sci-Fi" / "UI Pack Space Expansion" 검색
- **라이선스**: CC0 (무료, 상업적 사용 가능)

### 임포트 방법

1. [Kenney](https://kenney.nl/assets/ui-pack-space-expansion)에서 ZIP 다운로드
2. ZIP 압축 해제
3. 해제된 폴더 전체를 **`Assets/Art/UI/KenneySciFi/`** 안으로 드래그
4. Unity에서 자동 임포트 후, 필요한 스프라이트는 **Sprite (2D and UI)** 로 설정

### 프로젝트에서 기대하는 경로

- **경로**: `Assets/Art/UI/KenneySciFi/`
- 패널/버튼/프로그레스용 스프라이트를 이 폴더 또는 하위 폴더에 두면, 테마/프리팹에서 자동으로 참조할 수 있습니다.

### KenneySciFi 폴더 구성 검토

현재 구조가 아래와 같다면 **폴더 구성은 올바릅니다.**

| 경로 | 용도 |
|------|------|
| `Font/` | Kenney Future, Kenney Future Narrow — 테마 **Font** 슬롯에 사용 가능 |
| `PNG/` | UI용 스프라이트 (패널·버튼·바). **반드시 Sprite (2D and UI)로 임포트**해야 테마에 할당 가능 |
| `PNG/Extra/Default/` | `panel_square.png`, `panel_rectangle.png`, `button_square.png`, `button_rectangle.png` 등 — **Panel Sprite / Button Sprite** 추천 |
| `PNG/Grey/Default/`, `PNG/Blue/Default/` | `button_square_header_*.png`, `bar_round_*.png`, `bar_square_*.png` — 버튼·슬라이더용 |
| `PNG/*/Double/` | bar/button 변형 (gloss 등) |
| `Vector/` | SVG 원본. Unity는 기본적으로 PNG를 사용하므로 UI에는 **PNG** 사용 |
| `License.txt` | CC0 라이선스 안내 |

**테마 슬롯 ↔ 파일 매핑 예시**

- **Panel Sprite** → `PNG/Extra/Default/panel_square.png` 또는 `panel_rectangle.png`
- **Button Sprite** → `PNG/Extra/Default/button_square.png` 또는 `PNG/Grey/Default/button_square_header_large_rectangle.png`
- **Button Pressed Sprite** → 같은 폴더에서 눌린 느낌의 변형 또는 동일 스프라이트
- **Slider Background / Slider Fill** → `PNG/Grey/Default/bar_square_large_m.png`, `bar_square_gloss_large_m.png` 등

**⚠️ PNG가 테마에 안 들어갈 때**

- Unity에서 PNG가 **Texture(기본)** 로 임포트되면 **Sprite**로 쓸 수 없습니다. **Texture Type**을 **Sprite (2D and UI)** 로 바꿔야 합니다.
- **방법 1:** Project에서 `Assets/Art/UI/KenneySciFi/PNG` 폴더 선택 → Inspector에서 **Texture Type** = **Sprite (2D and UI)** → **Apply** (하위 PNG 일괄 적용).
- **방법 2:** 메뉴 **Circuit One-Stroke → UI → Set KenneySciFi PNGs as Sprites** 실행 시 해당 폴더 하위 PNG를 모두 Sprite로 설정합니다.

---

## 2. Skymon — Icon Pack Free (Unity Asset Store)

- **출처**: Unity Asset Store — "Skymon Icon Pack Free" 검색
- **용도**: 흰색 PNG 아이콘 (설정, 재생, 일시정지, 레벨 등)

### 임포트 방법

1. Unity 에디터에서 **Window → Asset Store** (또는 Package Manager)
2. **My Assets** 에서 "Skymon Icon Pack Free" 찾기
3. **Download** → **Import** (또는 **Import Unity Package**)
4. 임포트 후 아이콘 폴더를 **`Assets/Art/UI/SkymonIcons/`** 로 이동해 두면 관리하기 쉽습니다.

### 프로젝트에서 기대하는 경로

- **경로**: `Assets/Art/UI/SkymonIcons/`
- 아이콘 스프라이트를 이 폴더에 두면, 버튼/메뉴 아이콘으로 사용할 수 있습니다.

---

## 3. 에셋이 없을 때

- **Kenney / Skymon** 스프라이트가 없으면:
  - 패널·버튼·슬라이더는 **테마 색상만 적용된 단색(플레이스홀더)** 로 표시됩니다.
- 에셋을 나중에 추가하면:
  - `CircuitOneStrokeTheme` 에셋에서 스프라이트 참조만 넣어 주면, 같은 프리팹이 에셋 스타일로 바로 반영됩니다.

---

## 4. 심볼릭 링크(Symlink) 경고가 뜰 때

Unity는 **Assets 폴더 안의 심볼릭 링크**를 권장하지 않습니다.  
다른 프로젝트에서 에셋을 링크로 가져왔거나, .meta만 링크로 둔 경우 다음 경고가 나올 수 있습니다.

- *"… is a symbolic link. Using symlinks in Unity projects may cause your project to become corrupted …"*

### 해결 방법 (권장: 실제 파일로 교체)

1. **경고에 나온 .meta(또는 파일) 위치 확인**  
   예: `Assets/Art/UI/KenneySciFi/PNG/Yellow/Double/bar_square_gloss_small.png.meta`

2. **해당 파일이 링크인지 확인**  
   - 탐색기에서 아이콘에 화살표 등이 있거나  
   - 우클릭 → 속성에서 “대상” 경로가 보이면 심볼릭 링크입니다.

3. **실제 파일로 바꾸기**  
   - **방법 A**: 링크를 **삭제**한 뒤, Unity에서 해당 PNG(또는 에셋)가 있는 폴더를 **우클릭 → Reimport**. Unity가 새 .meta를 생성합니다. (이 에셋을 참조하는 씬/프리팹이 있으면 GUID가 바뀌어 참조가 깨질 수 있으니, 필요 시 씬/프리팹에서 다시 스프라이트를 지정합니다.)  
   - **방법 B**: 링크가 가리키는 **원본 파일**을 열어 내용을 복사한 뒤, 프로젝트 안 **같은 경로**에 일반 파일로 새로 저장하고, 기존 링크는 삭제합니다. (`.meta`라면 원본 .meta 내용을 그대로 붙여넣어 GUID를 유지할 수 있습니다.)

4. **Kenney 폴더 전체를 링크로 넣었다면**  
   - 원본 ZIP을 다시 풀어서 **`Assets/Art/UI/KenneySciFi/`** 에 **일반 폴더/파일로 복사**하고, 기존 링크 폴더는 삭제한 뒤 Unity에서 **Reimport** 하면 됩니다.

**요약**: Unity 프로젝트에서는 **Assets 안에 심볼릭 링크 대신 실제 파일 복사본**을 두는 것이 안전합니다.

---

## 5. 스타일 가이드 (일관 적용)

- **배경**: 매우 어두운 네이비/블랙 (회로판 느낌)
- **강조색**: 네온 틸(primary), 부드러운 블루(secondary), 경고용 앰버/레드
- Kenney Sci-Fi 패널/버튼으로 통일감 있는 UI 구성

---

## 6. 테마·프리팹 사용 (빠른 화면 제작)

### 6.1 테마 생성 (한 번만 하면 됨)

테마 에셋은 UI 색상·폰트·스프라이트 참조를 한 곳에서 관리하는 ScriptableObject입니다.

**단계:**

1. Unity 상단 메뉴에서 **Circuit One-Stroke → UI → Create Default Theme** 클릭.
2. 폴더가 없으면 자동으로 `Assets/UI/Theme/` 가 만들어지고, 그 안에 **CircuitOneStrokeTheme** 에셋이 생성됩니다.
3. **Project** 창에서 `Assets/UI/Theme/CircuitOneStrokeTheme` 을 선택하면 **Inspector**에서 배경색·Primary/Secondary 색·폰트·Kenney/Skymon 스프라이트 슬롯 등을 수정할 수 있습니다.
4. Kenney/Skymon 에셋을 이미 넣었다면, 여기서 해당 스프라이트를 **슬롯별로** 할당해 두면 Panel/Button 등에 자동 적용됩니다.
   - **Panel** → 테마의 **Panel Sprite** 슬롯
   - **Button** → **Button Sprite** / **Button Pressed Sprite**
   - **ProgressSlider** → **Slider Background** / **Slider Fill**
   - 적용은 **플레이 모드 진입 시**(Awake/OnEnable) 실행됩니다. 에디터에서만 수정했다면 **플레이**를 한 번 눌러야 반영됩니다.
   - 에디터에서 플레이 없이 보려면: Hierarchy에서 ScreenRoot 또는 ThemeApplier가 붙은 오브젝트를 선택한 뒤 메뉴 **Circuit One-Stroke → UI → Apply Theme to Selected** 를 실행하거나, ThemeApplier 컴포넌트에서 우클릭 → **Apply Theme Now** 를 사용하세요.

**테마 스프라이트가 적용되지 않을 때 체크리스트**

- [ ] **테마 에셋** `CircuitOneStrokeTheme` 에서 Panel Sprite / Button Sprite 등 **해당 슬롯에** Kenney/Skymon 스프라이트를 넣었는지 확인.
- [ ] **같은 테마 에셋**을 씬의 **ScreenRoot** 또는 **ThemeApplier**의 **Theme** 필드에 할당했는지 확인. (테마에만 넣고 ScreenRoot에는 다른 테마/비어 있으면 적용되지 않음.)
- [ ] Panel/Button은 **Circuit One-Stroke → UI → Create UI Prefabs** 로 만든 프리팹을 쓰는지 확인. (이 프리팹에는 ThemeRole이 붙어 있어서 테마 슬롯이 연결됨.)
- [ ] 적용 시점은 **런타임**(플레이 모드)입니다. 슬롯 할당 후 **플레이**를 눌러 확인하거나, 위 **Apply Theme to Selected** / **Apply Theme Now** 로 에디터에서 미리 적용해 보세요.

---

### 6.2 프리팹 생성 (한 번만 하면 됨)

게임에서 공통으로 쓸 Panel·Button·ProgressSlider 프리팹을 에디터 메뉴로 한 번에 만듭니다.

**단계:**

1. Unity 상단 메뉴에서 **Circuit One-Stroke → UI → Create UI Prefabs (Panel, Button, Progress)** 클릭.
2. `Assets/UI/Prefabs/` 폴더가 없으면 만들어지고, 아래 세 프리팹이 생성됩니다.

| 생성되는 프리팹 | 설명 |
|----------------|------|
| **Panel.prefab** | 배경용 패널. 자식으로 "Content" 빈 오브젝트가 있어서, 그 안에 버튼·텍스트 등을 넣을 수 있습니다. |
| **Button.prefab** | 버튼 하나 + "Label" 텍스트 자식. 버튼 클릭·테마 색/스프라이트가 적용되도록 구성되어 있습니다. |
| **ProgressSlider.prefab** | 진행률 표시용 슬라이더. 테마 Primary 색이 채워지는 영역에 적용됩니다. |

3. **(선택)** 이때 **CircuitOneStrokeTheme** 에셋을 선택한 상태로 메뉴를 실행하면, 생성 시점에 테마가 연결될 수 있습니다. 나중에 ScreenRoot나 ThemeApplier에서 테마를 할당해도 됩니다.
4. 생성 후 **Project** 창에서 각 프리팹을 더블클릭해 프리팹 편집 모드로 들어가 크기·폰트 등을 수정할 수 있습니다.

---

### 6.3 화면 제작 (씬에 UI 배치)

실제 씬에 Canvas를 두고, 그 안에 테마를 적용한 UI를 만드는 방법은 두 가지입니다.

**방법 A — ScreenRoot 사용 (화면 전체에 테마 적용)**

- 씬에 **UI → Canvas** 추가 후, Canvas **하위**에 빈 GameObject를 하나 만들고 **ScreenRoot** 컴포넌트를 붙입니다.
- ScreenRoot의 **Theme** 슬롯에 위에서 만든 **CircuitOneStrokeTheme** 에셋을 할당합니다.
- 이 빈 오브젝트 **하위**에 Panel/Button/ProgressSlider 프리팹을 넣으면, ScreenRoot가 자식들에 테마(색·스프라이트)를 자동 적용합니다.
- 여러 화면(홈·설정·게임 HUD 등)을 씬에 둘 때, 각 “화면 루트”에 ScreenRoot를 두고 같은 테마를 쓰면 일관된 look & feel을 유지하기 좋습니다.

**방법 B — ThemeApplier만 사용 (일부 영역만 테마 적용)**

- Canvas 하위에 빈 GameObject를 만들고 **ThemeApplier** 컴포넌트만 붙인 뒤, **Theme** 슬롯에 CircuitOneStrokeTheme을 할당합니다.
- 이 오브젝트 **하위**에 Panel/Button/ProgressSlider 프리팹을 배치합니다.
- ThemeApplier는 “이 오브젝트와 그 자식”에게만 테마를 적용합니다. ScreenRoot는 배경 이미지 색까지 바꿀 수 있는 반면, ThemeApplier는 할당한 테마를 자식 UI에만 적용할 때 쓰면 됩니다.

**공통:**

- 테마 에셋이 없어도 프리팹은 **테마 기본 색(플레이스홀더)** 으로 표시됩니다.
- 나중에 테마에 Kenney/Skymon 스프라이트를 넣으면, 이미 씬에 놓은 Panel/Button 등에도 **자동으로 반영**됩니다 (ThemeApplier·ScreenRoot가 실행 시 적용하기 때문).

### 화면 제작 — 단계별 (Canvas + ScreenRoot + 테마)

**Unity 6 기준**으로 작성되어 있습니다. Game 뷰가 보이려면 **씬에 카메라가 반드시 있어야** 합니다.

**Game 뷰에 "No Camera"가 뜰 때**

- Game 뷰에는 씬에 있는 **Camera**가 렌더링을 담당합니다. Canvas만 추가한 씬에는 카메라가 없을 수 있어 "No Camera"가 표시됩니다.  
- **해결:** **GameObject → Camera** 로 카메라를 추가하세요. 생성된 카메라의 **Tag**가 **MainCamera**인지 확인하면 됩니다.  
- 또는 **Circuit One-Stroke → UI → Create UI Scene (Camera + Canvas + ScreenRoot)** 메뉴를 실행하면 카메라 + Canvas + EventSystem + ScreenRoot를 현재 씬에 한 번에 추가합니다. (이미 있으면 건너뜀.)

1. **Canvas 추가**  
   - 상단 메뉴 **GameObject → UI → Canvas** 클릭.  
   - 씬에 `Canvas`와 그 하위로 `EventSystem`이 생성됩니다. (카메라는 따로 없으면 **GameObject → Camera** 로 추가.)

2. **빈 GameObject 추가**  
   - **Hierarchy**에서 `Canvas`를 우클릭 → **Create Empty** (또는 Canvas 선택 후 **Ctrl+Shift+N**).  
   - 생성된 오브젝트 이름을 `ScreenRoot` 등으로 바꿔도 됩니다.

3. **ScreenRoot 컴포넌트 붙이기**  
   - Hierarchy에서 방금 만든 빈 오브젝트를 선택.  
   - **Inspector** 하단 **Add Component** 클릭 → 검색창에 `ScreenRoot` 입력 → **Screen Root** 선택.

4. **테마 에셋 할당**  
   - 같은 오브젝트가 선택된 상태에서 Inspector의 **Screen Root** 컴포넌트를 봅니다.  
   - **Theme** 슬롯이 보이면, **Project** 창에서 `Assets/UI/Theme/CircuitOneStrokeTheme` 에셋을 찾아 **Theme** 슬롯에 드래그해서 넣거나, 슬롯 오른쪽 동그라미를 눌러 에셋을 선택합니다.

5. **패널/버튼 넣기 (필수)**  
   - 씬에 **패널이나 버튼이 보이려면** 이 단계가 꼭 필요합니다. Canvas + ScreenRoot만 있으면 흰 사각형 윤곽만 보입니다.  
   - **Project** 창에서 `Assets/UI/Prefabs/` 폴더를 연 다음, **Panel** 또는 **Button** 프리팹을 **Hierarchy**의 **ScreenRoot(빈 오브젝트) 위로 드래그**해서 자식으로 넣습니다. (Canvas 바로 아래가 아니라, ScreenRoot 아래로 넣어야 테마가 적용됩니다.)  
   - 프리팹이 없으면 먼저 **Circuit One-Stroke → UI → Create UI Prefabs (Panel, Button, Progress)** 메뉴를 실행해 생성하세요.  
   - 넣은 뒤 **Game** 뷰에서 확인하거나, **Circuit One-Stroke → UI → Apply Theme to Selected** 로 ScreenRoot를 선택한 상태에서 실행하면 테마(스프라이트)가 적용됩니다.

**씬에 패널/버튼이 안 보일 때**

- Canvas와 ScreenRoot만 만들고 **Panel·Button 프리팹을 자식으로 넣지 않으면** 씬에는 빈 캔버스(흰 사각형)만 보입니다.  
- **해결:** Project에서 `Assets/UI/Prefabs/Panel.prefab`(또는 `Button.prefab`)을 Hierarchy의 **ScreenRoot 오브젝트** 아래로 드래그해서 넣으세요.  
- UI는 **Game** 뷰에서 더 잘 보입니다. 상단 **Game** 탭을 눌러 확인해 보세요.

**Panel이 작게/아래로만 보일 때 (400×300, center, PosY -252 등)**

- 예전에 만든 Panel 프리팹은 **고정 크기(400×300) + 중앙 앵커**라서 화면에서 작고 치우쳐 보일 수 있습니다.
- **방법 1 — 프리팹 다시 만들기:** **Circuit One-Stroke → UI → Create UI Prefabs (Panel, Button, Progress)** 를 다시 실행하면 Panel이 **부모를 채우는 스트레치**로 생성됩니다. 기존 Panel 프리팹을 덮어쓰므로, 씬에 넣은 인스턴스는 한 번 삭제 후 새 Panel을 다시 넣으면 됩니다.
- **방법 2 — 씬에서만 수정:** Hierarchy에서 **Panel** 선택 → **Rect Transform**에서 **Anchor**를 왼쪽 아래 점 클릭 후 **Shift+Alt** 누른 채 **오른쪽 위 stretch**(가로·세로 막대) 선택 → **Left / Right / Top / Bottom**을 각각 24(또는 원하는 여백)으로 두면 화면을 채웁니다.
- **작은 팝업**이 필요하면: 빈 오브젝트를 만들고 그 안에 작은 Panel을 center 앵커 + 400×300으로 두는 식으로 구성하면 됩니다.

**씬 뷰에서 UI가 3D 오브젝트처럼 보이거나 Game 뷰에 안 맞을 때**

- 메뉴/패널은 화면 전체를 덮는 **2D 오버레이**로 쓰는 것이 일반적입니다. Canvas가 **World Space**(또는 Screen Space - Camera인데 카메라 미지정)이면 씬 뷰에서 3D 그리드 위의 오브젝트처럼 보이고, Game 뷰에서도 잘리거나 안 보일 수 있습니다.  
- **해결:** Hierarchy에서 **Canvas**를 선택한 뒤 **Inspector**에서 **Render Mode**를 **Screen Space - Overlay**로 바꿉니다.  
- **Canvas Scaler**가 있다면 **UI Scale Mode** = **Scale With Screen Size**, **Reference Resolution** 예: 1080×1920, **Match** = 0.5 정도로 두면 해상도에 맞게 잘 스케일됩니다.  
- 한 번에 고치려면 Canvas를 선택한 상태에서 메뉴 **Circuit One-Stroke → UI → Fix Canvas to Screen Space Overlay** 를 실행하세요.
