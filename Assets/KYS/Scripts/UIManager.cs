using System.Collections.Generic;
using System.Collections; // 추가 필요
using UnityEngine;
using System.Reflection;
using UnityEngine.SceneManagement; // 씬 관리 추가
using Photon.Pun; // Photon 네트워킹 추가

namespace KYS
{
    public class UIManager : Singleton<UIManager>
    {
        [SerializeField] string popUpPath = "UI/Canvas_PopUp"; // 경로 수정
        [SerializeField] string prefabPath = "UI"; // 경로 수정
        private PopUpUI popUp;

        // 메인 UI 패널들 관리
        private Dictionary<string, GameObject> mainPanels = new Dictionary<string, GameObject>();

        public PopUpUI PopUp
        {
            get
            {
                if (popUp == null)
                {
                    popUp = FindObjectOfType<PopUpUI>();
                    if (popUp != null) return popUp;

                    GameObject prefab = Resources.Load<GameObject>(popUpPath);
                    if (prefab == null)
                    {
                        Debug.LogWarning($"[UIManager] 해당 경로에 팝업 프리팹이 없음: {popUpPath}");
                        return null;
                    }

                    GameObject go = Instantiate(prefab);
                    popUp = go.GetComponent<PopUpUI>();
                    if (popUp == null)
                    {
                        Debug.LogError($"[UIManager] Canvas_PopUp 프리팹에 PopUpUI 컴포넌트가 없음");
                        Destroy(go);
                        return null;
                    }

                    DontDestroyOnLoad(go);
                }
                DontDestroyOnLoad(popUp);
                return popUp;
            }
        }

        // 메인 UI 패널 등록
        public void RegisterMainPanel(string panelName, GameObject panel)
        {
            if (string.IsNullOrEmpty(panelName) || panel == null)
            {
                Debug.LogError("[UIManager] 패널 이름이나 GameObject가 null입니다.");
                return;
            }

            if (mainPanels.ContainsKey(panelName))
            {
                Debug.LogWarning($"[UIManager] 이미 등록된 메인 패널: {panelName}");
                mainPanels[panelName] = panel; // 기존 패널 교체
            }
            else
            {
                mainPanels.Add(panelName, panel);
                //Debug.Log($"[UIManager] 메인 패널 등록 완료: {panelName}");
            }
        }

        // 메인 UI 패널 해제
        public void UnregisterMainPanel(string panelName)
        {
            if (mainPanels.Remove(panelName))
            {
                Debug.Log($"[UIManager] 메인 패널 해제: {panelName}");
            }
        }

        // 메인 UI 패널 가져오기
        public GameObject GetMainPanel(string panelName)
        {
            if (string.IsNullOrEmpty(panelName))
            {
                Debug.LogError("[UIManager] 패널 이름이 null입니다.");
                return null;
            }

            if (mainPanels.TryGetValue(panelName, out GameObject panel))
            {
                return panel;
            }
            Debug.LogWarning($"[UIManager] 등록되지 않은 메인 패널: {panelName}");
            return null;
        }

        // 모든 메인 패널 가져오기
        public Dictionary<string, GameObject> GetAllMainPanels()
        {
            return new Dictionary<string, GameObject>(mainPanels);
        }

        // 팝업 UI 인덱스
        public static int selectIndexUI { get; set; } = 0;
        public static bool canClosePopUp = true;
        bool canClose => PopUpUI.IsPopUpActive && !Util.escPressed && canClosePopUp && !IsCurrentPopUpNonClosable();

        private void Awake()
        {
            // 씬 전환 이벤트 리스너 등록
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // 초기화 플래그 설정
            isInitialized = true;
        }

        private void OnDestroy()
        {
            // 씬 전환 이벤트 리스너 해제
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // 씬 전환 시 자동으로 모든 UI 정리
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //Debug.Log($"[UIManager] 씬 전환 감지: {scene.name}");
            
            // 게임 씬으로 전환되는 경우 로딩 블로커 숨기기
            if (scene.name.Contains("Game") || scene.name.Contains("Arena") || 
                scene.name.Contains("Jump") || scene.name.Contains("Racing") ||
                scene.name.Contains("Tile") || scene.name.Contains("Rope") ||
                scene.name.Contains("Receive"))
            {
                Debug.Log("[UIManager] 게임 씬으로 전환 - 로딩 블로커 숨김");
                PopUp.HideLoadingBlocker();
            }
            
            // NetworkScene으로 돌아올 때 RoomPopUp 표시 (게임 종료 후)
            if (scene.name == "NetworkScene" && PhotonNetwork.InRoom)
            {
                Debug.Log("[UIManager] NetworkScene으로 돌아옴 - RoomPopUp 표시 및 초기화");
                // 약간의 지연을 두어 씬 로딩 완료 후 UI 표시
                StartCoroutine(ShowRoomPopUpAfterDelay());
            }
        }

