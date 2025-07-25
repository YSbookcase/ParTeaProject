using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace PJW
{
    public class TestNetworkManager : MonoBehaviourPunCallbacks
    {
        [Header("Spawn Points")]
        [SerializeField] private Transform[] spawnPoints;

        [Header("Player Prefab Name")]
        [SerializeField] private string playerPrefabName = "Player_PJW";

        private void Start()
        {
            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.ConnectUsingSettings();
            }
        }

        public override void OnConnectedToMaster()
        {

            RoomOptions options = new RoomOptions
            {
                MaxPlayers = 4
            };
            PhotonNetwork.JoinOrCreateRoom("TestRoom", options, TypedLobby.Default);
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("Joined Room");
            PhotonNetwork.LocalPlayer.NickName = $"Player_{PhotonNetwork.LocalPlayer.ActorNumber}";

            SpawnPlayer();

            var ropeManager = FindObjectOfType<RopeGameManager>();
            if (ropeManager != null)
                ropeManager.BeginCountdown();
        }


        private void SpawnPlayer()
        {
            // 1) 방에 들어와 있는 모든 플레이어를 ActorNumber 오름차순으로 가져옴
            Player[] players = PhotonNetwork.PlayerList;

            // 2) 내 플레이어가 리스트에서 몇 번째인지 찾음
            int myIndex = Array.IndexOf(players, PhotonNetwork.LocalPlayer);

            // 3) spawnPoints 범위를 넘지 않도록 모듈로 연산
            int spawnIndex = myIndex % spawnPoints.Length;

            // 4) 해당 위치에 스폰
            Transform spawnPoint = spawnPoints[spawnIndex];
            PhotonNetwork.Instantiate(
                playerPrefabName,
                spawnPoint.position,
                spawnPoint.rotation
            );
        }
    }
}
