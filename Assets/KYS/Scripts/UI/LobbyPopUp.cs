using Firebase.Auth;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

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
        private Transform roomListContent => GetUI<Transform>("RoomListContent");
        private GameObject roomListItemPrefab => GetUI("RoomListItemPrefab");
        private Dictionary<string, GameObject> roomListItems = new Dictionary<string, GameObject>();

        private new void Awake()
        {
            base.Awake();

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

            var deleteUserButton = GetEvent("DeleteUserButton");
            if (deleteUserButton != null)
            {
                deleteUserButton.Click += DeleteUser;
            }
            else
            {
                Debug.LogError("[LobbyPopUp] DeleteUserButton을 찾을 수 없습니다.");
            }
        }

        private void Start()
        {
            // Firebase 사용자 정보 초기화
            InitializePanel();

            // UI 요소가 준비된 후 이벤트 연결 (Awake에서 실패한 경우)
            ConnectEventsIfNeeded();
        }

        private void Update()
        {
            stateText.text = $"Current State : {PhotonNetwork.NetworkClientState}";
        }

        private void OnEnable()
        {
            // PhotonManager 이벤트 구독
            if (PhotonManager.Instance != null)
            {
                PhotonManager.Instance.OnJoinedLobbyEvent += OnJoinedLobby;
                PhotonManager.Instance.OnRoomListUpdateEvent += OnRoomListUpdate;
                PhotonManager.Instance.OnJoinedRoomEvent += OnJoinedRoom;

                // 닉네임 동기화
                PhotonManager.Instance.SyncNicknameWithFirebase();

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
            }


        }

        private void InitializePanel()
        {
            RegisterEvents();
            // 패널이 활성화될 때 로그인 정보 업데이트
            LoginInfo();
        }

        private void RegisterEvents()
        {
            // 기존 이벤트 해제 후 다시 등록 (중복 방지)
            var deleteUserButton = GetEvent("DeleteUserButton");


            if (deleteUserButton != null)
            {
                deleteUserButton.Click -= DeleteUser;
                deleteUserButton.Click += DeleteUser;
            }
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

            // DeleteUserButton 이벤트가 연결되지 않았다면 다시 시도
            var deleteUserButton = GetEvent("DeleteUserButton");
            if (deleteUserButton != null)
            {
                deleteUserButton.Click -= DeleteUser; // 중복 방지
                deleteUserButton.Click += DeleteUser;
            }
            var menuButton = GetEvent("MenuButton");
            if (menuButton != null)
            {
                menuButton.Click -= OnMenu;
                menuButton.Click += OnMenu;
            }

        }

        // Photon 로비 관련 메서드들
        private void OnCreateRoomClicked(PointerEventData eventData)
        {
            string roomName = roomNameField.text.Trim();
            if (string.IsNullOrEmpty(roomName))
            {
                ShowErrorMessage("방 이름을 입력해주세요.");
                return;
            }

            PhotonManager.Instance.CreateRoom(roomName);
            roomNameField.text = "";
        }

        private void OnJoinedLobby()
        {
            Debug.Log("로비 UI 활성화");
            // 로비 UI 표시 로직 (필요시 추가)
        }

        private void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            // 방 목록 UI 업데이트 로직
            foreach (RoomInfo info in roomList)
            {
                if (info.RemovedFromList)
                {
                    if (roomListItems.TryGetValue(info.Name, out GameObject obj))
                    {
                        Destroy(obj);
                        roomListItems.Remove(info.Name);
                    }
                    continue;
                }

                if (roomListItems.ContainsKey(info.Name))
                {
                    roomListItems[info.Name].GetComponent<RoomListItem>().Init(info);
                }
                else
                {
                    GameObject roomListItem = Instantiate(roomListItemPrefab);
                    roomListItem.transform.SetParent(roomListContent, false);
                    roomListItem.GetComponent<RoomListItem>().Init(info);
                    roomListItems.Add(info.Name, roomListItem);
                }
            }
        }

        private void OnJoinedRoom()
        {
            Debug.Log("방에 입장했습니다. 방 UI로 전환합니다.");
            // 방 UI로 전환
            UIManager.Instance.ClosePopUp();
            UIManager.Instance.ShowPopUp<RoomPopUp>();
        }



        public void LoginInfo()
        {
            // FirebaseManager 사용
            FirebaseUser user = FirebaseManager.Auth.CurrentUser;

            uiEmailText.text = user.Email;
            uiNameText.text = user.DisplayName;
            //UIuserIdText.text = user.UserId;
        }

        // 방 삭제 버튼 클릭 시
        private void DeleteUser(PointerEventData eventData)
        {
            // DeletePopUp 생성
            //UIManager.Instance.ShowPopUp<DeletePopUp>();
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


    }
}
