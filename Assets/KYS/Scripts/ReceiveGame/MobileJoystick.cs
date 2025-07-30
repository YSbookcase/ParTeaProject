using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace KYS
{
    public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Joystick Settings")]
        [SerializeField] private RectTransform joystickBackground;
        [SerializeField] private RectTransform joystickHandle;
        [SerializeField] private float joystickRadius = 50f;
        [SerializeField] private bool isDynamicJoystick = false; // 고정 조이스틱으로 변경
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color activeColor = Color.yellow;
        
        [Header("Mouse Settings")]
        [SerializeField] private bool enableMouseInput = true; // 마우스 입력 활성화
        
        private Vector2 inputVector;
        private Vector2 touchStartPosition;
        private bool isJoystickActive = false;
        private Image backgroundImage;
        private Image handleImage;
        
        public Vector2 InputVector => inputVector;
        public bool IsActive => isJoystickActive;
        
        private void Awake()
        {
            backgroundImage = joystickBackground.GetComponent<Image>();
            handleImage = joystickHandle.GetComponent<Image>();
            
            // 초기 상태 설정
            ResetJoystick();
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            // 마우스 입력이 활성화되어 있거나 터치 입력인 경우에만 처리
            if (!enableMouseInput && eventData.pointerId >= 0)
            {
                return; // 터치가 아닌 경우 무시 (마우스 입력 비활성화 시)
            }
            
            isJoystickActive = true;
            touchStartPosition = joystickBackground.position; // 고정 조이스틱의 중심점 사용
            
            // 시각적 피드백
            backgroundImage.color = activeColor;
            handleImage.color = activeColor;
            
            Debug.Log($"조이스틱 활성화 - 입력 타입: {(eventData.pointerId < 0 ? "마우스" : "터치")}");
            
            OnDrag(eventData);
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!isJoystickActive) return;
            
            Vector2 currentTouchPosition = eventData.position;
            Vector2 direction = currentTouchPosition - touchStartPosition;
            
            // 조이스틱 반경 내로 제한
            if (direction.magnitude > joystickRadius)
            {
                direction = direction.normalized * joystickRadius;
            }
            
            // 입력 벡터 계산 (0~1 범위로 정규화)
            inputVector = direction / joystickRadius;
            
            // 핸들 위치 업데이트
            joystickHandle.position = touchStartPosition + direction;
            
            // 디버그 로그 (입력이 있을 때만)
            if (inputVector.magnitude > 0.1f)
            {
                Debug.Log($"조이스틱 드래그 - 입력: {inputVector}, 입력 타입: {(eventData.pointerId < 0 ? "마우스" : "터치")}");
            }
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            Debug.Log($"조이스틱 비활성화 - 입력 타입: {(eventData.pointerId < 0 ? "마우스" : "터치")}");
            ResetJoystick();
        }
        
        private void ResetJoystick()
        {
            isJoystickActive = false;
            inputVector = Vector2.zero;
            
            // 핸들을 중심으로 되돌리기
            joystickHandle.position = joystickBackground.position;
            
            // 시각적 피드백 복원
            backgroundImage.color = normalColor;
            handleImage.color = normalColor;
        }
        
        // 외부에서 조이스틱 비활성화
        public void DisableJoystick()
        {
            ResetJoystick();
        }
        
        // 외부에서 조이스틱 활성화
        public void EnableJoystick()
        {
            joystickBackground.gameObject.SetActive(true);
        }
    }
} 