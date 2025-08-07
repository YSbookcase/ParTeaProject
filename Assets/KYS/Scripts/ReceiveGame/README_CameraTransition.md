# 카메라 전환 시스템 사용법

시네머신을 사용한 첫 화면에서 메인 게임 위치로의 카메라 이동 연출 시스템입니다.

## 🎯 개요

이 시스템은 게임 시작 시 첫 화면의 높은 위치에서 메인 게임 플레이 위치로 부드럽게 카메라가 이동하는 연출을 제공합니다.

## 🛠️ 구현 방법

### 방법 1: CameraTransitionManager 사용 (추천)

가장 전문적이고 유연한 방법입니다.

#### 설정 단계:

1. **CameraTransitionManager 스크립트 추가**
   - 빈 GameObject를 생성하고 `CameraTransitionManager` 스크립트를 추가
   - 이름을 "CameraTransitionManager"로 설정

2. **Cinemachine Virtual Camera 설정**
   - 첫 화면용 Virtual Camera 생성 (Intro Camera)
   - 게임용 Virtual Camera 생성 (Game Camera)
   - 각각의 위치와 회전 설정

3. **ReceiveGameManagerEnhanced 연결**
   - `ReceiveGameManagerEnhanced`의 `Camera Transition` 섹션에서:
     - `Camera Transition Manager`에 생성한 매니저 할당
     - `Enable Camera Transition` 체크

#### Inspector 설정:

```
Camera Transition Manager:
├── Intro Camera: 첫 화면용 Virtual Camera
├── Game Camera: 게임용 Virtual Camera
├── Transition Duration: 3.0 (전환 시간)
├── Transition Curve: EaseInOut (전환 커브)
├── Use Manual Transition: true (수동 전환 사용)
├── Intro Position: (0, 50, -30) (첫 화면 위치)
├── Intro Rotation: (60, 0, 0) (첫 화면 회전)
├── Game Position: (0, 20, -8) (게임 위치)
└── Game Rotation: (30, 0, 0) (게임 회전)
```

### 방법 2: ReceiveGameCamera 개선 버전 사용

기존 `ReceiveGameCamera`에 전환 기능을 추가한 방법입니다.

#### 설정 단계:

1. **ReceiveGameCamera 설정**
   - 기존 `ReceiveGameCamera` 컴포넌트에서:
     - `Enable Transition` 체크
     - `Intro Position`과 `Game Position` 설정
     - `Transition Duration` 조정

2. **ReceiveGameManagerEnhanced 연결**
   - `Enable Camera Transition` 체크
   - `Camera Transition Manager`는 비워둠

### 방법 3: Timeline 사용 (고급)

더 복잡한 카메라 움직임이 필요한 경우 Timeline을 사용할 수 있습니다.

#### 설정 단계:

1. **Timeline 생성**
   - Window > Sequencing > Timeline
   - 새 Timeline 에셋 생성

2. **Cinemachine Track 추가**
   - Timeline에 Cinemachine Track 추가
   - 여러 Virtual Camera를 시퀀스로 배치

3. **CameraTransitionManager 연결**
   - `Timeline Director`에 PlayableDirector 할당
   - `Camera Timeline`에 생성한 Timeline 할당

## 🎮 사용법

### 자동 전환
게임이 시작되면 자동으로 카메라 전환이 시작됩니다.

### 수동 전환
```csharp
// CameraTransitionManager 사용 시
cameraTransitionManager.StartCameraTransition();

// ReceiveGameCamera 사용 시
receiveGameCamera.StartCameraTransition();
```

### 전환 상태 확인
```csharp
bool isComplete = cameraTransitionManager.IsTransitionComplete();
```

## ⚙️ 커스터마이징

### 전환 커브 조정
- `AnimationCurve`를 사용하여 전환 속도 조정
- EaseInOut, EaseIn, EaseOut 등 다양한 커브 사용 가능

### 전환 시간 조정
- `Transition Duration` 값을 조정하여 전환 속도 변경
- 일반적으로 2-5초가 적당

### 위치 및 회전 조정
- `Intro Position/Rotation`: 첫 화면에서의 카메라 위치
- `Game Position/Rotation`: 게임 플레이 시 카메라 위치

## 🔧 문제 해결

### 카메라가 전환되지 않는 경우
1. `Enable Camera Transition`이 체크되어 있는지 확인
2. Virtual Camera의 Priority 설정 확인
3. Cinemachine Brain이 씬에 있는지 확인

### 전환이 부자연스러운 경우
1. 전환 커브 조정
2. 전환 시간 늘리기
3. 시작/끝 위치 간격 조정

### 성능 최적화
1. 불필요한 Virtual Camera 비활성화
2. 전환 중 다른 무거운 작업 피하기
3. 모바일에서는 전환 시간을 짧게 설정

## 📝 예시 설정

### 기본 설정 (추천)
```
Intro Position: (0, 50, -30)
Intro Rotation: (60, 0, 0)
Game Position: (0, 20, -8)
Game Rotation: (30, 0, 0)
Transition Duration: 3.0
Transition Curve: EaseInOut
```

### 빠른 전환
```
Transition Duration: 1.5
Transition Curve: EaseOut
```

### 느린 전환
```
Transition Duration: 5.0
Transition Curve: EaseInOut
```

## 🎨 연출 아이디어

1. **회전 전환**: 카메라가 회전하면서 이동
2. **줌 전환**: Field of View를 조정하며 전환
3. **경로 전환**: 곡선 경로를 따라 이동
4. **단계별 전환**: 여러 단계로 나누어 전환

이 시스템을 통해 게임 시작 시 더욱 몰입감 있는 연출을 만들 수 있습니다!
