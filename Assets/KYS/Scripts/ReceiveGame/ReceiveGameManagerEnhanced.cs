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
        [SerializeField] private float obstacleDropSpeed = 2f; // 장애물 떨어지는 속도
        [SerializeField] private float bonusItemChance = 0.3f; // 보너스 아이템 생성 확률 (0.0 ~ 1.0)
        
        [Header("PowerUp Type Spawn Settings")]
        [SerializeField] private float speedPowerUpChance = 0.4f; // Speed PowerUp 생성 확률 (0.0 ~ 1.0)
        [SerializeField] private float slowPowerUpChance = 0.3f; // Slow PowerUp 생성 확률 (0.0 ~ 1.0)
        [SerializeField] private float magnetPowerUpChance = 0.3f; // Magnet PowerUp 생성 확률 (0.0 ~ 1.0)
        [SerializeField] private bool enablePowerUpTypeControl = true; // PowerUp 타입별 개별 제어 활성화
        
        [Header("Spawn Range Settings")]
        [SerializeField] private Vector3 spawnCenter = Vector3.zero; // 스폰 중심 좌표
        [SerializeField] private float spawnRangeX = 3f; // X축 스폰 범위 (-spawnRangeX ~ +spawnRangeX)
        [SerializeField] private float spawnRangeZ = 3f; // Z축 스폰 범위 (-spawnRangeZ ~ +spawnRangeZ)
        [SerializeField] private float spawnHeight = 1f; // 아이템 스폰 높이 (Y축)
        [SerializeField] private float obstacleSpawnHeight = 1f; // 장애물 도착 높이 (Y축) - 아이템과 같은 높이에서 떨어짐
        
        [Header("Item Configuration")]
        [SerializeField] private ItemConfiguration itemConfiguration;
        
        [Header("Item Score Settings")]
        [SerializeField] private int defaultScore = 1; // Scriptable Object에서 값을 가져올 수 없을 때 사용할 기본값
        
        [Header("Audio Settings")]
        [SerializeField] private string receiveGameBgmName = "BGM_ReceiveGame"; // ReceiveGame BGM 이름
        
        [Header("//Debug Settings")]
        [SerializeField] private bool autoStartCountdown = true; // 테스트용: 자동 카운트다운 시작
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
        
        // 새로운 풀 시스템 참조
        private ItemPoolManager itemPoolManager;
        
        // PhotonView 참조 (MonoBehaviourPunCallbacks에서는 직접 할당 불가)
        private PhotonView photonViewRef;
        
        // 스폰 코루틴 참조들 (명시적 중지를 위해)
        private Coroutine spawnItemsCoroutine;
        private Coroutine spawnPowerUpsCoroutine;
        private Coroutine spawnObstaclesCoroutine;
        
        // Room Properties 키들
        private const string GAME_STARTED_KEY = "gameStarted";
        private const string GAME_TIME_KEY = "gameTime";
        private const string GAME_ENDED_KEY = "gameEnded";
        
        // 카운트다운 관련 변수들
        private bool isCountdownActive = false;
        private Coroutine countdownCoroutine;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // PowerUp 확률 검증
            ValidatePowerUpChances();
            
            InitializePhotonView();
            LoadResources();
            InitializeGameState();
            StartGameIfMasterClient();
            
            // 씬 로드 시 기존 BGM 정지 (게임 시작 전까지는 BGM 없음)
            if (Manager.Audio != null)
            {
                Manager.Audio.BgmPlay(null, 0f);
                Manager.Audio.BgmPlay(receiveGameBgmName,0); // BGM 페이드 아웃
            }
            else
            {
                //Debug.LogError("[ReceiveGameManagerEnhanced] AudioManager가 null입니다!");
            }
            
            // 테스트용: 자동 카운트다운 시작
            if (autoStartCountdown && PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(AutoStartCountdownDelayed());
            }
        }
        
        // 자동 카운트다운 시작 (테스트용)
        private IEnumerator AutoStartCountdownDelayed()
        {
            // 3초 후에 자동으로 카운트다운 시작
            yield return new WaitForSeconds(3f);
            
            if (!isGameStarted && !isCountdownActive)
            {
                StartCountdown();
            }
        }
        
        // PowerUp 확률 검증 메서드
        private void ValidatePowerUpChances()
        {
            if (!enablePowerUpTypeControl) return;
            
            float totalChance = speedPowerUpChance + slowPowerUpChance + magnetPowerUpChance;
            
            if (Mathf.Abs(totalChance - 1.0f) > 0.01f)
            {
                //Debug.LogWarning($"[ReceiveGameManagerEnhanced] PowerUp 확률 합계가 1.0이 아닙니다! (현재: {totalChance:F2})");
                //Debug.LogWarning($"[ReceiveGameManagerEnhanced] Speed: {speedPowerUpChance:F2}, Slow: {slowPowerUpChance:F2}, Magnet: {magnetPowerUpChance:F2}");
                
                // 확률 정규화
                speedPowerUpChance /= totalChance;
                slowPowerUpChance /= totalChance;
                magnetPowerUpChance /= totalChance;
                
                //Debug.LogWarning($"[ReceiveGameManagerEnhanced] 확률이 자동으로 정규화되었습니다.");
                //Debug.LogWarning($"[ReceiveGameManagerEnhanced] 정규화 후 - Speed: {speedPowerUpChance:F2}, Slow: {slowPowerUpChance:F2}, Magnet: {magnetPowerUpChance:F2}");
            }
            else
            {
                //Debug.Log($"[ReceiveGameManagerEnhanced] PowerUp 확률 검증 완료 - Speed: {speedPowerUpChance:F2}, Slow: {slowPowerUpChance:F2}, Magnet: {magnetPowerUpChance:F2}");
            }
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
                //Debug.Log("[ReceiveGameManagerEnhanced] PhotonView 컴포넌트를 추가했습니다.");
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
                    //Debug.LogWarning("[ReceiveGameManagerEnhanced] ItemConfiguration을 찾을 수 없습니다. 기본 설정을 사용합니다.");
                }
                else
                {
                    //Debug.Log("[ReceiveGameManagerEnhanced] ItemConfiguration 로드 완료");
                    
                    // 보너스 아이템 설정 확인
                    ItemConfig bonusConfig = itemConfiguration.GetItemConfig(ItemType.Bonus);
                    if (bonusConfig != null)
                    {
                        //Debug.Log($"[ReceiveGameManagerEnhanced] 보너스 아이템 설정 확인 - 점수: {bonusConfig.pointValue}, 색상: {bonusConfig.itemColor}, 크기: {bonusConfig.scale}");
                    }
                    else
                    {
                        //Debug.LogWarning("[ReceiveGameManagerEnhanced] 보너스 아이템 설정을 찾을 수 없습니다!");
                    }
                    
                    // 전체 아이템 설정 개수 확인
                    ItemConfig[] allConfigs = itemConfiguration.GetAllItemConfigs();
                    //Debug.Log($"[ReceiveGameManagerEnhanced] 총 {allConfigs.Length}개의 아이템 설정이 로드되었습니다.");
                }
            }
            
            // 아이템 프리팹 로드
            if (powerUpPrefab == null)
            {
                powerUpPrefab = Resources.Load<GameObject>("KYSPowerUp");
                if (powerUpPrefab != null)
                {
                    //Debug.Log("[ReceiveGameManagerEnhanced] KYSPowerUp을 Resources에서 로드했습니다.");
                }
                else
                {
                    //Debug.LogError("[ReceiveGameManagerEnhanced] Resources/KYSPowerUp을 찾을 수 없습니다!");
                }
            }
            
            // 풀 매니저 찾기 (싱글톤 패턴 사용)
            itemPoolManager = ItemPoolManager.Instance;
            if (itemPoolManager == null)
            {
                //Debug.LogError("[ReceiveGameManagerEnhanced] ItemPoolManager.Instance가 null입니다!");
            }
            else
            {
                //Debug.Log("[ReceiveGameManagerEnhanced] ItemPoolManager 싱글톤 인스턴스 연결 완료");
            }
        }
        
        private void InitializeGameState()
        {
            currentTime = gameTime;
            UpdateUI();
            
            //Debug.Log($"ReceiveGameManagerEnhanced 시작 - 플레이어 수: {PhotonNetwork.PlayerList.Length}, Master Client: {PhotonNetwork.IsMasterClient}");
            
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
                //Debug.Log("Master Client - 게임 시작 준비");
                StartCoroutine(StartGameDelayed());
            }
            else
            {
                //Debug.Log("Non-Master Client - 게임 시작 대기");
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
            //Debug.Log("게임 초기화 시작");
            
            // 게임 시작을 Room Properties로 설정
            StartGame();
            
            // 아이템 스폰 시작 (마스터 클라이언트만)
            if (PhotonNetwork.IsMasterClient)
            {
                spawnItemsCoroutine = StartCoroutine(SpawnItemsRoutine());
                spawnPowerUpsCoroutine = StartCoroutine(SpawnPowerUpsRoutine());
                spawnObstaclesCoroutine = StartCoroutine(SpawnObstaclesRoutine());
                //Debug.Log("아이템 스폰 루틴 시작됨");
            }
        }
        
        public void StartGame()
        {
            if (isGameStarted) return;
            
            isGameStarted = true;
            currentTime = gameTime;
            
            //Debug.Log($"[StartGame] 게임 시작! 초기 시간: {currentTime}초");
            
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
        
        private void StopAllSpawningCoroutines()
        {
            //Debug.Log("[ReceiveGameManagerEnhanced] 모든 스폰 코루틴 중지 시작");
            
            // 아이템 스폰 코루틴 중지
            if (spawnItemsCoroutine != null)
            {
                StopCoroutine(spawnItemsCoroutine);
                spawnItemsCoroutine = null;
                //Debug.Log("[ReceiveGameManagerEnhanced] 아이템 스폰 코루틴 중지됨");
            }
            
            // 파워업 스폰 코루틴 중지
            if (spawnPowerUpsCoroutine != null)
            {
                StopCoroutine(spawnPowerUpsCoroutine);
                spawnPowerUpsCoroutine = null;
                //Debug.Log("[ReceiveGameManagerEnhanced] 파워업 스폰 코루틴 중지됨");
            }
            
            // 장애물 스폰 코루틴 중지
            if (spawnObstaclesCoroutine != null)
            {
                StopCoroutine(spawnObstaclesCoroutine);
                spawnObstaclesCoroutine = null;
                //Debug.Log("[ReceiveGameManagerEnhanced] 장애물 스폰 코루틴 중지됨");
            }
            
            //Debug.Log("[ReceiveGameManagerEnhanced] 모든 스폰 코루틴 중지 완료");
        }
        
        private void EndGame()
        {
            if (isGameEnded) return;
            
            isGameEnded = true;
            //Debug.Log("ReceiveGame 종료 시작!");
            
            // 스폰 코루틴들을 명시적으로 중지
            StopAllSpawningCoroutines();
            
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
            //Debug.Log("아이템 스폰 루틴 시작");
            
            while (isGameStarted && !isGameEnded)
            {
                yield return new WaitForSeconds(itemSpawnInterval);
                
                if (PhotonNetwork.IsMasterClient)
                {
                    // 중심 좌표를 기준으로 범위 내에서 아이템 스폰
                    Vector3 spawnPosition = new Vector3(
                        spawnCenter.x + Random.Range(-spawnRangeX, spawnRangeX), 
                        spawnHeight, 
                        spawnCenter.z + Random.Range(-spawnRangeZ, spawnRangeZ)
                    );
                    SpawnItem(spawnPosition);
                }
            }
            
            //Debug.Log("아이템 스폰 루틴 종료");
        }
        
        private IEnumerator SpawnPowerUpsRoutine()
        {
            while (isGameStarted && !isGameEnded)
            {
                yield return new WaitForSeconds(powerUpSpawnInterval);
                
                if (PhotonNetwork.IsMasterClient)
                {
                    // 중심 좌표를 기준으로 범위 내에서 파워업 스폰
                    Vector3 spawnPosition = new Vector3(
                        spawnCenter.x + Random.Range(-spawnRangeX, spawnRangeX), 
                        spawnHeight, 
                        spawnCenter.z + Random.Range(-spawnRangeZ, spawnRangeZ)
                    );
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
                    // 중심 좌표를 기준으로 범위 내에서 장애물 스폰
                    // 장애물도 아이템처럼 높은 위치에서 시작하여 떨어지는 효과
                    Vector3 targetPosition = new Vector3(
                        spawnCenter.x + Random.Range(-spawnRangeX, spawnRangeX), 
                        obstacleSpawnHeight, // 도착 지점 (지면)
                        spawnCenter.z + Random.Range(-spawnRangeZ, spawnRangeZ)
                    );
                    SpawnObstacle(targetPosition);
                }
            }
        }
        
        private void SpawnItem(Vector3 position)
        {
            // 게임이 종료된 상태에서는 새로운 아이템을 생성하지 않음
            if (isGameEnded)
            {
                //Debug.Log("[ReceiveGameManagerEnhanced] 게임이 종료된 상태에서 아이템 스폰 시도 무시됨");
                return;
            }
            
            if (powerUpPrefab != null)
            {
                // 일반 아이템 스폰 시 보너스 아이템 생성 확률에 따른 타입 선택
                // 설계: 일반 아이템 스폰 위치에서 일정 확률로 보너스 아이템이 생성됨
                // 이는 게임 밸런스를 위한 의도된 동작입니다.
                ItemType selectedType;
                float randomValue = Random.Range(0f, 1f);
                
                if (randomValue < bonusItemChance)
                {
                    selectedType = ItemType.Bonus;
                    //Debug.Log($"[ReceiveGameManagerEnhanced] 일반 아이템 위치에서 보너스 아이템 생성! (확률: {bonusItemChance}, 랜덤값: {randomValue:F2})");
                }
                else
                {
                    selectedType = ItemType.Normal;
                    //Debug.Log($"[ReceiveGameManagerEnhanced] 일반 아이템 생성 (확률: {1f - bonusItemChance}, 랜덤값: {randomValue:F2})");
                }
                
                // 모든 클라이언트에서 동일한 위치에 아이템 생성 (장애물과 같은 방식)
                photonViewRef.RPC("SpawnItemRPC", RpcTarget.All, position, (int)selectedType);
            }
            else
            {
                //Debug.LogError("powerUpPrefab이 null입니다! 아이템을 생성할 수 없습니다.");
            }
        }
        
        private void SpawnPowerUp(Vector3 position)
        {
            // 게임이 종료된 상태에서는 새로운 파워업을 생성하지 않음
            if (isGameEnded)
            {
                //Debug.Log("[ReceiveGameManagerEnhanced] 게임이 종료된 상태에서 파워업 스폰 시도 무시됨");
                return;
            }
            
            // PowerUp 타입 결정 (개별 빈도 조절 지원)
            ItemType selectedPowerUpType = DeterminePowerUpType();
            
            // 모든 클라이언트에서 동일한 위치에 파워업 생성 (장애물과 같은 방식)
            photonViewRef.RPC("SpawnPowerUpRPC", RpcTarget.All, position, (int)selectedPowerUpType);
        }
        
        // PowerUp 타입 결정 메서드 (개별 빈도 조절 지원)
        private ItemType DeterminePowerUpType()
        {
            if (enablePowerUpTypeControl)
            {
                // 개별 확률 기반 타입 선택
                float randomValue = Random.Range(0f, 1f);
                float cumulativeChance = 0f;
                
                // Speed PowerUp 확률
                cumulativeChance += speedPowerUpChance;
                if (randomValue <= cumulativeChance)
                {
                    return ItemType.Speed;
                }
                
                // Slow PowerUp 확률
                cumulativeChance += slowPowerUpChance;
                if (randomValue <= cumulativeChance)
                {
                    return ItemType.Slow;
                }
                
                // Magnet PowerUp 확률 (나머지)
                return ItemType.Magnet;
            }
            else
            {
                // 기존 방식: 균등 확률
                ItemType[] powerUpTypes = { ItemType.Speed, ItemType.Slow, ItemType.Magnet };
                return powerUpTypes[Random.Range(0, powerUpTypes.Length)];
            }
        }
        
        private void SpawnObstacle(Vector3 position)
        {
            // 게임이 종료된 상태에서는 새로운 장애물을 생성하지 않음
            if (isGameEnded)
            {
                //Debug.Log("[ReceiveGameManagerEnhanced] 게임이 종료된 상태에서 장애물 스폰 시도 무시됨");
                return;
            }
            
            if (obstaclePrefab != null)
            {
                // 모든 클라이언트에서 동일한 위치에 방해물 생성
                photonViewRef.RPC("SpawnObstacleRPC", RpcTarget.All, position);
            }
        }
        
        [PunRPC]
        private void SpawnItemRPC(Vector3 targetPosition, int itemType)
        {
            // 게임이 종료된 상태에서는 새로운 아이템을 생성하지 않음
            if (isGameEnded)
            {
                //Debug.Log("[ReceiveGameManagerEnhanced] 게임이 종료된 상태에서 아이템 RPC 스폰 시도 무시됨");
                return;
            }
            
            GameObject item = null;
            ItemType selectedType = (ItemType)itemType;
            
            // 아이템도 높은 위치에서 시작 (하늘에서 떨어지는 효과)
            Vector3 spawnPosition = new Vector3(targetPosition.x, itemSpawnHeight, targetPosition.z);
            
            // 새로운 풀 시스템 우선 사용
            if (itemPoolManager != null)
            {
                CollectibleItem pooledItem;
                if (selectedType == ItemType.Bonus)
                {
                    // 보너스 아이템은 별도의 PowerUp 풀에서 가져오기
                    pooledItem = itemPoolManager.GetPowerUp(spawnPosition, ItemType.Bonus);
                    //Debug.Log($"[SpawnItemRPC] 보너스 아이템을 PowerUp 풀에서 가져옴: {spawnPosition}");
                }
                else
                {
                    // 일반 아이템은 일반 아이템 풀에서 가져오기
                    pooledItem = itemPoolManager.GetItem(spawnPosition, selectedType);
                    //Debug.Log($"[SpawnItemRPC] 일반 아이템을 아이템 풀에서 가져옴: {spawnPosition}, 타입: {selectedType}");
                }
                
                if (pooledItem != null)
                {
                    item = pooledItem.gameObject;
                    spawnedItems.Add(item);
                    
                    //Debug.Log($"[SpawnItemRPC] 풀에서 아이템 생성 완료: {spawnPosition} -> {targetPosition}, 타입: {selectedType}");
                    
                    // 아이템이 떨어지는 효과 시작
                    StartCoroutine(DropItemToGround(item, targetPosition));
                }
                else
                {
                    //Debug.LogError($"[SpawnItemRPC] 풀에서 {selectedType} 아이템을 가져올 수 없습니다!");
                }
            }
            else if (powerUpPrefab != null)
            {
                // 기존 방식으로 생성
                item = Instantiate(powerUpPrefab, spawnPosition, Quaternion.identity);
                spawnedItems.Add(item);
                
                // 아이템 타입 설정
                SetupItemComponents(item, selectedType);
                
                //Debug.Log($"[SpawnItemRPC] 기존 방식으로 아이템 생성 완료: {spawnPosition} -> {targetPosition}, 타입: {selectedType}");
                
                // 아이템이 떨어지는 효과 시작
                StartCoroutine(DropItemToGround(item, targetPosition));
            }
        }
        
        [PunRPC]
        private void SpawnPowerUpRPC(Vector3 targetPosition, int itemType)
        {
            // 게임이 종료된 상태에서는 새로운 파워업을 생성하지 않음
            if (isGameEnded)
            {
                //Debug.Log("[ReceiveGameManagerEnhanced] 게임이 종료된 상태에서 파워업 RPC 스폰 시도 무시됨");
                return;
            }
            
            GameObject powerUp = null;
            ItemType selectedType = (ItemType)itemType;
            
            // 파워업도 높은 위치에서 시작 (하늘에서 떨어지는 효과)
            Vector3 spawnPosition = new Vector3(targetPosition.x, itemSpawnHeight, targetPosition.z);
            
            // 새로운 풀 시스템 우선 사용
            if (itemPoolManager != null)
            {
                CollectibleItem pooledPowerUp = itemPoolManager.GetPowerUp(spawnPosition, selectedType);
                
                if (pooledPowerUp != null)
                {
                    powerUp = pooledPowerUp.gameObject;
                    spawnedPowerUps.Add(powerUp);
                    
                    //Debug.Log($"[SpawnPowerUpRPC] 풀에서 파워업 생성 완료: {spawnPosition} -> {targetPosition}, 타입: {selectedType}");
                    
                    // 파워업이 떨어지는 효과 시작
                    StartCoroutine(DropItemToGround(powerUp, targetPosition));
                    
                    // 60초 후 자동 제거
                    StartCoroutine(DestroyPowerUpAfterTime(powerUp, 60f));
                }
                else
                {
                    //Debug.LogError($"[SpawnPowerUpRPC] 풀에서 {selectedType} 파워업을 가져올 수 없습니다!");
                }
            }
            else if (powerUpPrefab != null)
            {
                // 기존 방식으로 생성
                powerUp = Instantiate(powerUpPrefab, spawnPosition, Quaternion.identity);
                spawnedPowerUps.Add(powerUp);
                
                // 파워업 타입 설정
                SetupItemComponents(powerUp, selectedType);
                
                //Debug.Log($"[SpawnPowerUpRPC] 기존 방식으로 파워업 생성 완료: {spawnPosition} -> {targetPosition}, 타입: {selectedType}");
                
                // 파워업이 떨어지는 효과 시작
                StartCoroutine(DropItemToGround(powerUp, targetPosition));
                
                // 60초 후 자동 제거
                StartCoroutine(DestroyPowerUpAfterTime(powerUp, 60f));
            }
        }
        
        [PunRPC]
        private void SpawnObstacleRPC(Vector3 targetPosition)
        {
            // 게임이 종료된 상태에서는 새로운 장애물을 생성하지 않음
            if (isGameEnded)
            {
                //Debug.Log("[ReceiveGameManagerEnhanced] 게임이 종료된 상태에서 장애물 RPC 스폰 시도 무시됨");
                return;
            }
            
            GameObject obstacle = null;
            
            // 장애물도 아이템처럼 높은 위치에서 시작 (하늘에서 떨어지는 효과)
            Vector3 spawnPosition = new Vector3(targetPosition.x, itemSpawnHeight, targetPosition.z);
            
            // 새로운 풀 시스템 우선 사용
            if (itemPoolManager != null)
            {
                ObstacleController pooledObstacle = itemPoolManager.GetObstacle(spawnPosition);
                if (pooledObstacle != null)
                {
                    obstacle = pooledObstacle.gameObject;
                    spawnedObstacles.Add(obstacle);
                    
                    //Debug.Log($"[SpawnObstacleRPC] 풀에서 장애물 생성 완료: {spawnPosition} -> {targetPosition}");
                    
                    // 장애물이 떨어지는 효과 시작
                    StartCoroutine(DropObstacleToGround(obstacle, targetPosition));
                    
                    // 8초 후 자동 제거
                    StartCoroutine(DestroyObstacleAfterTime(obstacle, 8f));
                }
            }
            else if (obstaclePrefab != null)
            {
                // 기존 방식으로 생성
                obstacle = Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity);
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
                
                //Debug.Log($"[SpawnObstacleRPC] 기존 방식으로 장애물 생성 완료: {spawnPosition} -> {targetPosition}, ObstacleController 추가됨");
                
                // 장애물이 떨어지는 효과 시작
                StartCoroutine(DropObstacleToGround(obstacle, targetPosition));
                
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
            
            // EnhancedItemController의 SetItemType을 호출하여 모든 설정을 적용
            // (시각적 설정, 게임플레이 속성, 마그네틱 힘 값 등 모든 설정이 포함됨)
            enhancedController.SetItemType(itemType);
            
            //Debug.Log($"[SetupItemComponents] {itemType} 아이템 설정 완료 - EnhancedItemController 사용");
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
                    
                    //Debug.Log($"[SetupItemVisual] {itemType} 아이템 시각적 설정 완료: 색상={itemConfig.itemColor}, 크기={itemConfig.scale}");
                }
                else
                {
                    //Debug.LogWarning($"[SetupItemVisual] {itemType} 타입에 대한 설정을 Scriptable Object에서 찾을 수 없습니다. 기본 색상만 적용합니다.");
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
                //Debug.LogWarning("[SetupItemVisual] ItemConfiguration이 null입니다. 기본 색상만 적용합니다.");
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
                    //Debug.LogWarning($"[GetItemColor] {itemType} 타입에 대한 설정을 Scriptable Object에서 찾을 수 없습니다.");
                }
            }
            else
            {
                //Debug.LogWarning("[GetItemColor] ItemConfiguration이 null입니다.");
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
                    //Debug.LogError("[ReceiveGameManagerEnhanced] PhotonView가 null입니다!");
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
                    photonViewRef.RPC(nameof(RequestScoreIncrease), RpcTarget.MasterClient, playerActorNumber, (int)itemType);
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
                    //Debug.LogError("[ReceiveGameManagerEnhanced] PhotonView가 null입니다!");
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
                //Debug.Log($"[RequestScoreIncrease] Master Client가 점수 증가 요청 처리: 플레이어 {playerActorNumber}, 아이템 타입: {(ItemType)itemType}");
                CollectItemRPC(playerActorNumber, (ItemType)itemType);
            }
        }
        
        [PunRPC]
        private void RequestObstaclePenalty(int playerActorNumber, int penaltyPoints)
        {
            // Master Client에서만 실행되는 RPC
            if (PhotonNetwork.IsMasterClient)
            {
                //Debug.Log($"[RequestObstaclePenalty] Master Client가 점수 감점 요청 처리: 플레이어 {playerActorNumber}, 감점: {penaltyPoints}");
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
                ItemType selectedItemType = (ItemType)itemType;
                
                //Debug.Log($"[SetupItemTypeRPC] 아이템 타입 설정 시작: {item.name}, 타입: {selectedItemType}");
                
                SetupItemComponents(item, selectedItemType);
                
                // 보너스 아이템인 경우 추가 로깅
                if (selectedItemType == ItemType.Bonus)
                {
                    //Debug.Log($"[SetupItemTypeRPC] 보너스 아이템 설정 완료: {item.name}");
                    
                    // ItemConfiguration 상태 확인
                    if (itemConfiguration != null)
                    {
                        ItemConfig bonusConfig = itemConfiguration.GetItemConfig(ItemType.Bonus);
                        if (bonusConfig != null)
                        {
                            //Debug.Log($"[SetupItemTypeRPC] 보너스 아이템 설정 확인 - 점수: {bonusConfig.pointValue}, 색상: {bonusConfig.itemColor}, 크기: {bonusConfig.scale}");
                        }
                        else
                        {
                            //Debug.LogWarning("[SetupItemTypeRPC] 보너스 아이템 설정을 찾을 수 없습니다!");
                        }
                    }
                    else
                    {
                        //Debug.LogWarning("[SetupItemTypeRPC] ItemConfiguration이 null입니다!");
                    }
                }
                
                //Debug.Log($"아이템 타입 설정 완료: {item.name}, 타입: {selectedItemType}");
            }
            else
            {
                //Debug.LogWarning($"ViewID {viewID}에 해당하는 아이템을 찾을 수 없습니다.");
            }
        }
        
        /// <summary>
        /// 마스터 클라이언트에서 풀 아이템을 활성화할 때 다른 클라이언트들에게 알리는 RPC
        /// </summary>

        
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
            
            // 보너스 아이템인 경우 추가 로깅
            if (itemType == ItemType.Bonus)
            {
                //Debug.Log($"[CollectItemRPC] 보너스 아이템 수집! 플레이어 {playerActorNumber} - 획득 점수: +{scoreToAdd}점, 총점: {playerScores[playerActorNumber]}");
            }
            else
            {
                //Debug.Log($"플레이어 {playerActorNumber} {itemType} 아이템 수집: +{scoreToAdd}점, 총점: {playerScores[playerActorNumber]}");
            }
            
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
            
            //Debug.Log($"플레이어 {playerActorNumber} 방해물 충돌: {penaltyPoints}점, 총점: {playerScores[playerActorNumber]}");
            
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
                    //Debug.Log($"[GetScoreForItemType] {itemType} 아이템 점수: {itemConfig.pointValue} (Scriptable Object에서 가져옴)");
                    return itemConfig.pointValue;
                }
                else
                {
                    //Debug.LogWarning($"[GetScoreForItemType] {itemType} 타입에 대한 설정을 Scriptable Object에서 찾을 수 없습니다. 기본값 사용: {defaultScore}");
                }
            }
            else
            {
                //Debug.LogWarning("[GetScoreForItemType] ItemConfiguration이 null입니다. 기본값 사용: " + defaultScore);
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
                
                // 새로운 풀 시스템으로 반환
                if (itemPoolManager != null)
                {
                    CollectibleItem powerUpItem = powerUp.GetComponent<CollectibleItem>();
                    if (powerUpItem != null)
                    {
                        itemPoolManager.ReturnPowerUp(powerUpItem);
                        //Debug.Log("[ReceiveGameManagerEnhanced] 파워업을 풀로 반환");
                    }
                    else
                    {
                        if (PhotonNetwork.IsMasterClient)
                        {
                            PhotonNetwork.Destroy(powerUp);
                        }
                    }
                }
                else
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        PhotonNetwork.Destroy(powerUp);
                    }
                }
            }
        }
        
        private IEnumerator DestroyObstacleAfterTime(GameObject obstacle, float time)
        {
            yield return new WaitForSeconds(time);
            
            if (obstacle != null && spawnedObstacles.Contains(obstacle))
            {
                spawnedObstacles.Remove(obstacle);
                
                // 새로운 풀 시스템으로 반환
                if (itemPoolManager != null)
                {
                    ObstacleController obstacleController = obstacle.GetComponent<ObstacleController>();
                    if (obstacleController != null)
                    {
                        itemPoolManager.ReturnObstacle(obstacleController);
                        ////Debug.Log("[ReceiveGameManagerEnhanced] 장애물을 풀로 반환");
                    }
                    else
                    {
                        Destroy(obstacle);
                    }
                }
                else
                {
                    Destroy(obstacle);
                }
            }
        }
        
        private void ClearAllItems()
        {
            //Debug.Log("[ReceiveGameManagerEnhanced] 모든 아이템 정리 시작");
            
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
                if (powerUp != null)
                {
                    // 새로운 풀 시스템으로 반환
                    if (itemPoolManager != null)
                    {
                        CollectibleItem powerUpItem = powerUp.GetComponent<CollectibleItem>();
                        if (powerUpItem != null)
                        {
                            itemPoolManager.ReturnPowerUp(powerUpItem);
                        }
                        else if (PhotonNetwork.IsMasterClient)
                        {
                            PhotonNetwork.Destroy(powerUp);
                        }
                    }
                    else if (PhotonNetwork.IsMasterClient)
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
                    // 새로운 풀 시스템으로 반환
                    if (itemPoolManager != null)
                    {
                        ObstacleController obstacleController = obstacle.GetComponent<ObstacleController>();
                        if (obstacleController != null)
                        {
                            itemPoolManager.ReturnObstacle(obstacleController);
                        }
                        else if (PhotonNetwork.IsMasterClient)
                        {
                            PhotonNetwork.Destroy(obstacle);
                        }
                    }
                    else if (PhotonNetwork.IsMasterClient)
                    {
                        PhotonNetwork.Destroy(obstacle);
                    }
                }
            }
            spawnedObstacles.Clear();
            
            //Debug.Log("[ReceiveGameManagerEnhanced] 모든 아이템 정리 완료");
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
        
        /// <summary>
        /// 장애물을 지면으로 떨어뜨리는 코루틴
        /// </summary>
        private IEnumerator DropObstacleToGround(GameObject obstacle, Vector3 targetPosition)
        {
            if (obstacle == null) yield break;
            
            Vector3 startPosition = obstacle.transform.position;
            float elapsedTime = 0f;
            
            while (elapsedTime < obstacleDropSpeed && obstacle != null)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / obstacleDropSpeed;
                
                // 부드러운 떨어지는 효과 (ease-out)
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                
                Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, easedProgress);
                obstacle.transform.position = newPosition;
                
                yield return null;
            }
            
            // 최종 위치로 정확히 설정
            if (obstacle != null)
            {
                obstacle.transform.position = targetPosition;
                //Debug.Log($"[DropObstacleToGround] 장애물 낙하 완료: {targetPosition}");
            }
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
            
            // 씬 전환 전 BGM 정지
            Manager.Audio.BgmPlay(null, 0f);
            
            // 풀 매니저 정리 (모든 클라이언트에서 실행)
            if (itemPoolManager != null)
            {
                itemPoolManager.ClearAllActiveObjects();
                //Debug.Log("[ReceiveGameManagerEnhanced] 게임 종료 시 활성 오브젝트 정리 완료");
            }
            
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel("Score");
            }
        }
        #endregion

        #region Photon Callbacks
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            //Debug.Log($"플레이어 {otherPlayer.NickName}이 방을 떠났습니다.");
            
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
                bool wasGameStarted = isGameStarted;
                isGameStarted = (bool)propertiesThatChanged[GAME_STARTED_KEY];
                
                // 게임이 시작되었을 때만 BGM 재생
                if (isGameStarted && !wasGameStarted)
                {
                    //Debug.Log($"[ReceiveGameManagerEnhanced] 게임 시작 - BGM 재생 시도: {receiveGameBgmName}");
                    //Debug.Log($"[ReceiveGameManagerEnhanced] AudioManager 상태: {Manager.Audio != null}");
                    if (Manager.Audio != null)
                    {
                        //Debug.Log($"[ReceiveGameManagerEnhanced] 현재 BGM 볼륨: {Manager.Audio.bgmVolume}");
                        //Debug.Log($"[ReceiveGameManagerEnhanced] 현재 Master 볼륨: {Manager.Audio.masterVolume}");
                        Manager.Audio.BgmPlay(receiveGameBgmName, 0f);
                    }
                    else
                    {
                        //Debug.LogError("[ReceiveGameManagerEnhanced] AudioManager가 null입니다!");
                    }
                }
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
                bool wasGameEnded = isGameEnded;
                isGameEnded = (bool)propertiesThatChanged[GAME_ENDED_KEY];
                
                if (isGameEnded && !wasGameEnded)
                {
                    // 스폰 코루틴들을 명시적으로 중지
                    StopAllSpawningCoroutines();
                    
                    ClearAllItems();
                    if (gameUI != null)
                    {
                        gameUI.ShowGameEnd();
                    }
                    
                    //Debug.Log("[ReceiveGameManagerEnhanced] 게임 종료 - BGM 정지");
                    Manager.Audio.BgmPlay(null, 0f); // BGM 정지
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
            
            // 플레이어 준비 상태 체크
            if (changedProps.ContainsKey("Ready"))
            {
                bool isReady = (bool)changedProps["Ready"];
                
                // 모든 플레이어가 준비되었는지 확인
                CheckAllPlayersReady();
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
                    InitializeGame();
                }
            }
        }
        
        private void CheckAllPlayersReady()
        {
            if (PhotonNetwork.PlayerList.Length < 2) 
            {
                return; // 최소 2명 필요
            }
            
            bool allReady = true;
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                bool isPlayerReady = PhotonManager.Instance.GetPlayerReady(player);
                
                if (!isPlayerReady)
                {
                    allReady = false;
                }
            }
            
            if (allReady && !isGameStarted && PhotonNetwork.IsMasterClient)
            {
                StartCountdown();
            }
        }
        
        // 카운트다운 시작
        private void StartCountdown()
        {
            if (isCountdownActive) 
            {
                return;
            }
            
            isCountdownActive = true;
            
            // PhotonView 확인
            if (photonViewRef == null)
            {
                //Debug.LogError("[ReceiveGameManagerEnhanced] PhotonView가 null입니다!");
                return;
            }
            
            // 모든 클라이언트에게 카운트다운 시작 알림
            photonViewRef.RPC(nameof(RPCBeginCountdown), RpcTarget.All);
        }
        
        [PunRPC]
        private void RPCBeginCountdown()
        {
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
            }
            
            countdownCoroutine = StartCoroutine(CountdownRoutine());
        }
        
        private IEnumerator CountdownRoutine()
        {
            // 카운트다운 UI 표시
            if (gameUI != null)
            {
                gameUI.ShowCountdown();
            }
            else
            {
                //Debug.LogError("[ReceiveGameManagerEnhanced] gameUI가 null입니다!");
            }
            
            // 3초 카운트다운
            for (int i = 3; i > 0; i--)
            {
                // UI 업데이트
                if (gameUI != null)
                {
                    gameUI.UpdateCountdownText(i);
                }
                else
                {
                    //Debug.LogError("[ReceiveGameManagerEnhanced] gameUI가 null입니다!");
                }
                
                yield return new WaitForSeconds(1f);
            }
            
            // "시작!" 메시지 표시
            if (gameUI != null)
            {
                gameUI.UpdateCountdownText(0);
            }
            else
            {
                //Debug.LogError("[ReceiveGameManagerEnhanced] gameUI가 null입니다!");
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // 카운트다운 UI 숨기기
            if (gameUI != null)
            {
                gameUI.HideCountdown();
            }
            else
            {
                //Debug.LogError("[ReceiveGameManagerEnhanced] gameUI가 null입니다!");
            }
            
            // 카운트다운 완료 후 게임 시작
            if (PhotonNetwork.IsMasterClient)
            {
                StartGame();
            }
            
            isCountdownActive = false;
            countdownCoroutine = null;
        }
        #endregion
        
        private void OnDestroy()
        {
            // 스폰 코루틴들을 명시적으로 중지
            StopAllSpawningCoroutines();
            
            // 게임 매니저가 파괴될 때 풀 정리
            if (itemPoolManager != null)
            {
                itemPoolManager.ClearAllActiveObjects();
            }
        }
    }
} 