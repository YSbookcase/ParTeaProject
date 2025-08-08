# KYS 매니저 초기화 설정 가이드

## 개요
UIManager, PhotonManager, FirebaseManager를 첫 화면에서 자동으로 생성하는 설정 방법입니다.

## 자동 생성 원리

### Singleton 클래스의 Resources 자동 로드
- `Singleton<T>` 클래스가 수정되어 Resources 폴더에서 자동으로 프리팹을 로드합니다
- `UIManager.Instance`, `PhotonManager.Instance`, `FirebaseManager.Instance`를 호출하면 자동으로 생성됩니다

### 동작 순서
1. `Instance` 프로퍼티 호출
2. 씬에서 기존 인스턴스 검색
3. 없으면 Resources 폴더에서 `{클래스명}.prefab` 로드 시도
4. 프리팹이 있으면 프리팹에서 생성, 없으면 빈 GameObject에 컴포넌트 추가
5. `DontDestroyOnLoad` 설정

## 설정 방법

### 방법 1: Resources 폴더에 프리팹 배치 (권장)

1. **프리팹 이름 규칙**
   - `UIManager.prefab`
   - `PhotonManager.prefab`
   - `FirebaseManager.prefab`
   - **중요**: 프리팹 이름이 클래스 이름과 정확히 일치해야 함

2. **프리팹 위치**
   - `Assets/KYS/Resources/` 폴더에 배치
   - 각 프리팹에 해당 스크립트 컴포넌트가 붙어있어야 함

3. **사용법**
   ```csharp
   // 어디서든 호출하면 자동으로 생성됨
   UIManager.Instance;
   PhotonManager.Instance;
   FirebaseManager.Instance;
   ```

### 방법 2: 씬에 직접 프리팹 배치

1. **첫 화면 씬에 배치**
   - Resources 폴더의 프리팹들을 첫 화면 씬에 직접 배치
   - Singleton 패턴으로 인해 씬 전환 시에도 유지됨

## 동작 원리

### Singleton 패턴
- 모든 매니저는 `Singleton<T>` 클래스를 상속
- `DontDestroyOnLoad`로 씬 전환 시에도 유지
- 중복 생성 방지

### 초기화 순서
1. `UIManager.Instance` 호출 시 자동 생성
2. `UIManager`의 `Start()`에서 첫 화면 표시
3. 다른 매니저들도 필요할 때 자동 생성

## 디버그 로그

초기화 과정에서 다음과 같은 로그가 출력됩니다:
```
[Singleton] UIManager의 인스턴스가 Resources 프리팹에서 생성되었습니다.
[Singleton] PhotonManager의 인스턴스가 Resources 프리팹에서 생성되었습니다.
[Singleton] FirebaseManager의 인스턴스가 Resources 프리팹에서 생성되었습니다.
[UIManager] 첫 화면 LoginPopUp 표시 완료
```

## 문제 해결

### 매니저가 생성되지 않는 경우
1. Resources 폴더에 프리팹이 있는지 확인
2. 프리팹 이름이 클래스 이름과 정확히 일치하는지 확인
3. 프리팹에 해당 스크립트가 붙어있는지 확인

### 중복 생성 문제
- Singleton 패턴으로 자동 해결됨
- 이미 존재하는 경우 새로 생성하지 않음

### 씬 전환 시 문제
- `DontDestroyOnLoad`로 해결됨
- 매니저들이 씬 전환 시에도 유지됨

## 사용 예시

```csharp
// UIManager 사용
UIManager.Instance.ShowPopUp<LoginPopUp>();

// PhotonManager 사용
PhotonManager.Instance.CreateRoom("테스트방");

// FirebaseManager 사용
if (FirebaseManager.Instance.IsInitialized)
{
    // Firebase 사용 가능
}
``` 