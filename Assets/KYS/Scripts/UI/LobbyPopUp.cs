using Firebase.Auth;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KYS
{
    public class LobbyPopUp : BaseUI
    {
        // 기존 Firebase 관련 UI
        private TMP_Text uiEmailText => GetUI<TMP_Text>("E-MailTextContent");
        private TMP_Text uiNameText => GetUI<TMP_Text>("NameTextContent");

        private TMP_Text stateText => GetUI<TMP_Text>("CurrentState");
        // Photon 로비 관련 UI
        private TMP_InputField roomNameField => GetUI<TMP_InputField>("RoomNameField");
        private Transform roomListContent => GetUI<Transform>("Content");
        private GameObject roomListItemPrefab;
        private Dictionary<string, GameObject> roomListItems = new Dictionary<string, GameObject>();
        private Dictionary<string, RoomInfo> currentRoomInfos = new Dictionary<string, RoomInfo>(); // 현재 방 정보 저장

        private new void Awake()
        {
            base.Awake();
            canCloseWithESC = false; // ESC로 닫을 수 없음
            
            // Resources 폴더에서 RoomListItemPrefab 로드
            Debug.Log("[LobbyPopUp] RoomListItemPrefab 로드 시도...");
            roomListItemPrefab = Resources.Load<GameObject>("UI/RoomListItemPrefab");
            if (roomListItemPrefab == null)
            {
                Debug.LogError("[LobbyPopUp] Resources/UI/RoomListItemPrefab을 찾을 수 없습니다.");
            }
            else
            {
                Debug.Log($"[LobbyPopUp] RoomListItemPrefab 로드 성공: {roomListItemPrefab.name}");
                
                // RoomListItem 컴포넌트 확인
                var roomListItemComponent = roomListItemPrefab.GetComponent<RoomListItem>();
                if (roomListItemComponent == null)
                {
                    Debug.LogError("[LobbyPopUp] RoomListItemPrefab에 RoomListItem 컴포넌트가 없습니다!");
                }
                else
                {
                    Debug.Log("[LobbyPopUp] RoomListItemPrefab에 RoomListItem 컴포넌트 확인됨");
                }
            }

            // Photon 이벤트 연결 (null 체크 추가)
            var createRoomButton = GetEvent("CreateRoomButton");
            if (createRoomButton != null)
            {
                createRoomButton.Click += OnCreateRoomClicked;
            }
            else
            {
                Debug.LogError("[LobbyPopUp] CreateRoomButton을 찾을 수 없습니다.");
            }
        }

        private void Start()
        {
            // Firebase 사용자 정보 초기화
            InitializePanel();

            // UI 요소가 준비된 후 이벤트 연결 (Awake에서 실패한 경우)
            ConnectEventsIfNeeded();
        }

        private ClientState lastNetworkState = ClientState.Disconnected;
        
        private void Update()
        {
            // 네트워크 상태가 변경된 경우에만 UI 업데이트
            ClientState currentState = PhotonNetwork.NetworkClientState;
            if (currentState != lastNetworkState)
            {
                stateText.text = $"Current State : {GetUserFriendlyStateName(currentState)}";
                lastNetworkState = currentState;
                
                // 상태 변경 로그 (디버깅용)
                Debug.Log($"[LobbyPopUp] 네트워크 상태 변경: {currentState}");
                
                // PeerCreated 상태에서 멈춘 경우 처리
                if (currentState == ClientState.PeerCreated)
                {
                    Debug.LogWarning("[LobbyPopUp] PeerCreated 상태 감지. 3초 후 재연결을 시도합니다.");
                    StartCoroutine(HandlePeerCreatedState());
                }
            }

            // 방 목록 상태 확인 (10초마다로 변경)
            if (Time.frameCount % 600 == 0) // 약 10초마다 (60fps 기준)
            {
                CheckAndRefreshRoomList();
            }
        }
        
        // 사용자 친화적인 상태 이름 반환
        private string GetUserFriendlyStateName(ClientState state)
        {
            switch (state)
            {
                case ClientState.Disconnected:
                    return "연결 해제됨";
                case ClientState.PeerCreated:
                    return "연결 초기화 중...";
                case ClientState.ConnectingToNameServer:
                    return "서버 연결 중...";
                case ClientState.ConnectedToNameServer:
                    return "서버 연결됨";
                case ClientState.ConnectingToMasterServer:
                    return "마스터 서버 연결 중...";
                case ClientState.ConnectedToMasterServer:
                    return "마스터 서버 연결됨";
                case ClientState.ConnectingToGameServer:
                    return "게임 서버 연결 중...";
                case ClientState.ConnectedToGameServer:
                    return "게임 서버 연결됨";
                case ClientState.Joining:
                    return "방 입장 중...";
                case ClientState.Joined:
                    return "방 입장됨";
                case ClientState.Leaving:
                    return "방 나가는 중...";
                case ClientState.DisconnectingFromGameServer:
                    return "게임 서버 연결 해제 중...";
                case ClientState.DisconnectingFromMasterServer:
                    return "마스터 서버 연결 해제 중...";
                case ClientState.DisconnectingFromNameServer:
                    return "서버 연결 해제 중...";
                case ClientState.Authenticating:
                    return "인증 중...";
                default:
                    return state.ToString();
            }
        }

        private void OnEnable()
        {
            // PhotonManager 이벤트 구독
            if (PhotonManager.Instance != null)
            {
                PhotonManager.Instance.OnJoinedLobbyEvent += OnJoinedLobby;
                PhotonManager.Instance.OnRoomListUpdateEvent += OnRoomListUpdate;
                PhotonManager.Instance.OnJoinedRoomEvent += OnJoinedRoom;
                PhotonManager.Instance.OnCreateRoomFailedEvent += OnCreateRoomFailed;
                PhotonManager.Instance.OnJoinRoomFailedEvent += OnJoinRoomFailed;

                // 닉네임 동기화
                PhotonManager.Instance.SyncNicknameWithFirebase();

                // Photon 연결 상태 확인 및 연결
                Debug.Log($"[LobbyPopUp] 현재 Photon 상태: {PhotonNetwork.NetworkClientState}, 연결됨: {PhotonNetwork.IsConnected}, 로비: {PhotonNetwork.InLobby}");
                
                // PeerCreated 상태에서 멈춘 경우 처리
                if (PhotonNetwork.NetworkClientState == ClientState.PeerCreated)
                {
                    Debug.LogWarning("[LobbyPopUp] PeerCreated 상태에서 멈춤. PhotonManager에서 재연결을 시도합니다.");
                    PhotonManager.Instance.ConnectToPhoton();
                    return;
                }
                
                if (!PhotonNetwork.IsConnected && PhotonNetwork.NetworkClientState == ClientState.Disconnected)
                {
                    PhotonManager.Instance.ConnectToPhoton();
                    Debug.Log("[LobbyPopUp] Photon 연결 시작");
                }
                else if (PhotonNetwork.IsConnected && !PhotonNetwork.InLobby)
                {
                    // 연결되어 있지만 로비에 없는 경우 로비 참가
                    Debug.Log("[LobbyPopUp] 이미 연결되어 있음. 로비 참가 시도");
                    PhotonNetwork.JoinLobby();
                }
                else
                {
                    Debug.Log("[LobbyPopUp] 이미 Photon에 연결되어 있고 로비에도 있음");
                }

                Debug.Log("[LobbyPopUp] PhotonManager 이벤트 구독 완료");
            }
            else
            {
                Debug.LogError("[LobbyPopUp] PhotonManager.Instance가 null입니다.");
            }
        }

        private void OnDisable()
        {
            // PhotonManager 이벤트 구독 해제
            if (PhotonManager.Instance != null)
            {
                PhotonManager.Instance.OnJoinedLobbyEvent -= OnJoinedLobby;
                PhotonManager.Instance.OnRoomListUpdateEvent -= OnRoomListUpdate;
                PhotonManager.Instance.OnJoinedRoomEvent -= OnJoinedRoom;
                PhotonManager.Instance.OnCreateRoomFailedEvent -= OnCreateRoomFailed;
                PhotonManager.Instance.OnJoinRoomFailedEvent -= OnJoinRoomFailed;
            }


        }

        private void InitializePanel()
        {

            // 패널이 활성화될 때 로그인 정보 업데이트
            LoginInfo();
        }


        private void ConnectEventsIfNeeded()
        {
            // CreateRoomButton 이벤트가 연결되지 않았다면 다시 시도
            var createRoomButton = GetEvent("CreateRoomButton");
            if (createRoomButton != null)
            {
                createRoomButton.Click -= OnCreateRoomClicked; // 중복 방지
                createRoomButton.Click += OnCreateRoomClicked;
            }

            var menuButton = GetEvent("MenuButton");
            if (menuButton != null)
            {
                menuButton.Click -= OnMenu;
                menuButton.Click += OnMenu;
            }

        }

        // Photon 부분 관련 메서드들
        private void OnCreateRoomClicked(PointerEventData eventData)
        {
            string roomName = roomNameField.text.Trim();
            if (string.IsNullOrEmpty(roomName))
            {
                ShowErrorMessage("방 이름을 입력해주세요.");
                return;
            }

            // Photon 네트워크 상태 확인
            if (!PhotonNetwork.IsConnected)
            {
                ShowErrorMessage("Photon에 연결되어 있지 않습니다. 잠시 기다려주세요.");
                Debug.Log("[LobbyPopUp] Photon 연결 대기 중...");
                return;
            }

            if (!PhotonNetwork.InLobby)
            {
                ShowErrorMessage("로비에 있지 않습니다. 잠시 기다려주세요.");
                Debug.Log("[LobbyPopUp] 로비 대기 중...");
                return;
            }

            Debug.Log($"[LobbyPopUp] 방 생성 요청: {roomName}");
            
            // 중복 클릭 방지를 위해 버튼 비활성화
            Button createRoomButton = GetUI<Button>("CreateRoomButton");
            if (createRoomButton != null)
            {
                createRoomButton.interactable = false;
                // 3초 후 버튼 다시 활성화
                StartCoroutine(ReenableButtonAfterDelay(createRoomButton, 3f));
            }
            else
            {
                Debug.LogWarning("[LobbyPopUp] CreateRoomButton을 찾을 수 없습니다.");
            }
            
            PhotonManager.Instance.CreateRoom(roomName);
            roomNameField.text = "";

            // 방 생성 요청 후 방 목록 새로고침
            StartCoroutine(RefreshRoomListAfterDelay(2f));
        }

                private void OnJoinedLobby()
        {
            Debug.Log("로비 UI 활성화");
            
            // Photon이 자동으로 방 목록을 업데이트하므로 별도 요청 불필요
            if (PhotonNetwork.InLobby)
            {
                Debug.Log("[LobbyPopUp] 로비 입장 완료 - 방 목록 초기화 및 자동 업데이트 대기");
                
                // 로비 입장 시 방 목록 초기화
                currentRoomInfos.Clear();
                Debug.Log("[LobbyPopUp] 기존 방 정보 초기화 완료");
            }
            else
            {
                Debug.LogError("[LobbyPopUp] 로비에 입장하지 못했습니다.");
            }
        }

        private void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            // 방 목록이 변경된 경우에만 로그 출력
            if (roomList == null)
            {
                Debug.LogError("[LobbyPopUp] PhotonManager에서 받은 roomList가 null입니다!");
                return;
            }

            // null 체크 추가
            if (roomListItemPrefab == null)
            {
                Debug.LogError("[LobbyPopUp] RoomListItemPrefab이 null입니다.");
                return;
            }

            if (roomListContent == null)
            {
                Debug.LogError("[LobbyPopUp] RoomListContent가 null입니다.");
                return;
            }

            // 방 목록 누적 업데이트 (Photon의 증분 업데이트 방식 대응)
            int addedRooms = 0;
            int removedRooms = 0;
            int updatedRooms = 0;
            
            // 새로운 방 목록으로 기존 방 정보 업데이트
            foreach (RoomInfo info in roomList)
            {
                if (info.RemovedFromList)
                {
                    // 방이 제거된 경우
                    if (currentRoomInfos.ContainsKey(info.Name))
                    {
                        currentRoomInfos.Remove(info.Name);
                        removedRooms++;
                    }
                }
                else
                {
                    // 방이 추가되거나 업데이트된 경우
                    bool isNewRoom = !currentRoomInfos.ContainsKey(info.Name);
                    currentRoomInfos[info.Name] = info;
                    
                    if (isNewRoom)
                    {
                        addedRooms++;
                    }
                    else
                    {
                        updatedRooms++;
                    }
                }
            }
            
            // 변경사항이 있을 때만 로그 출력
            if (addedRooms > 0 || removedRooms > 0 || updatedRooms > 0)
            {
                Debug.Log($"[LobbyPopUp] 방 목록 업데이트 - 추가: {addedRooms}개, 제거: {removedRooms}개, 업데이트: {updatedRooms}개, 총: {currentRoomInfos.Count}개");
            }

            // UI 업데이트 - 누적된 방 정보를 기반으로 전체 재생성
            // 기존 UI 아이템 정리
            foreach (var kvp in roomListItems)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            roomListItems.Clear();

            // 누적된 방 정보를 기반으로 UI 아이템 생성
            int visibleRoomCount = 0;
            
            foreach (var kvp in currentRoomInfos)
            {
                RoomInfo info = kvp.Value;

                // 방 아이템 생성
                GameObject roomListItem = Instantiate(roomListItemPrefab);
                if (roomListItem == null)
                {
                    Debug.LogError($"[LobbyPopUp] RoomListItem 인스턴스 생성 실패: {info.Name}");
                    continue;
                }

                roomListItem.transform.SetParent(roomListContent, false);
                
                // Vertical Layout Group에 맞게 RectTransform 설정
                RectTransform rectTransform = roomListItem.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.sizeDelta = new Vector2(0, 80f);
                    rectTransform.pivot = new Vector2(0.5f, 1f);
                }
                
                RoomListItem itemComponent = roomListItem.GetComponent<RoomListItem>();
                if (itemComponent != null)
                {
                    itemComponent.Init(info);
                    roomListItems.Add(info.Name, roomListItem);
                    visibleRoomCount++;
                }
                else
                {
                    Debug.LogError($"[LobbyPopUp] RoomListItem 컴포넌트를 찾을 수 없습니다: {info.Name}");
                    Destroy(roomListItem);
                }
            }

            // UI 레이아웃 강제 업데이트
            Canvas.ForceUpdateCanvases();
            if (roomListContent is RectTransform contentRectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);
            }
            
            // 변경사항이 있을 때만 디버그 정보 출력
            if (addedRooms > 0 || removedRooms > 0 || updatedRooms > 0)
            {
                DebugRoomListStatus();
            }
        }

        private void OnJoinedRoom()
        {
            Debug.Log("방에 입장했습니다. 방 UI로 전환합니다.");
            // 방 UI로 전환
            UIManager.Instance.ClosePopUp();
            UIManager.Instance.ShowPopUp<RoomPopUp>();
        }

        private void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[LobbyPopUp] 방 생성 실패 - 코드: {returnCode}, 메시지: {message}");
            
            // 방 생성 실패 시 사용자에게 에러 메시지 표시
            string errorMessage = GetErrorMessage(returnCode, message);
            ShowErrorMessage($"방 생성 실패: {errorMessage}");
            
            // 방 이름 필드 초기화
            if (roomNameField != null)
            {
                roomNameField.text = "";
            }
        }

        private void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[LobbyPopUp] 방 입장 실패 - 코드: {returnCode}, 메시지: {message}");
            
            // 방 입장 실패 시 사용자에게 에러 메시지 표시
            string errorMessage = GetErrorMessage(returnCode, message);
            ShowErrorMessage($"방 입장 실패: {errorMessage}");
        }

        // Photon 에러 코드를 사용자 친화적인 메시지로 변환
        private string GetErrorMessage(short returnCode, string message)
        {
            switch (returnCode)
            {
                case 32767: // GameFull
                    return "방이 가득 찼습니다.";
                case 32766: // GameClosed
                    return "방이 닫혀있습니다.";
                case 32765: // GameDoesNotExist
                    return "방이 존재하지 않습니다.";
                case 32764: // MaxCcuReached
                    return "서버가 가득 찼습니다. 잠시 후 다시 시도해주세요.";
                case 32763: // InvalidRegion
                    return "잘못된 지역입니다.";
                case 32762: // CustomAuthenticationFailed
                    return "인증에 실패했습니다.";
                default:
                    return string.IsNullOrEmpty(message) ? "알 수 없는 오류가 발생했습니다." : message;
            }
        }


        public void LoginInfo()
        {
            // FirebaseManager 사용
            FirebaseUser user = FirebaseManager.Auth.CurrentUser;

            if (user != null)
            {
                uiEmailText.text = user.Email ?? "이메일 없음";
                uiNameText.text = user.DisplayName ?? "닉네임 없음";
                //UIuserIdText.text = user.UserId;
            }
            else
            {
                // Firebase 사용자가 없는 경우 (로그아웃 상태)
                uiEmailText.text = "로그인 필요";
                uiNameText.text = "로그인 필요";
                Debug.LogWarning("[LobbyPopUp] Firebase 사용자 정보가 없습니다. 로그인이 필요합니다.");
            }
        }



        // 에러 메시지 표시
        private void ShowErrorMessage(string message)
        {
            MessagePopUp messagePopUp = UIManager.Instance.ShowPopUp<MessagePopUp>();
            if (messagePopUp != null)
            {
                messagePopUp.SetMessage(message, "확인");
            }
        }


        private void OnMenu(PointerEventData eventData)
        {
            UIManager.Instance.ShowPopUp<MenuPopUp>();
        }

        // 방 목록 새로고침을 위한 코루틴
        private System.Collections.IEnumerator RefreshRoomListAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (PhotonNetwork.InLobby)
            {
                //PhotonNetwork.GetCustomRoomList(TypedLobby.Default, "Map");
                Debug.Log("[LobbyPopUp] 방 생성 후 방 목록 새로고침 완료");
            }
        }

        // 버튼을 다시 활성화하는 코루틴
        private System.Collections.IEnumerator ReenableButtonAfterDelay(Button createRoomButton, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (createRoomButton != null)
            {
                createRoomButton.interactable = true;
                Debug.Log("[LobbyPopUp] 방 생성 버튼 다시 활성화");
            }
            else
            {
                Debug.LogWarning("[LobbyPopUp] Button이 null입니다.");
            }
        }

        // 디버그용 - 현재 방 목록 상태 출력
        public void DebugRoomListStatus()
        {
            Debug.Log($"[LobbyPopUp] 방 목록 상태 - 총 {roomListItems.Count}개 방");
            
            // 방이 많을 때는 요약만 출력
            if (roomListItems.Count > 5)
            {
                Debug.Log($"[LobbyPopUp] 방 목록 요약: {roomListItems.Count}개 방이 표시됨");
            }
            else
            {
                foreach (var kvp in roomListItems)
                {
                    Debug.Log($"[LobbyPopUp] - 방: {kvp.Key}");
                }
            }
        }

        // 수동으로 방 목록 새로고침하는 메서드 (디버깅용)
        public void RefreshRoomList()
        {
            if (PhotonNetwork.InLobby)
            {
                Debug.Log("[LobbyPopUp] 수동 방 목록 새로고침 요청");
                
                // 현재 Photon 상태 확인
                Debug.Log($"[LobbyPopUp] Photon 상태 - 연결됨: {PhotonNetwork.IsConnected}, 로비: {PhotonNetwork.InLobby}, 방: {PhotonNetwork.InRoom}");
                
                // 수동으로 방 목록 요청 (디버깅용)
                if (PhotonNetwork.IsConnected && PhotonNetwork.Server == ServerConnection.MasterServer)
                {
                    Debug.Log("[LobbyPopUp] Photon 서버에 방 목록 재요청");
                    // Photon이 자동으로 방 목록을 업데이트하므로 별도 요청 불필요
                }
            }
            else
            {
                Debug.LogError("[LobbyPopUp] 로비에 있지 않아 방 목록을 새로고침할 수 없습니다.");
            }
        }

        // 방 목록이 비어있을 때 자동으로 새로고침하는 메서드
        public void CheckAndRefreshRoomList()
        {
            if (roomListItems.Count == 0 && PhotonNetwork.InLobby)
            {
                // 로그 제거 - 너무 자주 호출됨
                StartCoroutine(AutoRefreshRoomList());
            }
        }

        // 자동 방 목록 새로고침 코루틴
        private System.Collections.IEnumerator AutoRefreshRoomList()
        {
            yield return new WaitForSeconds(1f);
            
            if (PhotonNetwork.InLobby && roomListItems.Count == 0)
            {
                // Photon 연결 상태 확인
                if (PhotonNetwork.IsConnected && PhotonNetwork.Server == ServerConnection.MasterServer)
                {
                    // 로비 재참가 시도
                    PhotonNetwork.JoinLobby();
                }
                else
                {
                    Debug.LogError("[LobbyPopUp] Photon 연결 상태가 이상합니다.");
                }
            }
        }
        
        // PeerCreated 상태 처리 코루틴
        private System.Collections.IEnumerator HandlePeerCreatedState()
        {
            yield return new WaitForSeconds(3f);
            
            // 3초 후에도 여전히 PeerCreated 상태인지 확인
            if (PhotonNetwork.NetworkClientState == ClientState.PeerCreated)
            {
                Debug.LogWarning("[LobbyPopUp] PeerCreated 상태가 지속됨. 강제로 연결을 해제하고 재연결을 시도합니다.");
                
                // 강제로 연결 해제
                PhotonNetwork.Disconnect();
                
                // 잠시 대기 후 재연결
                yield return new WaitForSeconds(1f);
                
                if (PhotonManager.Instance != null)
                {
                    PhotonManager.Instance.ConnectToPhoton();
                }
            }
        }
    }
}
