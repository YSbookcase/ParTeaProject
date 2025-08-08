# 아이템 설정 시스템 (Item Configuration System)

이 시스템은 파워업 아이템들이 색상뿐만 아니라 고유한 시각적 외형을 가질 수 있도록 하는 구성 시스템입니다.

## 개요

기존에는 아이템들이 색상으로만 구분되었지만, 이제 각 아이템 타입별로 고유한 시각적 프리팹을 설정할 수 있습니다.

## 주요 컴포넌트

### 1. ItemConfiguration (ScriptableObject)
- 각 아이템 타입별 설정을 관리하는 ScriptableObject
- 시각적 프리팹, 색상, 게임플레이 설정 등을 포함

### 2. EnhancedItemController
- 기존 ItemController의 향상된 버전
- ItemConfiguration을 사용하여 아이템의 시각적 외형을 동적으로 변경
- 레거시 시스템과의 호환성 유지

### 3. ItemVisualPrefab
- 아이템 시각적 프리팹을 위한 헬퍼 클래스
- 발광, 회전, 파티클 효과 등을 지원

## 설정 방법

### 1. ItemConfiguration 생성

1. Project 창에서 우클릭
2. Create > KYS > Item Configuration 선택
3. 생성된 파일을 `ItemConfiguration`으로 이름 변경
4. Resources 폴더로 이동

### 2. 아이템 설정 구성

ItemConfiguration에서 각 아이템 타입별로 다음을 설정:

- **Item Type**: 아이템 타입 (Normal, Bonus, Speed, Slow, Magnet)
- **Visual Prefab**: 해당 아이템 타입의 시각적 프리팹
- **Item Color**: 기본 색상 (프리팹이 없을 때 사용)
- **Point Value**: 점수 값
- **Effect Duration**: 효과 지속 시간
- **Physics Settings**: 물리 설정 (바운스, 회전 등)

### 3. 시각적 프리팹 생성

각 아이템 타입별로 고유한 시각적 프리팹을 생성:

1. 빈 GameObject 생성
2. MeshRenderer와 MeshFilter 추가
3. 원하는 메시 (Cube, Sphere, Custom Mesh 등) 설정
4. ItemVisualPrefab 스크립트 추가
5. 아이템 타입에 맞는 설정 적용
6. 프리팹으로 저장

### 4. EnhancedItemController 사용

기존 아이템 프리팹에서:
1. ItemController를 EnhancedItemController로 교체
2. ItemConfiguration 참조 설정
3. Visual Container 설정 (자동 생성됨)

## 아이템 타입별 권장 시각적 외형

### Normal (일반 아이템)
- **외형**: 단순한 큐브 또는 구체
- **색상**: 흰색
- **효과**: 발광 없음

### Bonus (보너스 아이템)
- **외형**: 별 모양 또는 다이아몬드
- **색상**: 노란색
- **효과**: 강한 노란색 발광

### Speed (속도 증가)
- **외형**: 화살표 또는 번개 모양
- **색상**: 파란색
- **효과**: 청록색 발광

### Slow (속도 감소)
- **외형**: 모래시계 또는 느린 기호
- **색상**: 빨간색
- **효과**: 빨간색 발광

### Magnet (자석 효과)
- **외형**: 자석 모양 또는 U자형
- **색상**: 초록색
- **효과**: 초록색 발광

## 사용 예시

### 코드에서 아이템 타입 설정
```csharp
// EnhancedItemController 사용
EnhancedItemController itemController = GetComponent<EnhancedItemController>();
itemController.SetItemType(ItemType.Speed);
```

### 런타임에 시각적 프리팹 변경
```csharp
// ItemVisualPrefab에서 색상 변경
ItemVisualPrefab visualPrefab = GetComponent<ItemVisualPrefab>();
visualPrefab.SetColor(Color.red);
visualPrefab.SetEmission(true, Color.red, 2f);
```

## 마이그레이션 가이드

### 기존 시스템에서 새 시스템으로 전환

1. **기존 아이템 프리팹 업데이트**:
   - ItemController를 EnhancedItemController로 교체
   - ItemConfiguration 참조 추가

2. **새로운 시각적 프리팹 생성**:
   - 각 아이템 타입별로 고유한 프리팹 생성
   - ItemConfiguration에 등록

3. **점진적 전환**:
   - 기존 색상 시스템은 그대로 유지
   - 시각적 프리팹이 없으면 색상으로 대체

## 주의사항

1. **성능**: 시각적 프리팹은 메모리를 사용하므로 적절한 최적화 필요
2. **네트워크 동기화**: 프리팹 변경은 모든 클라이언트에서 동기화되어야 함
3. **Resources 폴더**: ItemConfiguration은 반드시 Resources 폴더에 위치해야 함
4. **프리팹 크기**: 시각적 프리팹의 크기가 너무 크지 않도록 주의

## 문제 해결

### ItemConfiguration을 찾을 수 없음
- Resources 폴더에 ItemConfiguration 파일이 있는지 확인
- 파일 이름이 정확한지 확인

### 시각적 프리팹이 표시되지 않음
- 프리팹에 Renderer 컴포넌트가 있는지 확인
- Visual Container가 올바르게 설정되었는지 확인
- 프리팹의 스케일이 적절한지 확인

### 성능 문제
- 시각적 프리팹의 폴리곤 수 확인
- 불필요한 파티클 효과 제거
- 오브젝트 풀링 고려 