using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace KYS
{
    public class RoomPopUp : BaseUI
    {
        #region UI References
        // 방 관련 UI
        private Button startButton => GetUI<Button>("StartButton");
        private Button leaveButton => GetUI<Button>("LeaveButton");
        private Button gameLeftButton => GetUI<Button>("GameLeftButton");
        private Button gameRightButton => GetUI<Button>("GameRightButton");
        private Image gameImage => GetUI<Image>("GameImage");
        private TMP_Text gameNameText => GetUI<TMP_Text>("GameNameText");
        private GameObject playerPanelItemPrefab;
        private Transform playerPanelContent => GetUI<Transform>("PlayerPanelContent");

        // 채팅 관련 UI
        private TMP_InputField chatField => GetUI<TMP_InputField>("ChatField");
        private ScrollRect scrollRect => GetUI<ScrollRect>("ChatView");
        private GameObject chatTextPrefab;
        private Transform chatContent => GetUI<Transform>("ChatContent");
        #endregion

        #region Game Data
        // 방 상태
        public int selectedGameIndex = 0;
        public Dictionary<int, PlayerPanelItem> playerPanels = new Dictionary<int, PlayerPanelItem>();
        
        // 게임 정보 (8개 게임)
        private GameInfo[] availableGames = new GameInfo[]
        {
            new GameInfo("점프", "JumpGame", "점프 게임", new List<int>()),
            new GameInfo("아레나", "ArenaGame", "아레나 게임", new List<int>()),
            new GameInfo("타일", "TileGame", "타일 게임", new List<int> { 2, 4 }),
            new GameInfo("레이싱", "RacingGame", "레이싱 게임", new List<int>()),
            new GameInfo("로프", "RopeGame", "로프 게임", new List<int>()),
            new GameInfo("받기", "ReceiveGame", "물건받기 게임", new List<int>()),
            new GameInfo("4G 릴레이", null, "4개의 랜덤 게임의 릴레이", new List<int>()),
            new GameInfo("6G 릴레이", null, "6개의 랜덤 게임의 릴레이", new List<int>())
        };
        #endregion

        #region Photon Components
        // 모든 클라이언트가 같은 ViewID를 사용하는 PhotonView
        private PhotonView photonView;
        
        // 채팅용 고정 ViewID (모든 클라이언트가 공유)
        private const int CHAT_VIEW_ID = 9999;
        #endregion

        #region Unity Lifecycle
        private new void Awake()
        {
            base.Awake();
            canCloseWithESC = false; // ESC로 닫을 수 없음
            
            InitializePhotonView();
            LoadPrefabs();
            ConnectEvents();
        }

        private void OnEnable()
        {
            InitializePhotonViewID();
            SubscribePhotonEvents();
            InitializeRoom();
        }

        private void OnDisable()
        {
            UnsubscribePhotonEvents();
        }

        private void Start()
        {
            InitializePhotonViewID();
            SetupChatField();
        }
        #endregion

        #region Initialization
        private void InitializePhotonView()
        {
            photonView = GetComponent<PhotonView>();
            if (photonView == null)
            {
                photonView = gameObject.AddComponent<PhotonView>();
            }
            
            if (photonView != null)
            {
                photonView.ObservedComponents = new List<Component>();
                photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
                photonView.OwnershipTransfer = OwnershipOption.Takeover;
            }
        }

        private void InitializePhotonViewID()
        {
            if (photonView != null && photonView.ViewID == 0)
            {
                photonView.ViewID = CHAT_VIEW_ID;
            }
        }

        private void LoadPrefabs()
        {
            playerPanelItemPrefab = Resources.Load<GameObject>("UI/PlayerPanelItemPrefab");
            chatTextPrefab = Resources.Load<GameObject>("UI/ChatTextPrefab");
        }

        private void ConnectEvents()
        {
            var startButton = GetEvent("StartButton");
            if (startButton != null) startButton.Click += GameStart;

            var leaveButton = GetEvent("LeaveButton");
            if (leaveButton != null) leaveButton.Click += LeaveRoom;

            var gameLeftButton = GetEvent("GameLeftButton");
            if (gameLeftButton != null) gameLeftButton.Click += ClickLeftGameButton;

            var gameRightButton = GetEvent("GameRightButton");
            if (gameRightButton != null) gameRightButton.Click += ClickRightGameButton;

            var sendChatButton = GetEvent("SendChatButton");
            if (sendChatButton != null) sendChatButton.Click += SendChatMessage;
        }

        private void SubscribePhotonEvents()
        {
            if (PhotonManager.Instance != null)
            {
                PhotonManager.Instance.OnPlayerEnteredRoomEvent += OnPlayerEnteredRoom;
                PhotonManager.Instance.OnPlayerLeftRoomEvent += OnPlayerLeftRoom;
                PhotonManager.Instance.OnLeftRoomEvent += OnLeftRoom;
                PhotonManager.Instance.OnPlayerPropertiesUpdateEvent += OnPlayerPropertiesUpdate;
                PhotonManager.Instance.OnMasterClientSwitchedEvent += OnMasterClientSwitched;
            }
        }

        private void UnsubscribePhotonEvents()
        {
            if (PhotonManager.Instance != null)
            {
                PhotonManager.Instance.OnPlayerEnteredRoomEvent -= OnPlayerEnteredRoom;
                PhotonManager.Instance.OnPlayerLeftRoomEvent -= OnPlayerLeftRoom;
                PhotonManager.Instance.OnLeftRoomEvent -= OnLeftRoom;
                PhotonManager.Instance.OnPlayerPropertiesUpdateEvent -= OnPlayerPropertiesUpdate;
                PhotonManager.Instance.OnMasterClientSwitchedEvent -= OnMasterClientSwitched;
            }
        }

        private void SetupChatField()
        {
            if (chatField != null)
            {
                chatField.onEndEdit.AddListener(HandleChatInput);
                chatField.onSubmit.AddListener(HandleChatInput);
                
                #if UNITY_ANDROID || UNITY_IOS
                if (chatField.textViewport != null)
                {
                    var contentSizeFitter = chatField.textViewport.gameObject.GetComponent<ContentSizeFitter>();
                    if (contentSizeFitter == null)
                    {
                        contentSizeFitter = chatField.textViewport.gameObject.AddComponent<ContentSizeFitter>();
                    }
                    contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
                
                chatField.keyboardType = TouchScreenKeyboardType.Default;
                chatField.characterLimit = 100;
                #endif
            }
        }
        #endregion

        #region Room Management
        private void InitializeRoom()
        {
            ClearExistingPanels();
            InitializePlayerProperties();
            UpdateGameSelectionButtonStates();
            InitializeGameSelection();
            InitializeColorSelection();
            PlayerPanelSpawn();
            InitializeChat();
        }

        public void InitializeRoomAfterGame()
        {
            if (PhotonNetwork.IsMasterClient && photonView != null && photonView.ViewID == CHAT_VIEW_ID)
            {
                photonView.RPC(nameof(ResetAllPlayersReadyState), RpcTarget.All);
            }
            
            ClearChat();
            
            if (chatField != null)
            {
                chatField.text = "";
            }
            
            UpdateGameSelectionButtonStates();
        }

        private void ClearExistingPanels()
        {
            if (playerPanels.Count > 0)
            {
                foreach (var kvp in playerPanels)
                {
                    if (kvp.Value != null && kvp.Value.gameObject != null)
                    {
                        Destroy(kvp.Value.gameObject);
                    }
                }
                playerPanels.Clear();
            }
        }

        private void InitializePlayerProperties()
        {
            if (PhotonNetwork.LocalPlayer != null && !PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Ready"))
            {
                Hashtable playerProperties = new Hashtable();
                playerProperties["Ready"] = false;
                PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
            }
        }

        [PunRPC]
        private void ResetAllPlayersReadyState()
        {
            if (PhotonNetwork.LocalPlayer != null)
            {
                Hashtable playerProperties = new Hashtable();
                playerProperties["Ready"] = false;
                PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
            }
            
            foreach (var kvp in playerPanels)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.ResetReadyState();
                }
            }
        }
        #endregion

        #region Player Panel Management
        public void PlayerPanelSpawn(Player player)
        {
            if (playerPanels.ContainsKey(player.ActorNumber))
            {
                return;
            }

            GameObject obj = Instantiate(playerPanelItemPrefab);
            obj.transform.SetParent(playerPanelContent, false);
            PlayerPanelItem item = obj.GetComponent<PlayerPanelItem>();
            item.Init(player);
            playerPanels.Add(player.ActorNumber, item);

            if (player.IsLocal && !player.CustomProperties.ContainsKey("Color"))
            {
                AssignAutoColor(player);
            }

            UpdateLayout();
        }

        public void PlayerPanelSpawn()
        {
            PhotonNetwork.AutomaticallySyncScene = true;

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (!playerPanels.ContainsKey(player.ActorNumber))
                {
                    GameObject obj = Instantiate(playerPanelItemPrefab);
                    obj.transform.SetParent(playerPanelContent, false);
                    PlayerPanelItem item = obj.GetComponent<PlayerPanelItem>();
                    item.Init(player);
                    playerPanels.Add(player.ActorNumber, item);
                    
                    if (player.IsLocal && !player.CustomProperties.ContainsKey("Color"))
                    {
                        AssignAutoColor(player);
                    }
                }
            }

            UpdateLayout();
        }

        public void PlayerPanelDestroy(Player player)
        {
            if (playerPanels.TryGetValue(player.ActorNumber, out PlayerPanelItem panel))
            {
                if (panel != null && panel.gameObject != null)
                {
                    Destroy(panel.gameObject);
                }
                playerPanels.Remove(player.ActorNumber);
            }
        }

        private void UpdateLayout()
        {
            Canvas.ForceUpdateCanvases();
            if (playerPanelContent is RectTransform rectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }

        private void AssignAutoColor(Player player)
        {
            bool[] usedColors = new bool[4];
            
            foreach (Player otherPlayer in PhotonNetwork.PlayerList)
            {
                if (otherPlayer != player && otherPlayer.CustomProperties.TryGetValue("Color", out object colorValue))
                {
                    if (colorValue != null)
                    {
                        int colorIndex = (int)colorValue;
                        if (colorIndex >= 0 && colorIndex < 4)
                        {
                            usedColors[colorIndex] = true;
                        }
                    }
                }
            }
            
            for (int i = 0; i < 4; i++)
            {
                if (!usedColors[i])
                {
                    PhotonManager.Instance.SetPlayerColor(i);
                    break;
                }
            }
        }
        #endregion

        #region Game Management
        private void GameStart(PointerEventData eventData)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                ShowErrorMessage("방장만 게임을 시작할 수 있습니다.");
                return;
            }
            
            if (!AllPlayerReadyCheck())
            {
                ShowErrorMessage("모든 플레이어가 Ready 상태이고 색상을 선택해야 합니다.");
                return;
            }
            
            if (!CheckPlayerCountRequirement())
            {
                return;
            }
            
            InitializeGameStart();
            
            string sceneName = GetSelectedGameScene();
            
            Hashtable roomProperty = new Hashtable();
            roomProperty["SelectedGame"] = selectedGameIndex;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);
            
            if (Manager.game != null)
            {
                int maxGameCount = 1;
                
                if (selectedGameIndex == 6) // 4G 릴레이
                {
                    sceneName = null;
                    maxGameCount = 4;
                }
                else if (selectedGameIndex == 7) // 6G 릴레이
                {
                    sceneName = null;
                    maxGameCount = 6;
                }
                
                Manager.game.GameStart(sceneName, maxGameCount);
            }
            else
            {
                PhotonNetwork.LoadLevel(sceneName);
            }
            
            UIManager.Instance.CleanAllUI();
        }

        private bool CheckPlayerCountRequirement()
        {
            if (selectedGameIndex >= 0 && selectedGameIndex < availableGames.Length)
            {
                GameInfo selectedGame = availableGames[selectedGameIndex];
                int currentPlayerCount = PhotonNetwork.PlayerList.Length;

                if (selectedGame.requiredPlayers != null && selectedGame.requiredPlayers.Count > 0)
                {
                    if (!selectedGame.requiredPlayers.Contains(currentPlayerCount))
                    {
                        string requiredPlayersText = string.Join("/", selectedGame.requiredPlayers);
                        ShowErrorMessage($"{selectedGame.gameName}은(는) {requiredPlayersText}명의 플레이어가 필요합니다. (현재: {currentPlayerCount}명)");
                        return false;
                    }
                }
            }
            return true;
        }

        private void InitializeGameStart()
        {
            if (PhotonNetwork.IsMasterClient && photonView != null && photonView.ViewID == CHAT_VIEW_ID)
            {
                photonView.RPC(nameof(ResetAllPlayersReadyState), RpcTarget.All);
            }
            
            ClearChat();
            
            if (chatField != null)
            {
                chatField.text = "";
            }
        }

        public bool AllPlayerReadyCheck()
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (!player.CustomProperties.TryGetValue("Ready", out object readyValue) || !(bool)readyValue)
                {
                    return false;
                }
                
                if (!player.CustomProperties.TryGetValue("Color", out object colorValue) || colorValue == null)
                {
                    return false;
                }
            }
            
            return true;
        }

        private string GetSelectedGameScene()
        {
            if (selectedGameIndex >= 0 && selectedGameIndex < availableGames.Length)
            {
                return availableGames[selectedGameIndex].sceneName;
            }
            return "TetrisScene";
        }
        #endregion

        #region Room Exit
        private void LeaveRoom(PointerEventData eventData)
        {
            InitializeRoomExit();

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (playerPanels.TryGetValue(player.ActorNumber, out PlayerPanelItem panel))
                {
                    Destroy(panel.gameObject);
                }
            }

            playerPanels.Clear();
            PhotonManager.Instance.LeaveRoom();
            UIManager.Instance.CleanPopUp();
            UIManager.Instance.ShowPopUp<LobbyPopUp>();
        }

        private void InitializeRoomExit()
        {
            if (PhotonNetwork.IsMasterClient && photonView != null && photonView.ViewID == CHAT_VIEW_ID)
            {
                photonView.RPC(nameof(ResetAllPlayersReadyState), RpcTarget.All);
            }
            
            if (PhotonNetwork.LocalPlayer != null)
            {
                Hashtable playerProperties = new Hashtable();
                playerProperties["Ready"] = false;
                playerProperties["Color"] = null;
                playerProperties["SelectedGame"] = null;
                PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
            }
            
            ClearChat();
            selectedGameIndex = 0;
            
            if (chatField != null)
            {
                chatField.text = "";
            }
        }
        #endregion

        #region Game Selection
        private void ClickLeftGameButton(PointerEventData eventData)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            int oldIndex = selectedGameIndex;
            selectedGameIndex--;
            if (selectedGameIndex == -1)
            {
                selectedGameIndex = availableGames.Length - 1;
            }

            Hashtable roomProperty = new Hashtable();
            roomProperty["SelectedGame"] = selectedGameIndex;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);

            NotifyLobbyGameChange(selectedGameIndex);
            UpdateGameSelectionUI();
        }

        private void ClickRightGameButton(PointerEventData eventData)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            int oldIndex = selectedGameIndex;
            selectedGameIndex++;
            if (selectedGameIndex >= availableGames.Length)
            {
                selectedGameIndex = 0;
            }

            Hashtable roomProperty = new Hashtable();
            roomProperty["SelectedGame"] = selectedGameIndex;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);

            NotifyLobbyGameChange(selectedGameIndex);
            UpdateGameSelectionUI();
        }

        private void NotifyLobbyGameChange(int newGameIndex)
        {
            // 방 속성이 실제로 변경되었는지 확인
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("SelectedGame", out object currentGame))
            {
                // 로비 업데이트는 자동으로 처리됨
            }
        }

        private void InitializeGameSelection()
        {
            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("SelectedGame"))
            {
                selectedGameIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties["SelectedGame"];
            }
            else
            {
                selectedGameIndex = 0;
                
                if (PhotonNetwork.IsMasterClient)
                {
                    Hashtable roomProperty = new Hashtable();
                    roomProperty["SelectedGame"] = selectedGameIndex;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);
                }
            }

            UpdateGameSelectionUI();
        }

        public void UpdateGameSelectionUI()
        {
            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("SelectedGame"))
            {
                selectedGameIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties["SelectedGame"];
            }
            
            if (selectedGameIndex >= 0 && selectedGameIndex < availableGames.Length)
            {
                GameInfo selectedGame = availableGames[selectedGameIndex];
                int currentPlayerCount = PhotonNetwork.PlayerList.Length;

                if (gameNameText != null)
                {
                    if (selectedGame.requiredPlayers != null && selectedGame.requiredPlayers.Count > 0)
                    {
                        string requiredPlayersText = string.Join("/", selectedGame.requiredPlayers);
                        string displayText = $"{selectedGame.gameName} ({requiredPlayersText}명)";

                        if (selectedGame.requiredPlayers.Contains(currentPlayerCount))
                        {
                            gameNameText.color = Color.green;
                        }
                        else
                        {
                            gameNameText.color = Color.red;
                        }

                        gameNameText.text = displayText;
                    }
                    else
                    {
                        gameNameText.color = Color.black;
                        gameNameText.text = selectedGame.gameName;
                    }
                }
                
                if (gameImage != null)
                {
                    Sprite gameSprite = Resources.Load<Sprite>($"GameImages/{selectedGame.sceneName}");
                    if (gameSprite != null)
                    {
                        gameImage.sprite = gameSprite;
                    }
                }
            }
        }

        private void UpdateGameSelectionButtonStates()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (startButton != null) startButton.interactable = true;
                if (gameLeftButton != null) gameLeftButton.interactable = true;
                if (gameRightButton != null) gameRightButton.interactable = true;
            }
            else
            {
                if (startButton != null) startButton.interactable = false;
                if (gameLeftButton != null) gameLeftButton.interactable = false;
                if (gameRightButton != null) gameRightButton.interactable = false;
            }
        }
        #endregion

        #region Chat System
        private void HandleChatInput(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                SendChatMessage(null);
            }
        }

        private void Update()
        {
            #if UNITY_ANDROID || UNITY_IOS
            if (chatField != null && chatField.isFocused && TouchScreenKeyboard.visible)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    string text = chatField.text.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        SendChatMessage(null);
                    }
                }
            }
            #endif
        }

        private void SendChatMessage(PointerEventData eventData)
        {
            string message = chatField.text.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                if (photonView == null)
                {
                    DisplayChatMessage(PhotonNetwork.NickName, message);
                    chatField.text = "";
                    return;
                }
                
                if (photonView.ViewID == 0)
                {
                    photonView.ViewID = PhotonNetwork.AllocateViewID(false);
                }
                
                if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID == CHAT_VIEW_ID)
                {
                    photonView.RPC(nameof(SendChatMessage), RpcTarget.All, PhotonNetwork.NickName, message);
                }
                else
                {
                    DisplayChatMessage(PhotonNetwork.NickName, message);
                }
                
                chatField.text = "";
                
                #if UNITY_ANDROID || UNITY_IOS
                if (TouchScreenKeyboard.visible)
                {
                    chatField.DeactivateInputField();
                }
                #endif
                
                chatField.ActivateInputField();
            }
        }

        public void DisplayChatMessage(string sender, string message)
        {
            if (chatTextPrefab != null && chatContent != null)
            {
                GameObject item = Instantiate(chatTextPrefab, chatContent);
                item.GetComponent<TextMeshProUGUI>().text = $"{sender} : {message}";
                Canvas.ForceUpdateCanvases();
                if (scrollRect != null)
                {
                    scrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }
        
        [PunRPC]
        private void SendChatMessage(string sender, string message)
        {
            DisplayChatMessage(sender, message);
        }
        
        private void InitializeChat()
        {
            // RPC 기반 채팅은 별도 초기화가 필요 없음
        }
        
        public void TestRPC()
        {
            if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID == CHAT_VIEW_ID)
            {
                photonView.RPC(nameof(SendChatMessage), RpcTarget.All, "디버그", "테스트 메시지입니다!");
            }
        }

        public void ClearChat()
        {
            if (chatContent != null)
            {
                while (chatContent.childCount > 0)
                {
                    Transform child = chatContent.GetChild(0);
                    if (child != null)
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
        }
        #endregion

        #region Photon Event Handlers
        private void OnPlayerEnteredRoom(Player newPlayer)
        {
            // 마스터 클라이언트만 시스템 메시지 전송
            if (PhotonNetwork.IsMasterClient && photonView != null && photonView.ViewID == CHAT_VIEW_ID)
            {
                photonView.RPC(nameof(SendChatMessage), RpcTarget.All, "시스템", $"{newPlayer.NickName}님이 방에 입장했습니다.");
            }
            
            if (!playerPanels.ContainsKey(newPlayer.ActorNumber))
            {
                PlayerPanelSpawn(newPlayer);
            }
            else
            {
                if (playerPanels.TryGetValue(newPlayer.ActorNumber, out PlayerPanelItem panel))
                {
                    panel.Init(newPlayer);
                }
            }
            
            UpdateGameSelectionUI();
        }

        private void OnPlayerLeftRoom(Player otherPlayer)
        {
            // 마스터 클라이언트만 시스템 메시지 전송
            if (PhotonNetwork.IsMasterClient && photonView != null && photonView.ViewID == CHAT_VIEW_ID)
            {
                if (otherPlayer.IsMasterClient)
                {
                    photonView.RPC(nameof(SendChatMessage), RpcTarget.All, "시스템", $"{otherPlayer.NickName} 방장이 방을 나갔습니다.");
                }
                else
                {
                    photonView.RPC(nameof(SendChatMessage), RpcTarget.All, "시스템", $"{otherPlayer.NickName}님이 방을 나갔습니다.");
                }
            }
            
            PlayerPanelDestroy(otherPlayer);
            UpdateGameSelectionUI();
        }

        private void OnLeftRoom()
        {
            ClearChat();
            UIManager.Instance.ClosePopUp();
            UIManager.Instance.ShowPopUp<LobbyPopUp>();
        }

        private void OnPlayerPropertiesUpdate(Player player, Hashtable changedProps)
        {
            if (playerPanels.TryGetValue(player.ActorNumber, out PlayerPanelItem panel))
            {
                panel.UpdatePlayerProperties(player);
            }
        }

        private void OnMasterClientSwitched(Player newMasterClient)
        {
            UpdateGameSelectionButtonStates();
            UpdateAllPlayerPanelsMasterClientStatus();
            ShowMasterClientChangeMessage(newMasterClient);
            
            // 중복 메시지 제거 - ShowMasterClientChangeMessage에서 처리
            // string masterChangeMessage = $"{newMasterClient.NickName}님이 새로운 방장이 되었습니다!";
            // DisplayChatMessage("시스템", masterChangeMessage);
        }

        private void UpdateAllPlayerPanelsMasterClientStatus()
        {
            foreach (var kvp in playerPanels)
            {
                Player player = PhotonNetwork.CurrentRoom.GetPlayer(kvp.Key);
                if (player != null)
                {
                    kvp.Value.UpdatePlayerProperties(player);
                }
            }
        }

        private void ShowMasterClientChangeMessage(Player newMasterClient)
        {
            string message = "";
            if (newMasterClient.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                message = "🎉 당신이 새로운 방장이 되었습니다! 🎉";
            }
            else
            {
                message = $"👑 {newMasterClient.NickName}님이 새로운 방장이 되었습니다.";
            }
            
            // 마스터 클라이언트만 시스템 메시지 전송
            if (PhotonNetwork.IsMasterClient && photonView != null && photonView.ViewID == CHAT_VIEW_ID)
            {
                photonView.RPC(nameof(SendChatMessage), RpcTarget.All, "시스템", message);
            }
        }
        #endregion

        #region UI Utilities
        private void ShowErrorMessage(string message)
        {
            MessagePopUp existingMessagePopUp = UIManager.Instance.FindActivePopUp<MessagePopUp>();
            if (existingMessagePopUp != null)
            {
                existingMessagePopUp.SetMessage(message, "확인");
                return;
            }
            
            MessagePopUp messagePopUp = UIManager.Instance.ShowPopUp<MessagePopUp>();
            if (messagePopUp != null)
            {
                messagePopUp.SetMessage(message, "확인");
            }
        }

        public void SelectColor(int colorIndex)
        {
            PhotonManager.Instance.SetPlayerColor(colorIndex);
        }

        public void SelectPlayerGame(int gameIndex)
        {
            PhotonManager.Instance.SetPlayerSelectedGame(gameIndex);
        }

        private void InitializeColorSelection()
        {
            for (int i = 0; i < 4; i++)
            {
                var colorButton = GetUI<Button>($"ColorButton_{i}");
                if (colorButton != null)
                {
                    int colorIndex = i;
                    colorButton.onClick.AddListener(() => SelectColor(colorIndex));
                }
            }
        }
        #endregion
    }
}
