using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;

namespace KYS
{
    // ReceiveGameManager가 삭제되어 여기에 ItemType enum 추가
    public enum ItemType
    {
        Normal = 0,
        Bonus = 1,
        Speed = 2,
        Slow = 3,
        Magnet = 4
    }

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
        
        // Object Pool 관련 설정 (현재 사용되지 않음)
        // [SerializeField] private int itemPoolSize = 20;
        
        [Header("Item Spawn Settings")]
        [SerializeField] private float itemSpawnInterval = 2f; // 3초에서 2초로 변경
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
            
            Debug.Log($"ReceiveGameManagerEnhanced 시작 - 플레이어 수: {PhotonNetwork.PlayerList.Length}, Master Client: {PhotonNetwork.IsMasterClient}");
            
            // 모든 플레이어의 점수 초기화
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                playerScores[player.ActorNumber] = 0;
                alivePlayers.Add(player.ActorNumber);
                Debug.Log($"플레이어 {player.NickName} (ActorNumber: {player.ActorNumber}) 초기화");
            }
            
            // 게임 시작 로직 수정 - 모든 경우에 대해 게임 시작
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("Master Client - 게임 시작 준비");
                StartCoroutine(StartGameDelayed());
            }
            else
            {
                Debug.Log("Non-Master Client - 게임 시작 대기");
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
            
            // 디버그 로그 (5초마다)
            if (Mathf.FloorToInt(Time.time) % 5 == 0)
            {
                Debug.Log($"게임 상태 - 시작: {isGameStarted}, 종료: {isGameEnded}, Master Client: {PhotonNetwork.IsMasterClient}, 시간: {currentTime:F1}초");
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
            
            // 플레이어 점수 초기화 (이미 Start에서 처리됨)
            Debug.Log($"현재 플레이어 점수 상태: {playerScores.Count}명");
            foreach (var score in playerScores)
            {
                Debug.Log($"플레이어 {score.Key}: {score.Value}점");
            }
            
            // 게임 시작을 Room Properties로 설정
            StartGame();
            
            // 아이템 스폰 시작 (마스터 클라이언트만)
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(SpawnItemsRoutine());
                StartCoroutine(SpawnPowerUpsRoutine());
                StartCoroutine(SpawnObstaclesRoutine());
                Debug.Log("아이템 스폰 루틴 시작됨");
            }
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
            if (isGameStarted && !isGameEnded)
            {
                currentTime -= Time.deltaTime;
                
                // 매 프레임마다 UI 업데이트 (시간이 정확하게 줄어들도록)
                UpdateUI();
                
                // 0.5초마다 Room Properties 업데이트 (네트워크 최적화)
                if (Time.frameCount % 30 == 0) // 60fps 기준으로 0.5초마다
                {
                    ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
                    roomProps[GAME_TIME_KEY] = currentTime;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
                }
                
                // 디버그 로그 (10초마다)
                if (Mathf.FloorToInt(currentTime) % 10 == 0 && currentTime > 0)
                {
                    Debug.Log($"게임 시간: {currentTime:F1}초");
                }
                
                if (currentTime <= 0f)
                {
                    currentTime = 0f;
                    EndGame();
                }
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
            Debug.Log("아이템 스폰 루틴 시작");
            
            while (isGameStarted && !isGameEnded)
            {
                yield return new WaitForSeconds(itemSpawnInterval);
                
                if (PhotonNetwork.IsMasterClient)
                {
                    Vector3 spawnPosition = GetRandomPositionInMap();
                    Debug.Log($"아이템 스폰 시도 - 위치: {spawnPosition}");
                    SpawnItem(spawnPosition);
                }
            }
            
            Debug.Log("아이템 스폰 루틴 종료");
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
            Debug.Log($"SpawnItem 호출됨 - 위치: {position}");
            
            if (powerUpPrefab != null)
            {
                // PhotonNetwork.Instantiate를 사용하여 네트워크 동기화
                // Resources에서 로드된 프리팹의 경우 프리팹 이름을 직접 사용
                string prefabName = "KYSPowerUp"; // Resources 폴더 내의 프리팹 이름
                GameObject item = PhotonNetwork.Instantiate(prefabName, position, Quaternion.identity);
                spawnedItems.Add(item);
                
                // 아이템이 실제로 보이는지 확인
                Renderer itemRenderer = item.GetComponent<Renderer>();
                if (itemRenderer != null)
                {
                    Debug.Log($"아이템 렌더러 정보 - 활성화: {itemRenderer.enabled}, 머티리얼: {itemRenderer.material?.name}, 머티리얼 색상: {itemRenderer.material?.color}");
                }
                else
                {
                    Debug.LogWarning("아이템에 Renderer 컴포넌트가 없습니다!");
                }
                
                // Collider 확인
                Collider itemCollider = item.GetComponent<Collider>();
                if (itemCollider != null)
                {
                    Debug.Log($"아이템 콜라이더 정보 - 활성화: {itemCollider.enabled}, isTrigger: {itemCollider.isTrigger}");
                    // Trigger로 설정되어 있는지 확인
                    if (!itemCollider.isTrigger)
                    {
                        itemCollider.isTrigger = true;
                        Debug.Log("아이템 콜라이더를 Trigger로 설정했습니다.");
                    }
                }
                else
                {
                    Debug.LogWarning("아이템에 Collider 컴포넌트가 없습니다!");
                }
                
                // CollectibleItem 컴포넌트 확인 및 추가
                CollectibleItem collectibleItem = item.GetComponent<CollectibleItem>();
                if (collectibleItem != null)
                {
                    Debug.Log("CollectibleItem 컴포넌트 확인됨");
                }
                else
                {
                    Debug.LogWarning("아이템에 CollectibleItem 컴포넌트가 없습니다! 동적으로 추가합니다.");
                    collectibleItem = item.AddComponent<CollectibleItem>();
                    Debug.Log("CollectibleItem 컴포넌트를 동적으로 추가했습니다.");
                }
                
                // 아이템 타입 설정 (ReceiveGameManager 삭제로 인해 기본값 사용)
                if (useEnhancedItemController)
                {
                    // 새로운 EnhancedItemController 사용
                    EnhancedItemController enhancedController = item.GetComponent<EnhancedItemController>();
                    if (enhancedController != null)
                    {
                        // 일반 아이템으로 설정
                        enhancedController.SetItemType(ItemType.Normal);
                    }
                    else
                    {
                        // 기존 ItemController 사용
                        ItemController itemController = item.GetComponent<ItemController>();
                        if (itemController != null)
                        {
                            itemController.SetItemType(ItemType.Normal);
                        }
                    }
                }
                else
                {
                    // 기존 시스템 사용
                    ItemController itemController = item.GetComponent<ItemController>();
                    if (itemController != null)
                    {
                        itemController.SetItemType(ItemType.Normal);
                    }
                }
                
                Debug.Log($"아이템 스폰 완료: {position}, 아이템 이름: {item.name}");
            }
            else
            {
                Debug.LogError("powerUpPrefab이 null입니다! 아이템을 생성할 수 없습니다.");
            }
        }
        
        private void SpawnPowerUp(Vector3 position)
        {
            if (powerUpPrefab != null)
            {
                // PhotonNetwork.Instantiate를 사용하여 네트워크 동기화
                // Resources에서 로드된 프리팹의 경우 프리팹 이름을 직접 사용
                string prefabName = "KYSPowerUp"; // Resources 폴더 내의 프리팹 이름
                GameObject powerUp = PhotonNetwork.Instantiate(prefabName, position, Quaternion.identity);
                spawnedPowerUps.Add(powerUp);
                
                // CollectibleItem 컴포넌트 확인 및 추가
                CollectibleItem collectibleItem = powerUp.GetComponent<CollectibleItem>();
                if (collectibleItem == null)
                {
                    Debug.LogWarning("파워업에 CollectibleItem 컴포넌트가 없습니다! 동적으로 추가합니다.");
                    collectibleItem = powerUp.AddComponent<CollectibleItem>();
                    Debug.Log("파워업에 CollectibleItem 컴포넌트를 동적으로 추가했습니다.");
                }
                
                // 파워업 타입 설정 (Speed, Slow, Magnet 중 랜덤)
                ItemType[] powerUpTypes = { ItemType.Speed, ItemType.Slow, ItemType.Magnet };
                ItemType powerUpType = powerUpTypes[Random.Range(0, powerUpTypes.Length)];
                
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
                // 장애물은 로컬에서만 생성 (네트워크 동기화 불필요)
                GameObject obstacle = Instantiate(obstaclePrefab, position, Quaternion.identity);
                spawnedObstacles.Add(obstacle);
                
                // 8초 후 자동 제거
                StartCoroutine(DestroyObstacleAfterTime(obstacle, 8f));
            }
        }
        
        public void CollectItem(int playerActorNumber)
        {
            Debug.Log($"CollectItem 호출됨 - 플레이어: {playerActorNumber}, Master Client: {PhotonNetwork.IsMasterClient}, 게임 시작: {isGameStarted}, 게임 종료: {isGameEnded}");
            
            if (isGameStarted && !isGameEnded)
            {
                // 플레이어 점수 증가 (모든 클라이언트에서)
                if (!playerScores.ContainsKey(playerActorNumber))
                {
                    playerScores[playerActorNumber] = 0;
                    Debug.Log($"새 플레이어 {playerActorNumber} 점수 초기화");
                }
                
                // 점수 증가 (중복 방지를 위해 한 번만 증가)
                playerScores[playerActorNumber]++;
                Debug.Log($"플레이어 {playerActorNumber} 점수 증가: {playerScores[playerActorNumber]}");
                
                // Master Client만 플레이어 속성으로 점수 업데이트
                if (PhotonNetwork.IsMasterClient)
                {
                    Player player = PhotonNetwork.CurrentRoom.GetPlayer(playerActorNumber);
                    if (player != null)
                    {
                        ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
                        playerProps["score"] = playerScores[playerActorNumber];
                        player.SetCustomProperties(playerProps);
                        
                        Debug.Log($"플레이어 {player.NickName} (ActorNumber: {playerActorNumber}) 아이템 수집! 점수: {playerScores[playerActorNumber]}");
                    }
                    else
                    {
                        Debug.LogError($"플레이어 {playerActorNumber}를 찾을 수 없습니다!");
                    }
                }
                else
                {
                    // Non-Master Client는 로컬에서만 점수 업데이트
                    Debug.Log($"Non-Master Client - 플레이어 {playerActorNumber} 아이템 수집! 점수: {playerScores[playerActorNumber]}");
                }
                
                // UI 업데이트
                UpdateUI();
            }
            else
            {
                Debug.Log($"아이템 수집 조건 불만족 - Master Client: {PhotonNetwork.IsMasterClient}, 게임 시작: {isGameStarted}, 게임 종료: {isGameEnded}");
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
                // PhotonNetwork.Destroy를 사용하여 네트워크 동기화
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(powerUp);
                }
                Debug.Log("파워업 자동 제거됨");
            }
        }
        
        private IEnumerator DestroyObstacleAfterTime(GameObject obstacle, float time)
        {
            yield return new WaitForSeconds(time);
            
            if (obstacle != null && spawnedObstacles.Contains(obstacle))
            {
                spawnedObstacles.Remove(obstacle);
                // PhotonNetwork.Destroy를 사용하여 네트워크 동기화
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(obstacle);
                }
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
            float halfWidth = mapSize.x / 2f - 2f; // 벽에서 더 안쪽
            float halfHeight = mapSize.y / 2f - 2f;
            
            float x = Random.Range(-halfWidth, halfWidth);
            float z = Random.Range(-halfHeight, halfHeight);
            
            // 높이를 낮춰서 더 잘 보이도록 설정
            return new Vector3(x, 2f, z); // 높이 2에서 스폰
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
                    if (PhotonNetwork.IsMasterClient)
                    {
                        PhotonNetwork.Destroy(item);
                    }
                }
            }
            spawnedItems.Clear();
            
            // 모든 파워업 제거
            foreach (GameObject powerUp in spawnedPowerUps)
            {
                if (powerUp != null)
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        PhotonNetwork.Destroy(powerUp);
                    }
                }
            }
            spawnedPowerUps.Clear();
            
            // 모든 장애물 제거
            foreach (GameObject obstacle in spawnedObstacles)
            {
                if (obstacle != null)
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        PhotonNetwork.Destroy(obstacle);
                    }
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
                Debug.Log($"게임 시작 상태 변경: {isGameStarted}");
            }
            
            if (propertiesThatChanged.ContainsKey(GAME_TIME_KEY))
            {
                float newTime = (float)propertiesThatChanged[GAME_TIME_KEY];
                // 시간 차이가 0.1초 이상이면 동기화 (더 민감하게)
                if (Mathf.Abs(currentTime - newTime) > 0.1f)
                {
                    currentTime = newTime;
                    Debug.Log($"시간 동기화: {currentTime:F1}초");
                    UpdateUI(); // 즉시 UI 업데이트
                }
            }
            
            if (propertiesThatChanged.ContainsKey(GAME_ENDED_KEY))
            {
                isGameEnded = (bool)propertiesThatChanged[GAME_ENDED_KEY];
                if (isGameEnded)
                {
                    Debug.Log("게임 종료됨");
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
                Debug.Log($"플레이어 {targetPlayer.NickName} 점수 업데이트: {newScore}");
                UpdateUI();
            }
            
            // 기존 로드 완료 체크 (Manager 의존성 제거)
            if (changedProps.ContainsKey("isLoaded"))
            {
                Debug.Log($"플레이어 {targetPlayer.NickName} 로드 완료");
                
                // 모든 플레이어가 로드되었는지 확인
                bool allPlayersLoaded = true;
                foreach (Player player in PhotonNetwork.PlayerList)
                {
                    if (!player.CustomProperties.ContainsKey("isLoaded") || !(bool)player.CustomProperties["isLoaded"])
                    {
                        allPlayersLoaded = false;
                        break;
                    }
                }
                
                if (allPlayersLoaded && PhotonNetwork.IsMasterClient && !isGameStarted)
                {
                    Debug.Log("모든 플레이어 로드 완료 - 게임 시작");
                    InitializeGame();
                }
            }
        }
    }
} 