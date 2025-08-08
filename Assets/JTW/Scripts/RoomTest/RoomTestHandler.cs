using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JTW_Test
{
    public class RoomTestHandler : MonoBehaviourPunCallbacks
    {
        [SerializeField] private Button startButton;

        private void Start()
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.ConnectUsingSettings();
            startButton.onClick.AddListener(GameStart);
        }

        public override void OnConnectedToMaster()
        {
            PhotonNetwork.JoinOrCreateRoom("JumpGame", new RoomOptions(), TypedLobby.Default);
        }

        public override void OnJoinedRoom()
        {
            PhotonNetwork.LocalPlayer.NickName = $"Player_{PhotonNetwork.LocalPlayer.ActorNumber}";
            Debug.Log($"{PhotonNetwork.LocalPlayer.NickName} 입장완료");
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {

            Debug.Log($"{newPlayer.NickName} 입장완료ii");
        }

        private void GameStart()
        {
            Manager.game.GameStart("JumpGame");
        }
    }
}

