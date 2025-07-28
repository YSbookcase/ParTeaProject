using UnityEngine;
using Photon.Pun;

namespace KYS
{
    public class ReceiveGameTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool enableTestMode = true;
        [SerializeField] private KeyCode testStartKey = KeyCode.Space;
        [SerializeField] private KeyCode testItemSpawnKey = KeyCode.I;
        
        private ReceiveGameManager gameManager;
        private ReceiveGameSpawner spawner;
        private ReceiveGameUI gameUI;
        
        private void Start()
        {
            if (!enableTestMode) return;
            
            gameManager = FindObjectOfType<ReceiveGameManager>();
            spawner = FindObjectOfType<ReceiveGameSpawner>();
            gameUI = FindObjectOfType<ReceiveGameUI>();
            
            Debug.Log("=== ReceiveGame 테스트 모드 활성화 ===");
            Debug.Log($"GameManager: {(gameManager != null ? "찾음" : "없음")}");
            Debug.Log($"Spawner: {(spawner != null ? "찾음" : "없음")}");
            Debug.Log($"GameUI: {(gameUI != null ? "찾음" : "없음")}");
            Debug.Log($"Photon 연결: {PhotonNetwork.IsConnected}");
            Debug.Log($"방 입장: {PhotonNetwork.InRoom}");
            Debug.Log($"플레이어 수: {PhotonNetwork.PlayerList.Length}");
            Debug.Log($"마스터 클라이언트: {PhotonNetwork.IsMasterClient}");
            Debug.Log("=== 테스트 키 ===");
            Debug.Log($"게임 시작: {testStartKey}");
            Debug.Log($"아이템 스폰: {testItemSpawnKey}");
        }
        
        private void Update()
        {
            if (!enableTestMode) return;
            
            // 게임 시작 테스트
            if (Input.GetKeyDown(testStartKey))
            {
                TestGameStart();
            }
            
            // 아이템 스폰 테스트
            if (Input.GetKeyDown(testItemSpawnKey))
            {
                TestItemSpawn();
            }
        }
        
        private void TestGameStart()
        {
            Debug.Log("=== 게임 시작 테스트 ===");
            
            if (gameManager == null)
            {
                Debug.LogError("GameManager를 찾을 수 없습니다!");
                return;
            }
            
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogError("Photon에 연결되지 않았습니다!");
                return;
            }
            
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogError("방에 입장하지 않았습니다!");
                return;
            }
            
            Debug.Log("게임 시작 테스트 실행...");
            
            // 단일 플레이어 모드로 게임 시작
            if (PhotonNetwork.PlayerList.Length == 1 && PhotonNetwork.IsMasterClient)
            {
                Debug.Log("단일 플레이어 모드로 게임 시작");
                // 게임 매니저의 StartGameDelayed 코루틴 호출
                gameManager.SendMessage("StartGameDelayed");
            }
            else
            {
                Debug.Log("멀티플레이어 모드 - 모든 플레이어 대기 중");
            }
        }
        
        private void TestItemSpawn()
        {
            Debug.Log("=== 아이템 스폰 테스트 ===");
            
            if (gameManager == null)
            {
                Debug.LogError("GameManager를 찾을 수 없습니다!");
                return;
            }
            
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("아이템 스폰 테스트 실행...");
                // 랜덤 위치에 아이템 스폰
                Vector3 randomPosition = new Vector3(
                    Random.Range(-5f, 5f),
                    1f,
                    Random.Range(-5f, 5f)
                );
                
                gameManager.SendMessage("SpawnItem", randomPosition);
            }
            else
            {
                Debug.Log("마스터 클라이언트가 아닙니다!");
            }
        }
        
        [ContextMenu("게임 상태 확인")]
        private void CheckGameStatus()
        {
            Debug.Log("=== 게임 상태 확인 ===");
            Debug.Log($"Photon 연결: {PhotonNetwork.IsConnected}");
            Debug.Log($"방 입장: {PhotonNetwork.InRoom}");
            Debug.Log($"플레이어 수: {PhotonNetwork.PlayerList.Length}");
            Debug.Log($"마스터 클라이언트: {PhotonNetwork.IsMasterClient}");
            
            if (gameManager != null)
            {
                Debug.Log("GameManager 상태 확인 완료");
            }
            
            if (spawner != null)
            {
                Debug.Log("Spawner 상태 확인 완료");
            }
            
            if (gameUI != null)
            {
                Debug.Log("GameUI 상태 확인 완료");
            }
        }
    }
} 