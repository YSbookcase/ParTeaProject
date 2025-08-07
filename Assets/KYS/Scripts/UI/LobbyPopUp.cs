using Firebase.Auth;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KYS
{
    public class LobbyPopUp : BaseUI
    {
        #region UI References
        private TMP_Text uiEmailText => GetUI<TMP_Text>("E-MailTextContent");
        private TMP_Text uiNameText => GetUI<TMP_Text>("NameTextContent");
        private TMP_Text stateText => GetUI<TMP_Text>("CurrentState");
        private TMP_InputField roomNameField => GetUI<TMP_InputField>("RoomNameField");
        private Transform roomListContent => GetUI<Transform>("Content");
        #endregion

        #region Private Fields
        private GameObject roomListItemPrefab;
        private Dictionary<string, GameObject> roomListItems = new Dictionary<string, GameObject>();
        private Dictionary<string, RoomInfo> currentRoomInfos = new Dictionary<string, RoomInfo>();
        private bool isCreatingRoom = false; // 방 생성 중 플래그 추가
        private ClientState lastNetworkState = ClientState.Disconnected;
        #endregion

        #region Unity Lifecycle
        private new void Awake()
        {
            base.Awake();
            canCloseWithESC = false;
            
            roomListItemPrefab = Resources.Load<GameObject>("UI/RoomListItemPrefab");
            if (roomListItemPrefab == null)
            {
                Debug.LogError("[LobbyPopUp] Resources/UI/RoomListItemPrefab을 찾을 수 없습니다.");
            }

            var createRoomButton = GetEventWithSFX("CreateRoomButton", "SFX_ButtonClick");
            if (createRoomButton != null)
            {
                createRoomButton.Click += OnCreateRoomClicked;
            }
        }

        private void Start()
        {
            InitializePanel();
            ConnectEventsIfNeeded();
        }
        
        private void Update()
        {
            ClientState currentState = PhotonNetwork.NetworkClientState;
            if (currentState != lastNetworkState)
            {
                stateText.text = $"Current State : {GetUserFriendlyStateName(currentState)}";
                lastNetworkState = currentState;
                
                if (currentState == ClientState.PeerCreated)
                {
                    StartCoroutine(HandlePeerCreatedState());
                }
            }
        }

        private void OnEnable()
        {
            LoginInfo();
            
            if (PhotonManager.Instance != null)
            {
                PhotonManager.Instance.OnJoinedLobbyEvent += OnJoinedLobby;
                PhotonManager.Instance.OnRoomListUpdateEvent += OnRoomListUpdate;
                PhotonManager.Instance.OnJoinedRoomEvent += OnJoinedRoom;
                PhotonManager.Instance.OnCreateRoomFailedEvent += OnCreateRoomFailed;
                PhotonManager.Instance.OnJoinRoomFailedEvent += OnJoinRoomFailed;

                PhotonManager.Instance.SyncNicknameWithFirebase();
                
                if (PhotonNetwork.NetworkClientState == ClientState.PeerCreated)
                {
                    PhotonManager.Instance.ConnectToPhoton();
                    return;
                }
                
                if (!PhotonNetwork.IsConnected && PhotonNetwork.NetworkClientState == ClientState.Disconnected)
                {
                    PhotonManager.Instance.ConnectToPhoton();
                }
                else if (PhotonNetwork.IsConnected && !PhotonNetwork.InLobby)
                {
                    PhotonNetwork.JoinLobby();
                }
            }
        }

        private void OnDisable()
        {
            if (PhotonManager.Instance != null)
            {
                PhotonManager.Instance.OnJoinedLobbyEvent -= OnJoinedLobby;
                PhotonManager.Instance.OnRoomListUpdateEvent -= OnRoomListUpdate;
                PhotonManager.Instance.OnJoinedRoomEvent -= OnJoinedRoom;
                PhotonManager.Instance.OnCreateRoomFailedEvent -= OnCreateRoomFailed;
                PhotonManager.Instance.OnJoinRoomFailedEvent -= OnJoinRoomFailed;
            }
        }
        #endregion

        #region Initialization
        private void InitializePanel()
        {
            LoginInfo();
        }

        private void ConnectEventsIfNeeded()
        {
            var createRoomButton = GetEventWithSFX("CreateRoomButton", "SFX_ButtonClick");
            if (createRoomButton != null)
            {
                createRoomButton.Click -= OnCreateRoomClicked;
                createRoomButton.Click += OnCreateRoomClicked;
            }

            var menuButton = GetEventWithSFX("MenuButton", "SFX_ButtonClick");
            if (menuButton != null)
            {
                menuButton.Click -= OnMenu;
                menuButton.Click += OnMenu;
            }
        }
        #endregion

        #region Room Management
        private void OnCreateRoomClicked(PointerEventData eventData)
        {
            // 이미 방 생성 중이면 무시
            if (isCreatingRoom)
            {
                return;
            }

            Button createRoomButton = GetUI<Button>("CreateRoomButton");
            
            string roomName = roomNameField.text.Trim();
            if (string.IsNullOrEmpty(roomName))
            {
                ShowErrorMessage("방 이름을 입력해주세요.");
                return;
            }

            if (!PhotonNetwork.IsConnected)
            {
                ShowErrorMessage("Photon에 연결되어 있지 않습니다. 잠시 기다려주세요.");
                return;
            }

            if (!PhotonNetwork.InLobby)
            {
                ShowErrorMessage("로비에 있지 않습니다. 잠시 기다려주세요.");
                return;
            }

            if (PhotonNetwork.InRoom) return;

            // 방 생성 중 플래그 설정 및 버튼 비활성화
            isCreatingRoom = true;
            if (createRoomButton != null)
            {
                createRoomButton.interactable = false;
            }
            
            PhotonManager.Instance.CreateRoom(roomName);
        }

        private void OnJoinedLobby()
        {
            if (PhotonNetwork.InLobby)
            {
                currentRoomInfos.Clear();
            }
        }

        private void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            if (roomList == null || roomListItemPrefab == null || roomListContent == null) return;

            int addedRooms = 0;
            int removedRooms = 0;
            int updatedRooms = 0;
            int gameChangedRooms = 0;
            
            foreach (RoomInfo info in roomList)
            {
                if (info.RemovedFromList)
                {
                    if (currentRoomInfos.ContainsKey(info.Name))
                    {
                        currentRoomInfos.Remove(info.Name);
                        removedRooms++;
                    }
                }
                else
                {
                    bool isNewRoom = !currentRoomInfos.ContainsKey(info.Name);
                    bool gameChanged = false;
                    
                    if (!isNewRoom && currentRoomInfos.ContainsKey(info.Name))
                    {
                        RoomInfo oldInfo = currentRoomInfos[info.Name];
                        if (oldInfo.CustomProperties.TryGetValue("SelectedGame", out object oldGame) &&
                            info.CustomProperties.TryGetValue("SelectedGame", out object newGame))
                        {
                            if (!oldGame.Equals(newGame))
                            {
                                gameChanged = true;
                                gameChangedRooms++;
                            }
                        }
                    }
                    
                    currentRoomInfos[info.Name] = info;
                    
                    if (isNewRoom)
                    {
                        addedRooms++;
                    }
                    else if (gameChanged)
                    {
                        updatedRooms++;
                    }
                }
            }

            UpdateRoomListUI();
        }

        private void UpdateRoomListUI()
        {
            foreach (var kvp in roomListItems)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            roomListItems.Clear();

            int visibleRoomCount = 0;
            
            foreach (var kvp in currentRoomInfos)
            {
                RoomInfo info = kvp.Value;

                GameObject roomListItem = Instantiate(roomListItemPrefab);
                if (roomListItem == null) continue;

                roomListItem.transform.SetParent(roomListContent, false);
                
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
                    Destroy(roomListItem);
                }
            }

            Canvas.ForceUpdateCanvases();
            if (roomListContent is RectTransform contentRectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);
            }
        }

        private void OnJoinedRoom()
        {
            // 방 생성 중 플래그 해제
            isCreatingRoom = false;
            
            if (roomNameField != null)
            {
                roomNameField.text = "";
            }
            
            UIManager.Instance.ClosePopUp();
            UIManager.Instance.ShowPopUp<RoomPopUp>();
        }

        private void OnCreateRoomFailed(short returnCode, string message)
        {
            // 방 생성 중 플래그 해제
            isCreatingRoom = false;
            
            Button createRoomButton = GetUI<Button>("CreateRoomButton");
            if (createRoomButton != null)
            {
                createRoomButton.interactable = true;
            }
            
            string errorMessage = GetErrorMessage(returnCode, message);
            ShowErrorMessage($"방 생성 실패: {errorMessage}");
        }

        private void OnJoinRoomFailed(short returnCode, string message)
        {
            string errorMessage = GetErrorMessage(returnCode, message);
            ShowErrorMessage($"방 입장 실패: {errorMessage}");
        }
        #endregion

        #region Utility Methods
        private string GetUserFriendlyStateName(ClientState state)
        {
            switch (state)
            {
                case ClientState.Disconnected: return "연결 해제됨";
                case ClientState.PeerCreated: return "연결 초기화 중...";
                case ClientState.ConnectingToNameServer: return "서버 연결 중...";
                case ClientState.ConnectedToNameServer: return "서버 연결됨";
                case ClientState.ConnectingToMasterServer: return "마스터 서버 연결 중...";
                case ClientState.ConnectedToMasterServer: return "마스터 서버 연결됨";
                case ClientState.ConnectingToGameServer: return "게임 서버 연결 중...";
                case ClientState.ConnectedToGameServer: return "게임 서버 연결됨";
                case ClientState.Joining: return "방 입장 중...";
                case ClientState.Joined: return "방 입장됨";
                case ClientState.Leaving: return "방 나가는 중...";
                case ClientState.DisconnectingFromGameServer: return "게임 서버 연결 해제 중...";
                case ClientState.DisconnectingFromMasterServer: return "마스터 서버 연결 해제 중...";
                case ClientState.DisconnectingFromNameServer: return "서버 연결 해제 중...";
                case ClientState.Authenticating: return "인증 중...";
                default: return state.ToString();
            }
        }

        private string GetErrorMessage(short returnCode, string message)
        {
            switch (returnCode)
            {
                case 32767: return "방이 가득 찼습니다.";
                case 32766: return "방이 닫혀있습니다.";
                case 32765: return "방이 존재하지 않습니다.";
                case 32764: return "서버가 가득 찼습니다. 잠시 후 다시 시도해주세요.";
                case 32763: return "잘못된 지역입니다.";
                case 32762: return "인증에 실패했습니다.";
                default: return string.IsNullOrEmpty(message) ? "알 수 없는 오류가 발생했습니다." : message;
            }
        }

        public void LoginInfo()
        {
            FirebaseUser user = FirebaseManager.Auth.CurrentUser;

            if (user != null)
            {
                uiEmailText.text = user.Email ?? "이메일 없음";
                uiNameText.text = user.DisplayName ?? "닉네임 없음";
                
                if (PhotonManager.Instance != null)
                {
                    if (!PhotonNetwork.IsConnected)
                    {
                        PhotonManager.Instance.ConnectToPhoton();
                    }
                    
                    PhotonManager.Instance.SyncNicknameWithFirebase();
                }
            }
            else
            {
                uiEmailText.text = "로그인 필요";
                uiNameText.text = "로그인 필요";
            }
        }

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
        #endregion

        #region Coroutines
        private System.Collections.IEnumerator HandlePeerCreatedState()
        {
            yield return new WaitForSeconds(3f);
            
            if (PhotonNetwork.NetworkClientState == ClientState.PeerCreated)
            {
                PhotonNetwork.Disconnect();
                
                yield return new WaitForSeconds(1f);
                
                if (PhotonManager.Instance != null)
                {
                    PhotonManager.Instance.ConnectToPhoton();
                }
            }
        }
        #endregion
    }
}
