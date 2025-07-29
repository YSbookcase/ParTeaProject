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
            PhotonNetwork.NickName = $"Player_{Random.Range(1000, 9999)}";
            PhotonNetwork.ConnectUsingSettings();

            startButton.interactable = false;
            startButton.onClick.AddListener(OnClickStart);

            playerListText.text = PhotonNetwork.NickName + "\n";
        }

        public override void OnConnectedToMaster()
        {
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
