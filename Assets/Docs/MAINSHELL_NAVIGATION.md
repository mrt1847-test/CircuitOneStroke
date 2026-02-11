# MainShell + GamePlay Navigation (AppRouter)

## Overview

- **MainShell**: Bottom tab bar (Home / Shop / Settings) always visible when not in gameplay.
- **GamePlay**: Full-screen level play; no bottom bar. GameRoot is enabled, MainShellRoot disabled.
- **Overlays**: Transition, Result (Win/Lose), OutOfHearts, ConfirmExit, Toast. RESULT_* and OUT_OF_HEARTS never shown at once.

## Setup

1. Run **Circuit One-Stroke > Create MainShell + GameRoot + OverlayRoot** on a scene that already has a Canvas (e.g. after Create Game Scene).
2. In **AppRouter** Inspector assign:
   - **mainShellRoot** / **gameRoot** (set by the menu).
   - **homeTabView**: Level select content (e.g. panel with Continue + LevelSelectScreen).
   - **shopTabView** / **settingsTabView**: Shop and Settings panels (can use existing prefabs).
   - **overlayManager** (under OverlayRoot).
   - **levelLoader**, **levelManifest**, **gameFlowController**, **adServiceComponent** (optional).
3. Move or duplicate the existing **GameHUD** under **GameRoot > GameHUDRoot** so it is shown only in GamePlay mode.
4. Ensure **TransitionManager** exists (runtime or scene); transition overlay is managed by it.

## Bootstrap

- **AppRouter** runs `Boot()` in `Start()`: shows MainShell with Home tab and binds bottom nav.
- If **AppRouter** exists, **UIScreenRouter** skips its initial screen and defers to AppRouter.

## Back (Android Back / Escape)

- Transition active: ignore.
- Result visible: `ExitGameToHomeTab`.
- OutOfHearts visible: MainShell → close overlay; GamePlay → `ExitGameToHomeTab`.
- ConfirmExit visible: close overlay.
- GamePlay: show ConfirmExit (if enabled) or exit to home.
- MainShell Shop/Settings: switch to Home tab.
- MainShell Home: quit (editor) / Application.Quit().

## Guards

- **RequestStartLevel** / **RequestRetry** / **RequestNext**: if `!HeartsManager.CanStartAttempt()` → show OutOfHearts and do not enter/restart gameplay.
- HardFail consumes one heart once (in GameStateMachine); Reject does not.

## Refill and Resume

- When rewarded refill succeeds: `HeartsRefillAdFlow` calls `RefillFull()` and `GameFlowController.ResumeLastIntent()` (which delegates to **AppRouter.ResumeLastIntent()**).
- **AppRouter.ResumeLastIntent()** hides Result/OutOfHearts overlays and runs the last intent (StartLevel / Retry / NextLevel).

## 디자인(테마) 적용

- **CircuitOneStrokeTheme** 한 개를 에디터에서 로드해 사용.
- **MainShell**: 하단 바 `ThemeRole.FooterBar`(primary), 탭 버튼 `ThemeRole.Button` + `ThemeTextRole.useAccentColor = true`. **ThemeApplier**를 MainShellRoot에 부착해 테마 적용.
- **오버레이**: Result 패널 `ThemeRole.Panel`, 버튼 `ThemeRole.Button`. Exit는 `theme.danger`, Watch Ad(실패)는 `theme.warning`. **ThemeApplier**를 OverlayManager 루트에 부착.
- 딤/어두운 배경은 반투명 검정·어두운 회색으로 고정. 테마 **font**가 있으면 버튼/메시지에 적용.

## TODOs

- Ad provider: wire real rewarded/interstitial in **HeartsRefillAdFlow** and **AppRouter** (e.g. level index, placement).
- Tab content: assign Home (level grid + Continue), Shop, Settings views to AppRouter; optional prefabs for consistency.
- Result dialog layout: position Next / Level Select and Retry / Level Select / Watch Ad in OverlayManager’s ResultDialog.
- Optional: Settings overlay from GamePlay instead of exiting to Settings tab.
