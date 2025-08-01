# ReceiveGame (3D 물건 받기 게임)

## 게임 개요
- **게임 타입**: 3D 멀티플레이어 물건 수집 게임
- **최대 인원**: 4명
- **게임 시간**: 60초
- **화면 비율**: 세로 16:9
- **목표**: 제한 시간 내에 가장 많은 물건을 수집하여 높은 랭킹을 얻기

## 게임 규칙
1. 게임이 시작되면 맵에 물건들이 랜덤하게 생성됩니다
2. 플레이어는 WASD 키 또는 마우스 클릭으로 이동할 수 있습니다
3. 물건에 접근하면 자동으로 수집됩니다
4. 60초 후 게임이 종료되고 점수에 따라 랭킹이 결정됩니다
5. 랭킹 결과는 JTW의 스코어 시스템으로 전달됩니다

## 씬 설정 방법

### 1. 씬 생성
1. Unity에서 새 씬을 생성합니다
2. 씬 이름을 "ReceiveGame"으로 설정합니다
3. Build Settings에 씬을 추가합니다

### 2. 카메라 설정
1. Main Camera에 `ReceiveGameCamera` 스크립트를 추가합니다
2. 카메라 설정:
   - Projection: Perspective
   - Field of View: 60
   - Background Color: 원하는 색상

### 3. 게임 매니저 설정
1. 빈 GameObject를 생성하고 이름을 "GameManager"로 설정합니다
2. `ReceiveGameManagerEnhanced` 스크립트를 추가합니다
3. 설정값:
   - Game Time: 60
   - Max Players: 4
   - Item Prefab: 수집할 아이템 프리팹 할당
   - Spawn Points: 아이템 스폰 포인트들 할당

### 4. UI 설정
1. Canvas를 생성합니다
2. Canvas에 `ReceiveGameUI` 스크립트를 추가합니다
3. UI 요소들:
   - **Time Text**: 상단 중앙에 시간 표시용 TextMeshPro
   - **Score Panel**: 점수 표시용 Panel
   - **Player Score Prefab**: 개별 플레이어 점수 UI 프리팹
   - **Game End Panel**: 게임 종료 표시용 Panel

### 5. 플레이어 스폰 설정
1. 빈 GameObject를 생성하고 이름을 "PlayerSpawner"로 설정합니다
2. `ReceiveGameSpawner` 스크립트를 추가합니다
3. 설정값:
   - Player Prefab: 플레이어 프리팹 할당
   - Spawn Points: 플레이어 스폰 포인트들 할당

### 6. 플레이어 프리팹 설정
1. 플레이어용 GameObject를 생성합니다
2. 컴포넌트 추가:
   - MeshRenderer (또는 SkinnedMeshRenderer)
   - CapsuleCollider (IsTrigger = false)
   - Rigidbody (Use Gravity = true, Is Kinematic = false)
   - PhotonView
   - `ReceiveGamePlayer` 스크립트
3. Tag를 "Player"로 설정합니다
4. 프리팹으로 저장합니다

### 7. 아이템 프리팹 설정
1. 아이템용 GameObject를 생성합니다
2. 컴포넌트 추가:
   - MeshRenderer
   - SphereCollider (IsTrigger = true)
   - `CollectibleItem` 스크립트
3. 프리팹으로 저장합니다

### 8. 점수 UI 프리팹 설정
1. 플레이어 점수 표시용 GameObject를 생성합니다
2. 컴포넌트 추가:
   - `PlayerScoreUI` 스크립트
3. UI 요소들:
   - Player Name Text (TextMeshPro)
   - Score Text (TextMeshPro)
   - Player Color Image
4. 프리팹으로 저장합니다

## 3D 환경 설정

### 1. 지형 생성
1. **Hierarchy**에서 우클릭 > **3D Object > Plane**
2. 이름을 "Ground"로 변경
3. **Scale**: (10, 1, 10) - 게임 영역 크기 조정
4. **Material**: 원하는 지형 머티리얼 적용

### 2. 조명 설정
1. **Directional Light** 설정:
   - **Rotation**: (50, -30, 0) - 적절한 그림자와 조명
   - **Intensity**: 1.2
   - **Shadow Type**: Soft Shadows

### 3. 물리 설정
1. **Edit > Project Settings > Physics**
2. **Default Material**: 적절한 물리 머티리얼 설정
3. **Gravity**: (0, -9.81, 0)

## 네트워크 설정
1. PhotonManager가 씬에 있는지 확인합니다
2. PhotonView ID가 올바르게 설정되어 있는지 확인합니다
3. 프리팹들이 Photon Prefab List에 등록되어 있는지 확인합니다

## 게임 플레이
1. 플레이어들이 방에 입장합니다
2. 모든 플레이어가 씬에 로드되면 자동으로 게임이 시작됩니다
3. 플레이어들이 물건을 수집하며 경쟁합니다
4. 60초 후 게임이 종료되고 랭킹이 결정됩니다
5. 결과가 JTW 스코어 씬으로 전달됩니다

## 게임 시작 시스템
- JTW의 JumpGameHandler와 동일한 패턴을 사용합니다
- 모든 플레이어가 씬에 입장하고 로드가 완료되면 자동으로 게임이 시작됩니다
- `Manager.game.isAllPlayerLoaded()` 메서드를 통해 모든 플레이어의 로드 상태를 확인합니다
- `OnPlayerPropertiesUpdate` 이벤트를 통해 플레이어 속성 변경을 감지합니다

## 3D 특화 기능
- **3D 이동**: WASD 키로 XZ 평면 이동, 마우스 클릭으로 지점 이동
- **3D 카메라**: 플레이어들을 추적하는 3인칭 카메라
- **3D 물리**: 실제 물리 기반 충돌 감지
- **3D 효과**: 파티클 시스템을 활용한 수집 효과

## 주의사항
- 모든 프리팹은 Photon Prefab List에 등록되어야 합니다
- 플레이어 오브젝트는 "Player" 태그를 가져야 합니다
- 아이템 오브젝트는 "Item" 태그를 가져야 합니다
- 네트워크 동기화를 위해 PhotonView가 올바르게 설정되어야 합니다
- 3D 환경에서는 물리 설정이 중요합니다

## 문제 해결
- 플레이어가 스폰되지 않는 경우: Photon Prefab List 확인
- 아이템이 수집되지 않는 경우: Collider 설정 확인
- UI가 표시되지 않는 경우: Canvas 설정 확인
- 네트워크 동기화 문제: PhotonView 설정 확인
- 3D 카메라 문제: 카메라 위치와 회전 설정 확인 