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
        [Header("Audio Settings")]
        [SerializeField] private string roomBgmName = "BGM_ParTeaRoom";
        // 애플리케이션 종료 플래그
        private bool isApplicationQuitting = false;
        private bool isGameStarting = false; // 게임 시작 플래그 추가
        
        // 화면 크기 모니터링용
        private Coroutine screenSizeMonitorCoroutine;
        private Vector2 lastScreenSize;

        #region UI References
        // 방 관련 UI
        private Button startButton => GetUI<Button>("StartButton");
        private Button leaveButton => GetUI<Button>("LeaveButton");
        private Button gameLeftButton => GetUI<Button>("GameLeftButton");
        private Button gameRightButton => GetUI<Button>("GameRightButton");
        private Button menuButton => GetUI<Button>("MenuButton");
        private Image gameImage => GetUI<Image>("GameImage");
        private TMP_Text gameNameText => GetUI<TMP_Text>("GameNameText");
        private TMP_Text roomNameText => GetUI<TMP_Text>("RoomNameContent"); // 방 이름 표시용
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
            Debug.Log($"[RoomPopUp] OnEnable 호출됨 - isGameStarting: {isGameStarting}");
            InitializePhotonViewID();
            SubscribePhotonEvents();
            InitializeRoom();
            
            // 화면 크기 모니터링 시작
            StartScreenSizeMonitoring();
            
            // 게임에서 돌아온 상황이 아니라면 BGM 시작
            if (!isGameStarting)
            {
                StartCoroutine(StartRoomBGMWithDelay());
            }
            else
            {
                Debug.Log("[RoomPopUp] 게임에서 돌아온 상황이므로 OnEnable에서 BGM 시작을 건너뜁니다.");
            }
        }

        // 지연된 BGM 시작 (오디오 매니저 생성 대기)
        private IEnumerator StartRoomBGMWithDelay()
        {
            yield return new WaitForSeconds(0.1f);
            if (Manager.Audio != null)
            {
                StartRoomBGM();
            }
        }

        // 방 BGM 시작
        public void StartRoomBGM()
        {
          
            if (Manager.Audio == null)
            {
                Debug.LogWarning("[RoomPopUp] AudioManager가 null입니다. BGM 시작을 건너뜁니다.");
                return;
            }

            if (!string.IsNullOrEmpty(roomBgmName))
            {
                Debug.Log($"[RoomPopUp] BGM 재생 시도: {roomBgmName}");
                Manager.Audio.BgmPlay(roomBgmName, 0f);
            }
            else
            {
                Debug.LogWarning("[RoomPopUp] roomBgmName이 null이거나 비어있습니다.");
            }
        }


        private void OnDisable()
        {
            Debug.Log($"[RoomPopUp] OnDisable 호출됨 - isGameStarting: {isGameStarting}, isApplicationQuitting: {isApplicationQuitting}");
            UnsubscribePhotonEvents();
            
            // 화면 크기 모니터링 정지
            StopScreenSizeMonitoring();

            // 게임 시작 중이거나 포톤 룸에 있으면 BGM 변경하지 않음
            if (isGameStarting || PhotonNetwork.InRoom)
            {
                Debug.Log("[RoomPopUp] 게임 시작 중이거나 포톤 룸에 있으므로 BGM 변경을 건너뜁니다.");
                return;
            }

            // MenuPopUp이 열려있는지 확인
            if (UIManager.Instance.FindActivePopUp<MenuPopUp>() != null)
            {
                // MenuPopUp이 열려있으면 BGM 변경하지 않음
                Debug.Log("[RoomPopUp] MenuPopUp이 열려있어 BGM 변경을 건너뜁니다.");
                return;
            }

            // MessagePopUp이 열려있는지 확인
            if (UIManager.Instance.FindActivePopUp<MessagePopUp>() != null)
            {
                // MessagePopUp이 열려있으면 BGM 변경하지 않음
                Debug.Log("[RoomPopUp] MessagePopUp이 열려있어 BGM 변경을 건너뜁니다.");
                return;
            }

            // CheckPopUp이 열려있는지 확인
            if (UIManager.Instance.FindActivePopUp<CheckPopUp>() != null)
            {
                // CheckPopUp이 열려있으면 BGM 변경하지 않음
                Debug.Log("[RoomPopUp] CheckPopUp이 열려있어 BGM 변경을 건너뜁니다.");
                return;
            }

            // 애플리케이션이 종료 중이 아니고 게임오브젝트가 활성화되어 있을 때만 코루틴 시작
            if (!isApplicationQuitting && gameObject.activeInHierarchy)
            {
                // 방에서 나갈 때 BGM을 메인으로 자연스럽게 전환
                StartCoroutine(TransitionToMainBGM());
            }
            else
            {
                // 애플리케이션 종료 중이거나 게임오브젝트가 비활성화된 경우 직접 BGM 변경
                if (Manager.Audio != null)
                {
                    Manager.Audio.BgmPlay("BGM_ParTeaMain", 0f);
                    Debug.Log("[RoomPopUp] 메인 BGM으로 즉시 전환: BGM_ParTeaMain");
                }
            }
        }

        

        // 애플리케이션 종료 시 호출되는 메서드
        private void OnApplicationQuit()
        {
            isApplicationQuitting = true;
            UnsubscribePhotonEvents();
        }

        // 메인 BGM으로 자연스럽게 전환
        private IEnumerator TransitionToMainBGM()
        {
            Debug.Log("[RoomPopUp] TransitionToMainBGM 코루틴 시작");
            if (Manager.Audio != null)
            {
                // 현재 BGM을 페이드 아웃하면서 메인 BGM으로 전환
                Manager.Audio.BgmPlay("BGM_ParTeaMain", 0f); // fadeDuration을 0으로 설정하여 즉시 재생
                Debug.Log("[RoomPopUp] 메인 BGM으로 전환: BGM_ParTeaMain");
            }
            yield return null;
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
            var startButton = GetEventWithSFX("StartButton", "SFX_ButtonClick");
            if (startButton != null) startButton.Click += GameStart;

            var leaveButton = GetEventWithSFX("LeaveButton", "SFX_ButtonClick");
            if (leaveButton != null) leaveButton.Click += LeaveRoom;

            var gameLeftButton = GetEventWithSFX("GameLeftButton", "SFX_ButtonClick");
            if (gameLeftButton != null) gameLeftButton.Click += ClickLeftGameButton;

            var gameRightButton = GetEventWithSFX("GameRightButton", "SFX_ButtonClick");
            if (gameRightButton != null) gameRightButton.Click += ClickRightGameButton;

            var sendChatButton = GetEventWithSFX("SendChatButton", "SFX_ButtonClick");
            if (sendChatButton != null) sendChatButton.Click += SendChatMessage;

            var menuButton = GetEventWithSFX("MenuButton", "SFX_ButtonClick");
            if (menuButton != null) menuButton.Click += OnMenu;

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
            Debug.Log("[RoomPopUp] 방 초기화 시작");

            // 방 이름 업데이트
            UpdateRoomName();

            // 기존 플레이어 패널 정리
            ClearExistingPanels();

            // 현재 방의 모든 플레이어에 대해 패널 생성
            PlayerPanelSpawn();

            // 게임 선택 UI 초기화
            InitializeGameSelection();

            // 채팅 초기화
            InitializeChat();

            Debug.Log("[RoomPopUp] 방 초기화 완료");
        }

        // 방 이름 업데이트
        private void UpdateRoomName()
        {
            if (roomNameText != null && PhotonNetwork.InRoom)
            {
                string roomName = PhotonNetwork.CurrentRoom.Name;
                int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
                int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;

                roomNameText.text = $"{roomName} ({currentPlayers}/{maxPlayers})";
                //Debug.Log($"[RoomPopUp] 방 이름 업데이트: {roomName} ({currentPlayers}/{maxPlayers})");
            }
            else if (roomNameText != null)
            {
                roomNameText.text = "알 수 없음";
                Debug.LogWarning("[RoomPopUp] 방에 입장하지 않은 상태입니다.");
            }
        }

        public void InitializeRoomAfterGame()
        {
            // 게임 시작 플래그 리셋 (게임에서 돌아왔을 때)
            isGameStarting = false;
            Debug.Log("[RoomPopUp] InitializeRoomAfterGame - isGameStarting 플래그 리셋됨");

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
            
            // BGM 재시작 (지연 포함)
            StartCoroutine(StartRoomBGMAfterGameReturn());
        }
        

        
        // 게임에서 돌아왔을 때 BGM 시작 (지연 포함)
        private IEnumerator StartRoomBGMAfterGameReturn()
        {
            Debug.Log("[RoomPopUp] StartRoomBGMAfterGameReturn 코루틴 시작");
            
            // BGM 즉시 중지 (기본 BGM이 재생되는 것을 방지)
            if (Manager.Audio != null)
            {
                Manager.Audio.BgmPlay(null, 0f);
                Debug.Log("[RoomPopUp] BGM 즉시 중지");
            }
            
            yield return new WaitForSeconds(0.1f); // 1초에서 0.1초로 단축
            
            Debug.Log($"[RoomPopUp] AudioManager 상태 확인: {Manager.Audio != null}");
            if (Manager.Audio != null)
            {
                Debug.Log("[RoomPopUp] StartRoomBGM() 호출 직전");
                StartRoomBGM();
                Debug.Log("[RoomPopUp] StartRoomBGM() 호출 완료");
                Debug.Log("[RoomPopUp] 게임에서 돌아온 후 BGM 시작 완료");
            }
            else
            {
                Debug.LogWarning("[RoomPopUp] AudioManager가 null입니다. BGM 시작을 건너뜁니다.");
            }
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


        private void OnMenu(PointerEventData eventData)
        {
            UIManager.Instance.ShowPopUp<MenuPopUp>();
        }


        [PunRPC]
        private void ResetAllPlayersReadyState()
        {
            try
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
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RoomPopUp] ResetAllPlayersReadyState RPC 실행 중 오류 발생: {e.Message}");
            }
        }

        [PunRPC]
        private void StopBGMForAllClients()
        {
            try
            {
                // 게임 시작 시 BGM 중지 (게임별 BGM이 재생될 예정)
                isGameStarting = true; // 모든 클라이언트에서 게임 시작 플래그 설정
                Debug.Log($"[RoomPopUp] 게임 시작 - BGM 중지 (RPC 호출됨) - isGameStarting: {isGameStarting}");
                
                // 모든 클라이언트에서 UI 정리 및 로딩 블로커 표시
                if (UIManager.Instance != null)
                {
                    Debug.Log("[RoomPopUp] UIManager를 통한 UI 정리 시작");
                    UIManager.Instance.CleanAllUI();
                    
                    if (UIManager.Instance.PopUp != null)
                    {
                        UIManager.Instance.PopUp.ShowLoadingBlocker();
                        Debug.Log("[RoomPopUp] 로딩 블로커 표시 완료");
                    }
                    else
                    {
                        Debug.LogWarning("[RoomPopUp] PopUpUI가 null입니다");
                    }
                }
                else
                {
                    Debug.LogWarning("[RoomPopUp] UIManager.Instance가 null입니다");
                }
                
                Debug.Log("[RoomPopUp] 모든 클라이언트에서 UI 정리 및 로딩 블로커 표시 완료");
                
                if (Manager.Audio != null)
                {
                    Manager.Audio.BgmPlay(null, 0.5f); // BGM 중지
                    Debug.Log("[RoomPopUp] 게임 시작 - BGM 중지 완료");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RoomPopUp] StopBGMForAllClients RPC 실행 중 오류 발생: {e.Message}");
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

            // GridLayoutGroup 설정 (2x2 레이아웃)
            SetupGridLayout();

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

        // 2x2 그리드 레이아웃 설정
        private void SetupGridLayout()
        {
            if (playerPanelContent == null) return;

            // 기존 GridLayoutGroup이 있으면 제거
            GridLayoutGroup existingGrid = playerPanelContent.GetComponent<GridLayoutGroup>();
            if (existingGrid != null)
            {
                DestroyImmediate(existingGrid);
            }

            // 새로운 GridLayoutGroup 추가
            GridLayoutGroup gridLayout = playerPanelContent.gameObject.AddComponent<GridLayoutGroup>();
            
            // 2x2 설정
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2; // 2열
            
            // 간격 설정
            gridLayout.spacing = new Vector2(10f, 10f);
            
            // 패딩 설정
            gridLayout.padding = new RectOffset(10, 10, 10, 10);
            
            // 셀 크기 계산 및 설정
            UpdateGridCellSize();
        }

        // 그리드 셀 크기 업데이트
        private void UpdateGridCellSize()
        {
            if (playerPanelContent == null) return;

            GridLayoutGroup gridLayout = playerPanelContent.GetComponent<GridLayoutGroup>();
            if (gridLayout == null) return;

            RectTransform contentRect = playerPanelContent as RectTransform;
            if (contentRect == null) return;

            // 컨테이너 크기에서 패딩과 간격을 고려하여 셀 크기 계산
            float availableWidth = contentRect.rect.width - gridLayout.padding.left - gridLayout.padding.right - gridLayout.spacing.x;
            float availableHeight = contentRect.rect.height - gridLayout.padding.top - gridLayout.padding.bottom - gridLayout.spacing.y;
            
            // 2x2 레이아웃이므로 2로 나눔
            float cellWidth = availableWidth / 2f;
            float cellHeight = availableHeight / 2f;
            
            // 최소 크기 보장
            cellWidth = Mathf.Max(cellWidth, 150f);
            cellHeight = Mathf.Max(cellHeight, 100f);
            
            gridLayout.cellSize = new Vector2(cellWidth, cellHeight);
        }

        // 화면 크기 모니터링 시작
        private void StartScreenSizeMonitoring()
        {
            if (screenSizeMonitorCoroutine != null)
            {
                StopCoroutine(screenSizeMonitorCoroutine);
            }
            
            lastScreenSize = new Vector2(Screen.width, Screen.height);
            screenSizeMonitorCoroutine = StartCoroutine(MonitorScreenSize());
        }

        // 화면 크기 모니터링 정지
        private void StopScreenSizeMonitoring()
        {
            if (screenSizeMonitorCoroutine != null)
            {
                StopCoroutine(screenSizeMonitorCoroutine);
                screenSizeMonitorCoroutine = null;
            }
        }

        // 화면 크기 변경 모니터링 코루틴
        private IEnumerator MonitorScreenSize()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.5f); // 0.5초마다 체크
                
                Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
                
                // 화면 크기가 변경되었으면 그리드 레이아웃 업데이트
                if (currentScreenSize != lastScreenSize)
                {
                    Debug.Log($"[RoomPopUp] 화면 크기 변경 감지: {lastScreenSize} -> {currentScreenSize}");
                    lastScreenSize = currentScreenSize;
                    UpdateGridCellSize();
                }
            }
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
            
            // 그리드 레이아웃이 있으면 셀 크기 업데이트
            UpdateGridCellSize();
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
            // 버튼 비활성화
            if (startButton != null)
            {
                startButton.interactable = false;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                ShowErrorMessage("방장만 게임을 시작할 수 있습니다.");
                // 버튼 다시 활성화
                if (startButton != null)
                {
                    startButton.interactable = true;
                }
                return;
            }

            if (!AllPlayerReadyCheck())
            {
                ShowErrorMessage("모든 플레이어가 Ready 상태이고 색상을 선택해야 합니다.");
                // 버튼 다시 활성화
                if (startButton != null)
                {
                    startButton.interactable = true;
                }
                return;
            }

            if (!CheckPlayerCountRequirement())
            {
                // 버튼 다시 활성화
                if (startButton != null)
                {
                    startButton.interactable = true;
                }
                return;
            }

            // 게임 시작 플래그 설정
            isGameStarting = true;
            Debug.Log("[RoomPopUp] GameStart - isGameStarting 플래그 설정됨");

            InitializeGameStart();

            string sceneName = GetSelectedGameScene();

            Hashtable roomProperty = new Hashtable();
            roomProperty["SelectedGame"] = selectedGameIndex;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);

            // 모든 클라이언트에게 BGM 중지 RPC 호출 (씬 전환 전에 실행)
            if (photonView != null && photonView.ViewID == CHAT_VIEW_ID)
            {
                try
                {
                    photonView.RPC(nameof(StopBGMForAllClients), RpcTarget.All);
                    
                    // UIManager를 통해 게임 씬 로딩 시작
                    UIManager.Instance.StartGameSceneLoad(sceneName, selectedGameIndex);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[RoomPopUp] GameStart RPC 호출 중 오류: {e.Message}");
                    // 오류 발생 시 직접 게임 시작
                    UIManager.Instance.StartGameSceneLoad(sceneName, selectedGameIndex);
                }
            }
            else
            {
                // PhotonView가 없는 경우 직접 게임 시작
                UIManager.Instance.StartGameSceneLoad(sceneName, selectedGameIndex);
            }
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
            try
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
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RoomPopUp] InitializeGameStart 실행 중 오류 발생: {e.Message}");
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
            // 방을 나가기 전에 BGM을 메인으로 전환
            if (Manager.Audio != null)
            {
                Manager.Audio.BgmPlay("BGM_ParTeaMain", 0f); // fadeDuration을 0으로 설정하여 즉시 재생
                Debug.Log("[RoomPopUp] 방 나가기 - 메인 BGM으로 전환");
            }

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

            // 방 이름 업데이트
            UpdateRoomName();

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
                    string imageName;

                    // 릴레이 게임의 경우 특별한 이미지 이름 사용
                    if (selectedGame.sceneName == null)
                    {
                        // 4G 릴레이 또는 6G 릴레이에 따라 다른 이미지 사용
                        if (selectedGameIndex == 6) // 4G 릴레이
                        {
                            imageName = "4G_Relay";
                        }
                        else if (selectedGameIndex == 7) // 6G 릴레이
                        {
                            imageName = "6G_Relay";
                        }
                        else
                        {
                            imageName = "Relay"; // 기본 릴레이 이미지
                        }
                    }
                    else
                    {
                        imageName = selectedGame.sceneName;
                    }

                    // 먼저 지정된 이미지 로드 시도
                    Sprite gameSprite = Resources.Load<Sprite>($"GameImages/{imageName}");

                    // 릴레이 게임이고 이미지가 없으면 기본 Relay 이미지 시도
                    if (gameSprite == null && selectedGame.sceneName == null)
                    {
                        gameSprite = Resources.Load<Sprite>("GameImages/Relay");
                        if (gameSprite != null)
                        {
                            //Debug.Log($"[RoomPopUp] {imageName} 이미지가 없어 기본 Relay 이미지를 사용합니다.");
                        }
                    }

                    if (gameSprite != null)
                    {
                        gameImage.sprite = gameSprite;
                    }
                    else
                    {
                        Debug.LogWarning($"[RoomPopUp] 게임 이미지를 찾을 수 없습니다: GameImages/{imageName}");
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

            PlayerPanelSpawn(newPlayer);
            UpdateGameSelectionUI();

            // 방 이름 업데이트 (플레이어 수 변경)
            UpdateRoomName();
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

            // 방 이름 업데이트 (플레이어 수 변경)
            UpdateRoomName();
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

        // 닉네임 동기화를 위한 메서드 추가
        public void RefreshPlayerNicknames()
        {
            foreach (var kvp in playerPanels)
            {
                Player player = PhotonNetwork.CurrentRoom.GetPlayer(kvp.Key);
                if (player != null && kvp.Value != null)
                {
                    // 닉네임 텍스트 업데이트
                    var nicknameText = kvp.Value.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (nicknameText != null)
                    {
                        // 방장 표시가 있는지 확인하고 유지
                        bool hasMasterText = nicknameText.text.Contains("[방장]");
                        string baseNickname = player.NickName;
                        
                        if (hasMasterText)
                        {
                            nicknameText.text = $"{baseNickname} [방장]";
                        }
                        else
                        {
                            nicknameText.text = baseNickname;
                        }
                    }
                }
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
                    // 마스터 클라이언트 상태만 업데이트하고 Ready 상태는 건드리지 않음
                    kvp.Value.UpdateMasterClientDisplay(player.IsMasterClient);
                }
            }
        }

        private void ShowMasterClientChangeMessage(Player newMasterClient)
        {
            string message = "";
            if (newMasterClient.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                message = " 당신이 새로운 방장이 되었습니다! ";
            }
            else
            {
                message = $" {newMasterClient.NickName}님이 새로운 방장이 되었습니다.";
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
