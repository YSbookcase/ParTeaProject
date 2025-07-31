using UnityEngine;

namespace KYS
{
    public class PooledObject : MonoBehaviour
    {
        public ObjectPool returnPool;
        public void ReturnToPool()
        {
            if (returnPool != null)
            {
                returnPool.ReturnToPool(this);
            }
            else
            {
                Debug.LogWarning($"[PooledObject] returnPool이 null입니다. 오브젝트를 파괴합니다: {gameObject.name}");
                Destroy(gameObject);
            }
        }
        public void ReturnToPool(float returnTime)
        {
            if (returnPool != null)
            {
                returnPool.ReturnToPool(this, returnTime);
            }
            else
            {
                Debug.LogWarning($"[PooledObject] returnPool이 null입니다. 오브젝트를 파괴합니다: {gameObject.name}");
                Destroy(gameObject);
            }
        }
    }
}