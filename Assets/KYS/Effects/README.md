# 플레이어 이펙트 시스템 사용법

## 개요
아이템을 수집했을 때 플레이어 주변에 표시되는 시각적 이펙트 시스템입니다.
**MeshRenderer와 ParticleSystem을 모두 지원합니다!**

## 구현된 기능

### 1. ItemConfiguration 확장
- `ItemConfig`에 `playerEffectPrefab`, `effectOffset`, `followPlayer` 필드 추가
- 각 아이템 타입별로 고유한 이펙트 프리팹 설정 가능

### 2. ReceiveGamePlayer 이펙트 관리
- `ActivateEffect(ItemType, duration)`: 이펙트 활성화
- `DeactivateEffect(ItemType)`: 이펙트 비활성화
- `ClearAllEffects()`: 모든 이펙트 정리
- 자동 이펙트 관리 (효과 종료 시 자동 제거)

### 3. PlayerEffectController 스크립트
- **MeshRenderer 지원**: 회전, 펄스, 색상 변화 효과
- **ParticleSystem 지원**: 파티클 효과 + 색상 변화
- 자동 감지: 파티클 시스템이 있으면 자동으로 파티클 모드 사용
- 런타임 설정 가능

## 사용 방법

### 1. 이펙트 프리팹 생성

#### A. MeshRenderer 방식 (기본)
1. 빈 GameObject 생성
2. MeshRenderer, MeshFilter 추가
3. PlayerEffectController 스크립트 추가
4. 원하는 효과 설정
5. 프리팹으로 저장

#### B. ParticleSystem 방식 (권장)
1. 빈 GameObject 생성
2. **ParticleSystem 컴포넌트 추가**
3. PlayerEffectController 스크립트 추가
4. 파티클 효과 설정:
   - **Speed**: 빠른 스파클, 빨간색
   - **Slow**: 느린 연기, 파란색  
   - **Magnet**: 회전하는 오라, 노란색
5. 프리팹으로 저장

### 2. ItemConfiguration 설정
1. ItemConfiguration 에셋 열기
2. 각 ItemType의 `playerEffectPrefab` 필드에 이펙트 프리팹 할당
3. `effectOffset`으로 위치 조정
4. `followPlayer`로 플레이어 추적 여부 설정

### 3. 자동 활성화
- Speed 아이템 수집 시: 스피드 이펙트 자동 활성화
- Slow 아이템 수집 시: 슬로우 이펙트 자동 활성화  
- Magnet 아이템 수집 시: 마그네틱 이펙트 자동 활성화

## 예시 이펙트 프리팹

### Speed Effect (빨간색 스파클)
- **ParticleSystem 설정**:
  - Start Color: 빨간색
  - Start Speed: 3-5
  - Start Size: 0.1-0.3
  - Emission Rate: 20-30
  - Shape: Circle
  - Rotation Speed: 180도/초

### Slow Effect (파란색 연기)
- **ParticleSystem 설정**:
  - Start Color: 파란색
  - Start Speed: 1-2
  - Start Size: 0.5-1.0
  - Emission Rate: 10-15
  - Shape: Sphere
  - Rotation Speed: 45도/초

### Magnet Effect (노란색 오라)
- **ParticleSystem 설정**:
  - Start Color: 노란색
  - Start Speed: 2-3
  - Start Size: 0.2-0.5
  - Emission Rate: 15-20
  - Shape: Circle
  - Color Change: 활성화
  - Rotation Speed: 90도/초

## 파티클 시스템 장점

### ✅ **장점들:**
1. **더 화려한 시각 효과** - 스파클, 연기, 불꽃 등
2. **자연스러운 움직임** - 물리 기반 파티클
3. **성능 최적화** - GPU 기반 렌더링
4. **다양한 효과** - 색상 변화, 크기 변화, 회전 등
5. **자동 감지** - 파티클 시스템이 있으면 자동으로 파티클 모드 사용

### 🔧 **PlayerEffectController 파티클 지원:**
- `RestartParticles()`: 파티클 재시작
- `StopParticles()`: 파티클 중지
- `PauseParticles()`: 파티클 일시정지
- 자동 색상 변화 지원
- 회전 효과 지원

## 주의사항
- 로컬 플레이어에만 이펙트가 표시됩니다
- 이펙트는 플레이어의 자식으로 생성되어 자동으로 따라다닙니다
- 효과 종료 시 자동으로 이펙트가 제거됩니다
- **파티클 시스템이 있으면 자동으로 파티클 모드로 전환됩니다** 