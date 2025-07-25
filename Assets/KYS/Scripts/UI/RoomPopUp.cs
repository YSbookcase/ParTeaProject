using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace KYS
{
    public class RoomPopUp : BaseUI, IPunObservable
    {
        // 방 관련 UI
        private Button startButton => GetUI<Button>("StartButton");
        private Button leaveButton => GetUI<Button>("LeaveButton");
        private Button mapLeftButton => GetUI<Button>("MapLeftButton");
        private Button mapRightButton => GetUI<Button>("MapRightButton");
        private Image mapImage => GetUI<Image>("MapImage");
        private GameObject playerPanelItemPrefab => GetUI("PlayerPanelItemPrefab");
        private Transform playerPanelContent => GetUI<Transform>("PlayerPanelContent");

        // 채팅 관련 UI
        private TMP_InputField chatField => GetUI<TMP_InputField>("ChatField");
        private ScrollRect scrollRect => GetUI<ScrollRect>("ScrollRect");
        private GameObject chatTextPrefab => GetUI("ChatTextPrefab");
        private Transform chatContent => GetUI<Transform>("ChatContent");

        // 방 상태
        public int mapIndex;
        public Dictionary<int, PlayerPanelItem> playerPanels = new Dictionary<int, PlayerPanelItem>();
        
        // PhotonView 컴포넌트
        private PhotonView photonView;

        private new void Awake()
        {
            base.Awake();

            // PhotonView 설정
            photonView = GetComponent<PhotonView>();
            if (photonView == null)
            {
                photonView = gameObject.AddComponent<PhotonView>();
            }

            // 방 관련 이벤트 연결
            GetEvent("StartButton").Click += GameStart;
            GetEvent("LeaveButton").Click += LeaveRoom;
            GetEvent("MapLeftButton").Click += ClickLeftMapButton;
            GetEvent("MapRightButton").Click += ClickRightMapButton;

            // 채팅 이벤트 연결
            GetEvent("SendChatButton").Click += SendChatMessage;
        }

        private void OnEnable()
        {
            // PhotonManager 이벤트 구독
            if (PhotonManager.Instance != null)
            {
                PhotonManager.Instance.OnPlayerEnteredRoomEvent += OnPlayerEnteredRoom;
                PhotonManager.Instance.OnPlayerLeftRoomEvent += OnPlayerLeftRoom;
                PhotonManager.Instance.OnLeftRoomEvent += OnLeftRoom;
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
            obj.transform.SetParent(playerPanelContent);
            PlayerPanelItem item = obj.GetComponent<PlayerPanelItem>();
            item.Init(player);
            playerPanels.Add(player.ActorNumber, item);
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
                obj.transform.SetParent(playerPanelContent);
                PlayerPanelItem item = obj.GetComponent<PlayerPanelItem>();
                item.Init(player);
                playerPanels.Add(player.ActorNumber, item);
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
                PhotonNetwork.LoadLevel("GameScene");
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
                // mapImage.sprite = mapSprites[mapIndex]; // 맵 스프라이트 배열 필요
            }
        }

        // 채팅 관련 메서드들
        private void HandleChatInput(string text)
        {
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
                // RPC를 통해 채팅 메시지 전송
                photonView.RPC("SendMessage", RpcTarget.All, PhotonNetwork.NickName, message);
                chatField.text = "";
                chatField.ActivateInputField();
            }
        }

        [PunRPC]
        private void SendMessage(string sender, string message)
        {
            GameObject item = Instantiate(chatTextPrefab, chatContent);
            item.GetComponent<TextMeshProUGUI>().text = $"{sender} : {message}";
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
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

        // IPunObservable 구현 (필수)
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            // 동기화가 필요한 데이터가 있다면 여기에 구현
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
    }
} 