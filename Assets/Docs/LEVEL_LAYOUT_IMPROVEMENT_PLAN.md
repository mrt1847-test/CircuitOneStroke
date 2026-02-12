# 레벨 레이아웃 개선 계획 (템플릿 적용 + 가독성)

## 1. 문제 재현 및 원인 추적

### 1.1 “템플릿이 안 먹는” 가능한 원인 Top 3

| 순위 | 원인 | 설명 | 검증 방법 |
|------|------|------|-----------|
| **1** | **PlaceNodesWithLayout이 Accept 실패 시에도 마지막 try의 positions를 반환함** | 25회 재시도 후에도 Accept(minDist/교차/CV)를 통과하지 못하면, 현재 코드는 `return positions`로 **실패한 마지막 배치**를 그대로 반환한다. Fallback으로 `PlaceNodesOnCircleFallback`이 호출되지 않아, “템플릿처럼 보이지만 품질 검증에 실패한” 배치가 최종 레벨에 들어간다. | 로그: `PlaceNodesWithLayout` 내부에서 `templateUsed=true`인데 루프 종료 시 `Accept` 통과 여부와, 반환 직전 `return positions` vs `return null` 분기 확인. |
| **2** | **토폴로지(엣지)와 레이아웃(슬롯)이 독립적으로 선택됨** | `GenerateBaseInternal`에서 엣지 리스트는 **LevelTemplates**(N 일치 시) 또는 **GenerateBaseGraphTopology**(랜덤)로 정해지고, 위치는 **LayoutTemplates** 슬롯 + permutation + jitter로 정해진다. Ring 슬롯에 랜덤 그래프를 올리면 교차가 많아 Accept가 자주 실패하고, 그래도 반환되는 건 위 1번 때문에 “실패한 템플릿 배치”일 수 있다. | 로그: 이번 레벨의 N, 선택된 **레이아웃 템플릿 이름**, **토폴로지 출처**(LevelTemplate 이름 vs "RandomTopology"). Accept 실패 시 교차 수 / minDist / CV 로그. |
| **3** | **스케일/센터링 부재로 화면 비율·최소거리 일관성 없음** | 생성기는 항상 “월드 단위”(예: Ring 반경 3.5)로만 pos를 만들고, **Playable Rect(상·하단 UI 제외)** 에 맞춘 스케일/센터링이 없다. 기기/해상도에 따라 노드가 너무 빽빽하거나 흩어져 보일 수 있고, `minNodeSpacing`을 “화면 비율”이 아니라 고정값(0.5)으로만 써서 터치 UX가 일정하지 않다. | 로그: 템플릿 적용 직후 pos min/max, (도입 시) 정규화/스케일/센터링 후 min/max. 벤치: 여러 seed로 생성 후 minDist, 교차 수, bounding box 크기 로그. |

### 1.2 디버그 로그로 확인할 항목 (구현됨)

- 이번 레벨 **N, seed**, 선택된 **레이아웃 템플릿 이름**(또는 null/fallback)
- **템플릿 적용 여부**(bool): LayoutTemplates에서 슬롯을 썼는지
- 템플릿 적용 직후 **pos 배열 값 범위**(min/max x,y)
- (선택) 정규화/스케일/센터링 후 값 범위
- **Aesthetic 후보**: 유효 후보 수, 최고 점수 후보 선택 결과(점수 값)
- **Accept 실패 시**: 교차 수, minDist, CV, 실패 사유 요약

### 1.3 흔한 실수 케이스 점검

- **템플릿 적용 직후 GenerateBaseGraphTopology 또는 랜덤 pos가 덮어쓰는지**  
  → **아니오.** `GenerateBaseInternal` 순서: `edgeList` 결정 → `PlaceNodesWithLayout(n, rng, outEdges)` → 여기서만 pos 결정. 토폴로지는 pos를 덮어쓰지 않음.
- **템플릿 pos가 적용되지만 Aesthetic 후보 생성 과정에서 템플릿 후보가 제외되는지**  
  → **가능.** `GenerateBase`는 여러 시드(attempt)로 `GenerateBaseInternal`을 호출하고, **Score가 가장 높은 1개**만 반환한다. 템플릿 기반 배치가 Accept를 통과해도 Score가 다른 attempt(예: circle fallback)보다 낮으면 버려진다. 상위 M개 중 랜덤/다양성 혼합 전략 도입 권장.
- **템플릿 pos가 화면 스케일링 과정에서 압축되어 최소거리가 깨지는지**  
  → 현재 생성 파이프라인에는 **화면 스케일링 단계가 없음.** 최종 pos가 그대로 레벨에 들어가므로, “스케일 후 압축”보다는 “원래 슬롯+jitter만으로 minDist/Accept가 깨지는 경우”가 문제.

---

## 2. 수정 후 목표

- **노드 최소거리**: `minNodeSpacing`(월드 또는 화면 비율) 명시적 정의 및 강제(relax/repulsion 또는 Accept 강화).
- **교차 수**: 상한(예: 2 이하) 준수, 가독성 점수에서 교차 페널티 강화.
- **화면 안전영역**: Playable Rect 내로 bounding box 맞추기(센터링 + 스케일), 모바일 세로 기준.
- **템플릿 적용 보장**: 템플릿이 있으면 “템플릿 pos → (선택) 스케일/센터 → (선택) minSpacing relax → 확정” 순서로만 적용하고, Accept 실패 시 **null 반환**하여 fallback이 사용되도록 함.

---

## 3. 코드 변경 포인트 (파일/클래스/함수)

