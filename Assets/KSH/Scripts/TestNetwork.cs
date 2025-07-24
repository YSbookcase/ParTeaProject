using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace KSH
{
    public class TestNetwork : MonoBehaviourPunCallbacks
    {
        void Start()
        {
            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.ConnectUsingSettings();
            }
        }

        public override void OnConnectedToMaster()
        {
            PhotonNetwork.JoinRandomOrCreateRoom();
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("Joined Room");
            PhotonNetwork.LocalPlayer.NickName = $"Player_{PhotonNetwork.LocalPlayer.ActorNumber}";
            PlayerSpawn();
        }

        private void PlayerSpawn()
        {
            Vector3 spawnPos = new Vector3(Random.Range(-1,5), 2, Random.Range(-1,5));
            GameObject player = PhotonNetwork.Instantiate("Player_KSH", spawnPos, Quaternion.identity);
            
            MyCamera camera = Camera.main.GetComponent<MyCamera>();
            camera.SetTarget(player);
        }
    }    
}
