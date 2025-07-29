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
        [SerializeField] private bool isDynamicJoystick = true;
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color activeColor = Color.yellow;
        
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
            isJoystickActive = true;
            touchStartPosition = eventData.position;
            
            if (isDynamicJoystick)
            {
                // 동적 조이스틱: 터치 위치에 조이스틱 배치
                joystickBackground.position = touchStartPosition;
                joystickBackground.gameObject.SetActive(true);
            }
            
            // 시각적 피드백
            backgroundImage.color = activeColor;
            handleImage.color = activeColor;
            
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
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            ResetJoystick();
        }
        
        private void ResetJoystick()
        {
            isJoystickActive = false;
            inputVector = Vector2.zero;
            
            if (isDynamicJoystick)
            {
                joystickBackground.gameObject.SetActive(false);
            }
            else
            {
                joystickHandle.position = joystickBackground.position;
            }
            
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
            if (!isDynamicJoystick)
            {
                joystickBackground.gameObject.SetActive(true);
            }
        }
    }
} 