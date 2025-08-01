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
        #region Inspector Fields
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
        
        [Header("Item Spawn Settings")]
        [SerializeField] private float itemSpawnInterval = 2f;
        [SerializeField] private float powerUpSpawnInterval = 10f;
        [SerializeField] private float obstacleSpawnInterval = 5f;
        [SerializeField] private float itemSpawnHeight = 5f; // 아이템 스폰 높이 (하늘에서 떨어지는 효과)
        [SerializeField] private float itemDropSpeed = 2f; // 아이템 떨어지는 속도
        
        [Header("Item Configuration")]
        [SerializeField] private ItemConfiguration itemConfiguration;
        
        [Header("Item Score Settings")]
        [SerializeField] private int defaultScore = 1; // Scriptable Object에서 값을 가져올 수 없을 때 사용할 기본값
        #endregion

        #region Private Fields
        private float currentTime;
        private bool isGameStarted = false;
        private bool isGameEnded = false;
        
        private Dictionary<int, int> playerScores = new Dictionary<int, int>();
        private List<GameObject> spawnedItems = new List<GameObject>();
        private List<GameObject> spawnedObstacles = new List<GameObject>();
        private List<GameObject> spawnedPowerUps = new List<GameObject>();
        private List<int> alivePlayers = new List<int>();
        
        // PhotonView 참조 (MonoBehaviourPunCallbacks에서는 직접 할당 불가)
        private PhotonView photonViewRef;
        
        // Room Properties 키들
        private const string GAME_STARTED_KEY = "gameStarted";
        private const string GAME_TIME_KEY = "gameTime";
        private const string GAME_ENDED_KEY = "gameEnded";
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            InitializePhotonView();
            LoadResources();
            InitializeGameState();
            StartGameIfMasterClient();
        }
        
        private void Update()
        {
            if (isGameStarted && !isGameEnded && PhotonNetwork.IsMasterClient)
            {
                UpdateGameTime();
            }
        }
        #endregion

        #region Initialization
        private void InitializePhotonView()
        {
            photonViewRef = GetComponent<PhotonView>();
            if (photonViewRef == null)
            {
                photonViewRef = gameObject.AddComponent<PhotonView>();
                Debug.Log("[ReceiveGameManagerEnhanced] PhotonView 컴포넌트를 추가했습니다.");
            }
        }
        
        private void LoadResources()
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
            
            // 아이템 프리팹 로드
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
        }
        
        private void InitializeGameState()
        {
            currentTime = gameTime;
            UpdateUI();
            
            Debug.Log($"ReceiveGameManagerEnhanced 시작 - 플레이어 수: {PhotonNetwork.PlayerList.Length}, Master Client: {PhotonNetwork.IsMasterClient}");
            
            // 모든 플레이어의 점수 초기화
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                playerScores[player.ActorNumber] = 0;
                alivePlayers.Add(player.ActorNumber);
            }
        }
        
        private void StartGameIfMasterClient()
        {
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
        #endregion

        #region Game Management
        private void InitializeGame()
        {
            Debug.Log("게임 초기화 시작");
            
            // 맵 생성 (마스터 클라이언트만)
            if (PhotonNetwork.IsMasterClient)
            {
                CreateMap();
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
            
            Debug.Log($"[StartGame] 게임 시작! 초기 시간: {currentTime}초");
            
            // Room Properties에 게임 시작 상태 설정
            ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
            roomProps[GAME_STARTED_KEY] = true;
            roomProps[GAME_TIME_KEY] = currentTime;
            roomProps[GAME_ENDED_KEY] = false;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        }
        
        private void UpdateGameTime()
        {
            if (isGameStarted && !isGameEnded)
            {
                currentTime -= Time.deltaTime;
                
                // 매 프레임마다 UI 업데이트
                UpdateUI();
                
                // 0.5초마다 Room Properties 업데이트 (네트워크 최적화)
                if (Time.frameCount % 30 == 0)
                {
                    ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
                    roomProps[GAME_TIME_KEY] = currentTime;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
                }
                
                // 게임 종료 조건 체크
                if (currentTime <= 0f)
                {
                    currentTime = 0f;
                    EndGame();
                }
            }
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
        #endregion

        #region UI Management
        private void UpdateUI()
        {
            if (gameUI != null)
            {
                gameUI.UpdateTime(currentTime);
                gameUI.UpdateScore(playerScores);
            }
        }
        #endregion

        #region Item Spawning
        private IEnumerator SpawnItemsRoutine()
        {
            Debug.Log("아이템 스폰 루틴 시작");
            
            while (isGameStarted && !isGameEnded)
            {
                yield return new WaitForSeconds(itemSpawnInterval);
                
                if (PhotonNetwork.IsMasterClient)
                {
                    Vector3 spawnPosition = GetRandomPositionInMap();
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
            if (powerUpPrefab != null)
            {
                // 다양한 아이템 타입 중에서 랜덤 선택
                ItemType[] itemTypes = { ItemType.Normal, ItemType.Bonus };
                ItemType selectedType = itemTypes[Random.Range(0, itemTypes.Length)];
                
                // 높은 위치에서 스폰 (하늘에서 떨어지는 효과)
                Vector3 spawnPosition = new Vector3(position.x, itemSpawnHeight, position.z);
                GameObject item = PhotonNetwork.Instantiate(powerUpPrefab.name, spawnPosition, Quaternion.identity);
                spawnedItems.Add(item);
                
                // 모든 클라이언트에서 동일한 아이템 타입 설정
                photonViewRef.RPC("SetupItemTypeRPC", RpcTarget.All, item.GetComponent<PhotonView>().ViewID, (int)selectedType);
                
                Debug.Log($"아이템 스폰 완료: {spawnPosition}, 타입: {selectedType}");
                
                // 아이템이 떨어지는 효과 시작
                StartCoroutine(DropItemToGround(item, position));
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
                // 높은 위치에서 스폰 (하늘에서 떨어지는 효과)
                Vector3 spawnPosition = new Vector3(position.x, itemSpawnHeight, position.z);
                GameObject powerUp = PhotonNetwork.Instantiate(powerUpPrefab.name, spawnPosition, Quaternion.identity);
                spawnedPowerUps.Add(powerUp);
                
                // 파워업 타입 설정 (Speed, Slow, Magnet 중 랜덤)
                ItemType[] powerUpTypes = { ItemType.Speed, ItemType.Slow, ItemType.Magnet };
                ItemType powerUpType = powerUpTypes[Random.Range(0, powerUpTypes.Length)];
                
                // 모든 클라이언트에서 동일한 아이템 타입 설정
                photonViewRef.RPC("SetupItemTypeRPC", RpcTarget.All, powerUp.GetComponent<PhotonView>().ViewID, (int)powerUpType);
                
                Debug.Log($"파워업 스폰 완료: {spawnPosition}, 타입: {powerUpType}");
                
                // 아이템이 떨어지는 효과 시작
                StartCoroutine(DropItemToGround(powerUp, position));
                
                // 60초 후 자동 제거
                StartCoroutine(DestroyPowerUpAfterTime(powerUp, 60f));
            }
        }
        
        private void SpawnObstacle(Vector3 position)
        {
            if (obstaclePrefab != null)
            {
                // 모든 클라이언트에서 동일한 위치에 방해물 생성
                photonViewRef.RPC("SpawnObstacleRPC", RpcTarget.All, position);
            }
        }
        
        [PunRPC]
        private void SpawnObstacleRPC(Vector3 position)
        {
            if (obstaclePrefab != null)
            {
                GameObject obstacle = Instantiate(obstaclePrefab, position, Quaternion.identity);
                spawnedObstacles.Add(obstacle);
                
                // ObstacleController 컴포넌트 추가
                ObstacleController obstacleController = obstacle.GetComponent<ObstacleController>();
                if (obstacleController == null)
                {
                    obstacleController = obstacle.AddComponent<ObstacleController>();
                }
                
                // 방해물은 플레이어와 부딪혀야 하므로 Trigger가 아닌 일반 Collider로 설정
                Collider obstacleCollider = obstacle.GetComponent<Collider>();
                if (obstacleCollider != null)
                {
                    obstacleCollider.isTrigger = false;
                }
                
                Debug.Log($"[SpawnObstacleRPC] 방해물 생성 완료: {position}, ObstacleController 추가됨");
                
                // 8초 후 자동 제거
                StartCoroutine(DestroyObstacleAfterTime(obstacle, 8f));
            }
        }
        
        private void SetupItemComponents(GameObject item, ItemType itemType)
        {
            // CollectibleItem 컴포넌트 확인 및 추가
            CollectibleItem collectibleItem = item.GetComponent<CollectibleItem>();
            if (collectibleItem == null)
            {
                collectibleItem = item.AddComponent<CollectibleItem>();
            }
            
            // 아이템 타입 설정 - EnhancedItemController만 사용
            EnhancedItemController enhancedController = item.GetComponent<EnhancedItemController>();
            if (enhancedController == null)
            {
                enhancedController = item.AddComponent<EnhancedItemController>();
            }
            enhancedController.SetItemType(itemType);
            
            // 아이템 타입에 따른 시각적 구분 (색상 변경)
            SetupItemVisual(item, itemType);
        }
        
        private void SetupItemVisual(GameObject item, ItemType itemType)
        {
            // Scriptable Object에서 아이템 설정을 가져와서 시각적 설정 적용
            if (itemConfiguration != null)
            {
                ItemConfig itemConfig = itemConfiguration.GetItemConfig(itemType);
                if (itemConfig != null)
                {
                    // 색상 설정
                    Renderer renderer = item.GetComponent<Renderer>();
                    if (renderer != null && renderer.material != null)
                    {
                        renderer.material.color = itemConfig.itemColor;
                    }
                    
                    // 크기 설정
                    if (itemConfig.scale != Vector3.one)
                    {
                        item.transform.localScale = itemConfig.scale;
                    }
                    
                    // 발광 효과 설정
                    if (itemConfig.useEmission && renderer != null && renderer.material != null)
                    {
                        renderer.material.EnableKeyword("_EMISSION");
                        renderer.material.SetColor("_EmissionColor", itemConfig.emissionColor * itemConfig.emissionIntensity);
                    }
                    
                    Debug.Log($"[SetupItemVisual] {itemType} 아이템 시각적 설정 완료: 색상={itemConfig.itemColor}, 크기={itemConfig.scale}");
                }
                else
                {
                    Debug.LogWarning($"[SetupItemVisual] {itemType} 타입에 대한 설정을 Scriptable Object에서 찾을 수 없습니다. 기본 색상만 적용합니다.");
                    // 기본 색상만 적용
                    Renderer renderer = item.GetComponent<Renderer>();
                    if (renderer != null && renderer.material != null)
                    {
                        Color itemColor = GetItemColor(itemType);
                        renderer.material.color = itemColor;
                    }
                }
            }
            else
            {
                Debug.LogWarning("[SetupItemVisual] ItemConfiguration이 null입니다. 기본 색상만 적용합니다.");
                // 기본 색상만 적용
                Renderer renderer = item.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    Color itemColor = GetItemColor(itemType);
                    renderer.material.color = itemColor;
                }
            }
        }
        
        private Color GetItemColor(ItemType itemType)
        {
            // Scriptable Object에서 아이템 설정을 가져와서 색상 반환
            if (itemConfiguration != null)
            {
                ItemConfig itemConfig = itemConfiguration.GetItemConfig(itemType);
                if (itemConfig != null)
                {
                    return itemConfig.itemColor;
                }
                else
                {
                    Debug.LogWarning($"[GetItemColor] {itemType} 타입에 대한 설정을 Scriptable Object에서 찾을 수 없습니다.");
                }
            }
            else
            {
                Debug.LogWarning("[GetItemColor] ItemConfiguration이 null입니다.");
            }
            
            // Scriptable Object에서 값을 가져올 수 없으면 기본 색상 반환
            switch (itemType)
            {
                case ItemType.Normal:
                    return Color.white;
                case ItemType.Bonus:
                    return Color.yellow;
                case ItemType.Speed:
                    return Color.blue;
                case ItemType.Slow:
                    return Color.red;
                case ItemType.Magnet:
                    return Color.green;
                default:
                    return Color.white;
            }
        }
        #endregion

        #region Item Collection
        public void CollectItem(int playerActorNumber, ItemType itemType = ItemType.Normal)
        {
            if (isGameStarted && !isGameEnded)
            {
                if (photonViewRef == null)
                {
                    Debug.LogError("[ReceiveGameManagerEnhanced] PhotonView가 null입니다!");
                    return;
                }
                
                // Master Client에서만 점수 증가 처리
                if (PhotonNetwork.IsMasterClient)
                {
                    CollectItemRPC(playerActorNumber, itemType);
                }
                else
                {
                    // Non-Master Client는 Master Client에게 점수 증가 요청
                    photonViewRef.RPC("RequestScoreIncrease", RpcTarget.MasterClient, playerActorNumber, (int)itemType);
                }
            }
        }
        
        // 방해물 충돌 시 점수 감점을 위한 별도 메서드
        public void ApplyObstaclePenalty(int playerActorNumber, int penaltyPoints)
        {
            if (isGameStarted && !isGameEnded)
            {
                if (photonViewRef == null)
                {
                    Debug.LogError("[ReceiveGameManagerEnhanced] PhotonView가 null입니다!");
                    return;
                }
                
                // Master Client에서만 점수 감점 처리
                if (PhotonNetwork.IsMasterClient)
                {
                    ApplyObstaclePenaltyRPC(playerActorNumber, penaltyPoints);
                }
                else
                {
                    // Non-Master Client는 Master Client에게 점수 감점 요청
                    photonViewRef.RPC("RequestObstaclePenalty", RpcTarget.MasterClient, playerActorNumber, penaltyPoints);
                }
            }
        }
        
        [PunRPC]
        private void RequestScoreIncrease(int playerActorNumber, int itemType)
        {
            // Master Client에서만 실행되는 RPC
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log($"[RequestScoreIncrease] Master Client가 점수 증가 요청 처리: 플레이어 {playerActorNumber}, 아이템 타입: {(ItemType)itemType}");
                CollectItemRPC(playerActorNumber, (ItemType)itemType);
            }
        }
        
        [PunRPC]
        private void RequestObstaclePenalty(int playerActorNumber, int penaltyPoints)
        {
            // Master Client에서만 실행되는 RPC
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log($"[RequestObstaclePenalty] Master Client가 점수 감점 요청 처리: 플레이어 {playerActorNumber}, 감점: {penaltyPoints}");
                ApplyObstaclePenaltyRPC(playerActorNumber, penaltyPoints);
            }
        }
        
        [PunRPC]
        private void SetupItemTypeRPC(int viewID, int itemType)
        {
            // 모든 클라이언트에서 아이템 타입 설정
            PhotonView itemView = PhotonView.Find(viewID);
            if (itemView != null)
            {
                GameObject item = itemView.gameObject;
                SetupItemComponents(item, (ItemType)itemType);
                Debug.Log($"아이템 타입 설정 완료: {item.name}, 타입: {(ItemType)itemType}");
            }
            else
            {
                Debug.LogWarning($"ViewID {viewID}에 해당하는 아이템을 찾을 수 없습니다.");
            }
        }
        
        private void CollectItemRPC(int playerActorNumber, ItemType itemType)
        {
            // Master Client에서만 점수 증가 처리
            if (!PhotonNetwork.IsMasterClient) return;
            
            // 플레이어 점수 초기화
            if (!playerScores.ContainsKey(playerActorNumber))
            {
                playerScores[playerActorNumber] = 0;
            }
            
            // 아이템 타입에 따른 점수 계산
            int scoreToAdd = GetScoreForItemType(itemType);
            playerScores[playerActorNumber] += scoreToAdd;
            
            Debug.Log($"플레이어 {playerActorNumber} {itemType} 아이템 수집: +{scoreToAdd}점, 총점: {playerScores[playerActorNumber]}");
            
            // 플레이어 속성으로 점수 업데이트 (네트워크 동기화)
            Player player = PhotonNetwork.CurrentRoom.GetPlayer(playerActorNumber);
            if (player != null)
            {
                ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
                playerProps["score"] = playerScores[playerActorNumber];
                player.SetCustomProperties(playerProps);
            }
            
            // UI 업데이트
            UpdateUI();
        }
        
        private void ApplyObstaclePenaltyRPC(int playerActorNumber, int penaltyPoints)
        {
            // Master Client에서만 점수 감점 처리
            if (!PhotonNetwork.IsMasterClient) return;
            
            // 플레이어 점수 초기화
            if (!playerScores.ContainsKey(playerActorNumber))
            {
                playerScores[playerActorNumber] = 0;
            }
            
            // 점수 감점 적용
            playerScores[playerActorNumber] += penaltyPoints; // penaltyPoints는 음수이므로 감점됨
            
            Debug.Log($"플레이어 {playerActorNumber} 방해물 충돌: {penaltyPoints}점, 총점: {playerScores[playerActorNumber]}");
            
            // 플레이어 속성으로 점수 업데이트 (네트워크 동기화)
            Player player = PhotonNetwork.CurrentRoom.GetPlayer(playerActorNumber);
            if (player != null)
            {
                ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
                playerProps["score"] = playerScores[playerActorNumber];
                player.SetCustomProperties(playerProps);
            }
            
            // UI 업데이트
            UpdateUI();
        }
        
        private int GetScoreForItemType(ItemType itemType)
        {
            // Scriptable Object에서 아이템 설정을 가져와서 점수 반환
            if (itemConfiguration != null)
            {
                ItemConfig itemConfig = itemConfiguration.GetItemConfig(itemType);
                if (itemConfig != null)
                {
                    Debug.Log($"[GetScoreForItemType] {itemType} 아이템 점수: {itemConfig.pointValue} (Scriptable Object에서 가져옴)");
                    return itemConfig.pointValue;
                }
                else
                {
                    Debug.LogWarning($"[GetScoreForItemType] {itemType} 타입에 대한 설정을 Scriptable Object에서 찾을 수 없습니다. 기본값 사용: {defaultScore}");
                }
            }
            else
            {
                Debug.LogWarning("[GetScoreForItemType] ItemConfiguration이 null입니다. 기본값 사용: " + defaultScore);
            }
            
            // Scriptable Object에서 값을 가져올 수 없으면 기본값 반환
            return defaultScore;
        }
        
        public void RemoveItemFromList(GameObject item)
        {
            if (spawnedItems.Contains(item))
            {
                spawnedItems.Remove(item);
            }
        }
        #endregion

        #region Item Destruction
        private IEnumerator DestroyPowerUpAfterTime(GameObject powerUp, float time)
        {
            yield return new WaitForSeconds(time);
            
            if (powerUp != null && spawnedPowerUps.Contains(powerUp))
            {
                spawnedPowerUps.Remove(powerUp);
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(powerUp);
                }
            }
        }
        
        private IEnumerator DestroyObstacleAfterTime(GameObject obstacle, float time)
        {
            yield return new WaitForSeconds(time);
            
            if (obstacle != null && spawnedObstacles.Contains(obstacle))
            {
                spawnedObstacles.Remove(obstacle);
                Destroy(obstacle);
            }
        }
        
        private void ClearAllItems()
        {
            // 모든 아이템 제거
            foreach (GameObject item in spawnedItems)
            {
                if (item != null && PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(item);
                }
            }
            spawnedItems.Clear();
            
            // 모든 파워업 제거
            foreach (GameObject powerUp in spawnedPowerUps)
            {
                if (powerUp != null && PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(powerUp);
                }
            }
            spawnedPowerUps.Clear();
            
            // 모든 장애물 제거
            foreach (GameObject obstacle in spawnedObstacles)
            {
                if (obstacle != null && PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(obstacle);
                }
            }
            spawnedObstacles.Clear();
        }
        
        private IEnumerator DropItemToGround(GameObject item, Vector3 targetPosition)
        {
            if (item == null) yield break;
            
            Vector3 startPosition = item.transform.position;
            float elapsedTime = 0f;
            
            while (elapsedTime < itemDropSpeed && item != null)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / itemDropSpeed;
                
                // 부드러운 떨어지는 효과 (ease-out)
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                
                Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, easedProgress);
                item.transform.position = newPosition;
                
                yield return null;
            }
            
            // 최종 위치로 정확히 설정
            if (item != null)
            {
                item.transform.position = targetPosition;
            }
        }
        #endregion

        #region Map Generation
        private void CreateMap()
        {
            CreateGround();
            CreateWalls();
        }
        
        private void CreateGround()
        {
            // 기존 Ground 오브젝트가 있는지 확인
            GameObject existingGround = GameObject.Find("Ground");
            
            if (existingGround != null)
            {
                // 기존 Ground가 있으면 Collider만 활성화하고 MeshRenderer는 비활성화
                Collider groundCollider = existingGround.GetComponent<Collider>();
                if (groundCollider != null)
                {
                    groundCollider.enabled = true;
                }
                
                MeshRenderer groundRenderer = existingGround.GetComponent<MeshRenderer>();
                if (groundRenderer != null)
                {
                    groundRenderer.enabled = false; // MeshRenderer 비활성화
                }
                
                // 물리 머티리얼 적용
                PhysicMaterial groundMaterial = Resources.Load<PhysicMaterial>("KYSGroundPhysicMaterial");
                if (groundMaterial != null && groundCollider != null)
                {
                    groundCollider.material = groundMaterial;
                }
                
                Debug.Log("[CreateGround] 기존 Ground 오브젝트의 Collider만 활성화했습니다.");
            }
            else
            {
                // 기존 Ground가 없으면 새로 생성 (백업용)
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
                
                Debug.Log("[CreateGround] 새로운 Ground 오브젝트를 생성했습니다.");
            }
        }
        
        private void CreateWalls()
        {
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
            float halfWidth = mapSize.x / 2f - 2f;
            float halfHeight = mapSize.y / 2f - 2f;
            
            float x = Random.Range(-halfWidth, halfWidth);
            float z = Random.Range(-halfHeight, halfHeight);
            
            // 기본 지면 높이 (아이템이 떨어질 최종 위치)
            return new Vector3(x, 1f, z);
        }
        #endregion

        #region Game End Processing
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
                }
            }
        }
        
        private IEnumerator LoadScoreScene()
        {
            yield return new WaitForSeconds(3f);
            
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel("Score");
            }
        }
        #endregion

        #region Photon Callbacks
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
                float newTime = (float)propertiesThatChanged[GAME_TIME_KEY];
                if (Mathf.Abs(currentTime - newTime) > 0.1f)
                {
                    currentTime = newTime;
                    UpdateUI();
                }
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
            
            // 기존 로드 완료 체크
            if (changedProps.ContainsKey("isLoaded"))
            {
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
        #endregion
    }
} 