using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

namespace KYS
{
    public class ReceiveGameSpawner : MonoBehaviourPunCallbacks
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnDelay = 1f;
        
        private Dictionary<int, GameObject> spawnedPlayers = new Dictionary<int, GameObject>();
        private bool isInitialized = false;
        
        private void Start()
        {
            // 모든 플레이어가 로드될 때까지 대기
            Debug.Log("ReceiveGameSpawner 시작");
            
            // 테스트용: 단일 플레이어에서도 스폰
            if (PhotonNetwork.PlayerList.Length == 1 && PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(InitializeSpawner());
            }
        }
        
        private IEnumerator InitializeSpawner()
        {
            // 모든 플레이어가 준비될 때까지 대기
            yield return new WaitForSeconds(spawnDelay);
            
            Debug.Log("플레이어 스폰 초기화 시작");
            
            // 기존 플레이어들 스폰
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                SpawnPlayer(player);
            }
            
            isInitialized = true;
            Debug.Log("플레이어 스폰 초기화 완료");
        }
        
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (isInitialized && PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(SpawnPlayerDelayed(newPlayer));
            }
        }
        
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            if (changedProps.ContainsKey("isLoaded"))
            {
                try
                {
                    if (Manager.game.isAllPlayerLoaded())
                    {
                        StartCoroutine(InitializeSpawner());
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Manager.game.isAllPlayerLoaded() 호출 중 오류: {e.Message}");
                    // Manager가 없는 경우 단일 플레이어 모드로 시작
                    if (PhotonNetwork.PlayerList.Length == 1)
                    {
                        Debug.Log("Manager 없음 - 단일 플레이어 스폰 시작");
                        StartCoroutine(InitializeSpawner());
                    }
                }
            }
        }
        
        private IEnumerator SpawnPlayerDelayed(Player player)
        {
            yield return new WaitForSeconds(spawnDelay);
            SpawnPlayer(player);
        }
        
        private void SpawnPlayer(Player player)
        {
            if (spawnPoints.Length == 0)
            {
                Debug.LogError("스폰 포인트가 설정되지 않았습니다!");
                return;
            }
            
            // 플레이어별 스폰 포인트 선택
            int spawnIndex = (player.ActorNumber - 1) % spawnPoints.Length;
            Vector3 spawnPosition = spawnPoints[spawnIndex].position;
            
            // 플레이어 스폰
            GameObject playerObject = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
            spawnedPlayers[player.ActorNumber] = playerObject;
            
            // 플레이어 설정
            ReceiveGamePlayer playerController = playerObject.GetComponent<ReceiveGamePlayer>();
            if (playerController != null)
            {
                // 플레이어 이름 설정
                playerObject.name = $"Player_{player.NickName}";
            }
            
            Debug.Log($"플레이어 {player.NickName} 스폰 완료: {spawnPosition}");
        }
        
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            // 플레이어가 나가면 해당 플레이어 오브젝트 제거
            if (spawnedPlayers.ContainsKey(otherPlayer.ActorNumber))
            {
                GameObject playerObject = spawnedPlayers[otherPlayer.ActorNumber];
                if (playerObject != null)
                {
                    PhotonNetwork.Destroy(playerObject);
                }
                spawnedPlayers.Remove(otherPlayer.ActorNumber);
            }
        }
        
        public GameObject GetPlayerObject(int actorNumber)
        {
            if (spawnedPlayers.ContainsKey(actorNumber))
            {
                return spawnedPlayers[actorNumber];
            }
            return null;
        }
        
        public Vector3 GetSpawnPosition(int playerIndex)
        {
            if (spawnPoints.Length > 0)
            {
                int spawnIndex = playerIndex % spawnPoints.Length;
                return spawnPoints[spawnIndex].position;
            }
            return Vector3.zero;
        }
        
        public void RespawnPlayer(int actorNumber)
        {
            Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (player != null)
            {
                // 기존 플레이어 제거
                if (spawnedPlayers.ContainsKey(actorNumber))
                {
                    GameObject oldPlayer = spawnedPlayers[actorNumber];
                    if (oldPlayer != null)
                    {
                        PhotonNetwork.Destroy(oldPlayer);
                    }
                    spawnedPlayers.Remove(actorNumber);
                }
                
                // 새로 스폰
                SpawnPlayer(player);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // 스폰 포인트 시각화
            if (spawnPoints != null)
            {
                Gizmos.color = Color.green;
                foreach (Transform spawnPoint in spawnPoints)
                {
                    if (spawnPoint != null)
                    {
                        Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                        Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + Vector3.up * 2f);
                    }
                }
            }
        }
    }
} 