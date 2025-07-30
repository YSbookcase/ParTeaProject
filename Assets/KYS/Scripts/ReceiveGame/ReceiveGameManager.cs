using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace KYS
{
    public class ReceiveGameManager : MonoBehaviourPunCallbacks
    {
        [Header("Game Settings")]
        [SerializeField] private float gameTime = 60f;
        [SerializeField] private Vector2 mapSize = new Vector2(30f, 30f); // 맵 크기 (더 크게)
        [SerializeField] private float wallHeight = 2f; // 벽 높이 (더 낮게)
        
        [Header("UI References")]
        [SerializeField] private ReceiveGameUI gameUI;
        
        [Header("Game Objects")]
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private GameObject wallPrefab; // 벽 프리팹
        [SerializeField] private GameObject obstaclePrefab; // 장애물 프리팹
        [SerializeField] private GameObject powerUpPrefab; // 파워업 프리팹
        [SerializeField] private Transform[] spawnPoints; // 기존 스폰 포인트 (선택적)
        
        [Header("Object Pool")]
        [SerializeField] private ItemPoolManager itemPoolManager; // 아이템 풀 매니저
        
        [Header("Item Spawn Settings")]
        [SerializeField] private float itemSpawnInterval = 2f; // 아이템 스폰 간격
        [SerializeField] private float powerUpSpawnInterval = 10f; // 파워업 스폰 간격
        [SerializeField] private float obstacleSpawnInterval = 5f; // 장애물 스폰 간격
        
        private float currentTime;
        private bool isGameStarted = false;
        private bool isGameEnded = false;
        
        private Dictionary<int, int> playerScores = new Dictionary<int, int>();
        private List<GameObject> spawnedItems = new List<GameObject>();
        private List<GameObject> spawnedObstacles = new List<GameObject>();
        private List<GameObject> spawnedPowerUps = new List<GameObject>();
        private List<int> alivePlayers = new List<int>();
        
        // 아이템 타입 열거형
        public enum ItemType
        {
            Normal,     // 일반 아이템 (1점)
            Bonus,      // 보너스 아이템 (3점)
            Speed,      // 속도 증가
            Slow,       // 속도 감소
            Magnet      // 자석 효과 (아이템 끌어오기)
        }
        
        // Room Properties 키들
        private const string GAME_STARTED_KEY = "gameStarted";
        private const string GAME_TIME_KEY = "gameTime";
        private const string GAME_ENDED_KEY = "gameEnded";
        
        private void Start()
        {
            // 아이템 프리팹이 설정되지 않은 경우 Resources에서 로드
            if (itemPrefab == null)
            {
                itemPrefab = Resources.Load<GameObject>("KYSItemPrefab");
                if (itemPrefab != null)
                {
                    Debug.Log("[ReceiveGameManager] KYSItemPrefab을 Resources에서 로드했습니다.");
                }
                else
                {
                    Debug.LogError("[ReceiveGameManager] Resources/KYSItemPrefab을 찾을 수 없습니다!");
                }
            }
            
            currentTime = gameTime;
            UpdateUI();
            
            // 디버그 로그 추가
            Debug.Log($"ReceiveGameManager 시작 - 플레이어 수: {PhotonNetwork.PlayerList.Length}");
            
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
                Hashtable playerProps = new Hashtable();
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
            if (PhotonNetwork.IsMasterClient)
            {
                // Room Properties로 게임 상태 설정
                Hashtable roomProps = new Hashtable();
                roomProps[GAME_STARTED_KEY] = true;
                roomProps[GAME_TIME_KEY] = gameTime;
                roomProps[GAME_ENDED_KEY] = false;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            }
            
            isGameStarted = true;
            Debug.Log("ReceiveGame 시작!");
        }
        
        private float timeUpdateInterval = 0.1f; // 0.1초마다 시간 업데이트
        private float timeUpdateTimer = 0f;
        
        private void UpdateGameTime()
        {
            // 정확한 시간 계산
            currentTime -= Time.deltaTime;
            timeUpdateTimer += Time.deltaTime;
            
            // 시간이 0 이하가 되면 게임 종료
            if (currentTime <= 0)
            {
                currentTime = 0;
                EndGame();
                return;
            }
            
            // 0.1초마다 UI와 Room Properties 업데이트 (성능 최적화)
            if (timeUpdateTimer >= timeUpdateInterval)
            {
                timeUpdateTimer = 0f;
                
                // Room Properties로 시간 업데이트
                if (PhotonNetwork.IsMasterClient)
                {
                    Hashtable roomProps = new Hashtable();
                    roomProps[GAME_TIME_KEY] = currentTime;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
                }
                
                UpdateUI();
                
                // 디버그 로그 (1초마다)
                if (Mathf.FloorToInt(currentTime) % 10 == 0 && currentTime > 0)
                {
                    Debug.Log($"게임 시간: {currentTime:F1}초");
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
            Debug.Log("아이템 스폰 시작");
            
            while (isGameStarted && !isGameEnded)
            {
                yield return new WaitForSeconds(itemSpawnInterval);
                
                // 맵 전체 영역에서 랜덤하게 아이템 스폰
                Vector3 randomPosition = GetRandomPositionInMap();
                Debug.Log($"아이템 스폰 위치 계산: {randomPosition}");
                SpawnItem(randomPosition);
            }
        }
        
        private IEnumerator SpawnPowerUpsRoutine()
        {
            Debug.Log("파워업 스폰 시작");
            
            while (isGameStarted && !isGameEnded)
            {
                yield return new WaitForSeconds(powerUpSpawnInterval);
                
                Vector3 randomPosition = GetRandomPositionInMap();
                SpawnPowerUp(randomPosition);
                Debug.Log($"파워업 스폰: {randomPosition}");
            }
        }
        
        private IEnumerator SpawnObstaclesRoutine()
        {
            Debug.Log("장애물 스폰 시작");
            
            while (isGameStarted && !isGameEnded)
            {
                yield return new WaitForSeconds(obstacleSpawnInterval);
                
                Vector3 randomPosition = GetRandomPositionInMap();
                SpawnObstacle(randomPosition);
                Debug.Log($"장애물 스폰: {randomPosition}");
            }
        }
        
        private void SpawnItem(Vector3 position)
        {
            Debug.Log($"SpawnItem 호출됨 - 위치: {position}");
            
            // 일반 Instantiate 방식 사용 (오브젝트 풀링 대신)
            if (itemPrefab != null)
            {
                GameObject item = Instantiate(itemPrefab, position, Quaternion.identity);
                spawnedItems.Add(item);
                Debug.Log($"아이템 스폰 완료 (일반 방식): {position}");
                
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
            }
            // 오브젝트 풀 사용 (폴백)
            else if (itemPoolManager != null)
            {
                CollectibleItem item = itemPoolManager.GetItem(position);
                if (item != null)
                {
                    spawnedItems.Add(item.gameObject);
                    Debug.Log($"아이템 스폰 완료 (오브젝트 풀): {position}, 아이템 활성화 상태: {item.gameObject.activeInHierarchy}, 위치: {item.transform.position}");
                    
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
                    
                    // PooledObject 컴포넌트 확인 (CollectibleItem이 PooledObject를 상속받음)
                    if (item is PooledObject)
                    {
                        PooledObject pooledObj = item as PooledObject;
                        Debug.Log($"PooledObject 기능 확인됨 - returnPool: {pooledObj.returnPool != null}");
                    }
                    else
                    {
                        Debug.LogError("아이템이 PooledObject를 상속받지 않았습니다!");
                    }
                }
                else
                {
                    Debug.LogError($"아이템 풀에서 아이템을 가져오지 못했습니다!");
                }
            }
            else
            {
                Debug.LogWarning("ItemPrefab과 ItemPoolManager가 모두 할당되지 않았습니다!");
            }
        }
        

        
        // 맵 생성 메서드
        private void CreateMap()
        {
            if (wallPrefab == null)
            {
                Debug.LogWarning("WallPrefab이 할당되지 않았습니다!");
                return;
            }
            
            // 바닥 생성
            CreateGround();
            
            // 맵 경계에 벽 생성
            float halfWidth = mapSize.x / 2f;
            float halfHeight = mapSize.y / 2f;
            
            // 위쪽 벽
            CreateWall(new Vector3(0, wallHeight/2, halfHeight), new Vector3(mapSize.x, wallHeight, 1));
            // 아래쪽 벽
            CreateWall(new Vector3(0, wallHeight/2, -halfHeight), new Vector3(mapSize.x, wallHeight, 1));
            // 왼쪽 벽
            CreateWall(new Vector3(-halfWidth, wallHeight/2, 0), new Vector3(1, wallHeight, mapSize.y));
            // 오른쪽 벽
            CreateWall(new Vector3(halfWidth, wallHeight/2, 0), new Vector3(1, wallHeight, mapSize.y));
            
            Debug.Log("맵 생성 완료");
        }
        
        private void CreateGround()
        {
            // 바닥 생성
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(mapSize.x / 10f, 1, mapSize.y / 10f); // Plane은 기본 10x10 크기
            
            // 바닥 머티리얼 설정
            Renderer groundRenderer = ground.GetComponent<Renderer>();
            if (groundRenderer != null)
            {
                Material groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                groundMaterial.color = new Color(0.3f, 0.3f, 0.3f); // 어두운 회색
                groundRenderer.material = groundMaterial;
            }
            
            // 바닥에 Physic Material 추가
            BoxCollider groundCollider = ground.GetComponent<BoxCollider>();
            if (groundCollider != null)
            {
                PhysicMaterial groundPhysicMaterial = new PhysicMaterial("GroundPhysicMaterial");
                groundPhysicMaterial.dynamicFriction = 0.8f;
                groundPhysicMaterial.staticFriction = 0.8f;
                groundPhysicMaterial.bounciness = 0.0f;
                groundCollider.material = groundPhysicMaterial;
            }
            
            Debug.Log("바닥 생성 완료");
        }
        
        private void CreateWall(Vector3 position, Vector3 scale)
        {
            GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity);
            wall.transform.localScale = scale;
        }
        
        // 맵 내 랜덤 위치 생성 (하늘에서 떨어지는 위치)
        private Vector3 GetRandomPositionInMap()
        {
            // 맵 전체 영역에서 랜덤하게 스폰 (중앙 제외)
            float halfWidth = mapSize.x / 2f - 3f; // 벽에서 더 안쪽
            float halfHeight = mapSize.y / 2f - 3f;
            
            // 중앙 영역을 제외하고 스폰 (중앙 4x4 영역 제외)
            float x, z;
            do
            {
                x = Random.Range(-halfWidth, halfWidth);
                z = Random.Range(-halfHeight, halfHeight);
            } while (Mathf.Abs(x) < 2f && Mathf.Abs(z) < 2f); // 중앙 4x4 영역 제외
            
            // 높이도 랜덤하게 설정 (15-25 범위)
            float y = Random.Range(15f, 25f);
            
            Debug.Log($"아이템 스폰 위치: ({x}, {y}, {z}) - 맵 크기: {mapSize}");
            return new Vector3(x, y, z);
        }
        
        private void SpawnPowerUp(Vector3 position)
        {
            if (powerUpPrefab != null)
            {
                GameObject powerUp = Instantiate(powerUpPrefab, position, Quaternion.identity);
                spawnedPowerUps.Add(powerUp);
                
                // 파워업 타입 설정
                ItemType powerUpType = (ItemType)Random.Range(2, 5); // Speed, Slow, Magnet
                powerUp.GetComponent<ItemController>()?.SetItemType(powerUpType);
                
                // 파워업의 ItemController에서 자동으로 Rigidbody 설정됨
                Debug.Log($"파워업 스폰 완료: {position}, 타입: {powerUpType}");
                
                // 60초 후 자동 제거 (바닥에 오래 남아있도록)
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
                    Hashtable playerProps = new Hashtable();
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
                // Room Properties로 게임 종료 설정
                Hashtable roomProps = new Hashtable();
                roomProps[GAME_ENDED_KEY] = true;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
                
                Debug.Log("랭크 계산 및 JTW Score 씬 전환 준비 중...");
                CalculateAndSetRanks();
            }
            else
            {
                Debug.Log("마스터 클라이언트가 랭크 계산을 처리합니다...");
            }
        }
        
        private void ClearAllItems()
        {
            // 모든 아이템 제거 (오브젝트 풀 사용)
            foreach (GameObject item in spawnedItems)
            {
                if (item != null)
                {
                    CollectibleItem collectibleItem = item.GetComponent<CollectibleItem>();
                    if (collectibleItem != null && itemPoolManager != null)
                    {
                        itemPoolManager.ReturnItem(collectibleItem);
                    }
                    else
                    {
                        Debug.LogWarning($"[ReceiveGameManager] 아이템을 안전하게 파괴합니다: {item.name}");
                        Destroy(item);
                    }
                }
            }
            spawnedItems.Clear();
            
            // 모든 장애물 제거
            foreach (GameObject obstacle in spawnedObstacles)
            {
                if (obstacle != null)
                {
                    Destroy(obstacle);
                }
            }
            spawnedObstacles.Clear();
            
            // 모든 파워업 제거
            foreach (GameObject powerUp in spawnedPowerUps)
            {
                if (powerUp != null)
                {
                    Destroy(powerUp);
                }
            }
            spawnedPowerUps.Clear();
        }
        
        private void CalculateAndSetRanks()
        {
            // 점수별로 플레이어 정렬
            List<KeyValuePair<int, int>> sortedPlayers = new List<KeyValuePair<int, int>>(playerScores);
            sortedPlayers.Sort((a, b) => b.Value.CompareTo(a.Value));
            
            // 랭크 설정
            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                int rank = i + 1;
                int playerActorNumber = sortedPlayers[i].Key;
                
                Player player = PhotonNetwork.CurrentRoom.GetPlayer(playerActorNumber);
                if (player != null)
                {
                    Hashtable props = new Hashtable();
                    props["rank"] = rank;
                    player.SetCustomProperties(props);
                    Debug.Log($"플레이어 {player.NickName} 랭크: {rank}");
                }
            }
            
            // JTW 스코어 씬으로 이동
            StartCoroutine(LoadScoreScene());
        }
        
        private IEnumerator LoadScoreScene()
        {
            Debug.Log("게임 결과 표시 중...");
            yield return new WaitForSeconds(3f); // 결과 표시 시간
            
            Debug.Log("JTW Score 씬으로 전환 중...");
            
            // 모든 플레이어에게 랭크 정보 전달
            foreach (var score in playerScores)
            {
                Player player = PhotonNetwork.CurrentRoom.GetPlayer(score.Key);
                if (player != null)
                {
                    // 점수 정보를 Player Custom Properties에 저장
                    Hashtable props = new Hashtable();
                    props["finalScore"] = score.Value;
                    props["gameType"] = "ReceiveGame"; // 게임 타입 표시
                    player.SetCustomProperties(props);
                }
            }
            
            // JTW 스코어 씬 로드
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel("Score");
            }
        }
        
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            // 플레이어가 나가면 alivePlayers에서 제거
            alivePlayers.Remove(otherPlayer.ActorNumber);
            
            // 모든 플레이어가 나가면 게임 종료
            if (PhotonNetwork.PlayerList.Length < 1)
            {
                EndGame();
            }
        }
        
        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            // Room Properties 변경 감지
            if (propertiesThatChanged.ContainsKey(GAME_STARTED_KEY))
            {
                isGameStarted = (bool)propertiesThatChanged[GAME_STARTED_KEY];
                Debug.Log($"게임 시작 상태 변경: {isGameStarted}");
            }
            
            if (propertiesThatChanged.ContainsKey(GAME_TIME_KEY))
            {
                float newTime = (float)propertiesThatChanged[GAME_TIME_KEY];
                // 시간 차이가 0.5초 이상이면 동기화 (더 민감하게)
                if (Mathf.Abs(currentTime - newTime) > 0.5f)
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
                    if (gameUI != null)
                    {
                        gameUI.ShowGameEnd();
                    }
                }
            }
        }
        
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            // 플레이어 점수 업데이트 감지
            if (changedProps.ContainsKey("score"))
            {
                int newScore = (int)changedProps["score"];
                playerScores[targetPlayer.ActorNumber] = newScore;
                UpdateUI();
            }
            
            // 기존 로드 완료 체크
            if (changedProps.ContainsKey("isLoaded"))
            {
                try
                {
                    if (Manager.game.isAllPlayerLoaded())
                    {
                        Debug.Log("모든 플레이어 로드 완료 - 게임 시작");
                        InitializeGame();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Manager.game.isAllPlayerLoaded() 호출 중 오류: {e.Message}");
                    // Manager가 없는 경우 단일 플레이어 모드로 시작
                    if (PhotonNetwork.PlayerList.Length == 1)
                    {
                        Debug.Log("Manager 없음 - 단일 플레이어 모드로 시작");
                        InitializeGame();
                    }
                }
            }
        }
    }
} 