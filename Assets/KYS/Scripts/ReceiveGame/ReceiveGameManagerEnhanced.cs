using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;

namespace KYS
{
    /// <summary>
    /// 향상된 아이템 설정 시스템을 사용하는 ReceiveGameManager
    /// </summary>
    public class ReceiveGameManagerEnhanced : MonoBehaviourPunCallbacks
    {
        [Header("Game Settings")]
        [SerializeField] private float gameTime = 120f; // 게임 시간 (초)
        [SerializeField] private Vector2 mapSize = new Vector2(30f, 30f);
        [SerializeField] private float wallHeight = 2f;
        
        [Header("UI References")]
        [SerializeField] private ReceiveGameUI gameUI;
        
        [Header("Game Objects")]
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject obstaclePrefab;
        [SerializeField] private GameObject powerUpPrefab;
        [SerializeField] private Transform[] spawnPoints;
        
        [Header("Object Pool")]
        [SerializeField] private int itemPoolSize = 20;
        
        [Header("Item Spawn Settings")]
        [SerializeField] private float itemSpawnInterval = 3f;
        [SerializeField] private float powerUpSpawnInterval = 10f;
        [SerializeField] private float obstacleSpawnInterval = 5f;
        
        [Header("Item Configuration")]
        [SerializeField] private ItemConfiguration itemConfiguration;
        [SerializeField] private bool useEnhancedItemController = true; // 새로운 시스템 사용 여부
        
        private float currentTime;
        private bool isGameStarted = false;
        private bool isGameEnded = false;
        
        private Dictionary<int, int> playerScores = new Dictionary<int, int>();
        private List<GameObject> spawnedItems = new List<GameObject>();
        private List<GameObject> spawnedObstacles = new List<GameObject>();
        private List<GameObject> spawnedPowerUps = new List<GameObject>();
        private List<int> alivePlayers = new List<int>();
        
        // Room Properties 키들
        private const string GAME_STARTED_KEY = "gameStarted";
        private const string GAME_TIME_KEY = "gameTime";
        private const string GAME_ENDED_KEY = "gameEnded";
        
        private void Start()
        {
            // 아이템 설정 로드
            if (itemConfiguration == null)
            {
                itemConfiguration = Resources.Load<ItemConfiguration>("ItemConfiguration");
                if (itemConfiguration == null)
                {
                    Debug.LogWarning("[ReceiveGameManagerEnhanced] ItemConfiguration을 찾을 수 없습니다. 기본 설정을 사용합니다.");
                }
            }
            
            // 아이템 프리팹이 설정되지 않은 경우 Resources에서 로드
            if (powerUpPrefab == null)
            {
                powerUpPrefab = Resources.Load<GameObject>("KYSPowerUp");
                if (powerUpPrefab != null)
                {
                    Debug.Log("[ReceiveGameManagerEnhanced] KYSPowerUp을 Resources에서 로드했습니다.");
                }
                else
                {
                    Debug.LogError("[ReceiveGameManagerEnhanced] Resources/KYSPowerUp을 찾을 수 없습니다!");
                }
            }
            
            currentTime = gameTime;
            UpdateUI();
            
            Debug.Log($"ReceiveGameManagerEnhanced 시작 - 플레이어 수: {PhotonNetwork.PlayerList.Length}");
            
            // 테스트용: 단일 플레이어에서도 게임 시작
            if (PhotonNetwork.PlayerList.Length == 1 && PhotonNetwork.IsMasterClient)
            {
                Debug.Log("단일 플레이어 모드로 게임 시작");
                StartCoroutine(StartGameDelayed());
            }
        }
        
        private IEnumerator StartGameDelayed()
        {
            yield return new WaitForSeconds(2f);
            InitializeGame();
        }
        
        private void Update()
        {
            if (isGameStarted && !isGameEnded && PhotonNetwork.IsMasterClient)
            {
                UpdateGameTime();
            }
        }
        
        private void InitializeGame()
        {
            Debug.Log("게임 초기화 시작");
            
            // 맵 생성 (마스터 클라이언트만)
            if (PhotonNetwork.IsMasterClient)
            {
                CreateMap();
            }
            
            // 모든 플레이어의 점수 초기화
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                playerScores[player.ActorNumber] = 0;
                alivePlayers.Add(player.ActorNumber);
                
                // 플레이어 속성에 점수 설정
                ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
                playerProps["score"] = 0;
                player.SetCustomProperties(playerProps);
                
                Debug.Log($"플레이어 {player.NickName} 초기화 완료");
            }
            
