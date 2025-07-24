using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace KYS
{
    public class PhotonManager : MonoBehaviourPunCallbacks
    {
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private TextMeshProUGUI stateText;

        [Header("닉네임 관련")][SerializeField] private GameObject nicknamePanel;
        [SerializeField] private TMP_InputField nicknameField;
        [SerializeField] private Button nicknameAdmitButton;

        [Header("로비 관련")][SerializeField] private GameObject lobbyPanel;
        [SerializeField] private TMP_InputField roomNameField;
        [SerializeField] private Button roomNameAdmitButton;
        [SerializeField] private GameObject roomListItemPrefabs;
        [SerializeField] private Transform roomListContent;
        private Dictionary<string, GameObject> roomListItems = new Dictionary<string, GameObject>();

        [SerializeField] private RoomManager roomManager;
        [SerializeField] private ChatManager chatManager;

        private PhotonView _photonView;

        void Start()
        {
            // PhotonView 컴포넌트 가져오기
            _photonView = GetComponent<PhotonView>();
            if (_photonView == null)
            {
                _photonView = gameObject.AddComponent<PhotonView>();
            }

            // UI 요소들이 제대로 설정되어 있는지 확인
            if (roomListItemPrefabs == null)
                Debug.LogError("roomListItemPrefabs가 설정되지 않았습니다!");
            if (roomListContent == null)
                Debug.LogError("roomListContent가 설정되지 않았습니다!");
            if (lobbyPanel == null)
                Debug.LogError("lobbyPanel이 설정되지 않았습니다!");

            PhotonNetwork.ConnectUsingSettings();
            nicknameAdmitButton.onClick.AddListener(NicknameAdmit);
            roomNameAdmitButton.onClick.AddListener(CreateRoom);
        }

        private void Update()
        {
            stateText.text = $"Current State : {PhotonNetwork.NetworkClientState}";
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("마스터 연결");
            if (loadingPanel.activeSelf)
            {
                loadingPanel.SetActive(false);
            }
            else
            {
                PhotonNetwork.JoinLobby();
            }
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);
            PhotonNetwork.ConnectUsingSettings();
        }

        public void NicknameAdmit()
        {
            if (string.IsNullOrWhiteSpace(nicknameField.text))
            {
                Debug.LogError("닉네임 입력값 없음");
                return;
            }

            PhotonNetwork.NickName = nicknameField.text;
            PhotonNetwork.JoinLobby();
        }

        public override void OnJoinedLobby()
        {
            base.OnJoinedLobby();
            nicknamePanel.SetActive(false);
            lobbyPanel.SetActive(true);
            Debug.Log("로비 참가");

            // 로비에 참가했을 때 현재 방 목록 요청
            Debug.Log($"현재 로비 상태: {PhotonNetwork.InLobby}");
            Debug.Log($"현재 연결된 지역: {PhotonNetwork.CloudRegion}");


        }



        public void CreateRoom()
        {
            if (string.IsNullOrEmpty(roomNameField.text))
            {
                Debug.LogError("방 이름 입력 없음");
                return;
            }
            roomNameAdmitButton.interactable = false;

            RoomOptions options = new RoomOptions
            {
                MaxPlayers = 8,
                IsVisible = true,
                IsOpen = true
            };
            options.CustomRoomPropertiesForLobby = new string[] { "Map" };
            PhotonNetwork.CreateRoom(roomNameField.text, options);
            roomNameField.text = null;
        }

        public override void OnCreatedRoom()
        {
            roomNameAdmitButton.interactable = true;
            ExitGames.Client.Photon.Hashtable roomProperty = new Hashtable();
            roomProperty["Map"] = 0;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);
            Debug.Log($"방 생성 완료: {PhotonNetwork.CurrentRoom.Name}");
            Debug.Log($"방 보임 상태: {PhotonNetwork.CurrentRoom.IsVisible}");
            Debug.Log($"방 열림 상태: {PhotonNetwork.CurrentRoom.IsOpen}");
        }


        public override void OnJoinedRoom()
        {
            lobbyPanel.SetActive(false);
            roomManager.PlayerPanelSpawn();


            Debug.Log($"[NetManager] 방 참가 완료: {PhotonNetwork.CurrentRoom.Name}");
            Debug.Log($"[NetManager] 현재 로비 상태: {PhotonNetwork.InLobby}");
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (newPlayer != PhotonNetwork.LocalPlayer)
            {
                roomManager.PlayerPanelSpawn(newPlayer);

            }
        }



        public override void OnLeftRoom()
        {
            if (chatManager != null)
            {
                chatManager.ClearChat();
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (otherPlayer != PhotonNetwork.LocalPlayer)
            {
                roomManager.PlayerPanelDestory(otherPlayer);
            }
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            Debug.Log($"방 목록 업데이트: {roomList.Count}개의 방");

            // roomListContent가 null인지 확인
            if (roomListContent == null)
            {
                Debug.LogError("roomListContent가 null입니다!");
                return;
            }

            foreach (RoomInfo info in roomList)
            {
                if (info.RemovedFromList)
                {
                    Debug.Log($"방 제거됨: {info.Name}");
                    if (roomListItems.TryGetValue(info.Name, out GameObject obj))
                    {
                        Destroy(obj);
                        roomListItems.Remove(info.Name);
                    }
                    continue;
                }

                Debug.Log($"방 정보: {info.Name}, 플레이어: {info.PlayerCount}/{info.MaxPlayers}, 보임: {info.IsVisible}, 열림: {info.IsOpen}");

                if (roomListItems.ContainsKey(info.Name))
                {
                    roomListItems[info.Name].GetComponent<RoomListItem>().Init(info);
                }
                else
                {
                    GameObject roomListItem = Instantiate(roomListItemPrefabs);
                    roomListItem.transform.SetParent(roomListContent, false);
                    roomListItem.GetComponent<RoomListItem>().Init(info);
                    roomListItems.Add(info.Name, roomListItem);
                    Debug.Log($"새 방 추가됨: {info.Name}, 위치: {roomListItem.transform.position}");

                    // UI 강제 업데이트
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(roomListContent as RectTransform);
                }
            }
        }

        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesTahtChanged)
        {
            roomManager.MapChange();
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
        }

        public override void OnMasterClientSwitched(Player newClientPlayer)
        {
            roomManager.PlayerPanelSpawn(newClientPlayer);
        }

    }
}