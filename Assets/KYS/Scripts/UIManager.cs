using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace KYS
{
    public class UIManager : Singleton<UIManager>
    {
        [SerializeField] string popUpPath = "UI/Canvas_PopUp"; // 경로 수정
        [SerializeField] string prefabPath = "UI"; // 경로 수정
        private PopUpUI popUp;

        // 메인 UI 패널들 관리
        private Dictionary<string, GameObject> mainPanels = new Dictionary<string, GameObject>();

        public PopUpUI PopUp
        {
            get
            {
                if (popUp == null)
                {
                    popUp = FindObjectOfType<PopUpUI>();
                    if (popUp != null) return popUp;

                    GameObject prefab = Resources.Load<GameObject>(popUpPath);
                    if (prefab == null)
                    {
                        Debug.LogWarning($"[UIManager] 해당 경로에 팝업 프리팹이 없음: {popUpPath}");
                        return null;
                    }

                    GameObject go = Instantiate(prefab);
                    popUp = go.GetComponent<PopUpUI>();
                    if (popUp == null)
                    {
                        Debug.LogError($"[UIManager] Canvas_PopUp 프리팹에 PopUpUI 컴포넌트가 없음");
                        Destroy(go);
                        return null;
                    }

                    DontDestroyOnLoad(go);
                }
                DontDestroyOnLoad(popUp);
                return popUp;
            }
        }

        // 메인 UI 패널 등록
        public void RegisterMainPanel(string panelName, GameObject panel)
        {
            if (string.IsNullOrEmpty(panelName) || panel == null)
            {
                Debug.LogError("[UIManager] 패널 이름이나 GameObject가 null입니다.");
                return;
            }

            if (mainPanels.ContainsKey(panelName))
            {
                Debug.LogWarning($"[UIManager] 이미 등록된 메인 패널: {panelName}");
                mainPanels[panelName] = panel; // 기존 패널 교체
            }
            else
            {
                mainPanels.Add(panelName, panel);
                //Debug.Log($"[UIManager] 메인 패널 등록 완료: {panelName}");
            }
        }

        // 메인 UI 패널 해제
        public void UnregisterMainPanel(string panelName)
        {
            if (mainPanels.Remove(panelName))
            {
                Debug.Log($"[UIManager] 메인 패널 해제: {panelName}");
            }
        }

        // 메인 UI 패널 가져오기
        public GameObject GetMainPanel(string panelName)
        {
            if (string.IsNullOrEmpty(panelName))
            {
                Debug.LogError("[UIManager] 패널 이름이 null입니다.");
                return null;
            }

            if (mainPanels.TryGetValue(panelName, out GameObject panel))
            {
                return panel;
            }
            Debug.LogWarning($"[UIManager] 등록되지 않은 메인 패널: {panelName}");
            return null;
        }

        // 모든 메인 패널 가져오기
        public Dictionary<string, GameObject> GetAllMainPanels()
        {
            return new Dictionary<string, GameObject>(mainPanels);
        }

        // 팝업 UI 인덱스
        public static int selectIndexUI { get; set; } = 0;
        public static bool canClosePopUp = true;
        bool canClose => PopUpUI.IsPopUpActive && !Util.escPressed && canClosePopUp;

        protected override void Awake() => base.Awake();

        private void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && canClose)
            {
                ClosePopUp();
                Util.ConsumeESC();
            }
            Util.ResetESC();
        }

        // 팝업 UI를 띄운다
        public T ShowPopUp<T>() where T : BaseUI
        {
            string path = $"{prefabPath}/{typeof(T).Name}";
            T prefab = Resources.Load<T>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[UIManager] 해당 경로에 팝업 프리팹이 없음: {path}");
                return null;
            }

            if (PopUp == null)
            {
                Debug.LogError("[UIManager] PopUp이 null입니다.");
                return null;
            }

            T instance = Instantiate(prefab, PopUp.transform);
            PopUp.PushUIStack(instance);
            return instance;
        }

        public void ClosePopUp()
        {
            if (PopUp != null)
            {
                PopUp.PopUIStack();
            }
        }

        public void CleanPopUp()
        {
            if (PopUp != null)
            {
                while (PopUp.StackCount() > 0)
                {
                    PopUp.PopUIStack();
                }
            }
        }

        // 모든 UI 정리
        public void CleanAllUI()
        {
            CleanPopUp();
            mainPanels.Clear();
            Debug.Log("[UIManager] 모든 UI가 정리되었습니다.");
        }


        // 확인 팝업을 띄우는 편의 메서드
        public CheckPopUp ShowConfirmPopUp(string message, string confirmText = "확인", string cancelText = "취소",
                                          System.Action confirmCallback = null, System.Action cancelCallback = null)
        {
            CheckPopUp checkPopUp = ShowPopUp<CheckPopUp>();
            if (checkPopUp != null)
            {
                checkPopUp.SetMessage(message, confirmText, cancelText, confirmCallback, cancelCallback);
            }
            return checkPopUp;
        }

        // 간단한 확인 팝업 (확인 시에만 콜백)
        public CheckPopUp ShowConfirmPopUp(string message, System.Action confirmCallback)
        {
            return ShowConfirmPopUp(message, "확인", "취소", confirmCallback, null);
        }

        // 특정 타입의 팝업 찾기
        public T FindActivePopUp<T>() where T : BaseUI
        {
            if (PopUp == null) return null;

            return PopUp.GetComponentInChildren<T>();
        }

        // 특정 팝업 업데이트
        public void UpdatePopUp<T>() where T : BaseUI
        {
            T popUp = FindActivePopUp<T>();
            if (popUp != null)
            {
                // LoginInfo 메서드가 있는 경우 호출
                var loginInfoMethod = typeof(T).GetMethod("LoginInfo");
                if (loginInfoMethod != null)
                {
                    loginInfoMethod.Invoke(popUp, null);
                }
            }
        }
    }

}
