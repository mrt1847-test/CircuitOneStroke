# Circuit One-Stroke — UI 에셋 가이드

이 문서는 **Kenney UI Pack - Sci-Fi**와 **Skymon Icon Pack Free** 에셋을 프로젝트에 넣는 방법을 안내합니다.  
에셋이 없어도 UI 테마와 프리팹은 동작하며, 스프라이트는 플레이스홀더(단색)로 표시됩니다.

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

## 4. 스타일 가이드 (일관 적용)

- **배경**: 매우 어두운 네이비/블랙 (회로판 느낌)
- **강조색**: 네온 틸(primary), 부드러운 블루(secondary), 경고용 앰버/레드
- Kenney Sci-Fi 패널/버튼으로 통일감 있는 UI 구성

---

## 5. 테마·프리팹 사용 (빠른 화면 제작)

1. **테마 생성**  
   메뉴 **Circuit One-Stroke → UI → Create Default Theme** 로 `Assets/UI/Theme/CircuitOneStrokeTheme.asset` 생성.

2. **프리팹 생성**  
   메뉴 **Circuit One-Stroke → UI → Create UI Prefabs (Panel, Button, Progress)** 로 다음 프리팹 생성:
   - `Assets/UI/Prefabs/Panel.prefab` — 패널 + Content 자식
   - `Assets/UI/Prefabs/Button.prefab` — 버튼 + Label 텍스트
   - `Assets/UI/Prefabs/ProgressSlider.prefab` — 슬라이더(진행률)

3. **화면 제작**  
   - 씬에 **UI → Canvas** 추가 후, Canvas 하위에 `ScreenRoot` 컴포넌트가 있는 빈 GameObject를 두고 테마 에셋을 할당.
   - 또는 Canvas 하위에 **ThemeApplier**를 붙이고 테마를 할당한 뒤, 그 하위에 Panel/Button/ProgressSlider 프리팹을 배치.
   - 프리팹은 테마가 없어도 색상 플레이스홀더로 표시되며, 테마에 Kenney/Skymon 스프라이트를 넣으면 자동 반영됩니다.
