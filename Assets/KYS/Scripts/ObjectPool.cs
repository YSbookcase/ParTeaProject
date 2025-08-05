using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KYS
{
    public class ObjectPool : MonoBehaviour
    {
        
        [Header("Set References")]
        [SerializeField] public PooledObject poolObject;

        [Header("Set Value")]
        [Range(1, 100)][SerializeField] int poolSize = 5;

        private Stack<PooledObject> pool;
        private Coroutine returnRoutine;

        private void Awake()
        { }
        public void CreatePool()
        {
            if (poolObject == null)
            {
                Debug.LogError("[ObjectPool] poolObject가 null입니다. 풀을 생성할 수 없습니다.");
                return;
            }
            
            pool = new Stack<PooledObject>();
            for (int i = 0; i < poolSize; i++)
            {
                PooledObject go = Instantiate(poolObject, transform);
                if (go != null)
                {
                    go.returnPool = this;
                    go.gameObject.SetActive(false);
                    pool.Push(go);
                }
                else
                {
                    Debug.LogError($"[ObjectPool] {i}번째 오브젝트 생성 실패");
                }
            }
            
            Debug.Log($"[ObjectPool] 풀 생성 완료 - 크기: {pool.Count}");
        }
        public PooledObject ObjectOut()
        {
            PooledObject go;

            if (pool.Count > 0)
            {
                go = pool.Pop();
                
                // null 체크 추가 - 파괴된 오브젝트인지 확인
                if (go == null)
                {
                    Debug.LogWarning("[ObjectPool] 풀에서 null 오브젝트를 가져왔습니다. 새로 생성합니다.");
                    go = Instantiate(poolObject, transform);
                }
            }
            else
            {
                go = Instantiate(poolObject, transform);
            }

            // 추가 null 체크
            if (go == null)
            {
                Debug.LogError("[ObjectPool] poolObject가 null입니다. ObjectOut을 수행할 수 없습니다.");
                return null;
            }

            go.returnPool = this;
            go.gameObject.SetActive(true);
            
            // 위치 초기화 제거 - 호출하는 쪽에서 위치를 설정하도록 함
            // go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            
            return go;
        }
        public void ReturnToPool(PooledObject obj, float returnTime = 0f)
        {
            if (obj == null)
            {
                Debug.LogWarning("[ObjectPool] null 오브젝트를 풀로 반환하려고 시도했습니다.");
                return;
            }
            
            returnRoutine = StartCoroutine(ReturnRoutine(obj, returnTime));
        }
        IEnumerator ReturnRoutine(PooledObject obj, float returnTime)
        {
            yield return new WaitForSeconds(returnTime);
            
            if (obj != null)
            {
                obj.gameObject.SetActive(false);
                pool.Push(obj);
            }
            else
            {
                Debug.LogWarning("[ObjectPool] ReturnRoutine에서 null 오브젝트를 발견했습니다.");
            }
            
            returnRoutine = null;
        }
        public void ClearPool()
        {
            while (pool.Count > 0)
            {
                PooledObject obj = pool.Pop();
                if (obj != null)
                {
                    Destroy(obj.gameObject);
                }
            }
            pool.Clear();
            Debug.Log("[ObjectPool] 풀 정리 완료");
        }
    }
}