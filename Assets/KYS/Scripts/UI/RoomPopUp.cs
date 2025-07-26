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
        private Button mapLeftButton => GetUI<Button>("MapLeftButton");
        private Button mapRightButton => GetUI<Button>("MapRightButton");
        private Image mapImage => GetUI<Image>("MapImage");
        private GameObject playerPanelItemPrefab;
        private Transform playerPanelContent => GetUI<Transform>("PlayerPanelContent");

        // 채팅 관련 UI
        private TMP_InputField chatField => GetUI<TMP_InputField>("ChatField");
        private ScrollRect scrollRect => GetUI<ScrollRect>("ChatView");
        private GameObject chatTextPrefab;
        private Transform chatContent => GetUI<Transform>("ChatContent");

        // 방 상태
        public int mapIndex;
        public Dictionary<int, PlayerPanelItem> playerPanels = new Dictionary<int, PlayerPanelItem>();

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

            var mapLeftButton = GetEvent("MapLeftButton");
            if (mapLeftButton != null)
            {
                mapLeftButton.Click += ClickLeftMapButton;
            }
            else
            {
                Debug.LogError("[RoomPopUp] MapLeftButton을 찾을 수 없습니다.");
            }

            var mapRightButton = GetEvent("MapRightButton");
            if (mapRightButton != null)
            {
                mapRightButton.Click += ClickRightMapButton;
            }
            else
            {
                Debug.LogError("[RoomPopUp] MapRightButton을 찾을 수 없습니다.");
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
            }

            // 방 입장 시 초기화
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
            // 마스터 클라이언트가 아니면 일부 버튼 비활성화
            if (!PhotonNetwork.IsMasterClient)
            {
                startButton.interactable = false;
                mapLeftButton.interactable = false;
                mapRightButton.interactable = false;
            }

            // 맵 변경
            MapChange();

            // 플레이어 패널 생성
            PlayerPanelSpawn();
        }

        // 플레이어 패널 생성
        public void PlayerPanelSpawn(Player player)
        {
            if (playerPanels.TryGetValue(player.ActorNumber, out PlayerPanelItem panel))
            {
                startButton.interactable = true;
                mapLeftButton.interactable = true;
                mapRightButton.interactable = true;
                panel.Init(player);
                return;
            }

            GameObject obj = Instantiate(playerPanelItemPrefab);
            obj.transform.SetParent(playerPanelContent, false); // false로 설정하여 로컬 위치 유지
            PlayerPanelItem item = obj.GetComponent<PlayerPanelItem>();
            item.Init(player);
            playerPanels.Add(player.ActorNumber, item);

            // UI 레이아웃 강제 업데이트
            Canvas.ForceUpdateCanvases();
            if (playerPanelContent is RectTransform rectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }

        public void PlayerPanelSpawn()
        {
            PhotonNetwork.AutomaticallySyncScene = true;

            if (!PhotonNetwork.IsMasterClient)
            {
                startButton.interactable = false;
                mapLeftButton.interactable = false;
                mapRightButton.interactable = false;
                MapChange();
            }

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                GameObject obj = Instantiate(playerPanelItemPrefab);
                obj.transform.SetParent(playerPanelContent, false); // false로 설정하여 로컬 위치 유지
                PlayerPanelItem item = obj.GetComponent<PlayerPanelItem>();
                item.Init(player);
                playerPanels.Add(player.ActorNumber, item);
            }

            // UI 레이아웃 강제 업데이트
            Canvas.ForceUpdateCanvases();
            if (playerPanelContent is RectTransform rectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }

        public void PlayerPanelDestroy(Player player)
        {
            if (playerPanels.TryGetValue(player.ActorNumber, out PlayerPanelItem panel))
            {
                Destroy(panel.gameObject);
                playerPanels.Remove(player.ActorNumber);
            }
            else
            {
                Debug.LogError("플레이어 패널을 찾을 수 없음");
            }
        }

        // 게임 시작
        private void GameStart(PointerEventData eventData)
        {
            if (PhotonNetwork.IsMasterClient && AllPlayerReadyCheck())
            {
                Manager.game.GameStart("GameScene");
                UIManager.Instance.CleanAllUI();
            }
        }

        // 모든 플레이어 준비 상태 확인
        public bool AllPlayerReadyCheck()
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (!player.CustomProperties.TryGetValue("Ready", out object value) || !(bool)value)
                    return false;
            }
            return true;
        }

        // 방 나가기
        private void LeaveRoom(PointerEventData eventData)
        {
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

        // 맵 변경 버튼들
        private void ClickLeftMapButton(PointerEventData eventData)
        {
            mapIndex--;
            if (mapIndex == -1)
            {
                mapIndex = 2; // 맵 개수에 따라 조정
            }

            Hashtable roomProperty = new Hashtable();
            roomProperty["Map"] = mapIndex;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);

            MapChange();
        }

        private void ClickRightMapButton(PointerEventData eventData)
        {
            mapIndex++;
            if (mapIndex == 3) // 맵 개수에 따라 조정
            {
                mapIndex = 0;
            }

            Hashtable roomProperty = new Hashtable();
            roomProperty["Map"] = mapIndex;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);

            MapChange();
        }

        public void MapChange()
        {
            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("Map"))
            {
                mapIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties["Map"];
                Debug.Log($"맵 인덱스: {mapIndex}");
                //mapImage.sprite = mapSprites[mapIndex]; // 맵 스프라이트 배열 필요
            }
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
                PhotonManager.Instance.GetComponent<PhotonView>().RPC("SendChatMessage", RpcTarget.All, PhotonNetwork.NickName, message);
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
            PlayerPanelSpawn(newPlayer);
        }

        private void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"플레이어 퇴장: {otherPlayer.NickName}");
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


        // 에러 메시지 표시
        private void ShowErrorMessage(string message)
        {
            MessagePopUp messagePopUp = UIManager.Instance.ShowPopUp<MessagePopUp>();
            if (messagePopUp != null)
            {
                messagePopUp.SetMessage(message, "확인");
            }
        }

        // 색상 선택 메서드 (예시)
        public void SelectColor(int colorIndex)
        {
            // PhotonManager를 통해 색상 변경
            PhotonManager.Instance.SetPlayerColor(colorIndex);
        }

        // 게임 선택 메서드 (예시)
        public void SelectGame(int gameIndex)
        {
            // PhotonManager를 통해 게임 선택
            PhotonManager.Instance.SetPlayerSelectedGame(gameIndex);
        }

        // 색상 선택 UI 초기화 (예시)
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

        // 게임 선택 UI 초기화 (예시)
        private void InitializeGameSelection()
        {
            // 게임 선택 버튼들 초기화
            for (int i = 0; i < 3; i++)
            {
                var gameButton = GetUI<Button>($"GameButton_{i}");
                if (gameButton != null)
                {
                    int gameIndex = i;
                    gameButton.onClick.AddListener(() => SelectGame(gameIndex));
                }
            }
        }
    }
}