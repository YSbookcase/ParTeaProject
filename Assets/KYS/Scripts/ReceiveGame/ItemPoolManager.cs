using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace KYS
{
    public class ItemPoolManager : MonoBehaviour
    {
        // 싱글톤 인스턴스
        private static ItemPoolManager instance;
        public static ItemPoolManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ItemPoolManager>();
                    if (instance == null)
                    {
                        Debug.LogWarning("[ItemPoolManager] 씬에 ItemPoolManager가 없습니다. 새로 생성합니다.");
                        GameObject go = new GameObject("ItemPoolManager");
                        instance = go.AddComponent<ItemPoolManager>();
                    }
                }
                return instance;
            }
        }

        [Header("Item Pool Settings")]
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private int itemPoolSize = 20;
        
        [Header("PowerUp Pool Settings - Type Specific")]
        [SerializeField] private GameObject speedPowerUpPrefab;
        [SerializeField] private GameObject slowPowerUpPrefab;
        [SerializeField] private GameObject magnetPowerUpPrefab;
        [SerializeField] private GameObject bonusPowerUpPrefab;
        [SerializeField] private int powerUpPoolSizePerType = 5; // 타입별 풀 크기

        [Header("Obstacle Pool Settings")]
        [SerializeField] private GameObject obstaclePrefab;
        [SerializeField] private int obstaclePoolSize = 10;

        [Header("Pool Containers")]
        [SerializeField] private Transform itemPoolContainer;
        [SerializeField] private Transform powerUpPoolContainer;
        [SerializeField] private Transform obstaclePoolContainer;

        private ObjectPool itemPool;
        private Dictionary<ItemType, ObjectPool> powerUpPools; // 타입별 PowerUp 풀
        private ObjectPool obstaclePool;

        // 활성 오브젝트 추적
        private List<CollectibleItem> activeItems = new List<CollectibleItem>();
        private Dictionary<ItemType, List<CollectibleItem>> activePowerUps; // 타입별 활성 PowerUp
        private List<ObstacleController> activeObstacles = new List<ObstacleController>();

        // 설정 파일
        private ItemConfiguration itemConfiguration;
        
        // 초기화 완료 여부
        private bool isInitialized = false;
        
        private void Awake()
        {
            // 싱글톤 패턴 구현
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[ItemPoolManager] 싱글톤 인스턴스 생성 및 DontDestroyOnLoad 설정");
            }
            else if (instance != this)
            {
                Debug.LogWarning("[ItemPoolManager] 중복된 ItemPoolManager 발견. 기존 인스턴스를 유지하고 새 인스턴스를 파괴합니다.");
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            // 이미 초기화된 경우 스킵
            if (isInitialized)
            {
                Debug.Log("[ItemPoolManager] 이미 초기화되어 있습니다. 스킵합니다.");
                return;
            }
            
            // ItemConfiguration 로드
            LoadItemConfiguration();
            
            InitializePools();
            isInitialized = true;
        }
        
        private void LoadItemConfiguration()
        {
            // Resources 폴더에서 로드 시도 (실제 위치)
            itemConfiguration = Resources.Load<ItemConfiguration>("ItemConfiguration");
            if (itemConfiguration != null)
            {
                Debug.Log("[ItemPoolManager] Resources에서 ItemConfiguration 로드 완료");
                LogItemConfigurationDetails();
                return;
            }
            
            // KYS/Resources 경로에서 로드 시도 (대체 경로)
            itemConfiguration = Resources.Load<ItemConfiguration>("KYS/Resources/ItemConfiguration");
            if (itemConfiguration != null)
            {
                Debug.Log("[ItemPoolManager] KYS/Resources에서 ItemConfiguration 로드 완료");
                LogItemConfigurationDetails();
                return;
            }
            
            // KYS/ScriptableObject 경로에서 로드 시도 (대체 경로)
            itemConfiguration = Resources.Load<ItemConfiguration>("KYS/ScriptableObject/ItemConfiguration");
            if (itemConfiguration != null)
            {
                Debug.Log("[ItemPoolManager] KYS/ScriptableObject에서 ItemConfiguration 로드 완료");
                LogItemConfigurationDetails();
                return;
            }
            
            // 에디터에서만 사용 가능한 방법으로 로드 시도
            #if UNITY_EDITOR
            ItemConfiguration[] configs = Resources.FindObjectsOfTypeAll<ItemConfiguration>();
            if (configs.Length > 0)
            {
                itemConfiguration = configs[0];
                Debug.Log($"[ItemPoolManager] FindObjectsOfTypeAll로 ItemConfiguration 로드 완료: {itemConfiguration.name}");
                LogItemConfigurationDetails();
                return;
            }
            #endif
            
            Debug.LogError("[ItemPoolManager] ItemConfiguration을 찾을 수 없습니다! 모든 경로에서 시도했지만 실패했습니다.");
        }
        
        private void LogItemConfigurationDetails()
        {
            if (itemConfiguration == null) return;
            
            Debug.Log($"[ItemPoolManager] ItemConfiguration 상세 정보:");
            Debug.Log($"  - 이름: {itemConfiguration.name}");
            
            foreach (ItemConfig config in itemConfiguration.itemConfigs)
            {
                Debug.Log($"  - {config.itemType}: 색상={config.itemColor}, 크기={config.scale}, 사운드={config.collectSoundName}");
            }
        }
        
        private void InitializePools()
        {
            Debug.Log("[ItemPoolManager] 풀 초기화 시작");
            
            // 컨테이너 생성
            if (itemPoolContainer == null)
            {
                GameObject itemContainer = new GameObject("ItemPoolContainer");
                itemPoolContainer = itemContainer.transform;
                itemPoolContainer.SetParent(transform);
                DontDestroyOnLoad(itemContainer);
            }
            
            if (powerUpPoolContainer == null)
            {
                GameObject powerUpContainer = new GameObject("PowerUpPoolContainer");
                powerUpPoolContainer = powerUpContainer.transform;
                powerUpPoolContainer.SetParent(transform);
                DontDestroyOnLoad(powerUpContainer);
            }

            if (obstaclePoolContainer == null)
            {
                GameObject obstacleContainer = new GameObject("ObstaclePoolContainer");
                obstaclePoolContainer = obstacleContainer.transform;
                obstaclePoolContainer.SetParent(transform);
                DontDestroyOnLoad(obstacleContainer);
            }
            
            // 아이템 풀 초기화
            InitializeItemPool();
            
            // 파워업 풀 초기화 (타입별)
            InitializePowerUpPools();

            // 장애물 풀 초기화
            InitializeObstaclePool();
            
            Debug.Log($"[ItemPoolManager] 풀 초기화 완료 - 아이템: {itemPoolSize}개, 파워업(타입별): {powerUpPoolSizePerType}개, 장애물: {obstaclePoolSize}개");
        }
        
        private void InitializeItemPool()
        {
            if (itemPrefab == null)
            {
                Debug.LogError("[ItemPoolManager] 아이템 프리팹이 설정되지 않았습니다!");
                return;
            }
            
            // 아이템 풀 생성
            GameObject poolObject = new GameObject("ItemPool");
            poolObject.transform.SetParent(itemPoolContainer);
            
            // 풀 오브젝트를 파괴되지 않도록 설정
            DontDestroyOnLoad(poolObject);
            
            itemPool = poolObject.AddComponent<ObjectPool>();
            
            // GameObject를 PooledObject로 캐스팅
            PooledObject pooledItemPrefab = itemPrefab.GetComponent<PooledObject>();
            if (pooledItemPrefab == null)
            {
                Debug.LogError("[ItemPoolManager] 아이템 프리팹에 PooledObject 컴포넌트가 없습니다!");
                return;
            }
            itemPool.poolObject = pooledItemPrefab;
            
            // poolSize 설정
            var poolSizeField = typeof(ObjectPool).GetField("poolSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (poolSizeField != null)
            {
                poolSizeField.SetValue(itemPool, itemPoolSize);
            }
            
            itemPool.CreatePool();
            
            // 풀 오브젝트가 활성 상태인지 확인
            if (!poolObject.activeInHierarchy)
            {
                poolObject.SetActive(true);
                Debug.Log("[ItemPoolManager] 아이템 풀 오브젝트를 활성화했습니다.");
            }
        }
        
        private void InitializePowerUpPools()
        {
            Debug.Log("[ItemPoolManager] InitializePowerUpPools 시작");
            
            // PowerUp 풀 딕셔너리 초기화
            powerUpPools = new Dictionary<ItemType, ObjectPool>();
            activePowerUps = new Dictionary<ItemType, List<CollectibleItem>>();
            
            // PowerUp 타입별 프리팹 매핑
            Dictionary<ItemType, GameObject> powerUpPrefabs = new Dictionary<ItemType, GameObject>
            {
                { ItemType.Speed, speedPowerUpPrefab },
                { ItemType.Slow, slowPowerUpPrefab },
                { ItemType.Magnet, magnetPowerUpPrefab },
                { ItemType.Bonus, bonusPowerUpPrefab }
            };
            
            Debug.Log($"[ItemPoolManager] 파워업 프리팹 상태 - Speed: {(speedPowerUpPrefab != null ? "있음" : "없음")}, Slow: {(slowPowerUpPrefab != null ? "있음" : "없음")}, Magnet: {(magnetPowerUpPrefab != null ? "있음" : "없음")}, Bonus: {(bonusPowerUpPrefab != null ? "있음" : "없음")}");
            
            // 각 PowerUp 타입별로 풀 생성
            foreach (var kvp in powerUpPrefabs)
            {
                ItemType powerUpType = kvp.Key;
                GameObject prefab = kvp.Value;
                
                if (prefab == null)
                {
                    Debug.LogWarning($"[ItemPoolManager] {powerUpType} 파워업 프리팹이 설정되지 않았습니다!");
                    continue;
                }
                
                // 타입별 풀 생성
                GameObject poolObject = new GameObject($"{powerUpType}PowerUpPool");
                poolObject.transform.SetParent(powerUpPoolContainer);
                DontDestroyOnLoad(poolObject);
                
                ObjectPool powerUpPool = poolObject.AddComponent<ObjectPool>();
                
                // GameObject를 PooledObject로 캐스팅
                PooledObject pooledPowerUpPrefab = prefab.GetComponent<PooledObject>();
                if (pooledPowerUpPrefab == null)
                {
                    Debug.LogError($"[ItemPoolManager] {powerUpType} 파워업 프리팹에 PooledObject 컴포넌트가 없습니다!");
                    continue;
                }
                powerUpPool.poolObject = pooledPowerUpPrefab;
                
                // poolSize 설정
                var poolSizeField = typeof(ObjectPool).GetField("poolSize", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (poolSizeField != null)
                {
                    poolSizeField.SetValue(powerUpPool, powerUpPoolSizePerType);
                }
                
                powerUpPool.CreatePool();
                
                // 풀 오브젝트가 활성 상태인지 확인
                if (!poolObject.activeInHierarchy)
                {
                    poolObject.SetActive(true);
                }
                
                // 딕셔너리에 추가
                powerUpPools[powerUpType] = powerUpPool;
                activePowerUps[powerUpType] = new List<CollectibleItem>();
                
                Debug.Log($"[ItemPoolManager] {powerUpType} 파워업 풀 초기화 완료 (크기: {powerUpPoolSizePerType})");
            }
        }
        
        private void InitializeObstaclePool()
        {
            if (obstaclePrefab == null)
            {
                Debug.LogError("[ItemPoolManager] 장애물 프리팹이 설정되지 않았습니다!");
                return;
            }
            
            // 장애물 풀 생성
            GameObject poolObject = new GameObject("ObstaclePool");
            poolObject.transform.SetParent(obstaclePoolContainer);
            
            // 풀 오브젝트를 파괴되지 않도록 설정
            DontDestroyOnLoad(poolObject);
            
            obstaclePool = poolObject.AddComponent<ObjectPool>();
            
            // GameObject를 PooledObject로 캐스팅
            PooledObject pooledObstaclePrefab = obstaclePrefab.GetComponent<PooledObject>();
            if (pooledObstaclePrefab == null)
            {
                Debug.LogError("[ItemPoolManager] 장애물 프리팹에 PooledObject 컴포넌트가 없습니다!");
                return;
            }
            obstaclePool.poolObject = pooledObstaclePrefab;
            
            // poolSize 설정
            var poolSizeField = typeof(ObjectPool).GetField("poolSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (poolSizeField != null)
            {
                poolSizeField.SetValue(obstaclePool, obstaclePoolSize);
            }
            
            obstaclePool.CreatePool();
            
            // 풀 오브젝트가 활성 상태인지 확인
            if (!poolObject.activeInHierarchy)
            {
                poolObject.SetActive(true);
                Debug.Log("[ItemPoolManager] 장애물 풀 오브젝트를 활성화했습니다.");
            }
        }
        
        // 아이템 풀에서 가져오기
        public CollectibleItem GetItem(Vector3 position, ItemType itemType = ItemType.Normal)
        {
            if (itemPool == null)
            {
                Debug.LogError("[ItemPoolManager] 아이템 풀이 초기화되지 않았습니다!");
                return null;
            }
            
            // ObjectPool에서 PooledObject를 가져오고, 이를 CollectibleItem으로 캐스팅
            PooledObject pooledObj = itemPool.ObjectOut();
            
            // null 체크 추가
            if (pooledObj == null)
            {
                Debug.LogError("[ItemPoolManager] ObjectPool.ObjectOut()에서 null을 반환했습니다!");
                return null;
            }
            
            CollectibleItem item = pooledObj as CollectibleItem;

            if (item != null)
            {
                // 아이템 상태 초기화
                item.ResetItem();

                // 아이템 타입 설정
                item.itemType = itemType;

                // 위치 설정
                item.transform.position = position;

                // 아이템의 시각적 설정 적용
                ApplyItemVisualSettings(item.gameObject, itemType);

                // PhotonView 초기화 확인 및 네트워크 동기화 설정
                PhotonView photonView = item.GetComponent<PhotonView>();
                if (photonView != null)
                {
                    if (photonView.ViewID == 0)
                    {
                        // PhotonView가 초기화되지 않은 경우, 수동으로 초기화
                        bool success = PhotonNetwork.AllocateViewID(photonView);
                        if (success)
                        {
                            Debug.Log($"[ItemPoolManager] PhotonView 초기화: ViewID = {photonView.ViewID}");
                        }
                        else
                        {
                            Debug.LogError("[ItemPoolManager] PhotonView 초기화 실패");
                        }
                    }
                    
                    // 네트워크 동기화를 위해 Observed Components 설정
                    if (photonView.ObservedComponents == null || photonView.ObservedComponents.Count == 0)
                    {
                        // Transform을 Observed Component로 추가
                        photonView.ObservedComponents = new List<Component> { item.transform };
                        Debug.Log($"[ItemPoolManager] Transform을 Observed Component로 설정: {item.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ItemPoolManager] 아이템에 PhotonView가 없습니다: {item.name}");
                }

                // 풀 참조 설정 (returnPool이 null인 문제 해결)
                item.returnPool = itemPool;

                // 활성 아이템 목록에 추가
                if (activeItems != null)
                {
                    activeItems.Add(item);
                }

                Debug.Log($"[ItemPoolManager] 아이템 활성화 - 위치: {position}, 타입: {itemType}, ViewID: {photonView?.ViewID}, 활성 아이템 수: {activeItems?.Count ?? 0}");
            }
            else
            {
                Debug.LogError("[ItemPoolManager] PooledObject를 CollectibleItem으로 캐스팅할 수 없습니다!");
            }

            return item;
        }
        
        // 타입별 PowerUp 풀에서 가져오기
        public CollectibleItem GetPowerUp(Vector3 position, ItemType powerUpType)
        {
            Debug.Log($"[ItemPoolManager] GetPowerUp 시작 - 타입: {powerUpType}, 위치: {position}");
            
            if (powerUpPools == null)
            {
                Debug.LogError("[ItemPoolManager] powerUpPools가 null입니다!");
                return null;
            }
            
            if (!powerUpPools.ContainsKey(powerUpType))
            {
                Debug.LogError($"[ItemPoolManager] {powerUpType} 파워업 풀이 초기화되지 않았습니다! 사용 가능한 풀: {string.Join(", ", powerUpPools.Keys)}");
                return null;
            }

            ObjectPool powerUpPool = powerUpPools[powerUpType];
            if (powerUpPool == null)
            {
                Debug.LogError($"[ItemPoolManager] {powerUpType} 파워업 풀이 null입니다!");
                return null;
            }

            // ObjectPool에서 PooledObject를 가져오고, 이를 CollectibleItem으로 캐스팅
            PooledObject pooledObj = powerUpPool.ObjectOut();
            
            // null 체크 추가
            if (pooledObj == null)
            {
                Debug.LogError($"[ItemPoolManager] {powerUpType} ObjectPool.ObjectOut()에서 null을 반환했습니다!");
                return null;
            }
            
            CollectibleItem powerUp = pooledObj as CollectibleItem;

            if (powerUp != null)
            {
                // 파워업 상태 초기화
                powerUp.ResetItem();

                // 아이템 타입 설정
                powerUp.itemType = powerUpType;

                // 위치 설정
                powerUp.transform.position = position;

                // PowerUp 아이템의 시각적 설정 적용
                ApplyItemVisualSettings(powerUp.gameObject, powerUpType);

                // PhotonView 초기화 확인 및 네트워크 동기화 설정
                PhotonView photonView = powerUp.GetComponent<PhotonView>();
                if (photonView != null)
                {
                    if (photonView.ViewID == 0)
                    {
                        // PhotonView가 초기화되지 않은 경우, 수동으로 초기화
                        bool success = PhotonNetwork.AllocateViewID(photonView);
                        if (success)
                        {
                            Debug.Log($"[ItemPoolManager] PhotonView 초기화: ViewID = {photonView.ViewID}");
                        }
                        else
                        {
                            Debug.LogError("[ItemPoolManager] PhotonView 초기화 실패");
                        }
                    }
                    
                    // 네트워크 동기화를 위해 Observed Components 설정
                    if (photonView.ObservedComponents == null || photonView.ObservedComponents.Count == 0)
                    {
                        // Transform을 Observed Component로 추가
                        photonView.ObservedComponents = new List<Component> { powerUp.transform };
                        Debug.Log($"[ItemPoolManager] Transform을 Observed Component로 설정: {powerUp.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ItemPoolManager] PowerUp에 PhotonView가 없습니다: {powerUp.name}");
                }

                // 풀 참조 설정 (returnPool이 null인 문제 해결)
                powerUp.returnPool = powerUpPool;

                // 활성 파워업 목록에 추가
                if (activePowerUps != null && activePowerUps.ContainsKey(powerUpType))
                {
                    activePowerUps[powerUpType].Add(powerUp);
                }

                Debug.Log($"[ItemPoolManager] {powerUpType} 파워업 활성화 - 위치: {position}, ViewID: {photonView?.ViewID}, 활성 {powerUpType} 수: {activePowerUps?[powerUpType]?.Count ?? 0}");
            }
            else
            {
                Debug.LogError($"[ItemPoolManager] {powerUpType} PooledObject를 CollectibleItem으로 캐스팅할 수 없습니다!");
            }

            return powerUp;
        }
        
        // 개별 확률 기반 PowerUp 타입 선택 (ReceiveGameManagerEnhanced에서 확률 전달)
        public CollectibleItem GetRandomPowerUp(Vector3 position, float speedChance = 0.4f, float slowChance = 0.3f, float magnetChance = 0.3f)
        {
            Debug.Log($"[ItemPoolManager] GetRandomPowerUp 시작 - 위치: {position}, Speed: {speedChance}, Slow: {slowChance}, Magnet: {magnetChance}");
            
            // 확률 기반 타입 선택
            float randomValue = Random.Range(0f, 1f);
            float cumulativeChance = 0f;
            
            Debug.Log($"[ItemPoolManager] 랜덤 값: {randomValue}");
            
            // Speed PowerUp 확률
            cumulativeChance += speedChance;
            Debug.Log($"[ItemPoolManager] Speed 체크 - 누적 확률: {cumulativeChance}, 랜덤 값: {randomValue}");
            if (randomValue <= cumulativeChance)
            {
                Debug.Log("[ItemPoolManager] Speed PowerUp 선택됨");
                return GetPowerUp(position, ItemType.Speed);
            }
            
            // Slow PowerUp 확률
            cumulativeChance += slowChance;
            Debug.Log($"[ItemPoolManager] Slow 체크 - 누적 확률: {cumulativeChance}, 랜덤 값: {randomValue}");
            if (randomValue <= cumulativeChance)
            {
                Debug.Log("[ItemPoolManager] Slow PowerUp 선택됨");
                return GetPowerUp(position, ItemType.Slow);
            }
            
            // Magnet PowerUp 확률 (나머지)
            Debug.Log("[ItemPoolManager] Magnet PowerUp 선택됨 (기본값)");
            return GetPowerUp(position, ItemType.Magnet);
        }
        
        // 기존 방식 (균등 확률) - 하위 호환성 유지
        public CollectibleItem GetRandomPowerUp(Vector3 position)
        {
            // PowerUp 타입 배열 (Speed, Slow, Magnet만 - Bonus는 일반 아이템으로 처리)
            ItemType[] powerUpTypes = { ItemType.Speed, ItemType.Slow, ItemType.Magnet };
            
            // 랜덤 타입 선택
            ItemType randomType = powerUpTypes[Random.Range(0, powerUpTypes.Length)];
            
            return GetPowerUp(position, randomType);
        }
        
        // 아이템 타입에 따른 시각적 설정 적용
        private void ApplyItemVisualSettings(GameObject item, ItemType itemType)
        {
            // ItemConfiguration에서 설정 가져오기
            if (itemConfiguration == null)
            {
                Debug.LogWarning("[ItemPoolManager] ItemConfiguration이 null입니다.");
                return;
            }
            
            ItemConfig config = itemConfiguration.GetItemConfig(itemType);
            if (config != null)
            {
                // EnhancedItemController 컴포넌트 찾기
                EnhancedItemController itemController = item.GetComponent<EnhancedItemController>();
                if (itemController != null)
                {
                    // EnhancedItemController의 SetItemType을 호출하여 모든 설정을 적용
                    itemController.SetItemType(itemType);
                    Debug.Log($"[ItemPoolManager] {itemType} 아이템의 EnhancedItemController 설정 적용 완료");
                }
                else
                {
                    // EnhancedItemController가 없는 경우 기본 시각적 설정만 적용
                    ApplyBasicVisualSettings(item, config);
                }
                
                // CollectibleItem에도 ItemConfiguration 참조 설정
                CollectibleItem collectibleItem = item.GetComponent<CollectibleItem>();
                if (collectibleItem != null)
                {
                    // SetItemConfiguration 메서드를 사용하여 ItemConfiguration 참조 설정
                    collectibleItem.SetItemConfiguration(itemConfiguration);
                    Debug.Log($"[ItemPoolManager] {itemType} 아이템의 CollectibleItem에 ItemConfiguration 참조 설정 완료");
                }
            }
            else
            {
                Debug.LogWarning($"[ItemPoolManager] {itemType} 타입에 대한 설정을 찾을 수 없습니다.");
            }
        }
        
        private void ApplyBasicVisualSettings(GameObject item, ItemConfig config)
        {
            // 색상 설정
            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = config.itemColor;
            }
            
            // ScriptableObject에서 가져온 크기 설정 적용
            item.transform.localScale = config.scale;
            
            // 발광 효과 설정
            if (config.useEmission && renderer != null && renderer.material != null)
            {
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", config.emissionColor * config.emissionIntensity);
            }
            
            Debug.Log($"[ItemPoolManager] {config.itemType} 아이템 기본 시각적 설정 적용: 색상={config.itemColor}, 크기={config.scale}");
        }
        
        // 장애물 풀에서 가져오기
        public ObstacleController GetObstacle(Vector3 position)
        {
            if (obstaclePool == null)
            {
                Debug.LogError("[ItemPoolManager] 장애물 풀이 초기화되지 않았습니다!");
                return null;
            }
            
            // ObjectPool에서 PooledObject를 가져오고, 이를 ObstacleController로 캐스팅
            PooledObject pooledObj = obstaclePool.ObjectOut();
            ObstacleController obstacle = pooledObj as ObstacleController;

            if (obstacle != null)
            {
                // 장애물 상태 초기화
                obstacle.ResetObstacle();

                // 위치 설정
                obstacle.transform.position = position;

                // PhotonView 초기화 확인
                PhotonView photonView = obstacle.GetComponent<PhotonView>();
                if (photonView != null && photonView.ViewID == 0)
                {
                    // PhotonView가 초기화되지 않은 경우, 수동으로 초기화
                    bool success = PhotonNetwork.AllocateViewID(photonView);
                    if (success)
                    {
                        Debug.Log($"[ItemPoolManager] PhotonView 초기화: ViewID = {photonView.ViewID}");
                    }
                    else
                    {
                        Debug.LogError("[ItemPoolManager] PhotonView 초기화 실패");
                    }
                }

                // 풀 참조 설정 (returnPool이 null인 문제 해결)
                obstacle.returnPool = obstaclePool;

                // 활성 장애물 목록에 추가
                if (activeObstacles != null)
                {
                    activeObstacles.Add(obstacle);
                }

                Debug.Log($"[ItemPoolManager] 장애물 활성화 - 위치: {position}, ViewID: {photonView?.ViewID}, 활성 장애물 수: {activeObstacles?.Count ?? 0}");
            }
            else
            {
                Debug.LogError("[ItemPoolManager] PooledObject를 ObstacleController로 캐스팅할 수 없습니다!");
            }

            return obstacle;
        }
        
        // 아이템 풀로 반환
        public void ReturnItem(CollectibleItem item, float delay = 0f)
        {
            if (item == null) return;

            if (delay > 0f)
            {
                // 개별 아이템에서 지연 반환 처리
                item.ReturnToPoolWithDelay(delay);
            }
            else
            {
                if (activeItems.Contains(item))
                {
                    activeItems.Remove(item);
                }
                if (item.returnPool != null)
                {
                    item.returnPool.ReturnToPool(item);
                    Debug.Log($"[ItemPoolManager] 아이템 반환: {item.name}");
                }
                else
                {
                    Debug.LogWarning($"[ItemPoolManager] 아이템의 returnPool이 null입니다: {item.name}");
                }
            }
        }
        
        // PowerUp 풀로 반환 (타입별)
        public void ReturnPowerUp(CollectibleItem powerUp, float delay = 0f)
        {
            if (powerUp == null) return;

            if (delay > 0f)
            {
                // 개별 PowerUp에서 지연 반환 처리
                powerUp.ReturnToPoolWithDelay(delay);
            }
            else
            {
                // PowerUp 타입 확인 및 해당 풀에서 제거
                ItemType powerUpType = powerUp.itemType;
                if (activePowerUps != null && activePowerUps.ContainsKey(powerUpType) && activePowerUps[powerUpType].Contains(powerUp))
                {
                    activePowerUps[powerUpType].Remove(powerUp);
                }
                
                if (powerUp.returnPool != null)
                {
                    powerUp.returnPool.ReturnToPool(powerUp);
                    Debug.Log($"[ItemPoolManager] {powerUpType} 파워업 반환: {powerUp.name}");
                }
                else
                {
                    Debug.LogWarning($"[ItemPoolManager] PowerUp의 returnPool이 null입니다: {powerUp.name}");
                }
            }
        }
        
        // 장애물 풀로 반환
        public void ReturnObstacle(ObstacleController obstacle, float delay = 0f)
        {
            if (obstacle == null) return;

            if (delay > 0f)
            {
                // 개별 장애물에서 지연 반환 처리
                obstacle.ReturnToPoolWithDelay(delay);
            }
            else
            {
                if (activeObstacles.Contains(obstacle))
                {
                    activeObstacles.Remove(obstacle);
                }
                if (obstacle.returnPool != null)
                {
                    obstacle.returnPool.ReturnToPool(obstacle);
                    Debug.Log($"[ItemPoolManager] 장애물 반환: {obstacle.name}");
                }
                else
                {
                    Debug.LogWarning($"[ItemPoolManager] 장애물의 returnPool이 null입니다: {obstacle.name}");
                }
            }
        }
        
        // 지연 반환 코루틴들 - 더 이상 사용하지 않음 (개별 오브젝트에서 처리)
        // private System.Collections.IEnumerator ReturnItemDelayed(CollectibleItem item, float delay)
        // {
        //     yield return new WaitForSeconds(delay);
        //     ReturnItem(item);
        // }
        
        // private System.Collections.IEnumerator ReturnPowerUpDelayed(CollectibleItem powerUp, float delay)
        // {
        //     yield return new WaitForSeconds(delay);
        //     ReturnPowerUp(powerUp);
        // }
        
        // private System.Collections.IEnumerator ReturnObstacleDelayed(ObstacleController obstacle, float delay)
        // {
        //     yield return new WaitForSeconds(delay);
        //     ReturnObstacle(obstacle);
        // }
        
        // 모든 활성 오브젝트 정리
        public void ClearAllActiveObjects()
        {
            Debug.Log("[ItemPoolManager] 모든 활성 오브젝트 정리 시작");
            
            // 활성 아이템들 정리
            if (activeItems != null)
            {
                foreach (var item in activeItems.ToArray())
                {
                    if (item != null && item.gameObject != null)
                    {
                        ReturnItem(item);
                    }
                }
                activeItems.Clear();
            }
            
            // 활성 PowerUp들 정리 (타입별)
            if (activePowerUps != null)
            {
                foreach (var kvp in activePowerUps)
                {
                    if (kvp.Value != null)
                    {
                        foreach (var powerUp in kvp.Value.ToArray())
                        {
                            if (powerUp != null && powerUp.gameObject != null)
                            {
                                ReturnPowerUp(powerUp);
                            }
                        }
                        kvp.Value.Clear();
                    }
                }
            }
            
            // 활성 장애물들 정리
            if (activeObstacles != null)
            {
                foreach (var obstacle in activeObstacles.ToArray())
                {
                    if (obstacle != null && obstacle.gameObject != null)
                    {
                        ReturnObstacle(obstacle);
                    }
                }
                activeObstacles.Clear();
            }
            
            Debug.Log("[ItemPoolManager] 모든 활성 오브젝트 정리 완료");
        }
        
        // 모든 풀 완전 정리 (게임 세션 종료 시 사용)
        public void DestroyAllPools()
        {
            Debug.Log("[ItemPoolManager] 모든 풀 완전 정리 시작");
            
            // 활성 오브젝트들 먼저 정리
            ClearAllActiveObjects();
            
            // 아이템 풀 정리
            if (itemPool != null)
            {
                itemPool.ClearPool();
                if (itemPool.gameObject != null)
                {
                    Destroy(itemPool.gameObject);
                }
                itemPool = null;
            }
            
            // 파워업 풀들 정리
            if (powerUpPools != null)
            {
                foreach (var kvp in powerUpPools)
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.ClearPool();
                        if (kvp.Value.gameObject != null)
                        {
                            Destroy(kvp.Value.gameObject);
                        }
                    }
                }
                powerUpPools.Clear();
            }
            
            // 장애물 풀 정리
            if (obstaclePool != null)
            {
                obstaclePool.ClearPool();
                if (obstaclePool.gameObject != null)
                {
                    Destroy(obstaclePool.gameObject);
                }
                obstaclePool = null;
            }
            
            // 컨테이너들 정리
            if (itemPoolContainer != null)
            {
                Destroy(itemPoolContainer.gameObject);
                itemPoolContainer = null;
            }
            
            if (powerUpPoolContainer != null)
            {
                Destroy(powerUpPoolContainer.gameObject);
                powerUpPoolContainer = null;
            }
            
            if (obstaclePoolContainer != null)
            {
                Destroy(obstaclePoolContainer.gameObject);
                obstaclePoolContainer = null;
            }
            
            // 초기화 상태 리셋
            isInitialized = false;
            
            Debug.Log("[ItemPoolManager] 모든 풀 완전 정리 완료");
        }
        
        // 싱글톤 인스턴스 완전 정리 (게임 종료 시 사용)
        public static void DestroyInstance()
        {
            if (instance != null)
            {
                instance.DestroyAllPools();
                Destroy(instance.gameObject);
                instance = null;
                Debug.Log("[ItemPoolManager] 싱글톤 인스턴스 완전 정리 완료");
            }
        }
        
        // 풀 상태 로깅
        public void LogPoolStatus()
        {
            Debug.Log($"[ItemPoolManager] === 풀 상태 ===");
            Debug.Log($"[ItemPoolManager] 활성 아이템: {activeItems.Count}개");
            
            foreach (var kvp in activePowerUps)
            {
                Debug.Log($"[ItemPoolManager] 활성 {kvp.Key} 파워업: {kvp.Value.Count}개");
            }
            
            Debug.Log($"[ItemPoolManager] 활성 장애물: {activeObstacles.Count}개");
            Debug.Log($"[ItemPoolManager] =================");
        }
        
        private void OnDestroy()
        {
            // 초기화되지 않은 경우 정리 작업 스킵
            if (!isInitialized)
            {
                Debug.Log("[ItemPoolManager] 초기화되지 않은 상태에서 파괴됨. 정리 작업을 스킵합니다.");
                return;
            }
            
            // 게임 오브젝트가 이미 비활성화된 경우 정리 작업 스킵
            if (gameObject == null || !gameObject.activeInHierarchy)
            {
                Debug.Log("[ItemPoolManager] 게임 오브젝트가 비활성화된 상태에서 파괴됨. 정리 작업을 스킵합니다.");
                return;
            }
            
            Debug.Log("[ItemPoolManager] OnDestroy 호출 - 활성 오브젝트 정리 시작");
            
            try
            {
                ClearAllActiveObjects();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ItemPoolManager] OnDestroy에서 오류 발생: {e.Message}");
            }
        }
    }
} 