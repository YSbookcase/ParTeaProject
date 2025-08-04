using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KYS
{

    public class BaseUI : MonoBehaviour
    {
        [SerializeField] protected bool canCloseWithESC = true; // ESC로 닫을 수 있는지
        [Header("Audio Settings")]
        [SerializeField] protected string defaultClickSound = "SFX_ButtonClick";
        [SerializeField] protected string defaultBackSound = "SFX_ButtonClickBack";
        [SerializeField] protected bool enableSFX = true;

        public bool CanCloseWithESC => canCloseWithESC;

        private Dictionary<string, GameObject> goDict;
        private Dictionary<string, Component> compDict;

        protected void Awake()
        {
            RectTransform[] transforms = GetComponentsInChildren<RectTransform>(true);
            goDict = new Dictionary<string, GameObject>(transforms.Length << 2);
            foreach (Transform t in transforms)
            {
                goDict.TryAdd(t.gameObject.name, t.gameObject);
            }

            Component[] components = GetComponentsInChildren<Component>(true);
            compDict = new Dictionary<string, Component>(components.Length << 2);
            foreach (Component comp in components)
            {
                compDict.TryAdd($"{comp.gameObject.name}_{comp.GetType().Name}", comp);
            }
        }

  

        // string으로 특정 UI 게임오브젝트 찾기

        public GameObject GetUI(in string name)
        {
            if (goDict == null)
            {
                Debug.Log("goDict없음");
                return null;
            }
            goDict.TryGetValue(name, out GameObject gameObject);
            if (gameObject == null)
            {
                gameObject = GameObject.Find($"{name}");
                if (gameObject == null)
                {
                    Debug.LogError($"다음 UI 오브젝트가 없습니다: {name}");
                }
                goDict.TryAdd(name, gameObject);
            }
            return gameObject; // 없을경우 Null
        }

        // 이름으로 딕셔너리에 추가하기. 씬에서 추가되는 경우에 씀. 추가한 후 가져옴
        public GameObject AddUIToDictionary(GameObject go)
        {
            if (goDict == null)
            {
                Debug.Log("UI 딕셔너리가 없음");
                return null;
            }
            if (!goDict.TryAdd(go.name, go))
            {
                Debug.Log($"이미 UI가 딕셔너리에 있음.:{go.name}");
            }

            return go;
        }
        public GameObject DeleteFromDictionary(in string name, in GameObject go)
        {
            goDict.Remove<string, GameObject>(name, out GameObject outObject);
            return outObject;
        }
        // string으로 특정 UI 컴포넌트 찾기
        // private TMP_InputField roomNameField => GetUI<TMP_InputField>("RoomNameField");
        public T GetUI<T>(in string name) where T : Component
        {
            compDict.TryGetValue(name, out Component comp);
            if (comp != null)
            {
                return comp as T;
            }
            GameObject go = GetUI(name);
            if (go == null)
            {
                return null;
            }
            comp = go.GetComponent<T>();
            if (comp == null) return null;
            compDict.TryAdd($"{name}_{typeof(T).Name}", comp);
            return comp as T;
        }

        // UI에 포인터핸들러를 부착 후, 가져오기
        public PointerHandler GetEvent(in string name)
        {
            GameObject go = GetUI(name);
            if (go == null)
            {
                Debug.LogError($"UI를 찾을 수 없습니다: {name}");
                return null;
            }

            PointerHandler temp = go.GetComponent<PointerHandler>();
            if (temp == null)
            {
                temp = go.AddComponent<PointerHandler>();
            }

            return temp;
        }

        // 자기 자신의 이벤트핸들러를 가져오거나, 없으면
        public PointerHandler GetSelfEvent()
        {
            PointerHandler temp = GetComponent<PointerHandler>();
            if (temp == null)
            {
                temp = gameObject.AddComponent<PointerHandler>();
            }

            return temp;
            
                    }

      // SFX 재생 메서드들
        protected void PlayClickSound(string soundName = null)
        {
            if (!enableSFX) return;

            string soundToPlay = soundName ?? defaultClickSound;
            if (!string.IsNullOrEmpty(soundToPlay) && Manager.Audio != null)
            {
                Manager.Audio.SfxPlay(soundToPlay);
            }
        }

        protected void PlayBackSound(string soundName = null)
        {
            if (!enableSFX) return;

            string soundToPlay = soundName ?? defaultBackSound;
            if (!string.IsNullOrEmpty(soundToPlay) && Manager.Audio != null)
            {
                Manager.Audio.SfxPlay(soundToPlay);
            }
        }

        // 이벤트 등록 시 자동 SFX 추가
        public PointerHandler GetEventWithSFX(in string name, string clickSound = null)
        {
            PointerHandler handler = GetEvent(name);
            if (handler != null)
            {
                handler.Click += (data) => PlayClickSound(clickSound);
            }
            return handler;
        }

        // Back 버튼용 특별 메서드
        public PointerHandler GetBackEvent(in string name, string backSound = null)
        {
            PointerHandler handler = GetEvent(name);
            if (handler != null)
            {
                handler.Click += (data) => PlayBackSound(backSound);
            }
            return handler;
        }


    }
}