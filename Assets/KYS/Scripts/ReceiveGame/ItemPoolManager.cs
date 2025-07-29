using UnityEngine;

namespace KYS
{
    public class ItemPoolManager : MonoBehaviour
    {
        [Header("Item Pool Settings")]
        [SerializeField] private CollectibleItem itemPrefab;
        [SerializeField] private int poolSize = 20;
        
        private ObjectPool itemPool;
        
        private void Start()
        {
            InitializeItemPool();
        }
        
        private void InitializeItemPool()
        {
            if (itemPrefab == null)
            {
                Debug.LogError("Item Prefab이 설정되지 않았습니다!");
                return;
            }
            
            // 아이템 풀 생성
            GameObject poolObject = new GameObject("ItemPool");
            poolObject.transform.SetParent(transform);
            
            itemPool = poolObject.AddComponent<ObjectPool>();
            itemPool.poolObject = itemPrefab.GetComponent<PooledObject>();
            
            // Inspector에서 poolSize 설정을 위해 리플렉션 사용
            var poolSizeField = typeof(ObjectPool).GetField("poolSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (poolSizeField != null)
            {
                poolSizeField.SetValue(itemPool, poolSize);
            }
            
            itemPool.CreatePool();
            Debug.Log($"아이템 풀 초기화 완료: {poolSize}개");
        }
        
        public CollectibleItem GetItem(Vector3 position)
        {
            if (itemPool == null)
            {
                Debug.LogError("아이템 풀이 초기화되지 않았습니다!");
                return null;
            }
            
            PooledObject pooledObj = itemPool.ObjectOut();
            CollectibleItem item = pooledObj.GetComponent<CollectibleItem>();
            
            if (item != null)
            {
                // 위치 설정을 ResetItem 전에 해야 함
                item.transform.position = position;
                item.ResetItem();
                
                // 위치가 제대로 설정되었는지 확인
                if (Vector3.Distance(item.transform.position, position) > 0.1f)
                {
                    Debug.LogWarning($"아이템 위치 설정 실패! 예상: {position}, 실제: {item.transform.position}");
                    item.transform.position = position; // 강제로 다시 설정
                }
                
                Debug.Log($"아이템 풀에서 생성: {position}");
            }
            
            return item;
        }
        
        public void ReturnItem(CollectibleItem item, float delay = 0f)
        {
            if (item != null)
            {
                item.ReturnToPool(delay);
            }
        }
        
        public void ClearAllItems()
        {
            if (itemPool != null)
            {
                itemPool.ClearPool();
            }
        }
    }
} 