            // 게임 시작을 Room Properties로 설정
            StartGame();
            
            // 아이템 스폰 시작
            StartCoroutine(SpawnItemsRoutine());
            StartCoroutine(SpawnPowerUpsRoutine());
            StartCoroutine(SpawnObstaclesRoutine());
        }
        
        public void StartGame()
        {
            if (isGameStarted) return;
            
            isGameStarted = true;
            currentTime = gameTime;
            
            // Room Properties에 게임 시작 상태 설정
            ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
            roomProps[GAME_STARTED_KEY] = true;
            roomProps[GAME_TIME_KEY] = currentTime;
            roomProps[GAME_ENDED_KEY] = false;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            
            Debug.Log("게임 시작!");
        }
        
        private void UpdateGameTime()
        {
            currentTime -= Time.deltaTime;
            
            // 0.1초마다 UI 업데이트 (성능 최적화)
            if (Time.frameCount % 6 == 0) // 60fps 기준으로 0.1초마다
            {
                UpdateUI();
                
                // Room Properties에 시간 업데이트
                ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
                roomProps[GAME_TIME_KEY] = currentTime;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            }
            
            if (currentTime <= 0f)
            {
                EndGame();
            }
        }
        
        private void UpdateUI()
        {
            if (gameUI != null)
            {
                gameUI.UpdateTime(currentTime);
                gameUI.UpdateScore(playerScores);
            }
        }
        
        private IEnumerator SpawnItemsRoutine()
        {
            while (isGameStarted && !isGameEnded)
            {
                yield return new WaitForSeconds(itemSpawnInterval);
                
                if (PhotonNetwork.IsMasterClient)
                {
                    Vector3 spawnPosition = GetRandomPositionInMap();
                    SpawnItem(spawnPosition);
                }
            }
        }
        
        private IEnumerator SpawnPowerUpsRoutine()
        {
            while (isGameStarted && !isGameEnded)
            {
                yield return new WaitForSeconds(powerUpSpawnInterval);
                
                if (PhotonNetwork.IsMasterClient)
                {
                    Vector3 spawnPosition = GetRandomPositionInMap();
                    SpawnPowerUp(spawnPosition);
                }
            }
        }
        
        private IEnumerator SpawnObstaclesRoutine()
        {
            while (isGameStarted && !isGameEnded)
            {
                yield return new WaitForSeconds(obstacleSpawnInterval);
                
                if (PhotonNetwork.IsMasterClient)
                {
                    Vector3 spawnPosition = GetRandomPositionInMap();
                    SpawnObstacle(spawnPosition);
                }
            }
        }
        
        private void SpawnItem(Vector3 position)
        {
            if (powerUpPrefab != null)
            {
                GameObject item = Instantiate(powerUpPrefab, position, Quaternion.identity);
                spawnedItems.Add(item);
                
                // 아이템 타입 설정
                if (useEnhancedItemController)
                {
                    // 새로운 EnhancedItemController 사용
                    EnhancedItemController enhancedController = item.GetComponent<EnhancedItemController>();
                    if (enhancedController != null)
                    {
                        // 일반 아이템 또는 보너스 아이템 (50% 확률)
                        ReceiveGameManager.ItemType itemType = Random.value > 0.5f ? 
                            ReceiveGameManager.ItemType.Normal : ReceiveGameManager.ItemType.Bonus;
                        enhancedController.SetItemType(itemType);
                    }
                    else
                    {
                        // 기존 ItemController 사용
                        ItemController itemController = item.GetComponent<ItemController>();
                        if (itemController != null)
                        {
                            ReceiveGameManager.ItemType itemType = Random.value > 0.5f ? 
                                ReceiveGameManager.ItemType.Normal : ReceiveGameManager.ItemType.Bonus;
                            itemController.SetItemType(itemType);
                        }
                    }
                }
                else
                {
                    // 기존 시스템 사용
                    ItemController itemController = item.GetComponent<ItemController>();
                    if (itemController != null)
                    {
                        ReceiveGameManager.ItemType itemType = Random.value > 0.5f ? 
                            ReceiveGameManager.ItemType.Normal : ReceiveGameManager.ItemType.Bonus;
                        itemController.SetItemType(itemType);
                    }
                }
                
                Debug.Log($"아이템 스폰 완료: {position}");
            }
        }
        
