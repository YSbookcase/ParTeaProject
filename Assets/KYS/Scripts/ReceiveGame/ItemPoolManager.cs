using UnityEngine;
using System.Collections.Generic;

namespace KYS
{
    public class ItemPoolManager : MonoBehaviour
    {
        [Header("Item Pool Settings")]
        [SerializeField] private CollectibleItem itemPrefab;
        [SerializeField] private int itemPoolSize = 20;
        
        [Header("Obstacle Pool Settings")]
        [SerializeField] private ObstacleController obstaclePrefab;
        [SerializeField] private int obstaclePoolSize = 10;
        
        [Header("Pool Containers")]
        [SerializeField] private Transform itemPoolContainer;
        [SerializeField] private Transform obstaclePoolContainer;
        
        // 기존 ObjectPool 시스템 활용
        private ObjectPool itemPool;
        private ObjectPool obstaclePool;
        
        // 활성 오브젝트 추적
        private List<CollectibleItem> activeItems = new List<CollectibleItem>();
        private List<ObstacleController> activeObstacles = new List<ObstacleController>();
        
        private void Start()
        {
            InitializePools();
        }
        
        private void InitializePools()
        {
            // 컨테이너 생성
            if (itemPoolContainer == null)
            {
                GameObject itemContainer = new GameObject("ItemPoolContainer");
                itemPoolContainer = itemContainer.transform;
                itemPoolContainer.SetParent(transform);
            }
            
            if (obstaclePoolContainer == null)
            {
                GameObject obstacleContainer = new GameObject("ObstaclePoolContainer");
                obstaclePoolContainer = obstacleContainer.transform;
                obstaclePoolContainer.SetParent(transform);
            }
            
            // 아이템 풀 초기화
            InitializeItemPool();
            
            // 장애물 풀 초기화
            InitializeObstaclePool();
            
            Debug.Log($"[ItemPoolManager] 풀 초기화 완료 - 아이템: {itemPoolSize}개, 장애물: {obstaclePoolSize}개");
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
            
            itemPool = poolObject.AddComponent<ObjectPool>();
            itemPool.poolObject = itemPrefab;
            
            // poolSize 설정
            var poolSizeField = typeof(ObjectPool).GetField("poolSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (poolSizeField != null)
            {
                poolSizeField.SetValue(itemPool, itemPoolSize);
            }
            
            itemPool.CreatePool();
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
            
            obstaclePool = poolObject.AddComponent<ObjectPool>();
            obstaclePool.poolObject = obstaclePrefab;
            
            // poolSize 설정
            var poolSizeField = typeof(ObjectPool).GetField("poolSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (poolSizeField != null)
            {
                poolSizeField.SetValue(obstaclePool, obstaclePoolSize);
            }
            
            obstaclePool.CreatePool();
        }
        
        // 아이템 풀에서 가져오기
        public CollectibleItem GetItem(Vector3 position)
        {
            if (itemPool == null)
            {
                Debug.LogError("[ItemPoolManager] 아이템 풀이 초기화되지 않았습니다!");
                return null;
            }
            
            // ObjectPool에서 PooledObject를 가져오고, 이를 CollectibleItem으로 캐스팅
            PooledObject pooledObj = itemPool.ObjectOut();
            CollectibleItem item = pooledObj as CollectibleItem;
            
            if (item != null)
            {
                // 아이템 상태 초기화
                item.ResetItem();
                
                // 위치 설정
                item.transform.position = position;
                
                // 활성 아이템 목록에 추가
                activeItems.Add(item);
                
                Debug.Log($"[ItemPoolManager] 아이템 활성화 - 위치: {position}, 활성 아이템 수: {activeItems.Count}");
            }
            else
            {
                Debug.LogError("[ItemPoolManager] PooledObject를 CollectibleItem으로 캐스팅할 수 없습니다!");
            }
            
            return item;
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
                
                // 활성 장애물 목록에 추가
                activeObstacles.Add(obstacle);
                
                Debug.Log($"[ItemPoolManager] 장애물 활성화 - 위치: {position}, 활성 장애물 수: {activeObstacles.Count}");
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
            
            // 활성 아이템 목록에서 제거
            if (activeItems.Contains(item))
            {
                activeItems.Remove(item);
            }
            
            // 기존 ReturnToPool 메서드 사용
            item.ReturnToPool(delay);
            
            Debug.Log($"[ItemPoolManager] 아이템 풀 반환 - 활성 아이템 수: {activeItems.Count}");
        }
        
        // 장애물 풀로 반환
        public void ReturnObstacle(ObstacleController obstacle, float delay = 0f)
        {
            if (obstacle == null) return;
            
            // 활성 장애물 목록에서 제거
            if (activeObstacles.Contains(obstacle))
            {
                activeObstacles.Remove(obstacle);
            }
            
            // 기존 ReturnToPool 메서드 사용
            obstacle.ReturnToPool(delay);
            
            Debug.Log($"[ItemPoolManager] 장애물 풀 반환 - 활성 장애물 수: {activeObstacles.Count}");
        }
        
        // 모든 활성 오브젝트 정리
        public void ClearAllActiveObjects()
        {
            // 활성 아이템들 정리
            foreach (var item in activeItems.ToArray())
            {
                if (item != null)
                {
                    ReturnItem(item);
                }
            }
            
            // 활성 장애물들 정리
            foreach (var obstacle in activeObstacles.ToArray())
            {
                if (obstacle != null)
                {
                    ReturnObstacle(obstacle);
                }
            }
            
            Debug.Log("[ItemPoolManager] 모든 활성 오브젝트 정리 완료");
        }
        
        // 풀 상태 정보
        public void LogPoolStatus()
        {
            Debug.Log($"[ItemPoolManager] 풀 상태 - 아이템: {activeItems.Count}/{itemPoolSize} (활성/전체), 장애물: {activeObstacles.Count}/{obstaclePoolSize} (활성/전체)");
        }
        
        // 게임 종료 시 정리
        private void OnDestroy()
        {
            ClearAllActiveObjects();
        }
    }
} 