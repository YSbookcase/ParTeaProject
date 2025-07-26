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
    public class PhotonManager : MonoBehaviourPunCallbacks
    {
        // 싱글톤 인스턴스
        public static PhotonManager Instance { get; private set; }

        // 이벤트들 (UI와 분리하기 위해)
        public event Action OnConnectedToMasterEvent;
        public event Action OnJoinedLobbyEvent;
        public event Action OnJoinedRoomEvent;
        public event Action OnLeftRoomEvent;
        public event Action<Player> OnPlayerEnteredRoomEvent;
        public event Action<Player> OnPlayerLeftRoomEvent;
        public event Action<List<RoomInfo>> OnRoomListUpdateEvent;
        public event Action<Player, Hashtable> OnPlayerPropertiesUpdateEvent; // 플레이어 속성 업데이트 이벤트 추가

        private PhotonView _photonView;

        private void Awake()
        {
            // 싱글톤 패턴 구현
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
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
            if (string.IsNullOrEmpty(roomName))
            {
                Debug.LogError("방 이름이 비어있습니다.");
                return;
            }

            RoomOptions options = new RoomOptions
            {
                MaxPlayers = 4,
                IsVisible = true,
                IsOpen = true
            };
            options.CustomRoomPropertiesForLobby = new string[] { "Map" };
            PhotonNetwork.CreateRoom(roomName, options);
        }

        public void JoinRoom(string roomName)
        {
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
            Debug.Log($"Photon 연결 해제: {cause}");
            
            // 연결 해제 시 닉네임 초기화
            PhotonNetwork.NickName = "";
            
            // 로그아웃이 아닌 경우에만 자동 재연결 (네트워크 오류 등)
            if (cause != DisconnectCause.DisconnectByClientLogic)
            {
                Debug.Log("[PhotonManager] 네트워크 오류로 인한 재연결 시도");
                PhotonNetwork.ConnectUsingSettings(); // 재연결 시도
            }
            else
            {
                Debug.Log("[PhotonManager] 사용자 로그아웃으로 인한 연결 해제 - 자동 재연결하지 않음");
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
            Debug.Log($"방 생성 완료: {PhotonNetwork.CurrentRoom.Name}");
            
            // 방 속성 설정
            Hashtable roomProperty = new Hashtable();
            roomProperty["Map"] = 0;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);
            
            // 방 생성 후 자동으로 방에 입장 (LeaveRoom 제거)
            // 방 목록 업데이트는 다른 클라이언트들이 방을 볼 수 있도록 자동으로 처리됨
            
            // 디버그 정보 출력
            Debug.Log($"방 보임 상태: {PhotonNetwork.CurrentRoom.IsVisible}");
            Debug.Log($"방 열림 상태: {PhotonNetwork.CurrentRoom.IsOpen}");
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
            Debug.LogError($"방 생성 실패: {message}");
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"방 입장 실패: {message}");
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
            OnPlayerEnteredRoomEvent?.Invoke(newMasterClient);
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            Debug.Log($"방 목록 업데이트: {roomList.Count}개의 방");
            OnRoomListUpdateEvent?.Invoke(roomList);
            
            // 각 방의 상세 정보 출력
            foreach (RoomInfo info in roomList)
            {
                if (info.RemovedFromList)
                {
                    Debug.Log($"방 제거됨: {info.Name}");
                }
                else
                {
                    Debug.Log($"방 정보: {info.Name}, 플레이어: {info.PlayerCount}/{info.MaxPlayers}, 보임: {info.IsVisible}, 열림: {info.IsOpen}");
                }
            }
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            Debug.Log("방 속성 업데이트");
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            Debug.Log($"플레이어 속성 업데이트: {targetPlayer.NickName}");
            OnPlayerPropertiesUpdateEvent?.Invoke(targetPlayer, changedProps);
        }

        // PhotonManager에 채팅 RPC 메서드 추가
        [PunRPC]
        private void SendChatMessage(string sender, string message)
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
                return (int)value;
            }
            return 0; // 기본 색상
        }

        public int GetPlayerSelectedGame(Player player)
        {
            if (player.CustomProperties.TryGetValue("SelectedGame", out object value))
            {
                return (int)value;
            }
            return 0; // 기본 게임
        }
    }
}