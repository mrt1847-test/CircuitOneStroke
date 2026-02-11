# 로컬라이제이션 (ScriptableObject 기반)

## 개요

Settings 등 UI 문자열을 하드코딩하지 않고 **LocalizationTable** ScriptableObject로 관리합니다.  
언어는 `Application.systemLanguage` 기준으로 자동 선택(한국어 → ko, 그 외 → en).  
향후 Unity Localization 패키지 또는 String Table로 전환할 수 있도록 키 기반으로 구성했습니다.

## 사용

- **런타임**: `CircuitOneStroke.Core.Localization.Get("키")`  
  - 테이블/키가 없으면 `key` 그대로 반환(폴백).
- **에셋 위치**: `Resources/LocalizationTable.asset` (Resources.Load로 로드).

## 초기 설정 (최초 1회)

1. 메뉴 **Circuit One-Stroke → Localization → Create LocalizationTable (Resources)** 실행.
2. `Assets/Resources/LocalizationTable.asset` 이 생성되고, Settings 관련 기본 키(en/ko)가 채워짐.
3. 필요 시 Inspector에서 키 추가·번역 수정.

## 현재 키 (Settings·Shop)

| 키 | 용도 |
|----|------|
| `settings` | 설정 타이틀 |
| `haptics_light`, `haptics_normal` | 햅틱 강도 드롭다운 |
| `node_small`, `node_normal`, `node_large` | 노드 크기 |
| `line_thin`, `line_normal`, `line_thick` | 선 굵기 |
| `how_to_play_toast` | 방법 토스트 |
| `no_ads_desc` | 광고 제거 상품 설명 (Settings·Shop) |
| `toast_continue_from_tail` | Paused 시 다른 곳 터치 시 토스트 (게임) |

## 확장

- `LocalizationTable`에 `Entry` 추가 후 Inspector에서 en/ko 입력.
- 다른 언어 추가 시 `LocalizationTable.cs`의 `Get()`에 분기 추가하거나, Entry에 필드 추가 후 직렬화.
