using UnityEngine;

namespace KYS
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] 인스턴스 '{typeof(T)}'가 이미 애플리케이션 종료 시 파괴되었습니다. 다시 생성하지 않고 null을 반환합니다.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = (T)FindObjectOfType(typeof(T));

                        if (_instance == null)
                        {
                            GameObject singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = typeof(T).ToString();

                            DontDestroyOnLoad(singletonObject);

                            Debug.Log($"[Singleton] {typeof(T)}의 인스턴스가 DontDestroyOnLoad로 생성되었습니다.");
                        }
                        else
                        {
                            Debug.Log($"[Singleton] 이미 생성된 인스턴스를 사용합니다: {_instance.gameObject.name}");
                        }
                    }

                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                OnAwake();
                Debug.Log($"[Singleton] {typeof(T)}가 Awake에서 초기화되었습니다");
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[Singleton] {typeof(T)}의 다른 인스턴스가 이미 존재합니다! 이 인스턴스를 파괴합니다.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnAwake() { }

        public virtual void Init()
        {
            Debug.Log($"[Singleton] {typeof(T)} Init 호출됨");
        }

        private void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        private void OnDestroy()
        {
            _applicationIsQuitting = true;
        }

        public void DestroyManager()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }
    }
}
