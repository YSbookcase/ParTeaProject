using UnityEngine;
using Cinemachine;

namespace KYS
{
    public class ReceiveGameCamera : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private float targetAspectRatio = 16f / 9f; // 세로 화면 16:9
        [SerializeField] private float fieldOfView = 60f; // 더 넓은 시야각
        
        [Header("Camera Position & Angle")]
        [SerializeField] private float cameraHeight = 35f; // 카메라 높이 (Y축)
        [SerializeField] private float cameraDistance = 25f; // 카메라 거리 (Z축)
        [SerializeField] private float cameraAngle = 45f; // 카메라 각도 (X축 회전)
        [SerializeField] private bool followXAxis = true; // X축 따라가기 (플레이어 이동)
        
        [Header("Camera Offset (Advanced)")]
        [SerializeField] private Vector3 cameraOffset = new Vector3(0, 35, -25); // 고급 설정용
        [SerializeField] private bool useAdvancedOffset = false; // 고급 오프셋 사용 여부
        
        [Header("Follow Settings")]
        [SerializeField] private bool followPlayers = true;
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float minDistance = 5f;
        [SerializeField] private float maxDistance = 15f;
        
        [Header("Camera Transition")]
        [SerializeField] private bool enableTransition = true; // 카메라 전환 활성화
        [SerializeField] private Vector3 introPosition = new Vector3(0, 50, -30); // 첫 화면 카메라 위치
        [SerializeField] private Vector3 introRotation = new Vector3(60, 0, 0); // 첫 화면 카메라 회전
        [SerializeField] private Vector3 gamePosition = new Vector3(0, 20, -8); // 게임 카메라 위치
        [SerializeField] private Vector3 gameRotation = new Vector3(30, 0, 0); // 게임 카메라 회전
        [SerializeField] private float transitionDuration = 3f; // 전환 시간
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 전환 커브
        
        [Header("Cinemachine Integration")]
        [SerializeField] private CinemachineVirtualCamera virtualCamera; // 시네머신 가상 카메라
        [SerializeField] private bool useCinemachine = false; // 시네머신 사용 여부
        
        private Camera gameCamera;
        private Vector3 targetPosition;
        private float targetFieldOfView;
        private bool isTransitioning = false;
        private bool isGameStarted = false;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private Vector3 endPosition;
        private Quaternion endRotation;
        private float transitionStartTime;
        
        private void Start()
        {
            gameCamera = GetComponent<Camera>();
            if (gameCamera == null)
            {
                gameCamera = Camera.main;
            }
            
            // 시네머신 가상 카메라 찾기
            if (virtualCamera == null && useCinemachine)
            {
                virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            }
            
            SetupCamera();
            
            // 첫 화면 위치로 설정
            if (enableTransition)
            {
                SetIntroPosition();
            }
        }
        
        private void SetupCamera()
        {
            if (gameCamera != null)
            {
                // 종횡비 설정
                float currentAspect = (float)Screen.width / Screen.height;
                if (currentAspect != targetAspectRatio)
                {
                    float scaleHeight = currentAspect / targetAspectRatio;
                    if (scaleHeight < 1.0f)
                    {
                        Rect rect = gameCamera.rect;
                        rect.width = 1.0f / scaleHeight;
                        rect.x = (1.0f - rect.width) / 2.0f;
                        gameCamera.rect = rect;
                    }
                    else
                    {
                        float scaleWidth = 1.0f / scaleHeight;
                        Rect rect = gameCamera.rect;
                        rect.height = scaleWidth;
                        rect.y = (1.0f - rect.height) / 2.0f;
                        gameCamera.rect = rect;
                    }
                }
                
                // Field of View 설정
                gameCamera.fieldOfView = fieldOfView;
            }
        }
        
        private void Update()
        {
            if (isTransitioning)
            {
                UpdateTransition();
            }
            else if (followPlayers && !isTransitioning)
            {
                UpdateCameraPosition();
                UpdateCameraZoom();
            }
        }
        
        private void UpdateCameraPosition()
        {
            // 모든 플레이어의 평균 위치 계산
            Vector3 averagePosition = Vector3.zero;
            int playerCount = 0;
            
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            
            foreach (GameObject player in players)
            {
                if (player != null)
                {
                    averagePosition += player.transform.position;
                    playerCount++;
                }
            }
            
            if (playerCount > 0)
            {
                averagePosition /= playerCount;
                
                // Inspector 설정에 따른 카메라 위치 계산
                Vector3 basePosition = useAdvancedOffset ? cameraOffset : new Vector3(0, cameraHeight, -cameraDistance);
                
                if (followXAxis)
                {
                    // X축만 따라가기 (플레이어 이동)
                    targetPosition = new Vector3(averagePosition.x, basePosition.y, basePosition.z);
                }
                else
                {
                    // 고정 위치
                    targetPosition = basePosition;
                }
            }
            
            // 부드러운 카메라 이동
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
            
            // Inspector에서 설정한 각도 유지
            transform.rotation = Quaternion.Euler(cameraAngle, 0f, 0f);
        }
        