        private void SpawnPowerUp(Vector3 position)
        {
            if (powerUpPrefab != null)
            {
                GameObject powerUp = Instantiate(powerUpPrefab, position, Quaternion.identity);
                spawnedPowerUps.Add(powerUp);
                
                // 파워업 타입 설정
                ReceiveGameManager.ItemType powerUpType = (ReceiveGameManager.ItemType)Random.Range(2, 5); // Speed, Slow, Magnet
                
                if (useEnhancedItemController)
                {
                    // 새로운 EnhancedItemController 사용
                    EnhancedItemController enhancedController = powerUp.GetComponent<EnhancedItemController>();
                    if (enhancedController != null)
                    {
                        enhancedController.SetItemType(powerUpType);
                    }
                    else
                    {
                        // 기존 ItemController 사용
                        ItemController itemController = powerUp.GetComponent<ItemController>();
                        itemController?.SetItemType(powerUpType);
                    }
                }
                else
                {
                    // 기존 시스템 사용
                    ItemController itemController = powerUp.GetComponent<ItemController>();
                    itemController?.SetItemType(powerUpType);
                }
                
                Debug.Log($"파워업 스폰 완료: {position}, 타입: {powerUpType}");
                
                // 60초 후 자동 제거
                StartCoroutine(DestroyPowerUpAfterTime(powerUp, 60f));
            }
        }
        
        private void SpawnObstacle(Vector3 position)
        {
            if (obstaclePrefab != null)
            {
                GameObject obstacle = Instantiate(obstaclePrefab, position, Quaternion.identity);
                spawnedObstacles.Add(obstacle);
                
                // 8초 후 자동 제거
                StartCoroutine(DestroyObstacleAfterTime(obstacle, 8f));
            }
        }
        
        public void CollectItem(int playerActorNumber)
        {
            if (PhotonNetwork.IsMasterClient && isGameStarted && !isGameEnded)
            {
                playerScores[playerActorNumber]++;
                
                // 플레이어 속성으로 점수 업데이트
                Player player = PhotonNetwork.CurrentRoom.GetPlayer(playerActorNumber);
                if (player != null)
                {
                    ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
                    playerProps["score"] = playerScores[playerActorNumber];
                    player.SetCustomProperties(playerProps);
                }
                
                Debug.Log($"플레이어 {playerActorNumber} 아이템 수집! 점수: {playerScores[playerActorNumber]}");
            }
        }
        
        public void RemoveItemFromList(GameObject item)
        {
            if (spawnedItems.Contains(item))
            {
                spawnedItems.Remove(item);
                Debug.Log($"아이템이 리스트에서 제거됨: {item.name}");
            }
        }
        
        private IEnumerator DestroyPowerUpAfterTime(GameObject powerUp, float time)
        {
            yield return new WaitForSeconds(time);
            
            if (powerUp != null && spawnedPowerUps.Contains(powerUp))
            {
                spawnedPowerUps.Remove(powerUp);
                Destroy(powerUp);
                Debug.Log("파워업 자동 제거됨");
            }
        }
        
        private IEnumerator DestroyObstacleAfterTime(GameObject obstacle, float time)
        {
            yield return new WaitForSeconds(time);
            
            if (obstacle != null && spawnedObstacles.Contains(obstacle))
            {
                spawnedObstacles.Remove(obstacle);
                Destroy(obstacle);
                Debug.Log("장애물 자동 제거됨");
            }
        }
        
        private void CreateMap()
        {
            // 바닥 생성
            CreateGround();
            
            // 벽 생성
            float halfWidth = mapSize.x / 2f;
            float halfHeight = mapSize.y / 2f;
            
            // 위쪽 벽
            CreateWall(new Vector3(0, wallHeight / 2f, halfHeight), new Vector3(mapSize.x, wallHeight, 1f));
            // 아래쪽 벽
            CreateWall(new Vector3(0, wallHeight / 2f, -halfHeight), new Vector3(mapSize.x, wallHeight, 1f));
            // 왼쪽 벽
            CreateWall(new Vector3(-halfWidth, wallHeight / 2f, 0), new Vector3(1f, wallHeight, mapSize.y));
            // 오른쪽 벽
            CreateWall(new Vector3(halfWidth, wallHeight / 2f, 0), new Vector3(1f, wallHeight, mapSize.y));
        }
        
        private void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(mapSize.x / 10f, 1f, mapSize.y / 10f);
            
