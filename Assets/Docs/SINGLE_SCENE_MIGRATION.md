# Single-Scene Architecture (AppScene Only)

## 중요: GameScene을 AppScene에 붙이지 않음
AppScene은 **기존 GameScene을 불러와서 합치거나 붙여서 만들지 않습니다.**  
반드시 **Circuit One-Stroke > Create AppScene (Tab Flow + Set First Build)** 또는 **Circuit One-Stroke > Scenes > Create AppScene (From Scratch, No GameScene)** 메뉴로 **처음부터** 생성합니다.

## Overview
One scene (AppScene) contains both world (camera, gameplay, systems) and UI (tabs + game screen). No separate GameScene at runtime.

## Hierarchy (Target)
```
AppRoot [AppBootstrap]
  WorldRoot
    MainCamera (single)
    GameplayRoot   (runtime nodes/edges/stroke)
    SystemsRoot    (LevelLoader, GameFlowController, TouchInput, etc.)
  UIRoot (Canvas)
    TabsRoot
      HomeScreenRoot   (LevelSelectScreen / level select)
      ShopScreenRoot
      SettingsScreenRoot
    GameScreenRoot     (GameHUD; hidden when on tabs)
    BottomNavBar       (hidden when in Game)
    EventSystem        (single)
  ScreenRouter (on UIRoot)
```

## AppScene 생성 (마이그레이션 없음)
**메뉴:** `Circuit One-Stroke > Create AppScene (Tab Flow + Set First Build)` 또는  
`Circuit One-Stroke > Scenes > Create AppScene (From Scratch, No GameScene)`

- GameScene을 **열거나 로드하지 않습니다.**
- 새 씬에 Canvas, SafeArea, MainShellRoot(Home/Shop/Settings), BottomNavBar, GameScreenRoot를 만들고, FillGameScreenRoot로 LevelLoader·GameHUD 등을 **프리팹/코드 기준**으로 채웁니다.
- Build Settings에서 AppScene이 0번으로 설정됩니다.

## Runtime Flow
- **Play** starts in AppScene; Home tab + BottomNav visible.
- **Level select** → `ScreenRouter.EnterGame(levelId)` (or `AppRouter.RequestStartLevel` if AppRouter present); BottomNav hidden, GameScreenRoot shown, TouchInput enabled.
- **GameHUD Home/Back** → `ScreenRouter.ExitGameToHome()` or `AppRouter.ExitGameToHomeTab()`; back to Home tab, BottomNav visible, TouchInput disabled.

## Checklist
- [ ] One Canvas, one EventSystem, one Main Camera.
- [ ] Home/Shop/Settings switch panels and show selected tab.
- [ ] Level selection enters Game (no BottomNav).
- [ ] Home in GameHUD returns to Home tab (BottomNav visible).
- [ ] CanvasScaler 1080x1920, ScaleWithScreenSize.
