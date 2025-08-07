using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace KYS
{
    public class ItemPoolManager : MonoBehaviour
    {
        // Singleton instance
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
                        GameObject go = new GameObject("ItemPoolManager");
                        instance = go.AddComponent<ItemPoolManager>();
                    }
                }
                return instance;
            }
        }

        [Header("Item Pool Settings")]
        [SerializeField] private GameObject itemPrefab; // 아이템 프리팹
        [SerializeField] private int itemPoolSize = 20; // 아이템 풀 크기
        
        [Header("PowerUp Pool Settings - Type Specific")]
        [SerializeField] private GameObject speedPowerUpPrefab; // 속도 파워업 프리팹
        [SerializeField] private GameObject slowPowerUpPrefab; // 슬로우 파워업 프리팹
        [SerializeField] private GameObject magnetPowerUpPrefab; // 자석 파워업 프리팹
        [SerializeField] private GameObject bonusPowerUpPrefab; // 보너스 파워업 프리팹
        [SerializeField] private int powerUpPoolSizePerType = 5; // 타입별 파워업 풀 크기

        [Header("Obstacle Pool Settings")]
        [SerializeField] private GameObject obstaclePrefab; // 장애물 프리팹
        [SerializeField] private int obstaclePoolSize = 10; // 장애물 풀 크기

        [Header("Pool Containers")]
        [SerializeField] private Transform itemPoolContainer; // 아이템 풀 컨테이너
        [SerializeField] private Transform powerUpPoolContainer; // 파워업 풀 컨테이너
        [SerializeField] private Transform obstaclePoolContainer; // 장애물 풀 컨테이너

        // Pool references
        private ObjectPool itemPool; // 아이템 풀
        private Dictionary<ItemType, ObjectPool> powerUpPools; // 타입별 파워업 풀
        private ObjectPool obstaclePool; // 장애물 풀

        // Active objects tracking
        private List<CollectibleItem> activeItems = new List<CollectibleItem>(); // 활성 아이템 목록
        private Dictionary<ItemType, List<CollectibleItem>> activePowerUps; // 타입별 활성 파워업
        private List<ObstacleController> activeObstacles = new List<ObstacleController>(); // 활성 장애물 목록

        // Configuration
        private ItemConfiguration itemConfiguration; // 아이템 설정
        private bool isInitialized = false; // 초기화 완료 여부

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeSingleton();
        }
        
        private void Start()
        {
            if (isInitialized) return;
            
            LoadItemConfiguration();
            InitializePools();
            isInitialized = true;
        }
        
        private void OnDestroy()
        {
            if (!isInitialized || gameObject == null || !gameObject.activeInHierarchy) return;
            
            try
            {
                ClearAllActiveObjects();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ItemPoolManager] OnDestroy에서 오류 발생: {e.Message}");
            }
        }

        #endregion

        #region Singleton Management

        private void InitializeSingleton() // 싱글톤 초기화
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        public static void DestroyInstance() // 싱글톤 인스턴스 완전 정리
        {
            if (instance != null)
            {
                instance.DestroyAllPools();
                Destroy(instance.gameObject);
                instance = null;
            }
        }

        #endregion

        #region Configuration Loading

        private void LoadItemConfiguration() // 아이템 설정 로드
        {
            itemConfiguration = Resources.Load<ItemConfiguration>("ItemConfiguration");
            if (itemConfiguration != null) return;
            
            itemConfiguration = Resources.Load<ItemConfiguration>("KYS/Resources/ItemConfiguration");
            if (itemConfiguration != null) return;
            
            itemConfiguration = Resources.Load<ItemConfiguration>("KYS/ScriptableObject/ItemConfiguration");
            if (itemConfiguration != null) return;
            
            #if UNITY_EDITOR
            ItemConfiguration[] configs = Resources.FindObjectsOfTypeAll<ItemConfiguration>();
            if (configs.Length > 0)
            {
                itemConfiguration = configs[0];
            }
            #endif
        }

        #endregion

        #region Pool Initialization

        private void InitializePools() // 모든 풀 초기화
        {
            CreatePoolContainers();
            InitializeItemPool();
            InitializePowerUpPools();
            InitializeObstaclePool();
        }
        
        private void CreatePoolContainers() // 풀 컨테이너 생성
        {
            itemPoolContainer = CreateContainer("ItemPoolContainer");
            powerUpPoolContainer = CreateContainer("PowerUpPoolContainer");
            obstaclePoolContainer = CreateContainer("ObstaclePoolContainer");
        }
        
        private Transform CreateContainer(string containerName) // 컨테이너 생성
        {
            if (itemPoolContainer == null)
            {
                GameObject container = new GameObject(containerName);
                Transform containerTransform = container.transform;
                containerTransform.SetParent(transform);
                DontDestroyOnLoad(container);
                return containerTransform;
            }
            return itemPoolContainer;
        }
        
        private void InitializeItemPool() // 아이템 풀 초기화
        {
            if (itemPrefab == null) return;
            
            GameObject poolObject = CreatePoolObject("ItemPool", itemPoolContainer);
            itemPool = poolObject.AddComponent<ObjectPool>();
            
            SetupPool(itemPool, itemPrefab, itemPoolSize);
        }
        
        private void InitializePowerUpPools() // 파워업 풀 초기화
        {
            powerUpPools = new Dictionary<ItemType, ObjectPool>();
            activePowerUps = new Dictionary<ItemType, List<CollectibleItem>>();
            
            Dictionary<ItemType, GameObject> powerUpPrefabs = new Dictionary<ItemType, GameObject>
            {
                { ItemType.Speed, speedPowerUpPrefab },
                { ItemType.Slow, slowPowerUpPrefab },
                { ItemType.Magnet, magnetPowerUpPrefab },
                { ItemType.Bonus, bonusPowerUpPrefab }
            };
            
            foreach (var kvp in powerUpPrefabs)
            {
                if (kvp.Value == null) continue;
                
                GameObject poolObject = CreatePoolObject($"{kvp.Key}PowerUpPool", powerUpPoolContainer);
                ObjectPool powerUpPool = poolObject.AddComponent<ObjectPool>();
                
                SetupPool(powerUpPool, kvp.Value, powerUpPoolSizePerType);
                
                powerUpPools[kvp.Key] = powerUpPool;
                activePowerUps[kvp.Key] = new List<CollectibleItem>();
            }
        }
        
        private void InitializeObstaclePool() // 장애물 풀 초기화
        {
            if (obstaclePrefab == null) return;
            
            GameObject poolObject = CreatePoolObject("ObstaclePool", obstaclePoolContainer);
            obstaclePool = poolObject.AddComponent<ObjectPool>();
            
            SetupPool(obstaclePool, obstaclePrefab, obstaclePoolSize);
        }
        
        private GameObject CreatePoolObject(string poolName, Transform parent) // 풀 오브젝트 생성
        {
            GameObject poolObject = new GameObject(poolName);
            poolObject.transform.SetParent(parent);
            DontDestroyOnLoad(poolObject);
            return poolObject;
        }
        
        private void SetupPool(ObjectPool pool, GameObject prefab, int poolSize) // 풀 설정
        {
            PooledObject pooledPrefab = prefab.GetComponent<PooledObject>();
            if (pooledPrefab == null) return;
            
            pool.poolObject = pooledPrefab;
            
            var poolSizeField = typeof(ObjectPool).GetField("poolSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (poolSizeField != null)
            {
                poolSizeField.SetValue(pool, poolSize);
            }
            
            pool.CreatePool();
            
            if (!pool.gameObject.activeInHierarchy)
            {
                pool.gameObject.SetActive(true);
            }
        }

        #endregion

        #region Item Management

        public CollectibleItem GetItem(Vector3 position, ItemType itemType = ItemType.Normal) // 아이템 가져오기
        {
            if (itemPool == null) return null;
            
            PooledObject pooledObj = itemPool.ObjectOut();
            if (pooledObj == null) return null;
            
            CollectibleItem item = pooledObj as CollectibleItem;
            if (item != null)
            {
                SetupItem(item, position, itemType, itemPool);
            }
            
            return item;
        }
        
        public CollectibleItem GetPowerUp(Vector3 position, ItemType powerUpType) // 파워업 가져오기
        {
            if (powerUpPools == null || !powerUpPools.ContainsKey(powerUpType)) return null;
            
            ObjectPool powerUpPool = powerUpPools[powerUpType];
            if (powerUpPool == null) return null;
            
            PooledObject pooledObj = powerUpPool.ObjectOut();
            if (pooledObj == null) return null;
            
            CollectibleItem powerUp = pooledObj as CollectibleItem;
            if (powerUp != null)
            {
                SetupItem(powerUp, position, powerUpType, powerUpPool);
                
                if (activePowerUps.ContainsKey(powerUpType))
                {
                    activePowerUps[powerUpType].Add(powerUp);
                }
            }
            
            return powerUp;
        }
        
        public CollectibleItem GetRandomPowerUp(Vector3 position, float speedChance = 0.4f, float slowChance = 0.3f, float magnetChance = 0.3f) // 확률 기반 랜덤 파워업
        {
            float randomValue = Random.Range(0f, 1f);
            float cumulativeChance = 0f;
            
            cumulativeChance += speedChance;
            if (randomValue <= cumulativeChance)
            {
                return GetPowerUp(position, ItemType.Speed);
            }
            
            cumulativeChance += slowChance;
            if (randomValue <= cumulativeChance)
            {
                return GetPowerUp(position, ItemType.Slow);
            }
            
            return GetPowerUp(position, ItemType.Magnet);
        }
        
        public CollectibleItem GetRandomPowerUp(Vector3 position) // 균등 확률 랜덤 파워업
        {
            ItemType[] powerUpTypes = { ItemType.Speed, ItemType.Slow, ItemType.Magnet };
            ItemType randomType = powerUpTypes[Random.Range(0, powerUpTypes.Length)];
            return GetPowerUp(position, randomType);
        }
        
        private void SetupItem(CollectibleItem item, Vector3 position, ItemType itemType, ObjectPool pool) // 아이템 설정
        {
            item.transform.position = position;
            
            CollectibleItem collectibleItem = item.GetComponent<CollectibleItem>();
            if (collectibleItem != null)
            {
                collectibleItem.PreviousPosition = position;
            }
            
            item.ResetItem();
            item.itemType = itemType;
            
            ApplyItemVisualSettings(item.gameObject, itemType);
            
            item.returnPool = pool;
            
            if (activeItems != null)
            {
                activeItems.Add(item);
            }
        }

        #endregion

        #region Visual Settings

        private void ApplyItemVisualSettings(GameObject item, ItemType itemType) // 아이템 시각적 설정 적용
        {
            if (itemConfiguration == null) return;
            
            ItemConfig config = itemConfiguration.GetItemConfig(itemType);
            if (config != null)
            {
                ApplyEnhancedItemController(item, itemType);
                ApplyCollectibleItemConfiguration(item);
            }
        }
        
        private void ApplyEnhancedItemController(GameObject item, ItemType itemType) // EnhancedItemController 설정 적용
        {
            EnhancedItemController itemController = item.GetComponent<EnhancedItemController>();
            if (itemController != null)
            {
                itemController.SetItemType(itemType);
            }
            else
            {
                ItemConfig config = itemConfiguration.GetItemConfig(itemType);
                if (config != null)
                {
                    ApplyBasicVisualSettings(item, config);
                }
            }
        }
        
        private void ApplyCollectibleItemConfiguration(GameObject item) // CollectibleItem 설정 적용
        {
            CollectibleItem collectibleItem = item.GetComponent<CollectibleItem>();
            if (collectibleItem != null)
            {
                collectibleItem.SetItemConfiguration(itemConfiguration);
            }
        }
        
        private void ApplyBasicVisualSettings(GameObject item, ItemConfig config) // 기본 시각적 설정 적용
        {
            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = config.itemColor;
            }
            
            item.transform.localScale = config.scale;
            
            if (config.useEmission && renderer != null && renderer.material != null)
            {
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", config.emissionColor * config.emissionIntensity);
            }
        }

        #endregion

        #region Obstacle Management

        public ObstacleController GetObstacle(Vector3 position) // 장애물 가져오기
        {
            if (obstaclePool == null) return null;
            
            PooledObject pooledObj = obstaclePool.ObjectOut();
            ObstacleController obstacle = pooledObj as ObstacleController;
            
            if (obstacle != null)
            {
                SetupObstacle(obstacle, position);
            }
            
            return obstacle;
        }
        
        private void SetupObstacle(ObstacleController obstacle, Vector3 position) // 장애물 설정
        {
            obstacle.ResetObstacle();
            obstacle.transform.position = position;
            
            PhotonView photonView = obstacle.GetComponent<PhotonView>();
            if (photonView != null && photonView.ViewID == 0)
            {
                PhotonNetwork.AllocateViewID(photonView);
            }
            
            obstacle.returnPool = obstaclePool;
            
            if (activeObstacles != null)
            {
                activeObstacles.Add(obstacle);
            }
        }

        #endregion

        #region Return Methods

        public void ReturnItem(CollectibleItem item, float delay = 0f) // 아이템 반환
        {
            if (item == null) return;
            
            if (delay > 0f)
            {
                item.ReturnToPoolWithDelay(delay);
            }
            else
            {
                ReturnObjectToPool(item, activeItems, item.returnPool);
            }
        }
        
        public void ReturnPowerUp(CollectibleItem powerUp, float delay = 0f) // 파워업 반환
        {
            if (powerUp == null) return;
            
            if (delay > 0f)
            {
                powerUp.ReturnToPoolWithDelay(delay);
            }
            else
            {
                ItemType powerUpType = powerUp.itemType;
                if (activePowerUps != null && activePowerUps.ContainsKey(powerUpType))
                {
                    activePowerUps[powerUpType].Remove(powerUp);
                }
                
                ReturnObjectToPool(powerUp, null, powerUp.returnPool);
            }
        }
        
        public void ReturnObstacle(ObstacleController obstacle, float delay = 0f) // 장애물 반환
        {
            if (obstacle == null) return;
            
            if (delay > 0f)
            {
                obstacle.ReturnToPoolWithDelay(delay);
            }
            else
            {
                ReturnObjectToPool(obstacle, activeObstacles, obstacle.returnPool);
            }
        }
        
        private void ReturnObjectToPool<T>(T obj, List<T> activeList, ObjectPool pool) where T : PooledObject // 오브젝트를 풀로 반환
        {
            if (activeList != null && activeList.Contains(obj))
            {
                activeList.Remove(obj);
            }
            
            if (pool != null)
            {
                pool.ReturnToPool(obj);
            }
        }

        #endregion

        #region Cleanup Methods

        public void ClearAllActiveObjects() // 모든 활성 오브젝트 정리
        {
            ClearActiveList(activeItems, ReturnItem);
            ClearActivePowerUps();
            ClearActiveList(activeObstacles, ReturnObstacle);
        }
        
        private void ClearActiveList<T>(List<T> activeList, System.Action<T, float> returnAction) where T : PooledObject // 활성 리스트 정리
        {
            if (activeList != null)
            {
                foreach (var obj in activeList.ToArray())
                {
                    if (obj != null && obj.gameObject != null)
                    {
                        returnAction(obj, 0f);
                    }
                }
                activeList.Clear();
            }
        }
        
        private void ClearActivePowerUps() // 활성 파워업 정리
        {
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
                                ReturnPowerUp(powerUp, 0f);
                            }
                        }
                        kvp.Value.Clear();
                    }
                }
            }
        }
        
        public void DestroyAllPools() // 모든 풀 완전 정리
        {
            ClearAllActiveObjects();
            
            DestroyPool(itemPool);
            itemPool = null;
            
            if (powerUpPools != null)
            {
                foreach (var kvp in powerUpPools)
                {
                    DestroyPool(kvp.Value);
                }
                powerUpPools.Clear();
            }
            
            DestroyPool(obstaclePool);
            obstaclePool = null;
            
            DestroyContainer(itemPoolContainer);
            DestroyContainer(powerUpPoolContainer);
            DestroyContainer(obstaclePoolContainer);
            
            isInitialized = false;
        }
        
        private void DestroyPool(ObjectPool pool) // 풀 파괴
        {
            if (pool != null)
            {
                pool.ClearPool();
                if (pool.gameObject != null)
                {
                    Destroy(pool.gameObject);
                }
            }
        }
        
        private void DestroyContainer(Transform container) // 컨테이너 파괴
        {
            if (container != null)
            {
                Destroy(container.gameObject);
            }
        }

        #endregion

        #region Utility Methods

        public void LogPoolStatus() // 풀 상태 로깅
        {
            Debug.Log($"[ItemPoolManager] === 풀 상태 ===");
            Debug.Log($"[ItemPoolManager] 활성 아이템: {activeItems.Count}개");
            
            if (activePowerUps != null)
            {
                foreach (var kvp in activePowerUps)
                {
                    Debug.Log($"[ItemPoolManager] 활성 {kvp.Key} 파워업: {kvp.Value.Count}개");
                }
            }
            
            Debug.Log($"[ItemPoolManager] 활성 장애물: {activeObstacles.Count}개");
            Debug.Log($"[ItemPoolManager] =================");
        }

        #endregion
    }
} 
