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
            // CollectibleItem이 PooledObject를 상속받고 있으므로 직접 사용
            itemPool.poolObject = itemPrefab;
            
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
            
            // ObjectPool에서 PooledObject를 가져오고, 이를 CollectibleItem으로 캐스팅
            PooledObject pooledObj = itemPool.ObjectOut();
            CollectibleItem item = pooledObj as CollectibleItem;
            
            if (item != null)
            {
                // 먼저 아이템 리셋
                item.ResetItem();
                
                // 그 다음 위치 설정
                item.transform.position = position;
                
                // 위치가 제대로 설정되었는지 확인
                if (Vector3.Distance(item.transform.position, position) > 0.1f)
                {
                    Debug.LogWarning($"아이템 위치 설정 실패! 예상: {position}, 실제: {item.transform.position}");
                    item.transform.position = position; // 강제로 다시 설정
                }
                
                Debug.Log($"아이템 풀에서 생성: {position}, 실제 위치: {item.transform.position}, 활성화 상태: {item.gameObject.activeInHierarchy}");
            }
            else
            {
                Debug.LogError("PooledObject를 CollectibleItem으로 캐스팅할 수 없습니다!");
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