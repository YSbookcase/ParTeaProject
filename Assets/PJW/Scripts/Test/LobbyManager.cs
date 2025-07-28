using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PJW
{
    public class LobbyManager : MonoBehaviourPunCallbacks
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI playerListText;
        [SerializeField] private Button startButton;

        private void Awake()
        {
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        private void Start()
        {
            // 랜덤 닉네임 부여 & 서버 접속
            PhotonNetwork.NickName = $"Player_{Random.Range(1000, 9999)}";
            PhotonNetwork.ConnectUsingSettings();

            startButton.interactable = false;
            startButton.onClick.AddListener(OnClickStart);

            // 접속 전이라도 자신의 닉네임은 미리 보여줄 수 있음
            playerListText.text = PhotonNetwork.NickName + "\n";
        }

        public override void OnConnectedToMaster()
        {
            // 룸 입장
            PhotonNetwork.JoinOrCreateRoom(
                "PJWTestRoom",
                new RoomOptions { MaxPlayers = 4 },
                TypedLobby.Default
            );
        }

        public override void OnJoinedRoom()
        {
            UpdatePlayerList();
            UpdateStartButton();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            UpdatePlayerList();
            UpdateStartButton();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            UpdatePlayerList();
            UpdateStartButton();
        }

        private void OnClickStart()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            PhotonNetwork.LoadLevel("RopeGame");
        }

        private void UpdatePlayerList()
        {
            playerListText.text = "";
            foreach (var p in PhotonNetwork.PlayerList)
            {
                playerListText.text += p.NickName + "\n";
            }
        }

        private void UpdateStartButton()
        {
            startButton.interactable = PhotonNetwork.IsMasterClient;
        }
    }
}
