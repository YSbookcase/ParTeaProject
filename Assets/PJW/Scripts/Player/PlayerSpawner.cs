using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class PlayerSpawner : MonoBehaviourPunCallbacks
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform[] spawnPoints;
                        
        public override void OnJoinedRoom()
        {
            Debug.Log("방에 입장 완료");
            SpawnMyPlayer();
        }

        private void SpawnMyPlayer()
        {
            int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;

            playerIndex = playerIndex % spawnPoints.Length;

            Transform spawnPoint = spawnPoints[playerIndex];

            PhotonNetwork.Instantiate("Player", spawnPoint.position, spawnPoint.rotation);

        }
    }
     /*
    public override void OnJoinedRoom()
     {
        int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;

        if (playerIndex >= spawnPoints.Length)
        {
          playerINdex = 0;
        }

        Transform spawnPoiont = spawnPoints[playerIndex];

        PhotonNetwork.Instantiate();
     }
     */
    }
