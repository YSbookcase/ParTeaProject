using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PJW
{
    public class PlayerSpawner : MonoBehaviourPunCallbacks
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform[] spawnPoints;

        private bool hasSpawned = false;

        private new void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private new void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // RopeGame 씬이 로드됐고, 아직 스폰되지 않았다면
            if (scene.name == "RopeGame" && PhotonNetwork.InRoom && !hasSpawned)
            {
                SpawnMyPlayer();
                hasSpawned = true;  // 중복 스폰 방지
            }
        }

        private void SpawnMyPlayer()
        {
            int idx = GetJoinOrderIndex();
            idx = Mathf.Clamp(idx, 0, spawnPoints.Length - 1);

            Quaternion spawnRotation = Quaternion.Euler(0, 180f, 0);
            GameObject myPlayer = PhotonNetwork.Instantiate(
                playerPrefab.name,
                spawnPoints[idx].position,
                spawnRotation
            );

            IgnorePlayerCollisions(myPlayer);
        }

        private int GetJoinOrderIndex()
        {
            var players = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == PhotonNetwork.LocalPlayer)
                    return i;
            }
            return 0; 
        }

        private void IgnorePlayerCollisions(GameObject myPlayer)
        {
            Collider[] myColliders = myPlayer.GetComponentsInChildren<Collider>();

            GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");

            foreach (GameObject other in allPlayers)
            {
                if (other == myPlayer) continue;

                Collider[] otherColliders = other.GetComponentsInChildren<Collider>();

                foreach (Collider myCol in myColliders)
                {
                    foreach (Collider otherCol in otherColliders)
                    {
                        Physics.IgnoreCollision(myCol, otherCol);
                    }
                }
            }
        }
    }
}
