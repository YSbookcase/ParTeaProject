using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform[] spawnPoints;

        private void Start()
        {
            SpawnAllPlayers();
        }

        private void SpawnAllPlayers()
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Instantiate(playerPrefab, spawnPoints[i].position, spawnPoints[i].rotation);
            }
        }
    }
}
