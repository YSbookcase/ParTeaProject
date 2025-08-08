using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        
        private new PhotonView photonView; // MonoBehaviourPun에서 이미 제공됨
        
        private Dictionary<int, GameObject> spawnedPlayers = new Dictionary<int, GameObject>();
        private bool isInitialized = false;
        
        private void Awake()
        {
            // PhotonView 컴포넌트 확인 및 추가
            photonView = GetComponent<PhotonView>();
            if (photonView == null)
            {
                photonView = gameObject.AddComponent<PhotonView>();
            }
            
            // 플레이어 프리팹 로드
            playerPrefab = Resources.Load<GameObject>("ReceiveGamePlayer");
            if (playerPrefab == null)
            {
                //Debug.LogError("ReceiveGamePlayer 프리팹을 Resources 폴더에서 찾을 수 없습니다!");
            }
        }
        
        private void Start()
        {
            // 이미 스폰된 플레이어가 있는지 확인
            if (spawnedPlayers.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber))
            {
                return;
            }
            
            // 플레이어 로드 상태 설정
            SetPlayerLoaded();
            
            // 플레이어 스폰 초기화
            StartCoroutine(InitializeSpawner());
        }
        
        private void SetPlayerLoaded()
        {
            // 플레이어 로드 완료 상태를 CustomProperties에 설정
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["isLoaded"] = true;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
        
        private IEnumerator InitializeSpawner()
        {
            // 네트워크 준비 대기
            yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady);
            
            // 내 플레이어 스폰
            SpawnMyPlayer();
        }
        
        private void SpawnMyPlayer()
        {
            // 이미 스폰된 플레이어가 있는지 확인
            if (spawnedPlayers.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber))
            {
                return;
            }
            
            // 플레이어 프리팹 확인
            if (playerPrefab == null)
            {
                //Debug.LogError("플레이어 프리팹이 설정되지 않았습니다!");
                return;
            }
            
            // 스폰 포인트 확인
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                //Debug.LogError("스폰 포인트가 설정되지 않았습니다!");
                return;
            }
            
            // 스폰 위치 계산 (안전한 방식 사용)
            Vector3 spawnPosition = GetSpawnPositionSafe(PhotonNetwork.LocalPlayer);
            
            // 플레이어 생성 (-Z 방향을 바라보도록 회전 설정)
            Quaternion spawnRotation = Quaternion.Euler(0, 180, 0); // Y축 180도 회전 = -Z 방향
            GameObject playerObject = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, spawnRotation);
            if (playerObject != null)
            {
                // 플레이어 컨트롤러 설정
                ReceiveGamePlayer playerController = playerObject.GetComponent<ReceiveGamePlayer>();
                if (playerController != null)
                {
                    // 플레이어를 딕셔너리에 추가
                    spawnedPlayers[PhotonNetwork.LocalPlayer.ActorNumber] = playerObject;
                }
                else
                {
                    //Debug.LogWarning($"플레이어 오브젝트에 ReceiveGamePlayer 컴포넌트가 없습니다: {playerObject.name}");
                }
            }
            else
            {
                //Debug.LogError($"플레이어 스폰 실패: {playerPrefab.name}");
            }
        }
        
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (isInitialized)
            {
                // 새로 들어온 플레이어가 자신의 캐릭터를 스폰하도록 RPC 호출
                if (PhotonNetwork.IsMasterClient)
                {
                    photonView.RPC(nameof(RequestSpawnPlayer), newPlayer);
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
            //Debug.Log($"플레이어 스폰 시도: {player.NickName} (ActorNumber: {player.ActorNumber})");
            
            // 이미 스폰된 플레이어인지 확인
            if (spawnedPlayers.ContainsKey(player.ActorNumber))
            {
                //Debug.LogWarning($"플레이어 {player.NickName}는 이미 스폰되어 있습니다. 중복 스폰 방지.");
                return;
            }
            
            if (playerPrefab == null)
            {
                //Debug.LogError("플레이어 프리팹이 설정되지 않았습니다!");
                return;
            }
            
            if (spawnPoints.Length == 0)
            {
                //Debug.LogError("스폰 포인트가 설정되지 않았습니다!");
                return;
            }
            
            // 플레이어별 스폰 포인트 선택 (안전한 방식 사용)
            Vector3 spawnPosition = GetSpawnPositionSafe(player);
            
            //Debug.Log($"스폰 위치: {spawnPosition}");
            
            // 플레이어 스폰 (-Z 방향을 바라보도록 회전 설정)
            Quaternion spawnRotation = Quaternion.Euler(0, 180, 0); // Y축 180도 회전 = -Z 방향
            GameObject playerObject = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, spawnRotation);
            
            if (playerObject == null)
            {
                //Debug.LogError($"플레이어 스폰 실패: {playerPrefab.name}");
                return;
            }
            
            spawnedPlayers[player.ActorNumber] = playerObject;
            
            // 플레이어 설정
            ReceiveGamePlayer playerController = playerObject.GetComponent<ReceiveGamePlayer>();
            if (playerController != null)
            {
                // 플레이어 이름 설정
                playerObject.name = $"Player_{player.NickName}";
                //Debug.Log($"플레이어 컨트롤러 설정 완료: {playerObject.name}");
            }
            else
            {
                //Debug.LogWarning($"플레이어 오브젝트에 ReceiveGamePlayer 컴포넌트가 없습니다: {playerObject.name}");
            }
            
            //Debug.Log($"플레이어 {player.NickName} 스폰 완료: {spawnPosition}");
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
        
        /// <summary>
        /// 현재 활성 플레이어 기반으로 안전한 스폰 위치를 반환합니다.
        /// ActorNumber 순차성 문제를 해결합니다.
        /// </summary>
        public Vector3 GetSpawnPositionSafe(Player player)
        {
            if (spawnPoints.Length == 0)
            {
                Debug.LogError("스폰 포인트가 설정되지 않았습니다!");
                return Vector3.zero;
            }
            
            // 현재 활성 플레이어들을 ActorNumber 순으로 정렬
            var sortedPlayers = PhotonNetwork.PlayerList
                .OrderBy(p => p.ActorNumber)
                .ToList();
            
            // 해당 플레이어의 순서 인덱스 찾기
            int playerIndex = sortedPlayers.FindIndex(p => p.ActorNumber == player.ActorNumber);
            
            if (playerIndex == -1)
            {
                Debug.LogError($"플레이어 {player.NickName}를 찾을 수 없습니다!");
                return Vector3.zero;
            }
            
            // 순서 기반으로 스폰 포인트 할당
            int spawnIndex = playerIndex % spawnPoints.Length;
            
            Debug.Log($"플레이어 {player.NickName} (ActorNumber: {player.ActorNumber}) " +
                     $"→ 정렬된 인덱스: {playerIndex} → 스폰포인트: {spawnIndex}");
            
            return spawnPoints[spawnIndex].position;
        }
        
        /// <summary>
        /// 기존 방식 (ActorNumber 기반) - 하위 호환성을 위해 유지
        /// </summary>
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