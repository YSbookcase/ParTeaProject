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
            // Photon 연결 시작
            PhotonNetwork.ConnectUsingSettings();
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
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.Log($"Photon 연결 해제: {cause}");
            PhotonNetwork.ConnectUsingSettings(); // 재연결 시도
        }

        public override void OnJoinedLobby()
        {
            Debug.Log("로비 참가 완료");
            OnJoinedLobbyEvent?.Invoke();
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
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"방 참가 완료: {PhotonNetwork.CurrentRoom.Name}");
            OnJoinedRoomEvent?.Invoke();
        }

        public override void OnLeftRoom()
        {
            Debug.Log("방 나가기 완료");
            OnLeftRoomEvent?.Invoke();
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
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"플레이어 퇴장: {otherPlayer.NickName}");
            OnPlayerLeftRoomEvent?.Invoke(otherPlayer);
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            Debug.Log($"마스터 클라이언트 변경: {newMasterClient.NickName}");
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            Debug.Log($"방 목록 업데이트: {roomList.Count}개");
            OnRoomListUpdateEvent?.Invoke(roomList);
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            Debug.Log("방 속성 업데이트");
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            Debug.Log($"플레이어 속성 업데이트: {targetPlayer.NickName}");
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
    }
}