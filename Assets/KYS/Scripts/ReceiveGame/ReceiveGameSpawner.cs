using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

namespace KYS
{
    public class ReceiveGameSpawner : MonoBehaviourPunCallbacks, IPunObservable
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnDelay = 1f;
        
        // private PhotonView photonView; // MonoBehaviourPun에서 이미 제공됨
        
        private Dictionary<int, GameObject> spawnedPlayers = new Dictionary<int, GameObject>();
        private bool isInitialized = false;
        
        private void Awake()
        {
            // PhotonView 컴포넌트 자동 추가 (로컬 변수 사용)
            PhotonView pv = GetComponent<PhotonView>();
            if (pv == null)
            {
                pv = gameObject.AddComponent<PhotonView>();
                Debug.Log("ReceiveGameSpawner에 PhotonView 컴포넌트를 자동으로 추가했습니다.");
            }
            
            // PhotonView 설정
            pv.ObservedComponents = new List<Component> { this };
            pv.Synchronization = ViewSynchronization.UnreliableOnChange;
            pv.OwnershipTransfer = OwnershipOption.Takeover;
        }
        
        private void Start()
        {
            // 모든 플레이어가 로드될 때까지 대기
            Debug.Log("ReceiveGameSpawner 시작");
            
            // 플레이어 로드 상태 설정
            SetPlayerLoaded();
            
            // 이미 스폰된 플레이어가 있는지 확인
            if (spawnedPlayers.Count > 0)
            {
                Debug.Log("이미 스폰된 플레이어가 있습니다. 중복 스폰 방지.");
                return;
            }
            
            // 모든 플레이어 모드에서 스폰 시작
            StartCoroutine(InitializeSpawner());
        }
        
