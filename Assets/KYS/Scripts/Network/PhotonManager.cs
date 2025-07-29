using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace KYS
{
    public class PhotonManager : SingtonPunCallback<PhotonManager>
    {
        // 상속받은 Instance 속성을 사용하므로 중복 정의 제거
        // public static PhotonManager Instance { get; private set; }

        // 이벤트들 (UI와 분리하기 위해)
        public event Action OnConnectedToMasterEvent;
        public event Action OnJoinedLobbyEvent;
        public event Action OnJoinedRoomEvent;
        public event Action OnLeftRoomEvent;
        public event Action<Player> OnPlayerEnteredRoomEvent;
        public event Action<Player> OnPlayerLeftRoomEvent;
        public event Action<List<RoomInfo>> OnRoomListUpdateEvent;
        public event Action<Player, Hashtable> OnPlayerPropertiesUpdateEvent; // 플레이어 속성 업데이트 이벤트 추가
        public event Action<Player> OnMasterClientSwitchedEvent; // 마스터 클라이언트 변경 이벤트 추가
        public event Action<short, string> OnCreateRoomFailedEvent; // 방 생성 실패 이벤트 추가
        public event Action<short, string> OnJoinRoomFailedEvent; // 방 입장 실패 이벤트 추가

        private PhotonView _photonView;

        private void Awake()
        {
            // 상속받은 싱글톤 패턴을 사용하므로 별도 구현 불필요
            // PhotonView 설정
            _photonView = GetComponent<PhotonView>();
            if (_photonView == null)
            {
                _photonView = gameObject.AddComponent<PhotonView>();
            }
        }

        private void Start()
        {
            // Photon 연결은 로그인 후 로비에서 시작하도록 변경
            // PhotonNetwork.ConnectUsingSettings(); // 자동 연결 제거
        }

        // Firebase 닉네임과 동기화
        public void SyncNicknameWithFirebase()
        {
            var user = FirebaseManager.Auth.CurrentUser;
            if (user != null && !string.IsNullOrEmpty(user.DisplayName))
            {
                PhotonNetwork.NickName = user.DisplayName;
                Debug.Log($"Photon 닉네임 동기화: {user.DisplayName}");
            }
            else
            {
                // Firebase 사용자가 없거나 닉네임이 없는 경우 기본값 설정
                PhotonNetwork.NickName = "Guest";
                Debug.Log("Firebase 사용자 정보가 없어 기본 닉네임으로 설정: Guest");
            }
        }

        // 공개 메서드들 (UI에서 호출)
        public void CreateRoom(string roomName)
        {
            // 방 이름 검증
            if (string.IsNullOrEmpty(roomName) || string.IsNullOrWhiteSpace(roomName))
            {
                Debug.LogError("[PhotonManager] 방 이름이 비어있습니다.");
                OnCreateRoomFailedEvent?.Invoke(0, "방 이름을 입력해주세요.");
                return;
            }

            // 방 이름 길이 제한
            if (roomName.Length > 20)
            {
                Debug.LogError("[PhotonManager] 방 이름이 너무 깁니다. (최대 20자)");
                OnCreateRoomFailedEvent?.Invoke(0, "방 이름은 최대 20자까지 가능합니다.");
                return;
            }

            // Photon 연결 상태 확인
            if (!PhotonNetwork.IsConnected)
            {
                Debug.Log("[PhotonManager] Photon에 연결되어 있지 않습니다. 연결을 시도합니다.");
                ConnectToPhoton();
                return;
            }

            if (!PhotonNetwork.InLobby)
            {
                Debug.Log("[PhotonManager] 로비에 있지 않습니다. 로비 참가를 시도합니다.");
                if (PhotonNetwork.IsConnected && PhotonNetwork.Server == ServerConnection.MasterServer)
                {
                    PhotonNetwork.JoinLobby();
                }
                else
                {
                    Debug.LogError("[PhotonManager] 서버에 연결되어 있지 않습니다. 연결을 시도해주세요.");
                    OnCreateRoomFailedEvent?.Invoke(0, "서버에 연결되어 있지 않습니다.");
                    return;
                }
                return;
            }

            Debug.Log($"[PhotonManager] 방 생성 요청: {roomName}");
            
            // 방 옵션 설정 - 방이 보이고 열려있도록 설정
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = 4,
                IsVisible = true,
                IsOpen = true,
                PublishUserId = true
            };
            
            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }

        public void JoinRoom(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                Debug.LogError("방 이름이 비어있습니다.");
                return;
            }

            // Photon 네트워크 상태 확인
            if (!PhotonNetwork.IsConnected)
            {
                Debug.Log("[PhotonManager] Photon에 연결되지 않음. 연결을 시작합니다.");
                ConnectToPhoton();
                return;
            }

            if (!PhotonNetwork.InLobby)
            {
                Debug.Log("[PhotonManager] 로비에 있지 않음. 로비 참가를 시도합니다.");
                if (PhotonNetwork.IsConnected && PhotonNetwork.Server == ServerConnection.MasterServer)
                {
                    PhotonNetwork.JoinLobby();
                }
                else
                {
                    Debug.LogError("[PhotonManager] 마스터 서버에 연결되지 않음. 연결을 기다려주세요.");
                    return;
                }
                return;
            }

            Debug.Log($"[PhotonManager] 방 참가 시도: {roomName}");
            PhotonNetwork.JoinRoom(roomName);
        }

        public void LeaveRoom()
        {
            PhotonNetwork.LeaveRoom();
        }

        // 수동으로 Photon 연결 시작
        public void ConnectToPhoton()
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.Log("[PhotonManager] Photon 연결 시작");
                PhotonNetwork.ConnectUsingSettings();
            }
            else
            {
                Debug.Log("[PhotonManager] 이미 Photon에 연결되어 있습니다.");
            }
        }

        // MonoBehaviourPunCallbacks 오버라이드
        public override void OnConnected()
        {
            Debug.Log("Photon 서버에 연결됨");
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("마스터 서버 연결 완료");
            SyncNicknameWithFirebase(); // Firebase 닉네임 동기화
            OnConnectedToMasterEvent?.Invoke();
            
            // 마스터 연결 후 자동으로 로비 참가
            PhotonNetwork.JoinLobby();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.Log($"[PhotonManager] Photon 연결 해제: {cause}");
            
            // 연결 해제 시 닉네임 초기화
            PhotonNetwork.NickName = "";
            
            // 연결 끊김 원인에 따른 처리
            switch (cause)
            {
                case DisconnectCause.DisconnectByClientLogic:
                    Debug.Log("[PhotonManager] 사용자 로그아웃으로 인한 연결 해제 - 자동 재연결하지 않음");
                    break;
                    
                case DisconnectCause.Exception:
                case DisconnectCause.ExceptionOnConnect:
                    Debug.LogError("[PhotonManager] 예외로 인한 연결 해제 - 재연결 시도");
                    StartCoroutine(ReconnectAfterDelay(2f));
                    break;
                    
                case DisconnectCause.ServerTimeout:
                case DisconnectCause.ClientTimeout:
                    Debug.LogWarning("[PhotonManager] 타임아웃으로 인한 연결 해제 - 재연결 시도");
                    StartCoroutine(ReconnectAfterDelay(1f));
                    break;
                    
                case DisconnectCause.DisconnectByServerLogic:
                case DisconnectCause.DisconnectByServerReasonUnknown:
                    Debug.LogWarning("[PhotonManager] 서버에 의한 연결 해제 - 재연결 시도");
                    StartCoroutine(ReconnectAfterDelay(3f));
                    break;
                    
                default:
                    Debug.LogWarning($"[PhotonManager] 기타 이유로 인한 연결 해제 ({cause}) - 재연결 시도");
                    StartCoroutine(ReconnectAfterDelay(2f));
                    break;
            }
        }

        public override void OnJoinedLobby()
        {
            Debug.Log("로비 참가 완료");
            OnJoinedLobbyEvent?.Invoke();
            
            // GetCustomRoomList 제거 - Photon이 자동으로 OnRoomListUpdate 호출
            // PhotonNetwork.GetCustomRoomList(TypedLobby.Default, "Map");
            
            // 디버그 정보 출력
            Debug.Log($"현재 로비 상태: {PhotonNetwork.InLobby}");
            Debug.Log($"현재 연결된 지역: {PhotonNetwork.CloudRegion}");
            
            // 주기적으로 방 목록 업데이트 시작 (GetCustomRoomList 제거)
            StartCoroutine(PeriodicRoomListUpdate());
        }

        public override void OnLeftLobby()
        {
            Debug.Log("로비 나가기 완료");
        }

        public override void OnCreatedRoom()
        {
            Debug.Log($"[PhotonManager] 방 생성 완료: {PhotonNetwork.CurrentRoom.Name}");
            
            // 방 속성 설정 (기본 게임: 점프)
            Hashtable roomProperty = new Hashtable();
            roomProperty["SelectedGame"] = 0; // 0: 점프 게임
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);
            
            // 방 상태 확인
            Debug.Log($"[PhotonManager] 방 생성 후 상태 - 보임: {PhotonNetwork.CurrentRoom.IsVisible}, 열림: {PhotonNetwork.CurrentRoom.IsOpen}, 플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"방 참가 완료: {PhotonNetwork.CurrentRoom.Name}");
            OnJoinedRoomEvent?.Invoke();
            
            // 디버그 정보 출력
            Debug.Log($"[PhotonManager] 현재 로비 상태: {PhotonNetwork.InLobby}");
        }

        public override void OnLeftRoom()
        {
            Debug.Log("방 나가기 완료");
            OnLeftRoomEvent?.Invoke();
            
            // 방을 나간 후 로비로 돌아가면 자동으로 방 목록이 업데이트됨
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[PhotonManager] 방 생성 실패 - 코드: {returnCode}, 메시지: {message}");
            
            // 방 생성 실패 시 현재 상태 확인
            Debug.Log($"[PhotonManager] 방 생성 실패 후 상태 - 연결됨: {PhotonNetwork.IsConnected}, 로비: {PhotonNetwork.InLobby}");
            
            // 방 생성 실패 시 로비 상태 복구
            if (PhotonNetwork.IsConnected && !PhotonNetwork.InLobby)
            {
                Debug.Log("[PhotonManager] 방 생성 실패 후 로비 재참가 시도");
                StartCoroutine(RejoinLobbyAfterDelay(1f));
            }
            
            // 방 생성 실패 이벤트 발생 (UI에서 처리할 수 있도록)
            OnCreateRoomFailedEvent?.Invoke(returnCode, message);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[PhotonManager] 방 입장 실패 - 코드: {returnCode}, 메시지: {message}");
            
            // 방 입장 실패 시 현재 상태 확인
            Debug.Log($"[PhotonManager] 방 입장 실패 후 상태 - 연결됨: {PhotonNetwork.IsConnected}, 로비: {PhotonNetwork.InLobby}");
            
            // 방 입장 실패 시 로비 상태 복구
            if (PhotonNetwork.IsConnected && !PhotonNetwork.InLobby)
            {
                Debug.Log("[PhotonManager] 방 입장 실패 후 로비 재참가 시도");
                StartCoroutine(RejoinLobbyAfterDelay(1f));
            }
            
            // 방 입장 실패 이벤트 발생 (UI에서 처리할 수 있도록)
            OnJoinRoomFailedEvent?.Invoke(returnCode, message);
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"플레이어 입장: {newPlayer.NickName}");
            OnPlayerEnteredRoomEvent?.Invoke(newPlayer);
            
            // 로컬 플레이어가 아닌 경우에만 처리
            if (newPlayer != PhotonNetwork.LocalPlayer)
            {
                Debug.Log($"다른 플레이어 입장: {newPlayer.NickName}");
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"플레이어 퇴장: {otherPlayer.NickName}");
            OnPlayerLeftRoomEvent?.Invoke(otherPlayer);
            
            // 로컬 플레이어가 아닌 경우에만 처리
            if (otherPlayer != PhotonNetwork.LocalPlayer)
            {
                Debug.Log($"다른 플레이어 퇴장: {otherPlayer.NickName}");
            }
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            Debug.Log($"마스터 클라이언트 변경: {newMasterClient.NickName}");
            
            // 마스터 클라이언트 변경 이벤트 발생
            OnMasterClientSwitchedEvent?.Invoke(newMasterClient);
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            Debug.Log($"[PhotonManager] 방 목록 업데이트 호출됨 - 총 방 개수: {roomList.Count}");
            
            // 방 목록 상세 정보 로깅
            int visibleRooms = 0;
            int openRooms = 0;
            int removedRooms = 0;
            int totalRooms = 0;
            
            Debug.Log($"[PhotonManager] === 방 목록 상세 분석 시작 ===");
            
            foreach (RoomInfo info in roomList)
            {
                totalRooms++;
                Debug.Log($"[PhotonManager] [{totalRooms}/{roomList.Count}] 방: {info.Name}, 플레이어: {info.PlayerCount}/{info.MaxPlayers}, 제거됨: {info.RemovedFromList}, 보임: {info.IsVisible}, 열림: {info.IsOpen}");
                
                if (info.RemovedFromList)
                {
                    removedRooms++;
                    Debug.Log($"[PhotonManager] 방 제거됨: {info.Name}");
                }
                else
                {
                    if (info.IsVisible) visibleRooms++;
                    if (info.IsOpen) openRooms++;
                }
            }
            
            Debug.Log($"[PhotonManager] === 방 목록 상세 분석 완료 ===");
            Debug.Log($"[PhotonManager] 방 목록 요약 - 전체: {totalRooms}개, 보이는 방: {visibleRooms}개, 열린 방: {openRooms}개, 제거된 방: {removedRooms}개");
            
            // 이벤트 구독자 수 확인
            if (OnRoomListUpdateEvent != null)
            {
                Debug.Log($"[PhotonManager] OnRoomListUpdateEvent 구독자 수: {OnRoomListUpdateEvent.GetInvocationList().Length}");
            }
            else
            {
                Debug.LogWarning("[PhotonManager] OnRoomListUpdateEvent에 구독자가 없습니다!");
            }
            
            OnRoomListUpdateEvent?.Invoke(roomList);
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            // 방 속성 업데이트 로그는 제거 (너무 자주 호출됨)
            // Debug.Log("방 속성 업데이트");
            
            // RoomPopUp이 활성화되어 있다면 게임 선택 UI 업데이트
            RoomPopUp roomPopUp = FindObjectOfType<RoomPopUp>();
            if (roomPopUp != null)
            {
                // 선택된 게임이 변경된 경우 UI 업데이트
                if (propertiesThatChanged.ContainsKey("SelectedGame"))
                {
                    roomPopUp.Invoke("UpdateGameSelectionUI", 0.1f);
                }
            }
            
            // LobbyPopUp이 활성화되어 있다면 방 목록 업데이트
            LobbyPopUp lobbyPopUp = FindObjectOfType<LobbyPopUp>();
            if (lobbyPopUp != null)
            {
                // 방 속성이 변경되면 방 목록 새로고침
                lobbyPopUp.Invoke("RefreshRoomList", 0.1f);
            }
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            Debug.Log($"플레이어 속성 업데이트: {targetPlayer.NickName}");
            OnPlayerPropertiesUpdateEvent?.Invoke(targetPlayer, changedProps);
        }

        // PhotonManager에 채팅 RPC 메서드 추가
        [PunRPC]
        public void SendChatMessage(string sender, string message)
        {
            // RoomPopUp이 활성화되어 있다면 채팅 메시지 표시
            RoomPopUp roomPopUp = FindObjectOfType<RoomPopUp>();
            if (roomPopUp != null)
            {
                roomPopUp.DisplayChatMessage(sender, message);
            }
        }

        // 주기적으로 방 목록 업데이트하는 코루틴
        private System.Collections.IEnumerator PeriodicRoomListUpdate()
        {
            while (PhotonNetwork.InLobby)
            {
                yield return new WaitForSeconds(5f); // 5초마다 방 목록 업데이트
                
                if (PhotonNetwork.InLobby)
                {
                    // GetCustomRoomList 제거 - Photon이 자동으로 방 목록 업데이트
                    // PhotonNetwork.GetCustomRoomList(TypedLobby.Default, "Map");
                    Debug.Log("[PhotonManager] 주기적 방 목록 업데이트 완료");
                }
            }
        }

        // 플레이어 속성 변경 편의 메서드들
        public void SetPlayerReady(bool isReady)
        {
            Hashtable playerProperty = new Hashtable();
            playerProperty["Ready"] = isReady;
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperty);
            Debug.Log($"플레이어 준비 상태 변경: {isReady}");
        }

        public void SetPlayerColor(int colorIndex)
        {
            Hashtable playerProperty = new Hashtable();
            playerProperty["Color"] = colorIndex;
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperty);
            Debug.Log($"플레이어 색상 변경: {colorIndex}");
        }
        
        public void ClearPlayerColor()
        {
            Hashtable playerProperty = new Hashtable();
            playerProperty["Color"] = null;
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperty);
            Debug.Log("플레이어 색상 취소");
        }

        public void SetPlayerSelectedGame(int gameIndex)
        {
            Hashtable playerProperty = new Hashtable();
            playerProperty["SelectedGame"] = gameIndex;
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperty);
            Debug.Log($"플레이어 게임 선택 변경: {gameIndex}");
        }

        // 여러 속성을 한 번에 변경하는 메서드
        public void SetPlayerProperties(Hashtable properties)
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
            Debug.Log($"플레이어 속성 일괄 변경: {properties.Count}개");
        }

        // 플레이어 속성 가져오기 편의 메서드들
        public bool GetPlayerReady(Player player)
        {
            if (player.CustomProperties.TryGetValue("Ready", out object value))
            {
                return (bool)value;
            }
            return false;
        }

        public int GetPlayerColor(Player player)
        {
            if (player.CustomProperties.TryGetValue("Color", out object value))
            {
                if (value != null)
                {
                    return (int)value;
                }
            }
            return -1; // 색상이 선택되지 않음
        }

        public int GetPlayerSelectedGame(Player player)
        {
            if (player.CustomProperties.TryGetValue("SelectedGame", out object value))
            {
                return (int)value;
            }
            return 0; // 기본 게임
        }

        // 권한 이전 (마스터 클라이언트 전환)
        public void TransferMasterClient(Player newMasterClient)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log($"[PhotonManager] 권한 이전 시도: {newMasterClient.NickName}");
                bool success = PhotonNetwork.SetMasterClient(newMasterClient);
                if (success)
                {
                    Debug.Log($"[PhotonManager] 권한 이전 성공: {newMasterClient.NickName}");
                }
                else
                {
                    Debug.LogError($"[PhotonManager] 권한 이전 실패: {newMasterClient.NickName}");
                }
            }
            else
            {
                Debug.LogWarning("[PhotonManager] 마스터 클라이언트만 권한을 이전할 수 있습니다.");
            }
        }

        // 특정 플레이어에게 권한 이전 (ActorNumber로)
        public void TransferMasterClient(int actorNumber)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
                if (targetPlayer != null)
                {
                    Debug.Log($"[PhotonManager] 권한 이전 시도: {targetPlayer.NickName} (ActorNumber: {actorNumber})");
                    bool success = PhotonNetwork.SetMasterClient(targetPlayer);
                    if (success)
                    {
                        Debug.Log($"[PhotonManager] 권한 이전 성공: {targetPlayer.NickName}");
                    }
                    else
                    {
                        Debug.LogError($"[PhotonManager] 권한 이전 실패: {targetPlayer.NickName}");
                    }
                }
                else
                {
                    Debug.LogError($"[PhotonManager] ActorNumber {actorNumber}에 해당하는 플레이어를 찾을 수 없습니다.");
                }
            }
            else
            {
                Debug.LogWarning("[PhotonManager] 마스터 클라이언트만 권한을 이전할 수 있습니다.");
            }
        }

        // 자동으로 다음 방장 선택 (방장이 나갈 때 호출)
        public void AutoTransferMasterClient()
        {
            if (!PhotonNetwork.IsMasterClient || PhotonNetwork.PlayerList.Length <= 1)
            {
                return;
            }

            // 현재 방장을 제외한 다른 플레이어들 중에서 다음 방장 선택
            Player nextMasterClient = null;
            
            // ActorNumber 순서대로 다음 플레이어 선택
            for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
            {
                Player player = PhotonNetwork.PlayerList[i];
                if (player.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    nextMasterClient = player;
                    break;
                }
            }

            if (nextMasterClient != null)
            {
                Debug.Log($"[PhotonManager] 자동 권한 이전: {nextMasterClient.NickName}");
                TransferMasterClient(nextMasterClient);
            }
        }

        // 로비 재참가 코루틴
        private System.Collections.IEnumerator RejoinLobbyAfterDelay(float delay)
        {
            Debug.Log("[PhotonManager] 로비 재참가 대기 중...");
            yield return new WaitForSeconds(delay);
            Debug.Log("[PhotonManager] 로비 재참가 시도");
            PhotonNetwork.JoinLobby();
        }

        // 자동 재연결 코루틴
        private System.Collections.IEnumerator ReconnectAfterDelay(float delay)
        {
            Debug.Log("[PhotonManager] 자동 재연결 대기 중...");
            yield return new WaitForSeconds(delay);
            Debug.Log("[PhotonManager] 자동 재연결 시도");
            PhotonNetwork.ConnectUsingSettings();
        }
    }
}