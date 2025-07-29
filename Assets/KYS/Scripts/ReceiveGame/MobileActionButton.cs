using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace KYS
{
    public class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Button Settings")]
        [SerializeField] private string actionName = "Action";
        [SerializeField] private float pressThreshold = 0.1f;
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color pressedColor = Color.yellow;
        [SerializeField] private Vector3 normalScale = Vector3.one;
        [SerializeField] private Vector3 pressedScale = new Vector3(0.9f, 0.9f, 0.9f);
        
        private Image buttonImage;
        private bool isPressed = false;
        private float pressStartTime;
        
        public bool IsPressed => isPressed;
        public string ActionName => actionName;
        
        public System.Action<string> OnActionPressed;
        public System.Action<string> OnActionReleased;
        
        private void Awake()
        {
            buttonImage = GetComponent<Image>();
            if (buttonImage == null)
            {
                buttonImage = gameObject.AddComponent<Image>();
            }
            
            // 초기 상태 설정
            ResetButton();
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            pressStartTime = Time.time;
            
            // 시각적 피드백
            buttonImage.color = pressedColor;
            transform.localScale = pressedScale;
            
            // 액션 이벤트 발생
            OnActionPressed?.Invoke(actionName);
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            // 짧은 터치인 경우에만 액션 실행
            if (Time.time - pressStartTime <= pressThreshold)
            {
                // 액션 실행
                ExecuteAction();
            }
            
            ResetButton();
        }
        
        private void ResetButton()
        {
            isPressed = false;
            
            // 시각적 피드백 복원
            buttonImage.color = normalColor;
            transform.localScale = normalScale;
            
            // 액션 해제 이벤트 발생
            OnActionReleased?.Invoke(actionName);
        }
        
        private void ExecuteAction()
        {
            Debug.Log($"액션 버튼 실행: {actionName}");
            
            // 여기에 특정 액션 로직 추가
            switch (actionName.ToLower())
            {
                case "jump":
                    // 점프 액션
                    break;
                case "action":
                    // 일반 액션
                    break;
                case "special":
                    // 특수 액션
                    break;
            }
        }
        
        // 외부에서 버튼 비활성화
        public void DisableButton()
        {
            ResetButton();
            gameObject.SetActive(false);
        }
        
        // 외부에서 버튼 활성화
        public void EnableButton()
        {
            gameObject.SetActive(true);
        }
        
        // 액션 이름 설정
        public void SetActionName(string newActionName)
        {
            actionName = newActionName;
        }
    }
} 