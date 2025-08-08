using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace PJW
{
    public class TestNetworkManager : MonoBehaviourPunCallbacks
    {
        private void Start()
        {
            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.ConnectUsingSettings();
            }
        }

        public override void OnConnectedToMaster()
        {
            RoomOptions options = new RoomOptions { MaxPlayers = 4 };
            PhotonNetwork.JoinOrCreateRoom("TestRoom", options, TypedLobby.Default);
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("Joined Room");
            PhotonNetwork.LocalPlayer.NickName = $"Player_{PhotonNetwork.LocalPlayer.ActorNumber}";

            var ropeManager = FindObjectOfType<RopeGameManager>();
            if (ropeManager != null)
                ropeManager.BeginCountdown();
        }
    }
}