| 위치 | 변경 내용 |
|------|------------|
| **LevelGenerator** | `GenerateBase`: N, seed, 선택된 레이아웃 템플릿 이름, 템플릿 적용 여부, 후보 수, 최고 점수 로그. `PlaceNodesWithLayout`: 템플릿 이름/적용 여부, try별 Accept/minDist/교차 실패 로그, **모든 try 실패 시 `return null`** (현재는 마지막 positions 반환). `GenerateBaseInternal`: PlaceNodesWithLayout 반환 null이면 PlaceNodesOnCircleFallback 호출 유지. |
| **LevelGenerator** | (선택) `ScaleAndCenterToPlayableRect(Vector2[] positions, float playableWidth, float playableHeight)` 유틸 추가 후, 템플릿 적용 경로 끝에서 호출. |
| **LayoutTemplates** | N=4~25에 대해 Ring/DoubleRing/Grid/Layered 등 패밀리 확대. `GetLayoutsForNodeCount`에서 N=4, 7, 11~15 등 구간별로 동일한 Ring만 쓰지 않고 Grid/Star 등 추가. (선택) 토폴로지 특징(평균 차수, edge density)에 따른 선택 확장. |
| **AestheticEvaluator** | **가독성 중심 재정의**: (필수 패널티) 노드 최소거리 위반, 엣지-노드 근접, 엣지-엣지 교차 수, 과도한 길이/짧은 엣지; (선호) 전체 분산, 노드별 엣지 각도 분산. `Score()`를 위 항목 가중합으로 변경. `Accept()`는 기존 crossing/minDist/CV에 더해 “엣지-노드 거리 최소값” 등 조건 추가 가능. |
| **LevelGenerator** | 후보 선택: “최고 점수 1개” 대신 **상위 M개 중 랜덤** 또는 **점수 + 다양성(배치 분산) 혼합** 옵션 추가. |
| **Post-layout** | (선택) `RelaxMinSpacing(Vector2[] positions, float minSpacing, int maxIter)` 같은 repulsion/relax 함수를 템플릿 적용 후 호출해 minNodeSpacing 보장. |

---

## 4. 간단한 벤치(여러 seed로 생성 시 지표 로그)

- **방법**: `GenerateBase(N, seed, opts)`를 seed = 1000, 1001, … 1010 등으로 여러 번 호출. 각 레벨에 대해 `AestheticEvaluator.CountCrossings`, `MinNodeDistance`, bounding box 크기(또는 분산)를 로그.
- **목적**: 수정 전/후 교차 수, 최소거리, 분산이 개선되는지 확인. 템플릿 적용률(로그에서 templateUsed 비율)도 함께 확인.

---

## 5. 터치 UX 제약 (모바일 세로)

- **Playable Rect**: 상단/하단 UI 바를 제외한 영역만 사용. (현재는 생성 단계에서 좌표계만 사용; 실제 카메라/Canvas 설정은 씬 쪽.)
- **minNodeSpacing**: 월드 단위(예: 0.5) 또는 “평균 엣지 길이의 비율”(예: 0.25)로 정의하고, Accept/Relax에서 강제.
- **노드 크기/선 두께**: View 쪽 상수와 맞춰 “최소 터치 간격”이 보장되도록 minNodeSpacing 하한 설정 권장.

---

## 6. 구현 우선순위

1. **즉시**: PlaceNodesWithLayout에서 모든 try 실패 시 **return null** + 디버그 로그 추가. ✅
2. **즉시**: GenerateBase / GenerateBaseInternal에서 **N, seed, 템플릿 이름, 적용 여부, 후보 수, 최고 점수** 로그. ✅
3. **단기**: AestheticEvaluator를 가독성(교차/최소거리/엣지-노드/엣지 길이) 중심으로 재정의. ✅ ScoreReadability 추가, Score는 해당 호출.
4. **단기**: 템플릿 커버리지 확대(N=4~25, Ring/Grid/Star 등). N=4 명시(Ring4) 추가됨.
5. **중기**: 스케일/센터링(Playable Rect) 및 (선택) relax 후처리, 후보 선택 정책(상위 M개/다양성).

---

## 7. 벤치 실행 방법 (여러 seed로 지표 로그)

에디터에서 또는 테스트 코드에서 다음을 반복 호출하면 된다.

```csharp
// 예: N=8, seed 1000..1010
for (int s = 1000; s <= 1010; s++)
{
    var level = LevelGenerator.GenerateBase(8, s, opts);
    if (level == null) continue;
    var pos = LevelPositions(level);  // LevelGenerator 내부 유틸과 동일한 방식
    int cross = AestheticEvaluator.CountCrossings(level.edges, pos, level.nodes.Length);
    float minD = AestheticEvaluator.MinNodeDistance(pos, level.nodes.Length);
    float score = AestheticEvaluator.Score(level);
    Debug.Log($"seed={s} crossings={cross} minDist={minD:F3} score={score:F2}");
}
```

`LevelGenerator.EnableLayoutDebugLog = true`로 두면 각 attempt별 layout 이름, template 적용 여부, fallback 사용 여부가 로그에 찍힌다.

---

## 8. (선택) 스케일/센터 유틸 시그니처 제안

```csharp
// LevelGenerator 또는 별도 static 유틸 클래스
public static void ScaleAndCenterToPlayableRect(
    Vector2[] positions,
    float playableWidth,
    float playableHeight,
    float padding = 0.1f,
    float minNodeSpacingAfterScale = 0.5f)
```

- positions를 in-place 수정: bounding box 구한 뒤 중심을 (0,0)으로, playable 영역의 (1-padding) 비율로 스케일.
- minNodeSpacingAfterScale보다 작아지면 스케일을 줄여서 최소거리 유지.
- 호출 시점: PlaceNodesWithLayout 또는 PlaceNodesOnCircleFallback 반환 직후, 노드 리스트에 넣기 전.