        private System.Collections.IEnumerator ShowRoomPopUpAfterDelay()
        {
            Debug.Log("[UIManager] ShowRoomPopUpAfterDelay 코루틴 시작");
            yield return new WaitForSeconds(0.5f);
            
            // 이미 RoomPopUp이 표시되어 있는지 확인
            RoomPopUp existingRoomPopUp = FindActivePopUp<RoomPopUp>();
            if (existingRoomPopUp == null)
            {
                Debug.Log("[UIManager] 새로운 RoomPopUp 생성");
                ShowPopUp<RoomPopUp>();
                Debug.Log("[UIManager] RoomPopUp 표시 완료");
            }
            else
            {
                // 기존 RoomPopUp이 있다면 초기화
                Debug.Log("[UIManager] 기존 RoomPopUp 초기화");
                existingRoomPopUp.InitializeRoomAfterGame();
                Debug.Log("[UIManager] 기존 RoomPopUp 초기화 완료");
            }
        }

        private void Start()
        {
            // Start에서 첫 화면 설정 (더 안전)
            if (isInitialized && SceneManager.GetActiveScene().name =="NetworkScene")
            {
                ShowFirstScreen();
            }
        }

        private void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && canClose)
            {
                // 현재 활성화된 팝업이 ESC로 닫을 수 없는지 확인
                if (IsCurrentPopUpNonClosable())
                {
                    Debug.Log("[UIManager] 이 팝업은 ESC로 닫을 수 없습니다.");
                    return;
                }

                ClosePopUp();
                Util.ConsumeESC();
            }
            Util.ResetESC();
        }

        // 팝업 UI를 띄운다
        public T ShowPopUp<T>() where T : BaseUI
        {
            string path = $"{prefabPath}/{typeof(T).Name}";
            //Debug.Log($"[UIManager] ShowPopUp 호출: {typeof(T).Name}, 경로: {path}");
            
            T prefab = Resources.Load<T>(path);
            if (prefab == null)
            {
                Debug.LogError($"[UIManager] 해당 경로에 팝업 프리팹이 없음: {path}");
                return null;
            }
            //Debug.Log($"[UIManager] 프리팹 로딩 성공: {prefab.name}");

            if (PopUp == null)
            {
                Debug.LogError("[UIManager] PopUp이 null입니다.");
                return null;
            }
            //Debug.Log($"[UIManager] PopUp 확인됨: {PopUp.name}");

            T instance = Instantiate(prefab, PopUp.transform);
            if (instance == null)
            {
                Debug.LogError("[UIManager] 인스턴스 생성 실패");
                return null;
            }
            //Debug.Log($"[UIManager] 인스턴스 생성 성공: {instance.name}");
            
            // PushUIStack 호출 전 상태 확인
            //Debug.Log($"[UIManager] PushUIStack 호출 전 - PopUp 스택 개수: {PopUp.StackCount()}");
            
            PopUp.PushUIStack(instance);
            
            // 생성된 팝업의 상태 확인
            //Debug.Log($"[UIManager] {typeof(T).Name} 팝업 생성 및 표시 완료");
            //Debug.Log($"[UIManager] 팝업 활성화 상태: {instance.gameObject.activeInHierarchy}");
            //Debug.Log($"[UIManager] 팝업 부모: {instance.transform.parent?.name}");
            //Debug.Log($"[UIManager] 팝업 위치: {instance.transform.position}");
            //Debug.Log($"[UIManager] PushUIStack 호출 후 - PopUp 스택 개수: {PopUp.StackCount()}");
            
            return instance;
        }

        public void ClosePopUp()
        {
            if (PopUp != null)
            {
                PopUp.PopUIStack();
            }
        }

        public void CleanPopUp()
        {
            //Debug.Log($"[UIManager] CleanPopUp 시작 - 현재 팝업 개수: {PopUp?.StackCount() ?? 0}");
            
            if (PopUp != null)
            {
                while (PopUp.StackCount() > 0)
                {
                    Debug.Log($"[UIManager] 팝업 제거 중 - 남은 개수: {PopUp.StackCount()}");
                    PopUp.PopUIStack();
                }
            }
            
            //Debug.Log("[UIManager] CleanPopUp 완료");
        }

        // 모든 UI 정리
        public void CleanAllUI()
        {
            if (PopUp != null)
            {
                PopUp.ForceCleanAll(); // 강제 정리 사용
            }
            mainPanels.Clear();
            Debug.Log("[UIManager] 모든 UI가 정리되었습니다.");
        }


        // 확인 팝업을 띄우는 편의 메서드
        public CheckPopUp ShowConfirmPopUp(string message, string confirmText = "확인", string cancelText = "취소",
                                          System.Action confirmCallback = null, System.Action cancelCallback = null)
        {
            CheckPopUp checkPopUp = ShowPopUp<CheckPopUp>();
            if (checkPopUp != null)
            {
                checkPopUp.SetMessage(message, confirmText, cancelText, confirmCallback, cancelCallback);
            }
            return checkPopUp;
        }

        // 간단한 확인 팝업 (확인 시에만 콜백)
        public CheckPopUp ShowConfirmPopUp(string message, System.Action confirmCallback)
        {
            return ShowConfirmPopUp(message, "확인", "취소", confirmCallback, null);
        }

        // 로그아웃 처리 메서드
        public void PerformLogout()
        {
            Debug.Log("[UIManager] 로그아웃 처리 시작");

            // Firebase 로그아웃
            FirebaseManager.Auth.SignOut();

            // Photon 연결 해제
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
                Debug.Log("[UIManager] Photon 연결 해제 완료");
            }

            // 팝업만 정리 (mainPanels는 유지)
            CleanPopUp();
            
            // LoginPopUp 표시 (2번 호출로 해결)
            ShowPopUp<LoginPopUp>();
            ShowPopUp<LoginPopUp>();

            Debug.Log("[UIManager] 로그아웃 처리 완료");
        }

        // 특정 타입의 팝업 찾기
        public T FindActivePopUp<T>() where T : BaseUI
        {
            if (PopUp == null) return null;

            return PopUp.GetComponentInChildren<T>();
        }

        // 특정 팝업 업데이트
        public void UpdatePopUp<T>() where T : BaseUI
        {
            T popUp = FindActivePopUp<T>();
            if (popUp != null)
            {
                // LoginInfo 메서드가 있는 경우 호출
                var loginInfoMethod = typeof(T).GetMethod("LoginInfo");
                if (loginInfoMethod != null)
                {
                    loginInfoMethod.Invoke(popUp, null);
                }
            }
        }

        private bool isInitialized = false;

        private IEnumerator InitializeFirstScreen() // IEnumerator로 수정
        {
            // 모든 초기화가 완료될 때까지 대기
            yield return new WaitForEndOfFrame();
            
            // 첫 화면으로 LoginPopUp 표시
            ShowFirstScreen();
        }

        private void ShowFirstScreen()
        {
            // 이미 LoginPopUp이 표시되어 있다면 중복 방지
            if (FindActivePopUp<LoginPopUp>() != null)
            {
                return;
            }

            CleanPopUp();
            ShowPopUp<LoginPopUp>();
            
            Debug.Log("[UIManager] 첫 화면 LoginPopUp 표시 완료");
        }

        // 게임 시작 시 호출할 메서드
        public void StartGame()
        {
            ShowFirstScreen();
        }

        // 현재 활성화된 팝업이 ESC로 닫을 수 없는지 확인
        private bool IsCurrentPopUpNonClosable()
        {
            if (PopUp == null || PopUp.StackCount() == 0)
                return false;

            // 스택의 최상단 팝업 확인
            BaseUI topPopUp = PopUp.GetComponentInChildren<BaseUI>();
            if (topPopUp == null)
                return false;

            // ESC로 닫을 수 없는 팝업인지 확인
            return !topPopUp.CanCloseWithESC;
        }

        // 현재 활성화된 팝업이 ESC로 닫을 수 있는지 확인 (추가)
        private bool IsCurrentPopUpClosable()
        {
            if (PopUp == null || PopUp.StackCount() == 0)
                return true; // 팝업이 없으면 ESC 가능

            // 스택의 최상단 팝업 확인
            BaseUI topPopUp = PopUp.GetComponentInChildren<BaseUI>();
            if (topPopUp == null)
                return true;

            // ESC로 닫을 수 있는 팝업인지 확인
            return topPopUp.CanCloseWithESC;
        }
    }
}
