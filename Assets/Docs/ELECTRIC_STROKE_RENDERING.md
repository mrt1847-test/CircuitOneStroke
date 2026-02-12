# 활성 경로 전기 연출 (StrokeRenderer)

## 요약
- **점/비드/지속 파티클 제거**: 경로를 따라 움직이는 연속 점 없음. LED 스트립 느낌 제거.
- **2겹 LineRenderer**: Core(흰색에 가까운 노란빛) + Outer(파란 글로우). 흰 배경(#FFFFFF)에서도 선명.
- **간헐적 스파크 버스트**: 노드/접점 위주로 초당 4~10회 짧은 스파크(0.1~0.25초)만 사용.
- **전류 흐름**: 점 이동 대신 **텍스처 스크롤**(`mainTextureOffset` + Tile 모드)로 표현.
- **텍스처**: 점(spot) 무늬가 아닌 **연속 스트릭**(가운데 밝은 띠, `ProceduralSprites.ElectricFlowTexture`) 사용 → "구슬 행진" 대신 "빛이 흐르는 선".

---

## 수정 파일 목록

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/View/StrokeRenderer.cs` | 연속 점 제거, 스파크 버스트만 사용, 파라미터 정리 |

---

## 핵심 diff 요약 (StrokeRenderer.cs)

1. **제거**
   - `sparkEmitRate`, `sparkTravelSpeed` 필드
   - `_pathT` 및 경로를 따라 이동하는 에미터 위치 업데이트
   - `emission.rateOverTime = 80` → **0**, `emission.enabled` → **false**
   - shape radius 0.05 → 0.001 (버스트 시에만 위치 지정)

2. **추가/변경**
   - `sparkBurstsPerSecond` (4~10), `sparkLifetimeMin` / `sparkLifetimeMax` (0.1~0.25초)
   - `_sparkBurstAccum`: 시간 누적 후 `burstInterval = 1/sparkBurstsPerSecond`마다 `Emit(1)` 한 번
   - `GetSparkSpawnPosition(n)`: 70% 노드 인덱스(0, 1, n-1, n-2), 30% 경로 중간
   - 스파크 생성 시 `ParticleSystem.EmitParams`로 `startLifetime` 랜덤(0.1~0.25초)
   - Core/Outer 색·두께·정렬·cap/corner vertices 6은 기존 유지 (이미 스펙 충족)

---

## 체크리스트 (확인 방법)

- [ ] **점이 사라짐**: 활성 경로를 그려도 **경로를 따라 흐르는 연속 점/비드가 전혀 없음**. (이전: 80/s 연속 방출)
- [ ] **스파크만 간헐적**: 노드·접점 근처에서 **짧게 “빠직” 터지는 스파크만** 초당 4~10회 정도 보임.
- [ ] **스파크 수명**: 스파크가 0.1~0.25초 안에 사라짐 (길게 끌리지 않음).
- [ ] **전기 느낌**: 선 자체는 텍스처 스크롤로 흐르고, 스파크는 보조 연출로만 사용됨.
- [ ] **흰 배경 대비**: Core 색 (1, 0.98, 0.75), Outer (0.10, 0.55, 1.00, 0.55), Outer 두께 = Core×2, outer sortingOrder = core - 1.
- [ ] **캡/코너**: Core·Outer 모두 `numCapVertices` / `numCornerVertices` ≥ 6 유지.

---

## 인스펙터 튜닝 (선택)

- **Spark Bursts Per Second**: 4~10 (기본 6). 높을수록 스파크 빈도 증가.
- **Spark Lifetime Min/Max**: 0.1~0.25초. 짧을수록 “빠직” 느낌, 길면 살짝 더 보임.
- **Flow Speed** / **Texture Scale**: 전류 텍스처 스크롤 속도·타일 밀도.

---

## "구슬 느낌" 제거 체크리스트

전기처럼 보이려면 "점이 이동"이 아니라 "연속 하이라이트(밝은 띠) + 노이즈/깜빡임"이어야 함.

- [x] **텍스처가 점/원 패턴이 아님**: `ElectricFlowTexture`는 가운데 밝고 양끝으로 사라지는 **스트릭 띠** 한 줄 (점 8개 반복 → 제거됨).
- [x] **Filter Mode**: Point(Nearest)면 구슬/픽셀 강화 → 텍스처는 **Bilinear**.
- [x] **Wrap Mode**: Repeat (흐르는 스크롤에 적합).
- [x] **UV 스크롤**: `mainTextureOffset.x = Mathf.Repeat(Time.time * flowSpeed, 1f)`.
- [x] **깜빡임**: `flickerEnabled` 시 Core/Outer 알파에 Perlin 노이즈 적용.
- **추가 전기 연출(선택)**: Shader Graph로 이동 펄스 + Simple Noise + Additive 블렌딩 시 "정석" 전기 느낌. 현재는 텍스처 스트릭 + 스크롤 + 스파크 버스트로 90% 충족.
