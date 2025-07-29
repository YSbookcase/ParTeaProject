using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace KYS
{
    public class ReceiveGameDebugger : MonoBehaviourPunCallbacks
    {
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebug = true;
        [SerializeField] private KeyCode debugKey = KeyCode.F1;
        
        private ReceiveGameSpawner spawner;
        private ReceiveGameManager gameManager;
        
        private void Start()
        {
            if (!enableDebug) return;
            
            spawner = FindObjectOfType<ReceiveGameSpawner>();
            gameManager = FindObjectOfType<ReceiveGameManager>();
            
            Debug.Log("=== ReceiveGame Debugger 시작 ===");
            CheckInitialState();
        }
        
        private void Update()
        {
            if (!enableDebug) return;
            
            if (Input.GetKeyDown(debugKey))
            {
                DebugCurrentState();
            }
        }
        
        private void CheckInitialState()
        {
            Debug.Log("=== 초기 상태 확인 ===");
            Debug.Log($"Photon 연결: {PhotonNetwork.IsConnected}");
            Debug.Log($"방 입장: {PhotonNetwork.InRoom}");
            Debug.Log($"플레이어 수: {PhotonNetwork.PlayerList.Length}");
            Debug.Log($"마스터 클라이언트: {PhotonNetwork.IsMasterClient}");
            
            if (spawner != null)
            {
                Debug.Log("✅ ReceiveGameSpawner 찾음");
                CheckSpawnerSettings();
            }
            else
            {
                Debug.LogError("❌ ReceiveGameSpawner를 찾을 수 없음");
            }
            
            if (gameManager != null)
            {
                Debug.Log("✅ ReceiveGameManager 찾음");
            }
            else
            {
                Debug.LogError("❌ ReceiveGameManager를 찾을 수 없음");
            }
        }
        
        private void CheckSpawnerSettings()
        {
            var spawnerComponent = spawner.GetComponent<ReceiveGameSpawner>();
            if (spawnerComponent != null)
            {
                // 리플렉션을 사용하여 private 필드 확인
                var playerPrefabField = typeof(ReceiveGameSpawner).GetField("playerPrefab", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var spawnPointsField = typeof(ReceiveGameSpawner).GetField("spawnPoints", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (playerPrefabField != null)
                {
                    var playerPrefab = playerPrefabField.GetValue(spawnerComponent) as GameObject;
                    Debug.Log($"플레이어 프리팹: {(playerPrefab != null ? $"✅ {playerPrefab.name}" : "❌ 설정되지 않음")}");
                    
                    if (playerPrefab != null)
                    {
                        var playerScript = playerPrefab.GetComponent<ReceiveGamePlayer>();
                        Debug.Log($"ReceiveGamePlayer 스크립트: {(playerScript != null ? "✅ 있음" : "❌ 없음")}");
                        
                        var photonView = playerPrefab.GetComponent<PhotonView>();
                        Debug.Log($"PhotonView: {(photonView != null ? "✅ 있음" : "❌ 없음")}");
                    }
                }
                
                if (spawnPointsField != null)
                {
                    var spawnPoints = spawnPointsField.GetValue(spawnerComponent) as Transform[];
                    Debug.Log($"스폰 포인트: {(spawnPoints != null && spawnPoints.Length > 0 ? $"✅ {spawnPoints.Length}개" : "❌ 설정되지 않음")}");
                    
                    if (spawnPoints != null)
                    {
                        for (int i = 0; i < spawnPoints.Length; i++)
                        {
                            if (spawnPoints[i] != null)
                            {
                                Debug.Log($"  스폰 포인트 {i}: {spawnPoints[i].position}");
                            }
                            else
                            {
                                Debug.LogError($"  스폰 포인트 {i}: null");
                            }
                        }
                    }
                }
            }
        }
        
        private void DebugCurrentState()
        {
            Debug.Log("=== 현재 상태 디버그 ===");
            Debug.Log($"Photon 연결: {PhotonNetwork.IsConnected}");
            Debug.Log($"방 입장: {PhotonNetwork.InRoom}");
            Debug.Log($"플레이어 수: {PhotonNetwork.PlayerList.Length}");
            Debug.Log($"마스터 클라이언트: {PhotonNetwork.IsMasterClient}");
            
            if (PhotonNetwork.InRoom)
            {
                Debug.Log($"현재 룸: {PhotonNetwork.CurrentRoom.Name}");
                Debug.Log($"룸 플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
                
                foreach (Player player in PhotonNetwork.PlayerList)
                {
                    Debug.Log($"  플레이어: {player.NickName} (ActorNumber: {player.ActorNumber})");
                    foreach (var prop in player.CustomProperties)
                    {
                        Debug.Log($"    {prop.Key}: {prop.Value}");
                    }
                }
            }
            
            // 씬의 플레이어 오브젝트들 확인
            var players = FindObjectsOfType<ReceiveGamePlayer>();
            Debug.Log($"씬의 플레이어 오브젝트 수: {players.Length}");
            
            foreach (var player in players)
            {
                Debug.Log($"  플레이어 오브젝트: {player.name} (위치: {player.transform.position})");
            }
            
            // 스폰러 상태 확인
            if (spawner != null)
            {
                var spawnedPlayersField = typeof(ReceiveGameSpawner).GetField("spawnedPlayers", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var isInitializedField = typeof(ReceiveGameSpawner).GetField("isInitialized", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (spawnedPlayersField != null)
                {
                    var spawnedPlayers = spawnedPlayersField.GetValue(spawner) as System.Collections.Generic.Dictionary<int, GameObject>;
                    Debug.Log($"스폰된 플레이어 수: {spawnedPlayers?.Count ?? 0}");
                }
                
                if (isInitializedField != null)
                {
                    var isInitialized = (bool)isInitializedField.GetValue(spawner);
                    Debug.Log($"스폰러 초기화: {(isInitialized ? "✅ 완료" : "❌ 미완료")}");
                }
            }
        }
        
        [ContextMenu("수동 플레이어 스폰 테스트")]
        private void ManualSpawnTest()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogError("마스터 클라이언트가 아닙니다!");
                return;
            }
            
            if (spawner == null)
            {
                Debug.LogError("스폰러를 찾을 수 없습니다!");
                return;
            }
            
            Debug.Log("수동 플레이어 스폰 테스트 시작...");
            
            // 현재 플레이어들을 수동으로 스폰
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                Debug.Log($"플레이어 {player.NickName} 수동 스폰 시도...");
                spawner.SendMessage("SpawnPlayer", player);
            }
        }
        
        [ContextMenu("스폰러 강제 초기화")]
        private void ForceInitializeSpawner()
        {
            if (spawner == null)
            {
                Debug.LogError("스폰러를 찾을 수 없습니다!");
                return;
            }
            
            Debug.Log("스폰러 강제 초기화...");
            spawner.SendMessage("InitializeSpawner");
        }
    }
} 