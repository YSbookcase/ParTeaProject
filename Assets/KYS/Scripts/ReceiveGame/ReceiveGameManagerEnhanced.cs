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
        
        [Header("Item Spawn Settings")]
        [SerializeField] private float itemSpawnInterval = 2f;
        [SerializeField] private float powerUpSpawnInterval = 10f;
        [SerializeField] private float obstacleSpawnInterval = 5f;
        [SerializeField] private float itemSpawnHeight = 15f; // 아이템 스폰 높이 (하늘에서 떨어지는 효과) - 더 높게 설정
        [SerializeField] private float itemDropToGroundSpeed = 3f; // 아이템이 하늘에서 바닥까지 떨어지는 시간 - 더 오래 떨어지도록
        [SerializeField] private float obstacleDropToGroundSpeed = 3f; // 장애물이 하늘에서 바닥까지 떨어지는 시간 - 더 오래 떨어지도록
        [SerializeField] private float bonusItemChance = 0.3f; // 보너스 아이템 생성 확률 (0.0 ~ 1.0)
        
        [Header("Mobile Item Lifetime Settings")]
        [SerializeField] private float powerUpLifetime = 60f; // 파워업 수명 (초)
        [SerializeField] private float mobilePowerUpLifetime = 90f; // 모바일용 파워업 수명 (더 길게)
        [SerializeField] private float obstacleLifetime = 8f; // 장애물 수명 (초)
        [SerializeField] private float mobileObstacleLifetime = 12f; // 모바일용 장애물 수명 (더 길게)
        
        [Header("PowerUp Type Spawn Settings")]
        [SerializeField] private float speedPowerUpChance = 0.4f; // Speed PowerUp 생성 확률 (0.0 ~ 1.0)
        [SerializeField] private float slowPowerUpChance = 0.3f; // Slow PowerUp 생성 확률 (0.0 ~ 1.0)
        [SerializeField] private float magnetPowerUpChance = 0.3f; // Magnet PowerUp 생성 확률 (0.0 ~ 1.0)
        [SerializeField] private bool enablePowerUpTypeControl = true; // PowerUp 타입별 개별 제어 활성화
        
        [Header("Spawn Range Settings")]
        [SerializeField] private Vector3 spawnCenter = Vector3.zero; // 스폰 중심 좌표
        [SerializeField] private float spawnRangeX = 3f; // X축 스폰 범위 (-spawnRangeX ~ +spawnRangeX)
        [SerializeField] private float spawnRangeZ = 3f; // Z축 스폰 범위 (-spawnRangeZ ~ +spawnRangeZ)
        [SerializeField] private float itemTargetGroundHeight = 1f; // 아이템이 도착할 바닥 높이 (Y축)
        [SerializeField] private float obstacleTargetGroundHeight = 1f; // 장애물이 도착할 바닥 높이 (Y축)
        
        [Header("Item Configuration")]
        [SerializeField] private ItemConfiguration itemConfiguration;
        
        [Header("Item Score Settings")]
        [SerializeField] private int defaultScore = 1; // Scriptable Object에서 값을 가져올 수 없을 때 사용할 기본값
        
        [Header("Audio Settings")]
        [SerializeField] private string receiveGameBgmName = "BGM_ReceiveGame"; // ReceiveGame BGM 이름
        
        [Header("Camera Transition")]
        [SerializeField] private CameraTransitionManager cameraTransitionManager; // 카메라 전환 매니저
        [SerializeField] private bool enableCameraTransition = false; // 카메라 전환 활성화 (테스트를 위해 false로 변경)
        
        [Header("//Debug Settings")]
        [SerializeField] private bool autoStartCountdown = true; // 테스트용: 자동 카운트다운 시작 (플레이어 준비 상태 확인 후 시작)
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
        
        // 코루틴 추적을 위한 딕셔너리 추가
        private Dictionary<GameObject, List<Coroutine>> objectCoroutines = new Dictionary<GameObject, List<Coroutine>>();
        
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
            
            // 자동 카운트다운이 활성화된 경우에만 실행
            if (autoStartCountdown && PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(AutoStartCountdownDelayed());
            }
            else
            {
                // 자동 카운트다운이 비활성화된 경우, 모든 플레이어가 준비되었는지 확인
                StartCoroutine(CheckPlayersReadyAfterDelay());
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
        
        // 플레이어 준비 상태 확인 후 카운트다운 시작
        private IEnumerator CheckPlayersReadyAfterDelay()
        {
            // 씬 로드 후 잠시 대기
            yield return new WaitForSeconds(1f);
            
            // 모든 플레이어가 준비되었는지 확인
            CheckAllPlayersReady();
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
            }
            
            // 아이템 프리팹 로드
            if (powerUpPrefab == null)
            {
                powerUpPrefab = Resources.Load<GameObject>("KYSPowerUp");
            }
            
            // 풀 매니저 찾기 (싱글톤 패턴 사용)
            itemPoolManager = ItemPoolManager.Instance;
        }
        
        private void InitializeGameState()
        {
            currentTime = gameTime;
            UpdateUI();
            
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
                StartCoroutine(StartGameDelayed());
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
            // 게임 초기화 로직 (카메라 전환은 RPC에서 처리됨)
            Debug.Log("[ReceiveGameManagerEnhanced] InitializeGame() 호출됨");
        }
        
        [PunRPC]
        public void RPCStartGameAndTransition()
        {
            if (isGameStarted) return; // Prevent multiple starts

            Debug.Log("[ReceiveGameManagerEnhanced] RPCStartGameAndTransition() 호출됨");

            // 카메라 전환 로직 (이제 모든 클라이언트에서 실행)
            if (enableCameraTransition && cameraTransitionManager != null)
            {
                Debug.Log("[ReceiveGameManagerEnhanced] CameraTransitionManager로 카메라 전환 시작");
                cameraTransitionManager.StartCameraTransition();
            }
            else if (enableCameraTransition)
            {
                ReceiveGameCamera gameCamera = FindObjectOfType<ReceiveGameCamera>();
                if (gameCamera != null)
                {
                    Debug.Log("[ReceiveGameManagerEnhanced] ReceiveGameCamera로 카메라 전환 시작");
                    gameCamera.StartCameraTransition();
                }
                else
                {
                    Debug.LogWarning("[ReceiveGameManagerEnhanced] 카메라 전환 매니저를 찾을 수 없습니다. 카메라 전환을 건너뜁니다.");
                }
            }
            else
            {
                Debug.Log("[ReceiveGameManagerEnhanced] 카메라 전환 비활성화됨.");
            }

            // Start the coroutine that waits for transition and sets game state
            StartCoroutine(StartGameAfterCameraTransition());
        }
        
        private IEnumerator StartGameAfterCameraTransition()
        {
            Debug.Log("[ReceiveGameManagerEnhanced] 카메라 전환 대기 시작");
            
            // 카메라 전환이 완료될 때까지 대기
            if (enableCameraTransition && cameraTransitionManager != null)
            {
                Debug.Log("[ReceiveGameManagerEnhanced] CameraTransitionManager 사용하여 전환 대기");
                while (!cameraTransitionManager.IsTransitionComplete())
                {
                    yield return null;
                }
            }
            else if (enableCameraTransition)
            {
                // ReceiveGameCamera 사용 시
                ReceiveGameCamera gameCamera = FindObjectOfType<ReceiveGameCamera>();
                if (gameCamera != null)
                {
                    Debug.Log("[ReceiveGameManagerEnhanced] ReceiveGameCamera 사용하여 전환 대기");
                    while (!gameCamera.IsTransitionComplete())
                    {
                        yield return null;
                    }
                }
                else
                {
                    Debug.LogWarning("[ReceiveGameManagerEnhanced] 카메라 전환 매니저를 찾을 수 없습니다. 3초 후 게임 시작");
                    yield return new WaitForSeconds(3f); // 안전장치: 3초 후 게임 시작
                }
            }
            else
            {
                Debug.Log("[ReceiveGameManagerEnhanced] 카메라 전환 비활성화됨. 즉시 게임 시작");
                yield return new WaitForSeconds(0.5f); // 짧은 지연
            }
            
            Debug.Log("[ReceiveGameManagerEnhanced] 게임 시작!");
            
            // 게임 시작
            isGameStarted = true;
            currentTime = gameTime;
            
            // 아이템 스폰 시작 (마스터 클라이언트만)
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[ReceiveGameManagerEnhanced] 아이템 스폰 시작");
                spawnItemsCoroutine = StartCoroutine(SpawnItemsRoutine());
                spawnPowerUpsCoroutine = StartCoroutine(SpawnPowerUpsRoutine());
                spawnObstaclesCoroutine = StartCoroutine(SpawnObstaclesRoutine());
            }
            
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
            // 아이템 스폰 코루틴 중지
            if (spawnItemsCoroutine != null)
            {
                StopCoroutine(spawnItemsCoroutine);
                spawnItemsCoroutine = null;
            }
            
            // 파워업 스폰 코루틴 중지
            if (spawnPowerUpsCoroutine != null)
            {
                StopCoroutine(spawnPowerUpsCoroutine);
                spawnPowerUpsCoroutine = null;
            }
            
            // 장애물 스폰 코루틴 중지
            if (spawnObstaclesCoroutine != null)
            {
                StopCoroutine(spawnObstaclesCoroutine);
                spawnObstaclesCoroutine = null;
            }
        }
        
        private void EndGame()
        {
            if (isGameEnded) return;
            
            isGameEnded = true;
            
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

        #region Coroutine Management
        /// <summary>
        /// 오브젝트에 대한 코루틴을 추적에 추가
        /// </summary>
        private void TrackCoroutine(GameObject obj, Coroutine coroutine)
        {
            if (obj == null || coroutine == null) return;
            
            if (!objectCoroutines.ContainsKey(obj))
            {
                objectCoroutines[obj] = new List<Coroutine>();
            }
            objectCoroutines[obj].Add(coroutine);
        }
        
        /// <summary>
        /// 오브젝트의 모든 코루틴을 중지하고 추적에서 제거
        /// </summary>
        private void StopAndRemoveCoroutines(GameObject obj)
        {
            if (obj == null) return;
            
            // 이 매니저에서 시작한 코루틴들 중지
            if (objectCoroutines.ContainsKey(obj))
            {
                foreach (Coroutine coroutine in objectCoroutines[obj])
                {
                    if (coroutine != null)
                    {
                        StopCoroutine(coroutine);
                    }
                }
                objectCoroutines.Remove(obj);
            }
            
            // 컴포넌트의 코루틴도 중지
            CollectibleItem collectibleItem = obj.GetComponent<CollectibleItem>();
            if (collectibleItem != null)
            {
                collectibleItem.StopAllCoroutines();
            }
            
            EnhancedItemController enhancedController = obj.GetComponent<EnhancedItemController>();
            if (enhancedController != null)
            {
                enhancedController.StopAllCoroutines();
            }
        }
        
        /// <summary>
        /// 오브젝트를 안전하게 제거 (코루틴 중지 후 제거)
        /// </summary>
        private void SafeDestroyObject(GameObject obj, bool usePhotonDestroy = true)
        {
            if (obj == null) return;
            
            // 코루틴 중지
            StopAndRemoveCoroutines(obj);
            
            // PhotonView가 있는지 확인 (네트워크 오브젝트인지 확인)
            PhotonView photonView = obj.GetComponent<PhotonView>();
            
            if (usePhotonDestroy && photonView != null && PhotonNetwork.IsMasterClient)
            {
                // 네트워크 오브젝트인 경우 PhotonNetwork.Destroy 사용
                PhotonNetwork.Destroy(obj);
            }
            else
            {
                // 로컬 오브젝트이거나 Master Client가 아닌 경우 일반 Destroy 사용
                Destroy(obj);
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
                        itemTargetGroundHeight, 
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
                        itemTargetGroundHeight, 
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
                        obstacleTargetGroundHeight, // 도착 지점 (지면)
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
                photonViewRef.RPC(nameof(SpawnItemRPC), RpcTarget.All, position, (int)selectedType);
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
            photonViewRef.RPC(nameof(SpawnPowerUpRPC), RpcTarget.All, position, (int)selectedPowerUpType);
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
                photonViewRef.RPC(nameof(SpawnObstacleRPC), RpcTarget.All, position);
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
            
            // 바닥 도착 위치 설정 (Y축만 itemTargetGroundHeight로 변경)
            Vector3 groundTargetPosition = new Vector3(targetPosition.x, itemTargetGroundHeight, targetPosition.z);
            
            Debug.Log($"[SpawnItemRPC] 아이템 스폰 시작 - 타입: {selectedType}, 스폰위치: {spawnPosition}, 목표위치: {groundTargetPosition}");
            
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
                    
                    // 풀에서 가져온 아이템에도 컴포넌트 설정 적용
                    SetupItemComponents(item, selectedType);
                    
                    // EnhancedItemController와 CollectibleItem의 groundLevel 동기화
                    EnhancedItemController enhancedController = item.GetComponent<EnhancedItemController>();
                    if (enhancedController != null)
                    {
                        enhancedController.SetGroundLevel(itemTargetGroundHeight);
                    }
                    
                    CollectibleItem collectibleItem = item.GetComponent<CollectibleItem>();
                    if (collectibleItem != null)
                    {
                        collectibleItem.SetGroundLevel(itemTargetGroundHeight);
                    }
                    
                    //Debug.Log($"[SpawnItemRPC] 풀에서 아이템 생성 완료: {spawnPosition} -> {groundTargetPosition}, 타입: {selectedType}");
                    
                    // 아이템이 떨어지는 효과 시작
                    Debug.Log($"[SpawnItemRPC] 아이템 드롭 코루틴 시작: {spawnPosition} -> {groundTargetPosition}");
                    Coroutine dropCoroutine = StartCoroutine(DropItemToGround(item, groundTargetPosition));
                    TrackCoroutine(item, dropCoroutine);
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
                
                //Debug.Log($"[SpawnItemRPC] 기존 방식으로 아이템 생성 완료: {spawnPosition} -> {groundTargetPosition}, 타입: {selectedType}");
                
                // 아이템이 떨어지는 효과 시작
                Coroutine dropCoroutine = StartCoroutine(DropItemToGround(item, groundTargetPosition));
                TrackCoroutine(item, dropCoroutine);
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
            
            // 바닥 도착 위치 설정 (Y축만 itemTargetGroundHeight로 변경)
            Vector3 groundTargetPosition = new Vector3(targetPosition.x, itemTargetGroundHeight, targetPosition.z);
            
            // 새로운 풀 시스템 우선 사용
            if (itemPoolManager != null)
            {
                CollectibleItem pooledPowerUp = itemPoolManager.GetPowerUp(spawnPosition, selectedType);
                
                if (pooledPowerUp != null)
                {
                    powerUp = pooledPowerUp.gameObject;
                    spawnedPowerUps.Add(powerUp);
                    
                    // 풀에서 가져온 파워업에도 컴포넌트 설정 적용
                    SetupItemComponents(powerUp, selectedType);
                    
                    // EnhancedItemController와 CollectibleItem의 groundLevel 동기화
                    EnhancedItemController enhancedController = powerUp.GetComponent<EnhancedItemController>();
                    if (enhancedController != null)
                    {
                        enhancedController.SetGroundLevel(itemTargetGroundHeight);
                    }
                    
                    CollectibleItem collectibleItem = powerUp.GetComponent<CollectibleItem>();
                    if (collectibleItem != null)
                    {
                        collectibleItem.SetGroundLevel(itemTargetGroundHeight);
                    }
                    
                    //Debug.Log($"[SpawnPowerUpRPC] 풀에서 파워업 생성 완료: {spawnPosition} -> {groundTargetPosition}, 타입: {selectedType}");
                    
                    // 파워업이 떨어지는 효과 시작
                    Coroutine dropCoroutine = StartCoroutine(DropItemToGround(powerUp, groundTargetPosition));
                    TrackCoroutine(powerUp, dropCoroutine);
                    
                    // 플랫폼별 파워업 수명 적용
                    float powerUpLifetimeValue = Application.isMobilePlatform ? mobilePowerUpLifetime : powerUpLifetime;
                    
                    // 모바일 디버깅 로그
                    if (Application.isMobilePlatform)
                    {
                        Debug.Log($"[ReceiveGameManagerEnhanced] 모바일에서 파워업 생성: {selectedType}, 수명: {powerUpLifetimeValue}초");
                    }
                    
                    // 자동 제거
                    Coroutine destroyCoroutine = StartCoroutine(DestroyPowerUpAfterTime(powerUp, powerUpLifetimeValue));
                    TrackCoroutine(powerUp, destroyCoroutine);
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
                
                //Debug.Log($"[SpawnPowerUpRPC] 기존 방식으로 파워업 생성 완료: {spawnPosition} -> {groundTargetPosition}, 타입: {selectedType}");
                
                // 파워업이 떨어지는 효과 시작
                Coroutine dropCoroutine = StartCoroutine(DropItemToGround(powerUp, groundTargetPosition));
                TrackCoroutine(powerUp, dropCoroutine);
                
                // 플랫폼별 파워업 수명 적용
                float powerUpLifetimeValue = Application.isMobilePlatform ? mobilePowerUpLifetime : powerUpLifetime;
                
                // 모바일 디버깅 로그
                if (Application.isMobilePlatform)
                {
                    Debug.Log($"[ReceiveGameManagerEnhanced] 모바일에서 파워업 생성: {selectedType}, 수명: {powerUpLifetimeValue}초");
                }
                
                // 자동 제거
                Coroutine destroyCoroutine = StartCoroutine(DestroyPowerUpAfterTime(powerUp, powerUpLifetimeValue));
                TrackCoroutine(powerUp, destroyCoroutine);
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
            
            // 바닥 도착 위치 설정 (Y축만 obstacleTargetGroundHeight로 변경)
            Vector3 groundTargetPosition = new Vector3(targetPosition.x, obstacleTargetGroundHeight, targetPosition.z);
            
            Debug.Log($"[SpawnObstacleRPC] 장애물 스폰 시작 - 스폰위치: {spawnPosition}, 목표위치: {groundTargetPosition}");
            
            // 새로운 풀 시스템 우선 사용
            if (itemPoolManager != null)
            {
                ObstacleController pooledObstacle = itemPoolManager.GetObstacle(spawnPosition);
                if (pooledObstacle != null)
                {
                    obstacle = pooledObstacle.gameObject;
                    spawnedObstacles.Add(obstacle);
                    
                    //Debug.Log($"[SpawnObstacleRPC] 풀에서 장애물 생성 완료: {spawnPosition} -> {groundTargetPosition}");
                    
                    // 장애물이 떨어지는 효과 시작
                    Coroutine dropCoroutine = StartCoroutine(DropObstacleToGround(obstacle, groundTargetPosition));
                    TrackCoroutine(obstacle, dropCoroutine);
                    
                    // 플랫폼별 장애물 수명 적용
                    float obstacleLifetimeValue = Application.isMobilePlatform ? mobileObstacleLifetime : obstacleLifetime;
                    
                    // 모바일 디버깅 로그
                    if (Application.isMobilePlatform)
                    {
                        Debug.Log($"[ReceiveGameManagerEnhanced] 모바일에서 장애물 생성, 수명: {obstacleLifetimeValue}초");
                    }
                    
                    // 자동 제거
                    Coroutine destroyCoroutine = StartCoroutine(DestroyObstacleAfterTime(obstacle, obstacleLifetimeValue));
                    TrackCoroutine(obstacle, destroyCoroutine);
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
                
                //Debug.Log($"[SpawnObstacleRPC] 기존 방식으로 장애물 생성 완료: {spawnPosition} -> {groundTargetPosition}, ObstacleController 추가됨");
                
                // 장애물이 떨어지는 효과 시작
                Coroutine dropCoroutine = StartCoroutine(DropObstacleToGround(obstacle, groundTargetPosition));
                TrackCoroutine(obstacle, dropCoroutine);
                
                // 플랫폼별 장애물 수명 적용
                float obstacleLifetimeValue = Application.isMobilePlatform ? mobileObstacleLifetime : obstacleLifetime;
                
                // 모바일 디버깅 로그
                if (Application.isMobilePlatform)
                {
                    Debug.Log($"[ReceiveGameManagerEnhanced] 모바일에서 장애물 생성, 수명: {obstacleLifetimeValue}초");
                }
                
                // 자동 제거
                Coroutine destroyCoroutine = StartCoroutine(DestroyObstacleAfterTime(obstacle, obstacleLifetimeValue));
                TrackCoroutine(obstacle, destroyCoroutine);
            }
        }
        
        private void SetupItemComponents(GameObject item, ItemType itemType)
        {
            // CollectibleItem 컴포넌트 유지 (오브젝트 풀 시스템과 호환성 유지)
            CollectibleItem collectibleItem = item.GetComponent<CollectibleItem>();
            if (collectibleItem != null)
            {
                // CollectibleItem의 아이템 타입 설정
                collectibleItem.itemType = itemType;
            }
            
            // EnhancedItemController 추가/설정 (시각적 및 게임플레이 설정용)
            EnhancedItemController enhancedController = item.GetComponent<EnhancedItemController>();
            if (enhancedController == null)
            {
                enhancedController = item.AddComponent<EnhancedItemController>();
            }
            
            // EnhancedItemController의 SetItemType을 호출하여 모든 설정을 적용
            // (시각적 설정, 게임플레이 속성, 마그네틱 힘 값 등 모든 설정이 포함됨)
            enhancedController.SetItemType(itemType);
            
            Debug.Log($"[SetupItemComponents] {itemType} 아이템 설정 완료 - CollectibleItem + EnhancedItemController 사용: {item.name}");
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
            Debug.Log($"[ReceiveGameManagerEnhanced] CollectItem 호출됨: 플레이어 {playerActorNumber}, 아이템 타입: {itemType}, 게임 시작됨: {isGameStarted}, 게임 종료됨: {isGameEnded}");
            
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
                    Debug.Log($"[ReceiveGameManagerEnhanced] Master Client에서 직접 점수 처리: 플레이어 {playerActorNumber}, 아이템 타입: {itemType}");
                    CollectItemRPC(playerActorNumber, itemType);
                }
                else
                {
                    // Non-Master Client는 Master Client에게 점수 증가 요청
                    Debug.Log($"[ReceiveGameManagerEnhanced] Non-Master Client에서 Master Client에게 점수 요청: 플레이어 {playerActorNumber}, 아이템 타입: {itemType}");
                    photonViewRef.RPC(nameof(RequestScoreIncrease), RpcTarget.MasterClient, playerActorNumber, (int)itemType);
                }
            }
            else
            {
                Debug.LogWarning($"[ReceiveGameManagerEnhanced] 게임이 시작되지 않았거나 이미 종료됨. 게임 시작됨: {isGameStarted}, 게임 종료됨: {isGameEnded}");
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
                    photonViewRef.RPC(nameof(RequestObstaclePenalty), RpcTarget.MasterClient, playerActorNumber, penaltyPoints);
                }
            }
        }
        
        [PunRPC]
        private void RequestScoreIncrease(int playerActorNumber, int itemType)
        {
            // Master Client에서만 실행되는 RPC
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log($"[ReceiveGameManagerEnhanced] RequestScoreIncrease RPC 수신: 플레이어 {playerActorNumber}, 아이템 타입: {(ItemType)itemType}");
                CollectItemRPC(playerActorNumber, (ItemType)itemType);
            }
            else
            {
                Debug.LogWarning($"[ReceiveGameManagerEnhanced] RequestScoreIncrease RPC가 Non-Master Client에서 호출됨: 플레이어 {playerActorNumber}, 아이템 타입: {(ItemType)itemType}");
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
            
            Debug.Log($"[ReceiveGameManagerEnhanced] CollectItemRPC 실행: 플레이어 {playerActorNumber}, 아이템 타입: {itemType}");
            
            // 플레이어 점수 초기화
            if (!playerScores.ContainsKey(playerActorNumber))
            {
                playerScores[playerActorNumber] = 0;
                Debug.Log($"[ReceiveGameManagerEnhanced] 플레이어 {playerActorNumber} 점수 초기화: 0");
            }
            
            // 아이템 타입에 따른 점수 계산
            int scoreToAdd = GetScoreForItemType(itemType);
            int previousScore = playerScores[playerActorNumber];
            playerScores[playerActorNumber] += scoreToAdd;
            
            Debug.Log($"[ReceiveGameManagerEnhanced] 점수 추가: 플레이어 {playerActorNumber}, 이전 점수: {previousScore}, 추가 점수: {scoreToAdd}, 새로운 점수: {playerScores[playerActorNumber]}");
            
            // 플레이어 속성으로 점수 업데이트 (네트워크 동기화)
            Player player = PhotonNetwork.CurrentRoom.GetPlayer(playerActorNumber);
            if (player != null)
            {
                ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
                playerProps["score"] = playerScores[playerActorNumber];
                player.SetCustomProperties(playerProps);
                Debug.Log($"[ReceiveGameManagerEnhanced] 플레이어 {playerActorNumber} 속성 업데이트 완료: 점수 {playerScores[playerActorNumber]}");
            }
            else
            {
                Debug.LogError($"[ReceiveGameManagerEnhanced] 플레이어 {playerActorNumber}를 찾을 수 없습니다!");
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
                    Debug.Log($"[ReceiveGameManagerEnhanced] {itemType} ScriptableObject에서 점수 가져옴: {itemConfig.pointValue}");
                    return itemConfig.pointValue;
                }
                else
                {
                    Debug.LogWarning($"[ReceiveGameManagerEnhanced] {itemType} ScriptableObject 설정을 찾을 수 없음. 기본값 사용");
                }
            }
            else
            {
                Debug.LogWarning($"[ReceiveGameManagerEnhanced] ItemConfiguration이 null입니다. 기본값 사용");
            }
            
            // Scriptable Object에서 값을 가져올 수 없으면 아이템 타입별 기본값 반환
            int defaultScoreValue;
            switch (itemType)
            {
                case ItemType.Normal:
                    defaultScoreValue = 1;
                    break;
                case ItemType.Bonus:
                    defaultScoreValue = 5;
                    break;
                case ItemType.Speed:
                    defaultScoreValue = 3;
                    break;
                case ItemType.Slow:
                    defaultScoreValue = 2;
                    break;
                case ItemType.Magnet:
                    defaultScoreValue = 4;
                    break;
                default:
                    defaultScoreValue = defaultScore;
                    break;
            }
            
            Debug.Log($"[ReceiveGameManagerEnhanced] {itemType} 기본값 사용: {defaultScoreValue}");
            return defaultScoreValue;
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
                
                // 풀에서 가져온 파워업인지 확인
                CollectibleItem powerUpItem = powerUp.GetComponent<CollectibleItem>();
                if (powerUpItem != null && powerUpItem.returnPool != null)
                {
                    // 풀 시스템으로 반환
                    StopAndRemoveCoroutines(powerUp);
                    itemPoolManager?.ReturnPowerUp(powerUpItem);
                    //Debug.Log("[ReceiveGameManagerEnhanced] 파워업을 풀로 반환");
                }
                else
                {
                    // 일반 오브젝트는 안전하게 제거
                    SafeDestroyObject(powerUp, false);
                }
            }
        }
        
        private IEnumerator DestroyObstacleAfterTime(GameObject obstacle, float time)
        {
            yield return new WaitForSeconds(time);
            
            if (obstacle != null && spawnedObstacles.Contains(obstacle))
            {
                spawnedObstacles.Remove(obstacle);
                
                // 풀에서 가져온 장애물인지 확인
                ObstacleController obstacleController = obstacle.GetComponent<ObstacleController>();
                if (obstacleController != null && obstacleController.returnPool != null)
                {
                    // 풀 시스템으로 반환
                    StopAndRemoveCoroutines(obstacle);
                    itemPoolManager?.ReturnObstacle(obstacleController);
                    ////Debug.Log("[ReceiveGameManagerEnhanced] 장애물을 풀로 반환");
                }
                else
                {
                    // 일반 오브젝트는 안전하게 제거
                    SafeDestroyObject(obstacle, false);
                }
            }
        }
        
        private void ClearAllItems()
        {
            //Debug.Log("[ReceiveGameManagerEnhanced] 모든 아이템 정리 시작");
            
            // 모든 아이템 제거
            foreach (GameObject item in spawnedItems)
            {
                if (item != null)
                {
                    // 풀에서 가져온 아이템인지 확인
                    CollectibleItem collectibleItem = item.GetComponent<CollectibleItem>();
                    if (collectibleItem != null && collectibleItem.returnPool != null)
                    {
                        // 풀 시스템으로 반환
                        StopAndRemoveCoroutines(item);
                        itemPoolManager?.ReturnItem(collectibleItem);
                    }
                    else
                    {
                        // 일반 오브젝트는 안전하게 제거
                        SafeDestroyObject(item, false);
                    }
                }
            }
            spawnedItems.Clear();
            
            // 모든 파워업 제거
            foreach (GameObject powerUp in spawnedPowerUps)
            {
                if (powerUp != null)
                {
                    // 풀에서 가져온 파워업인지 확인
                    CollectibleItem powerUpItem = powerUp.GetComponent<CollectibleItem>();
                    if (powerUpItem != null && powerUpItem.returnPool != null)
                    {
                        // 풀 시스템으로 반환
                        StopAndRemoveCoroutines(powerUp);
                        itemPoolManager?.ReturnPowerUp(powerUpItem);
                    }
                    else
                    {
                        // 일반 오브젝트는 안전하게 제거
                        SafeDestroyObject(powerUp, false);
                    }
                }
            }
            spawnedPowerUps.Clear();
            
            // 모든 장애물 제거
            foreach (GameObject obstacle in spawnedObstacles)
            {
                if (obstacle != null)
                {
                    // 풀에서 가져온 장애물인지 확인
                    ObstacleController obstacleController = obstacle.GetComponent<ObstacleController>();
                    if (obstacleController != null && obstacleController.returnPool != null)
                    {
                        // 풀 시스템으로 반환
                        StopAndRemoveCoroutines(obstacle);
                        itemPoolManager?.ReturnObstacle(obstacleController);
                    }
                    else
                    {
                        // 일반 오브젝트는 안전하게 제거
                        SafeDestroyObject(obstacle, false);
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
            
            // 바닥 높이가 고정되어 있는지 확인
            Vector3 finalTargetPosition = new Vector3(targetPosition.x, itemTargetGroundHeight, targetPosition.z);
            
            Debug.Log($"[DropItemToGround] 아이템 드롭 시작 - 시작위치: {startPosition}, 목표위치: {finalTargetPosition}, 설정된 바닥높이: {itemTargetGroundHeight}, 아이템: {item.name}");
            
            // Rigidbody 중력 비활성화 (코루틴으로 위치 제어)
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            while (elapsedTime < itemDropToGroundSpeed && item != null && item.activeInHierarchy)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / itemDropToGroundSpeed;
                
                // 부드러운 떨어지는 효과 (ease-out)
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                
                Vector3 newPosition = Vector3.Lerp(startPosition, finalTargetPosition, easedProgress);
                item.transform.position = newPosition;
                
                yield return null;
            }
            
            // 최종 위치로 정확히 설정
            if (item != null && item.activeInHierarchy)
            {
                item.transform.position = finalTargetPosition;
                Debug.Log($"[DropItemToGround] 아이템 드롭 완료 - 최종위치: {finalTargetPosition}, 아이템: {item.name}");
                
                // Rigidbody 중력 다시 활성화 (바닥에 착지 후)
                if (rb != null)
                {
                    rb.useGravity = true;
                }
                
                // CollectibleItem에 바닥 착지 알림
                CollectibleItem collectibleItem = item.GetComponent<CollectibleItem>();
                if (collectibleItem != null)
                {
                    // HasLandedOnGround를 true로 설정하여 ItemAnimation이 바닥에 고정되도록 함
                    collectibleItem.HasLandedOnGround = true;
                    Debug.Log($"[DropItemToGround] CollectibleItem.HasLandedOnGround = true 설정 - 아이템: {item.name}");
                }
                
                // EnhancedItemController에 드롭 애니메이션 완료 알림
                EnhancedItemController enhancedController = item.GetComponent<EnhancedItemController>();
                if (enhancedController != null)
                {
                    enhancedController.OnDropAnimationComplete();
                    Debug.Log($"[DropItemToGround] EnhancedItemController.OnDropAnimationComplete() 호출됨 - 아이템: {item.name}");
                }
                else
                {
                    Debug.LogWarning($"[DropItemToGround] EnhancedItemController를 찾을 수 없음 - 아이템: {item.name}");
                }
            }
            else
            {
                // 아이템이 수집되었는지 확인
                CollectibleItem collectibleItem = item?.GetComponent<CollectibleItem>();
                if (collectibleItem != null && collectibleItem.IsCollected)
                {
                    // 아이템이 수집되어 비활성화된 경우 (예상된 동작)
                    Debug.Log($"[DropItemToGround] 아이템이 수집되어 비활성화됨 - 아이템: {item.name}");
                }
                else
                {
                    // 아이템이 예상치 못한 이유로 비활성화되거나 파괴된 경우 (진정한 경고)
                    Debug.LogWarning($"[DropItemToGround] 아이템이 중간에 비활성화되거나 파괴됨 (예상치 못한 이유) - 아이템: {(item != null ? item.name : "null")}");
                }
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
            
            while (elapsedTime < obstacleDropToGroundSpeed && obstacle != null)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / obstacleDropToGroundSpeed;
                
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
            
            // 동점 처리 로직으로 순위 설정
            int currentRank = 1;
            int currentScore = -1;
            
            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                int playerActorNumber = sortedPlayers[i].Key;
                int playerScore = sortedPlayers[i].Value;
                
                // 새로운 점수인 경우 랭킹 증가
                if (playerScore != currentScore)
                {
                    currentRank = i + 1;
                    currentScore = playerScore;
                }
                // 같은 점수인 경우 현재 랭킹 유지 (동점 처리)
                
                Player player = PhotonNetwork.CurrentRoom.GetPlayer(playerActorNumber);
                if (player != null)
                {
                    ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
                    playerProps["rank"] = currentRank;
                    player.SetCustomProperties(playerProps);
                    
                    // 디버그 로그 (동점 처리 확인용)
                    Debug.Log($"[랭킹 계산] 플레이어 {player.NickName}: 점수 {playerScore}, 랭킹 {currentRank}");
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
            
            // JTW GameManager의 isLoaded 시스템을 사용하여 모든 플레이어가 씬을 로드했는지 확인
            if (changedProps.ContainsKey("isLoaded"))
            {
                // 모든 플레이어가 로드되었는지 확인하고 카운트다운 시작
                CheckAllPlayersReady();
            }
        }
        
        private void CheckAllPlayersReady()
        {
            // 최소 1명 이상이면 게임 시작 가능
            if (PhotonNetwork.PlayerList.Length < 1) 
            {
                return;
            }
            
            // JTW GameManager의 isAllPlayerLoaded() 메서드를 사용하여 모든 플레이어가 씬을 로드했는지 확인
            bool allPlayersLoaded = false;
            if (Manager.game != null)
            {
                allPlayersLoaded = Manager.game.isAllPlayerLoaded();
            }
            else
            {
                // JTW GameManager가 없는 경우 기본 준비 상태 확인
                allPlayersLoaded = true;
                foreach (Player player in PhotonNetwork.PlayerList)
                {
                    bool isPlayerReady = PhotonManager.Instance.GetPlayerReady(player);
                    
                    if (!isPlayerReady)
                    {
                        allPlayersLoaded = false;
                    }
                }
            }
            
            if (allPlayersLoaded && !isGameStarted && PhotonNetwork.IsMasterClient)
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
                // 모든 클라이언트에게 카메라 전환과 게임 시작을 동기화
                photonViewRef.RPC(nameof(RPCStartGameAndTransition), RpcTarget.All);
            }
            
            isCountdownActive = false;
            countdownCoroutine = null;
        }
        #endregion
        
        private void OnDestroy()
        {
            // 스폰 코루틴들을 명시적으로 중지
            StopAllSpawningCoroutines();
            
            // 카운트다운 코루틴 중지
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }
            
            // 모든 오브젝트의 코루틴 중지
            foreach (var kvp in objectCoroutines)
            {
                if (kvp.Key != null)
                {
                    StopAndRemoveCoroutines(kvp.Key);
                }
            }
            objectCoroutines.Clear();
            
            // 게임 매니저가 파괴될 때 풀 정리
            if (itemPoolManager != null)
            {
                itemPoolManager.ClearAllActiveObjects();
            }
        }

        #region Debug Methods
        /// <summary>
        /// 점수 시스템 테스트를 위한 디버그 메서드
        /// </summary>
        [ContextMenu("Test Score System")]
        public void TestScoreSystem()
        {
            Debug.Log("[ReceiveGameManagerEnhanced] 점수 시스템 테스트 시작");
            
            // 현재 게임 상태 확인
            Debug.Log($"게임 시작됨: {isGameStarted}, 게임 종료됨: {isGameEnded}");
            Debug.Log($"Master Client: {PhotonNetwork.IsMasterClient}");
            Debug.Log($"현재 플레이어 수: {PhotonNetwork.PlayerList.Length}");
            
            // 각 플레이어의 현재 점수 출력
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                int currentScore = playerScores.ContainsKey(player.ActorNumber) ? playerScores[player.ActorNumber] : 0;
                Debug.Log($"플레이어 {player.ActorNumber} ({player.NickName}) 현재 점수: {currentScore}");
            }
            
            // ItemConfiguration 상태 확인
            if (itemConfiguration != null)
            {
                Debug.Log("ItemConfiguration 로드됨");
                foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType)))
                {
                    ItemConfig config = itemConfiguration.GetItemConfig(itemType);
                    if (config != null)
                    {
                        Debug.Log($"{itemType} 아이템 설정 - 점수: {config.pointValue}");
                    }
                    else
                    {
                        Debug.LogWarning($"{itemType} 아이템 설정을 찾을 수 없음");
                    }
                }
            }
            else
            {
                Debug.LogError("ItemConfiguration이 null입니다!");
            }
        }
        
        /// <summary>
        /// 특정 플레이어에게 테스트 점수 추가
        /// </summary>
        [ContextMenu("Add Test Score")]
        public void AddTestScore()
        {
            if (PhotonNetwork.LocalPlayer != null)
            {
                Debug.Log($"[ReceiveGameManagerEnhanced] 테스트 점수 추가: 플레이어 {PhotonNetwork.LocalPlayer.ActorNumber}");
                CollectItem(PhotonNetwork.LocalPlayer.ActorNumber, ItemType.Bonus);
            }
        }
        #endregion
    }
} 