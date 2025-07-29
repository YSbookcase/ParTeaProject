using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KSH
{
    public class LobbyManager : MonoBehaviourPunCallbacks
    {
        [SerializeField] private TextMeshProUGUI playerText;
        [SerializeField] private Button startButton;

        void Awake()
        {
            PhotonNetwork.AutomaticallySyncScene = true;
        }
        void Start()
        {
            PhotonNetwork.NickName = $"Player_{Random.Range(1000,9999)}";
            PhotonNetwork.ConnectUsingSettings();
            startButton.onClick.AddListener(OnClickStart);
        }
        
        public override void OnConnectedToMaster()
        {
            PhotonNetwork.JoinOrCreateRoom("LobbyTest", new RoomOptions { MaxPlayers = 4 }, TypedLobby.Default);
        }
        
        public override void OnJoinedRoom()
        {
            PlayerListUpdate();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            PlayerListUpdate();
        }
        
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            PlayerListUpdate();
        }

        public void OnClickStart()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel("TileGame");
            }
        }

        private void PlayerListUpdate()
        {
            playerText.text = "";
            
            foreach (var player in PhotonNetwork.PlayerList)
            {
                playerText.text += player.NickName + "\n";
            }
        }
    }    
}
