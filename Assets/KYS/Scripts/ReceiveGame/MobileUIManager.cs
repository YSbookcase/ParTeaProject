using UnityEngine;
using UnityEngine.UI;

namespace KYS
{
    public class MobileUIManager : MonoBehaviour
    {
        [Header("Mobile UI Elements")]
        [SerializeField] private MobileJoystick leftJoystick;
        [SerializeField] private MobileJoystick rightJoystick;
        [SerializeField] private MobileActionButton jumpButton;
        [SerializeField] private MobileActionButton actionButton;
        [SerializeField] private MobileActionButton specialButton;
        
        [Header("UI Layout")]
        [SerializeField] private Canvas mobileCanvas;
        [SerializeField] private CanvasScaler canvasScaler;
        [SerializeField] private bool isMobilePlatform = false;
        
        [Header("Settings")]
        [SerializeField] private bool enableJoystick = true;
        [SerializeField] private bool enableButtons = true;
        [SerializeField] private bool autoDetectPlatform = true;
        
        private bool isInitialized = false;
        
        public MobileJoystick LeftJoystick => leftJoystick;
        public MobileJoystick RightJoystick => rightJoystick;
        public MobileActionButton JumpButton => jumpButton;
        public MobileActionButton ActionButton => actionButton;
        public MobileActionButton SpecialButton => specialButton;
        
        private void Awake()
        {
            // 플랫폼 자동 감지
            if (autoDetectPlatform)
            {
                isMobilePlatform = IsMobilePlatform();
            }
            
            // 모바일이 아닌 경우 UI 비활성화
            if (!isMobilePlatform)
            {
                DisableMobileUI();
                return;
            }
            
            InitializeMobileUI();
        }
        
        private void Start()
        {
            if (isMobilePlatform && !isInitialized)
            {
                InitializeMobileUI();
            }
        }
        
        private bool IsMobilePlatform()
        {
            #if UNITY_ANDROID || UNITY_IOS || UNITY_WSA
                return true;
            #else
                return false;
            #endif
        }
        
        private void InitializeMobileUI()
        {
            if (isInitialized) return;
            
            // Canvas 설정
            if (mobileCanvas != null)
            {
                SetupCanvas();
            }
            
            // 조이스틱 설정
            if (enableJoystick && leftJoystick != null)
            {
                SetupJoysticks();
            }
            
            // 버튼 설정
            if (enableButtons)
            {
                SetupButtons();
            }
            
            isInitialized = true;
            Debug.Log("모바일 UI 초기화 완료");
        }
        
        private void SetupCanvas()
        {
            if (canvasScaler != null)
            {
                // 모바일 최적화된 Canvas 설정
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1920, 1080);
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = 0.5f; // 중간값으로 설정
            }
        }
        
        private void SetupJoysticks()
        {
            if (leftJoystick != null)
            {
                leftJoystick.EnableJoystick();
            }
            
            if (rightJoystick != null)
            {
                rightJoystick.EnableJoystick();
            }
        }
        
        private void SetupButtons()
        {
            if (jumpButton != null)
            {
                jumpButton.SetActionName("Jump");
                jumpButton.OnActionPressed += OnJumpPressed;
                jumpButton.EnableButton();
            }
            
            if (actionButton != null)
            {
                actionButton.SetActionName("Action");
                actionButton.OnActionPressed += OnActionPressed;
                actionButton.EnableButton();
            }
            
            if (specialButton != null)
            {
                specialButton.SetActionName("Special");
                specialButton.OnActionPressed += OnSpecialPressed;
                specialButton.EnableButton();
            }
        }
        
        private void OnJumpPressed(string actionName)
        {
            Debug.Log("점프 버튼 눌림");
            // ReceiveGamePlayer에 점프 신호 전달
            var player = FindObjectOfType<ReceiveGamePlayer>();
            if (player != null && player.photonView.IsMine)
            {
                // 점프 로직 실행
            }
        }
        
        private void OnActionPressed(string actionName)
        {
            Debug.Log("액션 버튼 눌림");
            // ReceiveGamePlayer에 액션 신호 전달
            var player = FindObjectOfType<ReceiveGamePlayer>();
            if (player != null && player.photonView.IsMine)
            {
                // 액션 로직 실행
            }
        }
        
        private void OnSpecialPressed(string actionName)
        {
            Debug.Log("특수 액션 버튼 눌림");
            // ReceiveGamePlayer에 특수 액션 신호 전달
            var player = FindObjectOfType<ReceiveGamePlayer>();
            if (player != null && player.photonView.IsMine)
            {
                // 특수 액션 로직 실행
            }
        }
        
        public void DisableMobileUI()
        {
            if (mobileCanvas != null)
            {
                mobileCanvas.gameObject.SetActive(false);
            }
            
            if (leftJoystick != null)
            {
                leftJoystick.DisableJoystick();
            }
            
            if (rightJoystick != null)
            {
                rightJoystick.DisableJoystick();
            }
            
            if (jumpButton != null)
            {
                jumpButton.DisableButton();
            }
            
            if (actionButton != null)
            {
                actionButton.DisableButton();
            }
            
            if (specialButton != null)
            {
                specialButton.DisableButton();
            }
        }
        
        public void EnableMobileUI()
        {
            if (mobileCanvas != null)
            {
                mobileCanvas.gameObject.SetActive(true);
            }
            
            if (enableJoystick)
            {
                SetupJoysticks();
            }
            
            if (enableButtons)
            {
                SetupButtons();
            }
        }
        
        // 외부에서 조이스틱 입력 가져오기
        public Vector2 GetLeftJoystickInput()
        {
            return leftJoystick != null ? leftJoystick.InputVector : Vector2.zero;
        }
        
        public Vector2 GetRightJoystickInput()
        {
            return rightJoystick != null ? rightJoystick.InputVector : Vector2.zero;
        }
        
        // 외부에서 버튼 상태 확인
        public bool IsJumpButtonPressed()
        {
            return jumpButton != null && jumpButton.IsPressed;
        }
        
        public bool IsActionButtonPressed()
        {
            return actionButton != null && actionButton.IsPressed;
        }
        
        public bool IsSpecialButtonPressed()
        {
            return specialButton != null && specialButton.IsPressed;
        }
    }
} 