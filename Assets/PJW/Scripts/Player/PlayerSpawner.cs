// Assets/PJW/Scripts/Player/PlayerSpawner.cs
using UnityEngine;
using Photon.Pun;
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
            // ActorNumber 에 따라 스폰 포인트 선택
            int idx = PhotonNetwork.LocalPlayer.ActorNumber - 1;
            idx = Mathf.Clamp(idx, 0, spawnPoints.Length - 1);

            PhotonNetwork.Instantiate(
                playerPrefab.name,
                spawnPoints[idx].position,
                spawnPoints[idx].rotation
            );
        }
    }
}