        private void UpdateCameraZoom()
        {
            if (gameCamera == null) return;
            
            // 플레이어 수에 따른 줌 조정
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            if (players.Length > 0)
            {
                // 플레이어들 간의 최대 거리 계산
                float maxDistance = 0f;
                for (int i = 0; i < players.Length; i++)
                {
                    for (int j = i + 1; j < players.Length; j++)
                    {
                        float distance = Vector3.Distance(players[i].transform.position, players[j].transform.position);
                        if (distance > maxDistance)
                        {
                            maxDistance = distance;
                        }
                    }
                }
                
                // 거리에 따른 Field of View 조정
                float targetFOV = Mathf.Lerp(this.minDistance, this.maxDistance, maxDistance / 20f);
                gameCamera.fieldOfView = Mathf.Lerp(gameCamera.fieldOfView, targetFOV, Time.deltaTime * followSpeed);
            }
        }
        
        /// <summary>
        /// 첫 화면 위치로 설정
        /// </summary>
        public void SetIntroPosition()
        {
            if (useCinemachine && virtualCamera != null)
            {
                // 시네머신 사용 시
                virtualCamera.transform.position = introPosition;
                virtualCamera.transform.rotation = Quaternion.Euler(introRotation);
            }
            else
            {
                // 일반 카메라 사용 시
                transform.position = introPosition;
                transform.rotation = Quaternion.Euler(introRotation);
            }
        }
        
        /// <summary>
        /// 게임 시작 시 카메라 전환 시작
        /// </summary>
        public void StartCameraTransition()
        {
            if (isTransitioning || !enableTransition) return;
            
            isTransitioning = true;
            isGameStarted = true;
            transitionStartTime = Time.time;
            
            // 시작 위치와 회전 저장
            startPosition = transform.position;
            startRotation = transform.rotation;
            
            // 목표 위치와 회전 설정
            endPosition = gamePosition;
            endRotation = Quaternion.Euler(gameRotation);
            
            Debug.Log("[ReceiveGameCamera] 카메라 전환 시작");
        }
        
        private void UpdateTransition()
        {
            float elapsed = Time.time - transitionStartTime;
            float t = elapsed / transitionDuration;
            
            if (t >= 1f)
            {
                // 전환 완료
                transform.position = endPosition;
                transform.rotation = endRotation;
                isTransitioning = false;
                
                Debug.Log("[ReceiveGameCamera] 카메라 전환 완료");
            }
            else
            {
                // 전환 중
                float curveValue = transitionCurve.Evaluate(t);
                transform.position = Vector3.Lerp(startPosition, endPosition, curveValue);
                transform.rotation = Quaternion.Lerp(startRotation, endRotation, curveValue);
            }
        }
        
        /// <summary>
        /// 카메라 전환이 완료되었는지 확인
        /// </summary>
        public bool IsTransitionComplete()
        {
            return !isTransitioning;
        }
        
        /// <summary>
        /// 카메라 높이 설정
        /// </summary>
        public void SetCameraHeight(float height)
        {
            cameraHeight = height;
            if (!isTransitioning)
            {
                Vector3 newPosition = transform.position;
                newPosition.y = height;
                transform.position = newPosition;
            }
        }
        
        /// <summary>
        /// 카메라 거리 설정
        /// </summary>
        public void SetCameraDistance(float distance)
        {
            cameraDistance = distance;
            if (!isTransitioning)
            {
                Vector3 newPosition = transform.position;
                newPosition.z = -distance;
                transform.position = newPosition;
            }
        }
        
        /// <summary>
        /// 카메라 각도 설정
        /// </summary>
        public void SetCameraAngle(float angle)
        {
            cameraAngle = angle;
            if (!isTransitioning)
            {
                transform.rotation = Quaternion.Euler(angle, 0f, 0f);
            }
        }
        
        /// <summary>
        /// 카메라 위치 설정
        /// </summary>
        public void SetCameraPosition(Vector3 position)
        {
            if (!isTransitioning)
            {
                transform.position = position;
            }
        }
        
        /// <summary>
        /// 카메라 줌 설정
        /// </summary>
        public void SetCameraZoom(float zoom)
        {
            if (gameCamera != null)
            {
                gameCamera.fieldOfView = zoom;
            }
        }
        
        /// <summary>
        /// 카메라 리셋
        /// </summary>
        public void ResetCamera()
        {
            isTransitioning = false;
            isGameStarted = false;
            SetIntroPosition();
        }
        
        /// <summary>
        /// 고급 오프셋 토글
        /// </summary>
        public void ToggleAdvancedOffset(bool useAdvanced)
        {
            useAdvancedOffset = useAdvanced;
        }
        
        private void OnValidate()
        {
            // Inspector에서 값이 변경될 때 실시간 적용 (에디터에서만)
            if (Application.isPlaying && !isTransitioning)
            {
                SetupCamera();
            }
        }
        
        /// <summary>
        /// 테스트용: 즉시 게임 위치로 이동
        /// </summary>
        [ContextMenu("Move to Game Position")]
        public void MoveToGamePosition()
        {
            transform.position = gamePosition;
            transform.rotation = Quaternion.Euler(gameRotation);
        }
        
        /// <summary>
        /// 테스트용: 즉시 첫 화면 위치로 이동
        /// </summary>
        [ContextMenu("Move to Intro Position")]
        public void MoveToIntroPosition()
        {
            transform.position = introPosition;
            transform.rotation = Quaternion.Euler(introRotation);
        }
    }
} 