        private void SetPlayerLoaded()
        {
            // 플레이어 로드 상태를 CustomProperties에 설정
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "isLoaded", true }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            Debug.Log("플레이어 로드 상태 설정 완료");
        }
        
        private IEnumerator InitializeSpawner()
        {
            // 모든 플레이어가 준비될 때까지 대기
            yield return new WaitForSeconds(spawnDelay);
            
            Debug.Log("플레이어 스폰 초기화 시작");
            
            // 각 플레이어가 자신의 캐릭터를 스폰
            SpawnMyPlayer();
            
            isInitialized = true;
            Debug.Log("플레이어 스폰 초기화 완료");
        }
        
        private void SpawnMyPlayer()
        {
            // 이미 스폰된 플레이어인지 확인
            if (spawnedPlayers.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber))
            {
                Debug.LogWarning($"플레이어 {PhotonNetwork.LocalPlayer.NickName}는 이미 스폰되어 있습니다. 중복 스폰 방지.");
                return;
            }
            
            if (playerPrefab == null)
            {
                Debug.LogError("플레이어 프리팹이 설정되지 않았습니다!");
                return;
            }
            
            if (spawnPoints.Length == 0)
            {
                Debug.LogError("스폰 포인트가 설정되지 않았습니다!");
                return;
            }
            
            // 플레이어별 스폰 포인트 선택
            int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
            Vector3 spawnPosition = spawnPoints[spawnIndex].position;
            
            Debug.Log($"내 플레이어 스폰: {PhotonNetwork.LocalPlayer.NickName} at {spawnPosition} (인덱스: {spawnIndex})");
            
            // 플레이어 스폰 (자신의 캐릭터만)
            GameObject playerObject = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
            
            if (playerObject == null)
            {
                Debug.LogError($"플레이어 스폰 실패: {playerPrefab.name}");
                return;
            }
            
            spawnedPlayers[PhotonNetwork.LocalPlayer.ActorNumber] = playerObject;
            
            // 플레이어 설정
            ReceiveGamePlayer playerController = playerObject.GetComponent<ReceiveGamePlayer>();
            if (playerController != null)
            {
                // 플레이어 이름 설정
                playerObject.name = $"Player_{PhotonNetwork.LocalPlayer.NickName}";
                Debug.Log($"내 플레이어 컨트롤러 설정 완료: {playerObject.name}");
            }
            else
            {
                Debug.LogWarning($"플레이어 오브젝트에 ReceiveGamePlayer 컴포넌트가 없습니다: {playerObject.name}");
            }
            
            Debug.Log($"내 플레이어 {PhotonNetwork.LocalPlayer.NickName} 스폰 완료: {spawnPosition}");
        }
        
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (isInitialized)
            {
                // 새로 들어온 플레이어가 자신의 캐릭터를 스폰하도록 RPC 호출
                if (PhotonNetwork.IsMasterClient)
                {
                    photonView.RPC("RequestSpawnPlayer", newPlayer);
                }
            }
        }
        
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (changedProps.ContainsKey("isLoaded"))
            {
                // 모든 플레이어가 로드되었는지 확인
                bool allPlayersLoaded = true;
                foreach (Player player in PhotonNetwork.PlayerList)
                {
                    if (!player.CustomProperties.ContainsKey("isLoaded") || 
                        !(bool)player.CustomProperties["isLoaded"])
                    {
                        allPlayersLoaded = false;
                        break;
                    }
                }
                
                if (allPlayersLoaded)
                {
                    // 모든 플레이어가 로드되면 각자 자신의 캐릭터를 스폰
                    SpawnMyPlayer();
                }
            }
        }
        
        private IEnumerator SpawnPlayerDelayed(Player player)
        {
            yield return new WaitForSeconds(spawnDelay);
            SpawnPlayer(player);
        }
        
        public void SpawnPlayer(Player player)
        {
            Debug.Log($"플레이어 스폰 시도: {player.NickName} (ActorNumber: {player.ActorNumber})");
            
            // 이미 스폰된 플레이어인지 확인
            if (spawnedPlayers.ContainsKey(player.ActorNumber))
            {
                Debug.LogWarning($"플레이어 {player.NickName}는 이미 스폰되어 있습니다. 중복 스폰 방지.");
                return;
            }
            
            if (playerPrefab == null)
            {
                Debug.LogError("플레이어 프리팹이 설정되지 않았습니다!");
                return;
            }
            
            if (spawnPoints.Length == 0)
            {
                Debug.LogError("스폰 포인트가 설정되지 않았습니다!");
                return;
            }
            
            // 플레이어별 스폰 포인트 선택
            int spawnIndex = (player.ActorNumber - 1) % spawnPoints.Length;
            Vector3 spawnPosition = spawnPoints[spawnIndex].position;
            
            Debug.Log($"스폰 위치: {spawnPosition} (인덱스: {spawnIndex})");
            
            // 플레이어 스폰
            GameObject playerObject = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
            
            if (playerObject == null)
            {
                Debug.LogError($"플레이어 스폰 실패: {playerPrefab.name}");
                return;
            }
            
            spawnedPlayers[player.ActorNumber] = playerObject;
            
            // 플레이어 설정
            ReceiveGamePlayer playerController = playerObject.GetComponent<ReceiveGamePlayer>();
            if (playerController != null)
            {
                // 플레이어 이름 설정
                playerObject.name = $"Player_{player.NickName}";
                Debug.Log($"플레이어 컨트롤러 설정 완료: {playerObject.name}");
            }
            else
            {
                Debug.LogWarning($"플레이어 오브젝트에 ReceiveGamePlayer 컴포넌트가 없습니다: {playerObject.name}");
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
        
        [PunRPC]
        private void RequestSpawnPlayer()
        {
            // RPC를 받은 플레이어가 자신의 캐릭터를 스폰
            SpawnMyPlayer();
        }
        
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            // 현재는 특별한 동기화가 필요하지 않으므로 비워둠
            // 필요시 여기에 스폰 상태나 기타 정보를 동기화할 수 있음
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