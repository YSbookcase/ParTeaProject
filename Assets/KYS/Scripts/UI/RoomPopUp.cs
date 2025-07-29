using Photon.Pun;
using Photon.Realtime;
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
            new GameInfo("점프", "JumpGame", "점프 게임"),
            new GameInfo("아레나", "ArenaGame", "아레나 게임"),
            new GameInfo("타일", "TileGame", "타일 게임"),
            new GameInfo("레이싱", "RacingGame", "레이싱 게임"),
            new GameInfo("로프", "RopeGame", "로프 게임"),
            new GameInfo("받기", "ReceiveGame", "물건받기 게임")
        };

        // PhotonView 컴포넌트
        // private PhotonView photonView; // 삭제

        private new void Awake()
        {
            base.Awake();
            canCloseWithESC = false; // ESC로 닫을 수 없음
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

            // PhotonView 설정 제거
            // photonView = GetComponent<PhotonView>();
            // if (photonView == null)
            // {
            //     photonView = gameObject.AddComponent<PhotonView>();
            // }

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
            // PhotonManager 이벤트 구독
            if (PhotonManager.Instance != null)
            {
                PhotonManager.Instance.OnPlayerEnteredRoomEvent += OnPlayerEnteredRoom;
                PhotonManager.Instance.OnPlayerLeftRoomEvent += OnPlayerLeftRoom;
                PhotonManager.Instance.OnLeftRoomEvent += OnLeftRoom;
                PhotonManager.Instance.OnPlayerPropertiesUpdateEvent += OnPlayerPropertiesUpdate; // 플레이어 속성 업데이트 이벤트 구독
                PhotonManager.Instance.OnMasterClientSwitchedEvent += OnMasterClientSwitched; // 마스터 클라이언트 변경 이벤트 구독
            }

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
        }

        private void Start()
        {
            // 채팅 입력 필드 이벤트 연결
            if (chatField != null)
            {
                chatField.onEndEdit.AddListener(HandleChatInput);
            }
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

            // 게임 선택 버튼들의 상태 설정
            UpdateGameSelectionButtonStates();

            // 게임 선택 UI 초기화
            InitializeGameSelection();
            
            // 색상 선택 UI 초기화
            InitializeColorSelection();

            // 플레이어 패널 생성
            PlayerPanelSpawn();
            
            Debug.Log("[RoomPopUp] 방 초기화 완료");
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
            
            // 선택된 게임에 따라 씬 이동
            string sceneName = GetSelectedGameScene();
            Debug.Log($"[RoomPopUp] 게임 시작: {sceneName}");
            
            // 방 속성에 선택된 게임 저장
            Hashtable roomProperty = new Hashtable();
            roomProperty["SelectedGame"] = selectedGameIndex;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);
            
            // JTW.GameManager의 GameStart 기능 사용 (씬 이름 전달)
            if (Manager.game != null)
            {
                Manager.game.GameStart(sceneName);
                Debug.Log($"[RoomPopUp] JTW.GameManager.GameStart 호출: {sceneName}");
            }
            else
            {
                Debug.LogWarning("[RoomPopUp] JTW.Manager.game이 null입니다. 기본 씬 이동을 사용합니다.");
                // 씬 이동
                PhotonNetwork.LoadLevel(sceneName);
            }
            
            UIManager.Instance.CleanAllUI();
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



        // 게임 선택 버튼들
        private void ClickLeftGameButton(PointerEventData eventData)
        {
            // 마스터 클라이언트만 게임 변경 가능
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[RoomPopUp] 마스터 클라이언트만 게임을 변경할 수 있습니다.");
                return;
            }

            selectedGameIndex--;
            if (selectedGameIndex == -1)
            {
                selectedGameIndex = availableGames.Length - 1;
            }

            Hashtable roomProperty = new Hashtable();
            roomProperty["SelectedGame"] = selectedGameIndex;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);

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

            selectedGameIndex++;
            if (selectedGameIndex >= availableGames.Length)
            {
                selectedGameIndex = 0;
            }

            Hashtable roomProperty = new Hashtable();
            roomProperty["SelectedGame"] = selectedGameIndex;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);

            UpdateGameSelectionUI();
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
            }

            UpdateGameSelectionUI();
        }

        // 게임 선택 UI 업데이트
        public void UpdateGameSelectionUI()
        {
            // 방 속성에서 현재 선택된 게임 가져오기
            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("SelectedGame"))
            {
                selectedGameIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties["SelectedGame"];
            }
            
            if (selectedGameIndex >= 0 && selectedGameIndex < availableGames.Length)
            {
                GameInfo selectedGame = availableGames[selectedGameIndex];
                
                if (gameNameText != null)
                {
                    gameNameText.text = selectedGame.gameName;
                }
                
                if (gameImage != null)
                {
                    // 게임 이미지가 있다면 설정 (Resources에서 로드)
                    Sprite gameSprite = Resources.Load<Sprite>($"GameImages/{selectedGame.sceneName}");
                    if (gameSprite != null)
                    {
                        gameImage.sprite = gameSprite;
                    }
                }
                
                Debug.Log($"[RoomPopUp] 선택된 게임: {selectedGame.gameName} ({selectedGame.sceneName})");
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
            // onEndEdit는 포커스가 벗어날 때 호출되므로 Enter 키가 아닌 경우 무시
            if (!Input.GetKeyDown(KeyCode.Return))
                return;

            if (!string.IsNullOrWhiteSpace(text))
            {
                SendChatMessage(null);
            }
        }

        private void SendChatMessage(PointerEventData eventData)
        {
            string message = chatField.text.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                // PhotonManager의 PhotonView 사용
                PhotonManager.Instance.GetComponent<PhotonView>().RPC(nameof(PhotonManager.SendChatMessage), RpcTarget.All, PhotonNetwork.NickName, message);
                chatField.text = "";
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
        }

        private void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"플레이어 퇴장: {otherPlayer.NickName}");
            
            // 방장이 나간 경우 채팅에 알림
            if (otherPlayer.IsMasterClient)
            {
                DisplayChatMessage("시스템", $"{otherPlayer.NickName} 방장이 방을 나갔습니다.");
            }
            else
            {
                DisplayChatMessage("시스템", $"{otherPlayer.NickName}님이 방을 나갔습니다.");
            }
            
            PlayerPanelDestroy(otherPlayer);
        }

        private void OnLeftRoom()
        {
            Debug.Log("방을 나갔습니다. 로비로 돌아갑니다.");
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
            string masterChangeMessage = $"👑 {newMasterClient.NickName}님이 새로운 방장이 되었습니다! 👑";
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
            
            // 간단한 알림 메시지 표시 (선택사항)
            Debug.Log($"[RoomPopUp] {message}");
            
            // 방장 변경 시 특별한 채팅 메시지 색상으로 표시 (선택사항)
            // DisplayChatMessage("시스템", message);
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