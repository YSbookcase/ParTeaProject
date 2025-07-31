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

        // PhotonView 제거 - 각 UI 컴포넌트가 자체 PhotonView를 가져야 함

        private void Awake()
        {
            // 상속받은 싱글톤 패턴을 사용하므로 별도 구현 불필요
            Debug.Log("[PhotonManager] Awake 완료 - 순수한 연결 관리자로 동작합니다.");
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
            
            // 초기 방 속성 설정 (기본 게임: 점프)
            Hashtable initialRoomProperties = new Hashtable();
            initialRoomProperties["SelectedGame"] = 0; // 0: 점프 게임
            
            // 방 옵션 설정 - 방이 보이고 열려있도록 설정
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = 4,
                IsVisible = true,
                IsOpen = true,
                PublishUserId = true,
                CustomRoomProperties = initialRoomProperties,
                CustomRoomPropertiesForLobby = new string[] { "SelectedGame" }
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
            Debug.Log("[PhotonManager] 방 나가기 시작");
            
            // 방 나가기 전 초기화
            InitializeBeforeLeaveRoom();
            
            PhotonNetwork.LeaveRoom();
        }

        // 방 나가기 전 초기화
        private void InitializeBeforeLeaveRoom()
        {
            Debug.Log("[PhotonManager] 방 나가기 전 초기화 시작");
            
            // 로컬 플레이어 속성 초기화
            if (PhotonNetwork.LocalPlayer != null)
            {
                Hashtable playerProperties = new Hashtable();
                playerProperties["Ready"] = false;
                playerProperties["Color"] = null;
                playerProperties["SelectedGame"] = null;
                PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
                Debug.Log("[PhotonManager] 로컬 플레이어 속성 초기화 완료");
            }
            
            Debug.Log("[PhotonManager] 방 나가기 전 초기화 완료");
        }

        // 수동으로 Photon 연결 시작
        public void ConnectToPhoton()
        {
            Debug.Log($"[PhotonManager] ConnectToPhoton 호출 - 현재 상태: {PhotonNetwork.NetworkClientState}, 연결됨: {PhotonNetwork.IsConnected}");
            
            // PeerCreated 상태에서 멈춘 경우 강제로 연결 해제 후 재연결
            if (PhotonNetwork.NetworkClientState == ClientState.PeerCreated)
            {
                Debug.LogWarning("[PhotonManager] PeerCreated 상태에서 멈춤. 연결을 강제로 해제하고 재연결을 시도합니다.");
                PhotonNetwork.Disconnect();
                StartCoroutine(ReconnectAfterDelay(1f));
                return;
            }
            
            // 이미 연결 중이거나 연결된 경우 중복 연결 방지
            if (PhotonNetwork.IsConnected || PhotonNetwork.NetworkClientState == ClientState.ConnectingToNameServer || 
                PhotonNetwork.NetworkClientState == ClientState.ConnectingToMasterServer || 
                PhotonNetwork.NetworkClientState == ClientState.ConnectingToGameServer)
            {
                Debug.Log($"[PhotonManager] 이미 Photon에 연결되어 있거나 연결 중입니다. 현재 상태: {PhotonNetwork.NetworkClientState}");
                return;
            }
            
            // 연결 중이 아닌 경우에만 연결 시도
            if (PhotonNetwork.NetworkClientState == ClientState.Disconnected)
            {
                Debug.Log("[PhotonManager] Photon 연결 시작");
                
                // 연결 전에 기본 설정 확인
                if (string.IsNullOrEmpty(PhotonNetwork.NickName))
                {
                    PhotonNetwork.NickName = "Guest";
                }
                
                PhotonNetwork.ConnectUsingSettings();
            }
            else
            {
                Debug.Log($"[PhotonManager] 연결할 수 없는 상태입니다. 현재 상태: {PhotonNetwork.NetworkClientState}");
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
                    
                case DisconnectCause.InvalidAuthentication:
                    Debug.LogError("[PhotonManager] 인증 실패로 인한 연결 해제 - 재연결 시도");
                    StartCoroutine(ReconnectAfterDelay(2f));
                    break;
                    
                case DisconnectCause.InvalidRegion:
                    Debug.LogError("[PhotonManager] 잘못된 지역으로 인한 연결 해제 - 재연결 시도");
                    StartCoroutine(ReconnectAfterDelay(2f));
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
            Debug.Log($"[PhotonManager] OnRoomListUpdate 호출됨 - 방 개수: {roomList?.Count ?? 0}");
            
            // 방 목록 업데이트 이벤트만 호출 (로그 제거)
            OnRoomListUpdateEvent?.Invoke(roomList);
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            Debug.Log($"[PhotonManager] 방 속성 변경 감지: {string.Join(", ", propertiesThatChanged.Keys)}");
            
            // 각 변경된 속성의 값도 로깅
            foreach (var kvp in propertiesThatChanged)
            {
                Debug.Log($"[PhotonManager] 속성 변경: {kvp.Key} = {kvp.Value}");
            }
            
            // RoomPopUp이 활성화되어 있다면 게임 선택 UI 업데이트
            RoomPopUp roomPopUp = FindObjectOfType<RoomPopUp>();
            if (roomPopUp != null)
            {
                // 선택된 게임이 변경된 경우 UI 업데이트 (즉시 호출)
                if (propertiesThatChanged.ContainsKey("SelectedGame"))
                {
                    Debug.Log("[PhotonManager] RoomPopUp에 게임 선택 UI 업데이트 요청");
                    roomPopUp.UpdateGameSelectionUI();
                }
            }
            else
            {
                Debug.Log("[PhotonManager] RoomPopUp을 찾을 수 없습니다 (방에 있지 않음)");
            }
            
            // OnRoomPropertiesUpdate는 방에 있는 클라이언트에게만 호출되므로
            // 로비에 있는 클라이언트는 OnRoomListUpdate를 통해 변경사항을 감지합니다.
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            // 플레이어 속성 업데이트 이벤트만 호출 (로그 제거)
            OnPlayerPropertiesUpdateEvent?.Invoke(targetPlayer, changedProps);
        }

        // PhotonManager에서 채팅 RPC 메서드 제거 (PhotonChatManager로 대체)
        // [PunRPC]
        // public void SendChatMessage(string sender, string message)
        // {
        //     // RoomPopUp이 활성화되어 있다면 채팅 메시지 표시
        //     RoomPopUp roomPopUp = FindObjectOfType<RoomPopUp>();
        //     if (roomPopUp != null)
        //     {
        //         roomPopUp.DisplayChatMessage(sender, message);
        //     }
        // }

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
                    // 로그 제거 - 주기적 업데이트는 정상 동작
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

        // PhotonView 관련 코루틴들 제거 - 각 UI 컴포넌트가 자체 PhotonView를 관리
    }
}