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

        // 방 상태
        public int selectedGameIndex = 0;
        public Dictionary<int, PlayerPanelItem> playerPanels = new Dictionary<int, PlayerPanelItem>();
        
        // 게임 정보 (6개 게임)
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

        // 모든 클라이언트가 같은 ViewID를 사용하는 PhotonView
        private PhotonView photonView;
        
        // 채팅용 고정 ViewID (모든 클라이언트가 공유)
        private const int CHAT_VIEW_ID = 9999;

        private new void Awake()
        {
            base.Awake();
            canCloseWithESC = false; // ESC로 닫을 수 없음
            
            // 모든 클라이언트가 자체 PhotonView 생성
            photonView = GetComponent<PhotonView>();
            if (photonView == null)
            {
                photonView = gameObject.AddComponent<PhotonView>();
            }
            
            // PhotonView 설정 (RPC만 사용하므로 ObservedComponents는 비워둠)
            if (photonView != null)
            {
                photonView.ObservedComponents = new List<Component>(); // RPC만 사용하므로 비워둠
                photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
                photonView.OwnershipTransfer = OwnershipOption.Takeover;
                
                Debug.Log($"[RoomPopUp] PhotonView 설정 완료 - ViewID: {photonView.ViewID}");
            }
            else
            {
                Debug.LogError("[RoomPopUp] PhotonView 설정 실패!");
            }
            
            // Resources 폴더에서 프리팹 로드
            playerPanelItemPrefab = Resources.Load<GameObject>("UI/PlayerPanelItemPrefab");
            if (playerPanelItemPrefab == null)
            {
                Debug.LogError("[RoomPopUp] Resources/UI/PlayerPanelItemPrefab을 찾을 수 없습니다.");
            }

            // 채팅 텍스트 프리팹도 Resources에서 로드
            chatTextPrefab = Resources.Load<GameObject>("UI/ChatTextPrefab");
            if (chatTextPrefab == null)
            {
                Debug.LogError("[RoomPopUp] Resources/UI/ChatTextPrefab을 찾을 수 없습니다.");
            }

            // 방 관련 이벤트 연결 (null 체크 추가)
            var startButton = GetEvent("StartButton");
            if (startButton != null)
            {
                startButton.Click += GameStart;
            }
            else
            {
                Debug.LogError("[RoomPopUp] StartButton을 찾을 수 없습니다.");
            }

            var leaveButton = GetEvent("LeaveButton");
            if (leaveButton != null)
            {
                leaveButton.Click += LeaveRoom;
            }
            else
            {
                Debug.LogError("[RoomPopUp] LeaveButton을 찾을 수 없습니다.");
            }

            var gameLeftButton = GetEvent("GameLeftButton");
            if (gameLeftButton != null)
            {
                gameLeftButton.Click += ClickLeftGameButton;
            }
            else
            {
                Debug.LogError("[RoomPopUp] GameLeftButton을 찾을 수 없습니다.");
            }

            var gameRightButton = GetEvent("GameRightButton");
            if (gameRightButton != null)
            {
                gameRightButton.Click += ClickRightGameButton;
            }
            else
            {
                Debug.LogError("[RoomPopUp] GameRightButton을 찾을 수 없습니다.");
            }

            // 채팅 이벤트 연결
            var sendChatButton = GetEvent("SendChatButton");
            if (sendChatButton != null)
            {
                sendChatButton.Click += SendChatMessage;
            }
            else
            {
                Debug.LogError("[RoomPopUp] SendChatButton을 찾을 수 없습니다.");
            }
        }

        private void OnEnable()
        {
            Debug.Log($"[RoomPopUp] OnEnable 호출 - 방: {PhotonNetwork.InRoom}, 연결: {PhotonNetwork.IsConnected}");
            
            // PhotonView 초기화 확인 - 고정 ViewID 사용
            if (photonView != null && photonView.ViewID == 0)
            {
                // 고정 ViewID 할당 (모든 클라이언트가 같은 ID 사용)
                photonView.ViewID = CHAT_VIEW_ID;
                Debug.Log($"[RoomPopUp] PhotonView 고정 ViewID 할당: {photonView.ViewID}");
            }
            
            Debug.Log($"[RoomPopUp] PhotonView 최종 상태 - ViewID: {photonView?.ViewID}, IsMine: {photonView?.IsMine}, 마스터: {PhotonNetwork.IsMasterClient}");
            
            // PhotonManager 이벤트 구독
            if (PhotonManager.Instance != null)
            {
                PhotonManager.Instance.OnPlayerEnteredRoomEvent += OnPlayerEnteredRoom;
                PhotonManager.Instance.OnPlayerLeftRoomEvent += OnPlayerLeftRoom;
                PhotonManager.Instance.OnLeftRoomEvent += OnLeftRoom;
                PhotonManager.Instance.OnPlayerPropertiesUpdateEvent += OnPlayerPropertiesUpdate; // 플레이어 속성 업데이트 이벤트 구독
                PhotonManager.Instance.OnMasterClientSwitchedEvent += OnMasterClientSwitched; // 마스터 클라이언트 변경 이벤트 구독
            }

            // RPC 기반 채팅은 별도 이벤트 구독이 필요 없음
            Debug.Log($"[RoomPopUp] RPC 기반 채팅 초기화 완료 - PhotonView ViewID: {photonView?.ViewID}");

            // 방 입장 시 초기화 (이미 방에 있는 플레이어들에 대해서만)
            InitializeRoom();
        }

        private void OnDisable()
        {
            // PhotonManager 이벤트 구독 해제
            if (PhotonManager.Instance != null)
            {
                PhotonManager.Instance.OnPlayerEnteredRoomEvent -= OnPlayerEnteredRoom;
                PhotonManager.Instance.OnPlayerLeftRoomEvent -= OnPlayerLeftRoom;
                PhotonManager.Instance.OnLeftRoomEvent -= OnLeftRoom;
                PhotonManager.Instance.OnPlayerPropertiesUpdateEvent -= OnPlayerPropertiesUpdate; // 이벤트 구독 해제
                PhotonManager.Instance.OnMasterClientSwitchedEvent -= OnMasterClientSwitched; // 마스터 클라이언트 변경 이벤트 구독 해제
            }
            
            // RPC 기반 채팅은 별도 이벤트 구독 해제가 필요 없음
        }

        private void Start()
        {
            // PhotonView 초기화 확인 - 고정 ViewID 사용
            if (photonView != null && photonView.ViewID == 0)
            {
                // 고정 ViewID 할당 (모든 클라이언트가 같은 ID 사용)
                photonView.ViewID = CHAT_VIEW_ID;
                Debug.Log($"[RoomPopUp] Start에서 PhotonView 고정 ViewID 할당: {photonView.ViewID}");
            }
            
            // 채팅 입력 필드 이벤트 연결
            if (chatField != null)
            {
                chatField.onEndEdit.AddListener(HandleChatInput);
                chatField.onSubmit.AddListener(HandleChatInput); // 모바일 전송 버튼 지원
                
                // 모바일 환경을 위한 추가 설정
                #if UNITY_ANDROID || UNITY_IOS
                // 모바일에서 키보드 완료 버튼 텍스트 설정
                if (chatField.textViewport != null)
                {
                    var contentSizeFitter = chatField.textViewport.gameObject.GetComponent<ContentSizeFitter>();
                    if (contentSizeFitter == null)
                    {
                        contentSizeFitter = chatField.textViewport.gameObject.AddComponent<ContentSizeFitter>();
                    }
                    contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
                
                // 모바일 키보드 설정
                chatField.keyboardType = TouchScreenKeyboardType.Default;
                chatField.characterLimit = 100; // 최대 100자
                // TMP_InputField는 hideMobileInput 속성이 없으므로 제거
                #endif
                
                Debug.Log("[RoomPopUp] 채팅 입력 필드 설정 완료");
            }
            else
            {
                Debug.LogError("[RoomPopUp] ChatField를 찾을 수 없습니다!");
            }
            
            Debug.Log("[RoomPopUp] Start 완료");
        }

        // 방 초기화
        private void InitializeRoom()
        {
            Debug.Log("[RoomPopUp] 방 초기화 시작");
            
            // 기존 패널들 정리
            if (playerPanels.Count > 0)
            {
                Debug.Log($"[RoomPopUp] 기존 패널 {playerPanels.Count}개 정리");
                foreach (var kvp in playerPanels)
                {
                    if (kvp.Value != null && kvp.Value.gameObject != null)
                    {
                        Destroy(kvp.Value.gameObject);
                    }
                }
                playerPanels.Clear();
            }

            // 로컬 플레이어 속성 초기화 (방에 처음 입장할 때)
            if (PhotonNetwork.LocalPlayer != null)
            {
                // Ready 상태가 설정되지 않은 경우에만 초기화
                if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Ready"))
                {
                    Hashtable playerProperties = new Hashtable();
                    playerProperties["Ready"] = false;
                    PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
                    Debug.Log("[RoomPopUp] 로컬 플레이어 Ready 상태 초기화");
                }
            }

            // 게임 선택 버튼들의 상태 설정
            UpdateGameSelectionButtonStates();

            // 게임 선택 UI 초기화
            InitializeGameSelection();
            
            // 색상 선택 UI 초기화
            InitializeColorSelection();

            // 플레이어 패널 생성
            PlayerPanelSpawn();
            
            // 채팅 초기화
            InitializeChat();
            
            Debug.Log("[RoomPopUp] 방 초기화 완료");
        }

        // 게임에서 돌아온 후 방 초기화
        public void InitializeRoomAfterGame()
        {
            Debug.Log("[RoomPopUp] 게임 후 방 초기화 시작");
            
            // 마스터 클라이언트가 모든 플레이어의 Ready 상태를 초기화
            if (PhotonNetwork.IsMasterClient)
            {
                // RPC를 통해 모든 클라이언트에게 Ready 상태 초기화 요청
                if (photonView != null && photonView.ViewID == CHAT_VIEW_ID)
                {
                    photonView.RPC(nameof(ResetAllPlayersReadyState), RpcTarget.All);
                    Debug.Log("[RoomPopUp] 모든 플레이어 Ready 상태 초기화 RPC 호출");
                }
            }
            
            // 채팅 초기화
            ClearChat();
            
            // UI 상태 초기화
            if (chatField != null)
            {
                chatField.text = "";
            }
            
            // 게임 선택 버튼들의 상태 업데이트
            UpdateGameSelectionButtonStates();
            
            Debug.Log("[RoomPopUp] 게임 후 방 초기화 완료");
        }

        // 모든 플레이어의 Ready 상태 초기화 (RPC)
        [PunRPC]
        private void ResetAllPlayersReadyState()
        {
            Debug.Log("[RoomPopUp] 모든 플레이어 Ready 상태 초기화 RPC 수신");
            
            // 로컬 플레이어의 Ready 상태 초기화
            if (PhotonNetwork.LocalPlayer != null)
            {
                Hashtable playerProperties = new Hashtable();
                playerProperties["Ready"] = false;
                PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
                Debug.Log("[RoomPopUp] 로컬 플레이어 Ready 상태 초기화");
            }
            
            // 플레이어 패널 상태 초기화
            foreach (var kvp in playerPanels)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.ResetReadyState();
                }
            }
            
            Debug.Log("[RoomPopUp] 모든 플레이어 Ready 상태 초기화 완료");
        }

        // 플레이어 패널 생성
        public void PlayerPanelSpawn(Player player)
        {
            // 이미 패널이 존재하는지 확인
            if (playerPanels.ContainsKey(player.ActorNumber))
            {
                Debug.LogWarning($"[RoomPopUp] 플레이어 패널이 이미 존재합니다: {player.NickName}");
                return;
            }

            GameObject obj = Instantiate(playerPanelItemPrefab);
            obj.transform.SetParent(playerPanelContent, false); // false로 설정하여 로컬 위치 유지
            PlayerPanelItem item = obj.GetComponent<PlayerPanelItem>();
            item.Init(player);
            playerPanels.Add(player.ActorNumber, item);

            // 새로 들어온 플레이어에게 자동으로 색상 할당
            if (player.IsLocal && !player.CustomProperties.ContainsKey("Color"))
            {
                AssignAutoColor(player);
            }

            // UI 레이아웃 강제 업데이트
            Canvas.ForceUpdateCanvases();
            if (playerPanelContent is RectTransform rectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
            
            Debug.Log($"[RoomPopUp] 플레이어 패널 생성 완료: {player.NickName}");
        }
        
        // 자동 색상 할당
        private void AssignAutoColor(Player player)
        {
            // 사용 가능한 색상 찾기
            bool[] usedColors = new bool[4]; // 4가지 색상
            
            // 다른 플레이어들이 사용 중인 색상 체크
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
            
            // 사용 가능한 첫 번째 색상 할당
            for (int i = 0; i < 4; i++)
            {
                if (!usedColors[i])
                {
                    PhotonManager.Instance.SetPlayerColor(i);
                    Debug.Log($"[RoomPopUp] 자동 색상 할당: {player.NickName} -> 색상 {i}");
                    break;
                }
            }
        }

        public void PlayerPanelSpawn()
        {
            PhotonNetwork.AutomaticallySyncScene = true;

            // 모든 플레이어에 대해 패널 생성
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                // 이미 존재하는 패널인지 확인
                if (!playerPanels.ContainsKey(player.ActorNumber))
                {
                    GameObject obj = Instantiate(playerPanelItemPrefab);
                    obj.transform.SetParent(playerPanelContent, false); // false로 설정하여 로컬 위치 유지
                    PlayerPanelItem item = obj.GetComponent<PlayerPanelItem>();
                    item.Init(player);
                    playerPanels.Add(player.ActorNumber, item);
                    
                    // 새로 들어온 플레이어에게 자동으로 색상 할당
                    if (player.IsLocal && !player.CustomProperties.ContainsKey("Color"))
                    {
                        AssignAutoColor(player);
                    }
                    
                    Debug.Log($"[RoomPopUp] 플레이어 패널 생성: {player.NickName}");
                }
            }

            // UI 레이아웃 강제 업데이트
            Canvas.ForceUpdateCanvases();
            if (playerPanelContent is RectTransform rectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
            
            Debug.Log($"[RoomPopUp] 총 {playerPanels.Count}개의 플레이어 패널 생성 완료");
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
                Debug.Log($"[RoomPopUp] 플레이어 패널 제거 완료: {player.NickName}");
            }
            else
            {
                Debug.LogWarning($"[RoomPopUp] 플레이어 패널을 찾을 수 없음: {player.NickName}");
            }
        }

        // 게임 시작
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
            
            // 선택된 게임의 플레이어 수 조건 확인
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
                        return;
                    }
                }
            }
            
            // 게임 시작 전 초기화
            InitializeGameStart();
            
            // 선택된 게임에 따라 씬 이동
            string sceneName = GetSelectedGameScene();
            Debug.Log($"[RoomPopUp] 게임 시작: {sceneName}");
            
            // 방 속성에 선택된 게임 저장
            Hashtable roomProperty = new Hashtable();
            roomProperty["SelectedGame"] = selectedGameIndex;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);
            
            // JTW.GameManager의 GameStart 기능 사용 (씬 이름과 maxGameCount 전달)
            if (Manager.game != null)
            {
                // 4G 릴레이와 6G 릴레이에 따라 maxGameCount 설정
                int maxGameCount = 1; // 기본값
                
                if (selectedGameIndex == 6) // 4G 릴레이
                {
                    maxGameCount = 4;
                    Debug.Log($"[RoomPopUp] 4G 릴레이 시작 - maxGameCount: {maxGameCount}");
                }
                else if (selectedGameIndex == 7) // 6G 릴레이
                {
                    maxGameCount = 6;
                    Debug.Log($"[RoomPopUp] 6G 릴레이 시작 - maxGameCount: {maxGameCount}");
                }
                
                Manager.game.GameStart(sceneName, maxGameCount);
                Debug.Log($"[RoomPopUp] JTW.GameManager.GameStart 호출: {sceneName}, maxGameCount: {maxGameCount}");
            }
            else
            {
                Debug.LogWarning("[RoomPopUp] JTW.Manager.game이 null입니다. 기본 씬 이동을 사용합니다.");
                // 씬 이동
                PhotonNetwork.LoadLevel(sceneName);
            }
            
            UIManager.Instance.CleanAllUI();
        }

        // 게임 시작 시 초기화
        private void InitializeGameStart()
        {
            Debug.Log("[RoomPopUp] 게임 시작 초기화 시작");
            
            // 마스터 클라이언트가 모든 플레이어의 Ready 상태를 초기화
            if (PhotonNetwork.IsMasterClient)
            {
                // RPC를 통해 모든 클라이언트에게 Ready 상태 초기화 요청
                if (photonView != null && photonView.ViewID == CHAT_VIEW_ID)
                {
                    photonView.RPC(nameof(ResetAllPlayersReadyState), RpcTarget.All);
                    Debug.Log("[RoomPopUp] 게임 시작 시 모든 플레이어 Ready 상태 초기화 RPC 호출");
                }
            }
            
            // 채팅 초기화
            ClearChat();
            
            // UI 상태 초기화
            if (chatField != null)
            {
                chatField.text = "";
            }
            
            Debug.Log("[RoomPopUp] 게임 시작 초기화 완료");
        }

        // 모든 플레이어 준비 상태 확인
        public bool AllPlayerReadyCheck()
        {
            Debug.Log($"[RoomPopUp] 플레이어 준비 상태 확인 시작 - 총 {PhotonNetwork.PlayerList.Length}명");
            
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                Debug.Log($"[RoomPopUp] 플레이어 {player.NickName} 상태 확인 중...");
                
                // Ready 상태 확인
                if (!player.CustomProperties.TryGetValue("Ready", out object readyValue) || !(bool)readyValue)
                {
                    Debug.Log($"[RoomPopUp] 플레이어 {player.NickName}이 Ready 상태가 아닙니다. Ready: {readyValue}");
                    return false;
                }
                
                // 색상 선택 확인
                if (!player.CustomProperties.TryGetValue("Color", out object colorValue) || colorValue == null)
                {
                    Debug.Log($"[RoomPopUp] 플레이어 {player.NickName}이 색상을 선택하지 않았습니다. Color: {colorValue}");
                    return false;
                }
                
                Debug.Log($"[RoomPopUp] 플레이어 {player.NickName} - Ready: {readyValue}, Color: {colorValue}");
            }
            
            Debug.Log("[RoomPopUp] 모든 플레이어가 Ready 상태이고 색상을 선택했습니다.");
            return true;
        }

        // 방 나가기
        private void LeaveRoom(PointerEventData eventData)
        {
            // 방장이 나가는 경우, Photon이 자동으로 다음 플레이어에게 방장 권한을 넘김
            // 별도로 권한을 넘길 필요 없음 (Photon이 자동 처리)
            Debug.Log("[RoomPopUp] 방을 나갑니다.");

            // 방 나가기 전 초기화
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

        // 방 나가기 시 초기화
        private void InitializeRoomExit()
        {
            Debug.Log("[RoomPopUp] 방 나가기 초기화 시작");
            
            // 마스터 클라이언트가 모든 플레이어의 Ready 상태를 초기화
            if (PhotonNetwork.IsMasterClient)
            {
                // RPC를 통해 모든 클라이언트에게 Ready 상태 초기화 요청
                if (photonView != null && photonView.ViewID == CHAT_VIEW_ID)
                {
                    photonView.RPC(nameof(ResetAllPlayersReadyState), RpcTarget.All);
                    Debug.Log("[RoomPopUp] 방 나가기 시 모든 플레이어 Ready 상태 초기화 RPC 호출");
                }
            }
            
            // 로컬 플레이어 속성 초기화
            if (PhotonNetwork.LocalPlayer != null)
            {
                Hashtable playerProperties = new Hashtable();
                playerProperties["Ready"] = false;
                playerProperties["Color"] = null;
                playerProperties["SelectedGame"] = null;
                PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
                Debug.Log("[RoomPopUp] 로컬 플레이어 속성 초기화 완료");
            }
            
            // 채팅 초기화
            ClearChat();
            
            // 게임 선택 초기화
            selectedGameIndex = 0;
            
            // UI 상태 초기화
            if (chatField != null)
            {
                chatField.text = "";
            }
            
            Debug.Log("[RoomPopUp] 방 나가기 초기화 완료");
        }



        // 게임 선택 버튼들
        private void ClickLeftGameButton(PointerEventData eventData)
        {
            // 마스터 클라이언트만 게임 변경 가능
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[RoomPopUp] 마스터 클라이언트만 게임을 변경할 수 있습니다.");
                return;
            }

            int oldIndex = selectedGameIndex;
            selectedGameIndex--;
            if (selectedGameIndex == -1)
            {
                selectedGameIndex = availableGames.Length - 1;
            }

            Debug.Log($"[RoomPopUp] 왼쪽 버튼 클릭: 게임 변경 {oldIndex} -> {selectedGameIndex} ({availableGames[selectedGameIndex].gameName})");

            Hashtable roomProperty = new Hashtable();
            roomProperty["SelectedGame"] = selectedGameIndex;
            Debug.Log($"[RoomPopUp] 방 속성 설정 시도: SelectedGame = {selectedGameIndex}");
            bool setResult = PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);
            Debug.Log($"[RoomPopUp] 방 속성 설정 결과: {setResult}");

            // 로비에 있는 클라이언트들에게 게임 변경 알림 (즉시 호출)
            NotifyLobbyGameChange(selectedGameIndex);

            UpdateGameSelectionUI();
        }

        private void ClickRightGameButton(PointerEventData eventData)
        {
            // 마스터 클라이언트만 게임 변경 가능
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[RoomPopUp] 마스터 클라이언트만 게임을 변경할 수 있습니다.");
                return;
            }

            int oldIndex = selectedGameIndex;
            selectedGameIndex++;
            if (selectedGameIndex >= availableGames.Length)
            {
                selectedGameIndex = 0;
            }

            Debug.Log($"[RoomPopUp] 오른쪽 버튼 클릭: 게임 변경 {oldIndex} -> {selectedGameIndex} ({availableGames[selectedGameIndex].gameName})");

            Hashtable roomProperty = new Hashtable();
            roomProperty["SelectedGame"] = selectedGameIndex;
            Debug.Log($"[RoomPopUp] 방 속성 설정 시도: SelectedGame = {selectedGameIndex}");
            bool setResult = PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);
            Debug.Log($"[RoomPopUp] 방 속성 설정 결과: {setResult}");

            // 로비에 있는 클라이언트들에게 게임 변경 알림 (즉시 호출)
            NotifyLobbyGameChange(selectedGameIndex);

            UpdateGameSelectionUI();
        }

        // 로비에 있는 클라이언트들에게 게임 변경 알림 (RPC)
        private void NotifyLobbyGameChange(int newGameIndex)
        {
            Debug.Log($"[RoomPopUp] 게임 변경 완료: {PhotonNetwork.CurrentRoom.Name} -> {newGameIndex}");
            Debug.Log("[RoomPopUp] 방 속성 변경으로 로비의 방 목록이 자동으로 업데이트됩니다.");
            Debug.Log("[RoomPopUp] 로비에 있는 클라이언트들은 OnRoomListUpdate를 통해 변경사항을 감지합니다.");
            
            // 방 속성이 실제로 변경되었는지 확인
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("SelectedGame", out object currentGame))
            {
                Debug.Log($"[RoomPopUp] 방 속성 확인 - SelectedGame: {currentGame}");
            }
            else
            {
                Debug.LogWarning("[RoomPopUp] 방 속성에서 SelectedGame을 찾을 수 없습니다!");
            }
        }

        // 게임 선택 UI 초기화
        private void InitializeGameSelection()
        {
            // 방 속성에서 선택된 게임 가져오기
            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("SelectedGame"))
            {
                selectedGameIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties["SelectedGame"];
            }
            else
            {
                selectedGameIndex = 0; // 기본값
                Debug.LogWarning("[RoomPopUp] 방 속성에서 SelectedGame을 찾을 수 없어 기본값(0) 사용");
                
                // 방장인 경우 기본값을 방 속성에 설정 (방 생성 시 이미 설정되어야 함)
                if (PhotonNetwork.IsMasterClient)
                {
                    Hashtable roomProperty = new Hashtable();
                    roomProperty["SelectedGame"] = selectedGameIndex;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);
                }
            }

            UpdateGameSelectionUI();
        }

        // 게임 선택 UI 업데이트
        public void UpdateGameSelectionUI()
        {
            Debug.Log("[RoomPopUp] 게임 선택 UI 업데이트 시작");
            
            // 방 속성에서 현재 선택된 게임 가져오기
            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("SelectedGame"))
            {
                selectedGameIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties["SelectedGame"];
                Debug.Log($"[RoomPopUp] 방 속성에서 게임 인덱스 가져옴: {selectedGameIndex}");
            }
            else
            {
                Debug.LogWarning("[RoomPopUp] 방 속성에서 SelectedGame을 찾을 수 없습니다.");
            }
            
            if (selectedGameIndex >= 0 && selectedGameIndex < availableGames.Length)
            {
                GameInfo selectedGame = availableGames[selectedGameIndex];
                int currentPlayerCount = PhotonNetwork.PlayerList.Length; // Get current player count

                if (gameNameText != null)
                {
                    // 플레이어 수 조건이 있는 경우 표시
                    if (selectedGame.requiredPlayers != null && selectedGame.requiredPlayers.Count > 0)
                    {
                        string requiredPlayersText = string.Join("/", selectedGame.requiredPlayers);
                        string displayText = $"{selectedGame.gameName} ({requiredPlayersText}명)";

                        // 플레이어 수가 조건과 일치하는지 확인하여 색상 설정
                        if (selectedGame.requiredPlayers.Contains(currentPlayerCount))
                        {
                            gameNameText.color = Color.green; // 조건 만족 시 초록색
                        }
                        else
                        {
                            gameNameText.color = Color.red; // 조건 불만족 시 빨간색
                        }

                        gameNameText.text = displayText;
                        Debug.Log($"[RoomPopUp] 게임 이름 텍스트 업데이트: {displayText} (현재 플레이어: {currentPlayerCount}명)");
                    }
                    else
                    {
                        gameNameText.color = Color.white; // 기본 색상
                        gameNameText.text = selectedGame.gameName;
                        Debug.Log($"[RoomPopUp] 게임 이름 텍스트 업데이트: {selectedGame.gameName}");
                    }
                }
                
                if (gameImage != null)
                {
                    // 게임 이미지가 있다면 설정 (Resources에서 로드)
                    Sprite gameSprite = Resources.Load<Sprite>($"GameImages/{selectedGame.sceneName}");
                    if (gameSprite != null)
                    {
                        gameImage.sprite = gameSprite;
                        Debug.Log($"[RoomPopUp] 게임 이미지 업데이트: {selectedGame.sceneName}");
                    }
                }
                
                Debug.Log($"[RoomPopUp] 선택된 게임: {selectedGame.gameName} ({selectedGame.sceneName})");
            }
            else
            {
                Debug.LogError($"[RoomPopUp] 게임 인덱스가 범위를 벗어남: {selectedGameIndex}");
            }
        }

        // 선택된 게임의 씬 이름 반환
        private string GetSelectedGameScene()
        {
            if (selectedGameIndex >= 0 && selectedGameIndex < availableGames.Length)
            {
                return availableGames[selectedGameIndex].sceneName;
            }
            return "TetrisScene"; // 기본값
        }

        // 채팅 관련 메서드들
        private void HandleChatInput(string text)
        {
            // 모바일과 PC 모두 지원하도록 수정
            // onEndEdit는 Enter 키나 모바일 키보드의 전송 버튼을 눌렀을 때 호출됨
            if (!string.IsNullOrWhiteSpace(text))
            {
                Debug.Log($"[RoomPopUp] 채팅 입력 감지: {text}");
                SendChatMessage(null);
            }
        }

        // 모바일에서 키보드 상태 변경 감지
        private void Update()
        {
            // 모바일에서 키보드가 열려있을 때 Enter 키 감지
            #if UNITY_ANDROID || UNITY_IOS
            if (chatField != null && chatField.isFocused && TouchScreenKeyboard.visible)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    string text = chatField.text.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        Debug.Log($"[RoomPopUp] 모바일 키보드 Enter 감지: {text}");
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
                Debug.Log($"[RoomPopUp] RPC 채팅 메시지 전송 시도: {message}");
                Debug.Log($"[RoomPopUp] 현재 상태 - 방: {PhotonNetwork.InRoom}, 연결: {PhotonNetwork.IsConnected}, 닉네임: {PhotonNetwork.NickName}");
                
                // PhotonView 상태 확인
                if (photonView == null)
                {
                    Debug.LogError("[RoomPopUp] PhotonView가 null입니다!");
                    DisplayChatMessage(PhotonNetwork.NickName, message);
                    chatField.text = "";
                    return;
                }
                
                Debug.Log($"[RoomPopUp] PhotonView 상태 - ViewID: {photonView.ViewID}, IsMine: {photonView.IsMine}");
                
                if (photonView.ViewID == 0)
                {
                    Debug.LogWarning("[RoomPopUp] PhotonView ViewID가 0입니다. ViewID를 할당합니다.");
                    photonView.ViewID = PhotonNetwork.AllocateViewID(false);
                    Debug.Log($"[RoomPopUp] ViewID 할당 후: {photonView.ViewID}");
                }
                
                // RPC를 통해 모든 클라이언트에게 메시지 전송
                if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID == CHAT_VIEW_ID)
                {
                    Debug.Log($"[RoomPopUp] RPC 호출 시도 - ViewID: {photonView.ViewID}, IsMine: {photonView.IsMine}, 메시지: {message}");
                    // RPC 호출
                    photonView.RPC(nameof(SendChatMessage), RpcTarget.All, PhotonNetwork.NickName, message);
                    Debug.Log($"[RoomPopUp] RPC 채팅 메시지 전송 완료: {message}");
                }
                else
                {
                    if (!PhotonNetwork.InRoom)
                    {
                        Debug.LogWarning("[RoomPopUp] 방에 입장하지 않았습니다.");
                    }
                    else if (photonView == null)
                    {
                        Debug.LogWarning("[RoomPopUp] PhotonView가 null입니다.");
                    }
                    else if (photonView.ViewID == 0)
                    {
                        Debug.LogWarning("[RoomPopUp] PhotonView ViewID가 0입니다.");
                    }
                    
                    Debug.LogWarning("[RoomPopUp] RPC 호출 조건을 만족하지 않아 로컬에서만 표시합니다.");
                    // 로컬에서만 표시
                    DisplayChatMessage(PhotonNetwork.NickName, message);
                }
                
                chatField.text = "";
                
                // 모바일에서 키보드 숨기기
                #if UNITY_ANDROID || UNITY_IOS
                if (TouchScreenKeyboard.visible)
                {
                    chatField.DeactivateInputField();
                }
                #endif
                
                chatField.ActivateInputField();
            }
        }

        // RPC 메서드 제거하고 일반 메서드로 변경
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
        
        // RPC 채팅 메서드
        [PunRPC]
        private void SendChatMessage(string sender, string message)
        {
            Debug.Log($"[RoomPopUp] RPC 채팅 메시지 수신: {sender} -> {message}");
            Debug.Log($"[RoomPopUp] 현재 클라이언트: {PhotonNetwork.NickName}, 방: {PhotonNetwork.CurrentRoom?.Name}");
            DisplayChatMessage(sender, message);
        }
        
        // 채팅 초기화
        private void InitializeChat()
        {
            Debug.Log("[RoomPopUp] RPC 기반 채팅 초기화");
            // RPC 기반 채팅은 별도 초기화가 필요 없음
        }
        
        // 디버그용: 수동 RPC 테스트
        public void TestRPC()
        {
            Debug.Log("[RoomPopUp] 디버그 RPC 테스트 시작");
            Debug.Log($"[RoomPopUp] 현재 상태 - 방: {PhotonNetwork.InRoom}, 연결: {PhotonNetwork.IsConnected}");
            Debug.Log($"[RoomPopUp] PhotonView - ViewID: {photonView?.ViewID}, IsMine: {photonView?.IsMine}");
            
            if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID == CHAT_VIEW_ID)
            {
                photonView.RPC(nameof(SendChatMessage), RpcTarget.All, "디버그", "테스트 메시지입니다!");
                Debug.Log("[RoomPopUp] 디버그 RPC 호출 완료");
            }
            else
            {
                Debug.LogError("[RoomPopUp] 디버그 RPC 호출 실패 - 조건을 만족하지 않음");
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

        // Photon 이벤트 핸들러들
        private void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"플레이어 입장: {newPlayer.NickName}");
            
            // 플레이어 입장 메시지 전송 (RPC)
            if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID == CHAT_VIEW_ID)
            {
                photonView.RPC(nameof(SendChatMessage), RpcTarget.All, "시스템", $"{newPlayer.NickName}님이 방에 입장했습니다.");
            }
            else
            {
                DisplayChatMessage("시스템", $"{newPlayer.NickName}님이 방에 입장했습니다.");
            }
            
            // 이미 패널이 존재하는지 확인
            if (!playerPanels.ContainsKey(newPlayer.ActorNumber))
            {
                PlayerPanelSpawn(newPlayer);
            }
            else
            {
                // 기존 패널 업데이트
                if (playerPanels.TryGetValue(newPlayer.ActorNumber, out PlayerPanelItem panel))
                {
                    panel.Init(newPlayer);
                }
            }
            
            // 게임 선택 UI 업데이트 (플레이어 수 변경으로 인한 조건 표시 업데이트)
            UpdateGameSelectionUI();
        }

        private void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"플레이어 퇴장: {otherPlayer.NickName}");
            
            // 방장이 나간 경우 채팅에 알림 (RPC로 전송)
            if (otherPlayer.IsMasterClient)
            {
                if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID == CHAT_VIEW_ID)
                {
                    photonView.RPC(nameof(SendChatMessage), RpcTarget.All, "시스템", $"{otherPlayer.NickName} 방장이 방을 나갔습니다.");
                }
                else
                {
                    DisplayChatMessage("시스템", $"{otherPlayer.NickName} 방장이 방을 나갔습니다.");
                }
            }
            else
            {
                if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID == CHAT_VIEW_ID)
                {
                    photonView.RPC(nameof(SendChatMessage), RpcTarget.All, "시스템", $"{otherPlayer.NickName}님이 방을 나갔습니다.");
                }
                else
                {
                    DisplayChatMessage("시스템", $"{otherPlayer.NickName}님이 방을 나갔습니다.");
                }
            }
            
            PlayerPanelDestroy(otherPlayer);
            
            // 게임 선택 UI 업데이트 (플레이어 수 변경으로 인한 조건 표시 업데이트)
            UpdateGameSelectionUI();
        }

        private void OnLeftRoom()
        {
            Debug.Log("방을 나갔습니다. 로비로 돌아갑니다.");
            
            // RPC 기반 채팅은 별도 정리가 필요 없음
            ClearChat();

            // 로비로 돌아가기
            UIManager.Instance.ClosePopUp();
            UIManager.Instance.ShowPopUp<LobbyPopUp>();
        }

        // IPunObservable 제거 (PhotonView가 없으므로 불필요)
        // public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        // {
        //     // 동기화가 필요한 데이터가 있다면 여기에 구현
        // }

        // 플레이어 속성 업데이트 이벤트 핸들러
        private void OnPlayerPropertiesUpdate(Player player, Hashtable changedProps)
        {
            if (playerPanels.TryGetValue(player.ActorNumber, out PlayerPanelItem panel))
            {
                panel.UpdatePlayerProperties(player);
            }
        }

        // 마스터 클라이언트 변경 이벤트 핸들러
        private void OnMasterClientSwitched(Player newMasterClient)
        {
            Debug.Log($"[RoomPopUp] 마스터 클라이언트 변경: {newMasterClient.NickName}");
            
            // 게임 선택 버튼들의 상태 업데이트
            UpdateGameSelectionButtonStates();
            
            // 모든 플레이어 패널의 방장 표시 업데이트
            UpdateAllPlayerPanelsMasterClientStatus();
            
            // 방장 변경 알림 메시지 표시
            ShowMasterClientChangeMessage(newMasterClient);
            
            // 채팅에 방장 변경 메시지 추가 (특별한 형식으로)
            string masterChangeMessage = $"{newMasterClient.NickName}님이 새로운 방장이 되었습니다!";
            DisplayChatMessage("시스템", masterChangeMessage);
        }

        // 모든 플레이어 패널의 방장 표시 업데이트
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

        // 방장 변경 알림 메시지 표시
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
            
            // RPC로 시스템 메시지 전송
            if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID == CHAT_VIEW_ID)
            {
                photonView.RPC(nameof(SendChatMessage), RpcTarget.All, "시스템", message);
            }
            else
            {
                DisplayChatMessage("시스템", message);
            }
            
            Debug.Log($"[RoomPopUp] {message}");
        }

        // 게임 선택 버튼들의 상태 업데이트
        private void UpdateGameSelectionButtonStates()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                // 현재 플레이어가 마스터 클라이언트가 된 경우
                if (startButton != null) startButton.interactable = true;
                if (gameLeftButton != null) gameLeftButton.interactable = true;
                if (gameRightButton != null) gameRightButton.interactable = true;
                Debug.Log("[RoomPopUp] 마스터 클라이언트가 되어 게임 선택 버튼 활성화");
            }
            else
            {
                // 현재 플레이어가 마스터 클라이언트가 아닌 경우
                if (startButton != null) startButton.interactable = false;
                if (gameLeftButton != null) gameLeftButton.interactable = false;
                if (gameRightButton != null) gameRightButton.interactable = false;
                Debug.Log("[RoomPopUp] 클라이언트가 되어 게임 선택 버튼 비활성화");
            }
        }


        // 에러 메시지 표시
        private void ShowErrorMessage(string message)
        {
            // 이미 MessagePopUp이 열려있는지 확인
            MessagePopUp existingMessagePopUp = UIManager.Instance.FindActivePopUp<MessagePopUp>();
            if (existingMessagePopUp != null)
            {
                // 기존 메시지 팝업 업데이트
                existingMessagePopUp.SetMessage(message, "확인");
                return;
            }
            
            // 새로운 메시지 팝업 생성
            MessagePopUp messagePopUp = UIManager.Instance.ShowPopUp<MessagePopUp>();
            if (messagePopUp != null)
            {
                messagePopUp.SetMessage(message, "확인");
                Debug.Log($"[RoomPopUp] 에러 메시지 표시: {message}");
            }
            else
            {
                Debug.LogError("[RoomPopUp] MessagePopUp을 생성할 수 없습니다.");
            }
        }

        // 플레이어 색상 선택 메서드
        public void SelectColor(int colorIndex)
        {
            // PhotonManager를 통해 색상 변경
            PhotonManager.Instance.SetPlayerColor(colorIndex);
        }

        // 플레이어 개인 게임 선택 메서드 (개인 설정용)
        public void SelectPlayerGame(int gameIndex)
        {
            // PhotonManager를 통해 개인 게임 선택
            PhotonManager.Instance.SetPlayerSelectedGame(gameIndex);
        }

        // 색상 선택 UI 초기화
        private void InitializeColorSelection()
        {
            // 색상 선택 버튼들 초기화
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


    }
}