            // 물리 머티리얼 적용
            PhysicMaterial groundMaterial = Resources.Load<PhysicMaterial>("KYSGroundPhysicMaterial");
            if (groundMaterial != null)
            {
                ground.GetComponent<Collider>().material = groundMaterial;
            }
        }
        
        private void CreateWall(Vector3 position, Vector3 scale)
        {
            if (wallPrefab != null)
            {
                Instantiate(wallPrefab, position, Quaternion.identity).transform.localScale = scale;
            }
            else
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "Wall";
                wall.transform.position = position;
                wall.transform.localScale = scale;
            }
        }
        
        private Vector3 GetRandomPositionInMap()
        {
            float halfWidth = mapSize.x / 2f - 1f;
            float halfHeight = mapSize.y / 2f - 1f;
            
            float x = Random.Range(-halfWidth, halfWidth);
            float z = Random.Range(-halfHeight, halfHeight);
            
            return new Vector3(x, 5f, z); // 높이 5에서 스폰
        }
        
        private void EndGame()
        {
            if (isGameEnded) return;
            
            isGameEnded = true;
            Debug.Log("ReceiveGame 종료 시작!");
            
            // 모든 아이템을 풀로 반환
            ClearAllItems();
            
            // UI에 게임 종료 표시
            if (gameUI != null)
            {
                gameUI.ShowGameEnd();
            }
            
            if (PhotonNetwork.IsMasterClient)
            {
                // Room Properties에 게임 종료 상태 설정
                ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
                roomProps[GAME_ENDED_KEY] = true;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
                
                // 순위 계산 및 설정
                CalculateAndSetRanks();
                
                // 3초 후 점수 씬으로 이동
                StartCoroutine(LoadScoreScene());
            }
        }
        
        private void ClearAllItems()
        {
            // 모든 아이템 제거
            foreach (GameObject item in spawnedItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            spawnedItems.Clear();
            
            // 모든 파워업 제거
            foreach (GameObject powerUp in spawnedPowerUps)
            {
                if (powerUp != null)
                {
                    Destroy(powerUp);
                }
            }
            spawnedPowerUps.Clear();
            
            // 모든 장애물 제거
            foreach (GameObject obstacle in spawnedObstacles)
            {
                if (obstacle != null)
                {
                    Destroy(obstacle);
                }
            }
            spawnedObstacles.Clear();
        }
        
        private void CalculateAndSetRanks()
        {
            // 점수 순으로 플레이어 정렬
            var sortedPlayers = new List<KeyValuePair<int, int>>(playerScores);
            sortedPlayers.Sort((a, b) => b.Value.CompareTo(a.Value));
            
            // 순위 설정
            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                int playerActorNumber = sortedPlayers[i].Key;
                int rank = i + 1;
                
                Player player = PhotonNetwork.CurrentRoom.GetPlayer(playerActorNumber);
                if (player != null)
                {
                    ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
                    playerProps["rank"] = rank;
                    player.SetCustomProperties(playerProps);
                    
                    Debug.Log($"플레이어 {player.NickName} 순위: {rank} (점수: {sortedPlayers[i].Value})");
                }
            }
        }
        
        private IEnumerator LoadScoreScene()
        {
            yield return new WaitForSeconds(3f);
            
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel("ScoreScene");
            }
        }
        
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"플레이어 {otherPlayer.NickName}이 방을 떠났습니다.");
            
            // 점수에서 제거
            if (playerScores.ContainsKey(otherPlayer.ActorNumber))
            {
                playerScores.Remove(otherPlayer.ActorNumber);
            }
            
            // 생존 플레이어에서 제거
            if (alivePlayers.Contains(otherPlayer.ActorNumber))
            {
                alivePlayers.Remove(otherPlayer.ActorNumber);
            }
        }
        
        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            if (propertiesThatChanged.ContainsKey(GAME_STARTED_KEY))
            {
                isGameStarted = (bool)propertiesThatChanged[GAME_STARTED_KEY];
            }
            
            if (propertiesThatChanged.ContainsKey(GAME_TIME_KEY))
            {
                currentTime = (float)propertiesThatChanged[GAME_TIME_KEY];
                UpdateUI();
            }
            
            if (propertiesThatChanged.ContainsKey(GAME_ENDED_KEY))
            {
                isGameEnded = (bool)propertiesThatChanged[GAME_ENDED_KEY];
                if (isGameEnded)
                {
                    ClearAllItems();
                    if (gameUI != null)
                    {
                        gameUI.ShowGameEnd();
                    }
                }
            }
        }
        
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (changedProps.ContainsKey("score"))
            {
                int newScore = (int)changedProps["score"];
                playerScores[targetPlayer.ActorNumber] = newScore;
                UpdateUI();
            }
        }
    }
} 