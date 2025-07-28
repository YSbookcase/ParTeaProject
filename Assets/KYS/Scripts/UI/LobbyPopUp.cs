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
        private Transform roomListContent => GetUI<Transform>("RoomViewport");
        private GameObject roomListItemPrefab;
        private Dictionary<string, GameObject> roomListItems = new Dictionary<string, GameObject>();

        private new void Awake()
        {
            base.Awake();
            canCloseWithESC = false; // ESC로 닫을 수 없음
            // Resources 폴더에서 RoomListItemPrefab 로드
            roomListItemPrefab = Resources.Load<GameObject>("UI/RoomListItemPrefab");
            if (roomListItemPrefab == null)
            {
                Debug.LogError("[LobbyPopUp] Resources/UI/RoomListItemPrefab을 찾을 수 없습니다.");
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

                // Photon 연결 시작 (로그인 후 로비에 입장했을 때)
                PhotonManager.Instance.ConnectToPhoton();

                Debug.Log("[LobbyPopUp] PhotonManager 이벤트 구독 완료 및 Photon 연결 시작");
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

            // 방 생성 요청 후 잠시 대기 후 방 목록 새로고침
            StartCoroutine(RefreshRoomListAfterDelay(2f));
        }

                private void OnJoinedLobby()
        {
            Debug.Log("로비 UI 활성화");
            
            // Photon이 자동으로 방 목록을 업데이트하므로 별도 요청 불필요
            if (PhotonNetwork.InLobby)
            {
                Debug.Log("[LobbyPopUp] 로비 입장 완료 - 방 목록 자동 업데이트 대기");
            }
            else
            {
                Debug.LogError("[LobbyPopUp] 로비에 입장하지 못했습니다.");
            }
        }

        private void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            Debug.Log($"[LobbyPopUp] 방 목록 업데이트 시작 - 방 개수: {roomList.Count}");

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

            // 기존 방 목록 정리
            foreach (var kvp in roomListItems)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            roomListItems.Clear();

            // 방 목록 UI 업데이트 로직
            foreach (RoomInfo info in roomList)
            {
                Debug.Log($"[LobbyPopUp] 방 정보 처리: {info.Name}, 플레이어: {info.PlayerCount}/{info.MaxPlayers}, 보임: {info.IsVisible}, 열림: {info.IsOpen}");

                // 게임이 진행중이지 않고, 보이고, 열려있는 방만 표시
                if (info.RemovedFromList || !info.IsVisible || !info.IsOpen)
                {
                    Debug.Log($"[LobbyPopUp] 방 제외: {info.Name} (제거됨: {info.RemovedFromList}, 보임: {info.IsVisible}, 열림: {info.IsOpen})");
                    continue;
                }

                // 새 방 생성
                Debug.Log($"[LobbyPopUp] 새 방 생성 시도: {info.Name}");

                GameObject roomListItem = Instantiate(roomListItemPrefab);
                if (roomListItem == null)
                {
                    Debug.LogError("[LobbyPopUp] RoomListItem 인스턴스 생성 실패");
                    continue;
                }

                roomListItem.transform.SetParent(roomListContent, false);

                RoomListItem itemComponent = roomListItem.GetComponent<RoomListItem>();
                if (itemComponent != null)
                {
                    itemComponent.Init(info);
                    roomListItems.Add(info.Name, roomListItem);
                    Debug.Log($"[LobbyPopUp] 새 방 생성 완료: {info.Name}");
                }
                else
                {
                    Debug.LogError("[LobbyPopUp] RoomListItem 컴포넌트를 찾을 수 없습니다.");
                    Destroy(roomListItem);
                }
            }

            Debug.Log($"[LobbyPopUp] 방 목록 업데이트 완료 - 총 방 개수: {roomListItems.Count}");

            // UI 레이아웃 강제 업데이트
            Canvas.ForceUpdateCanvases();
            if (roomListContent is RectTransform rectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
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

        // 수동으로 방 목록 새로고침하는 메서드 (디버깅용)
        public void RefreshRoomList()
        {
            if (PhotonNetwork.InLobby)
            {
                //PhotonNetwork.GetCustomRoomList(TypedLobby.Default, "Map");
                Debug.Log("[LobbyPopUp] 수동 방 목록 새로고침 완료");
            }
            else
            {
                Debug.LogError("[LobbyPopUp] 로비에 있지 않아 방 목록을 새로고침할 수 없습니다.");
            }
        }
    }
}
