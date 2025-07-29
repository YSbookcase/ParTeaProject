using UnityEngine;

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
        
        private Camera gameCamera;
        private Vector3 targetPosition;
        private float targetFieldOfView;
        
        private void Start()
        {
            gameCamera = GetComponent<Camera>();
            if (gameCamera == null)
            {
                gameCamera = Camera.main;
            }
            
            SetupCamera();
        }
        
        private void SetupCamera()
        {
            if (gameCamera == null) return;
            
            // 카메라를 원근 투영으로 설정
            gameCamera.orthographic = false;
            gameCamera.fieldOfView = fieldOfView;
            
            // 화면 비율 설정 (세로 화면 16:9)
            float currentAspect = (float)Screen.width / Screen.height;
            float scaleHeight = currentAspect / targetAspectRatio;
            
            if (scaleHeight < 1.0f)
            {
                // 화면이 더 넓은 경우
                Rect rect = gameCamera.rect;
                rect.width = 1.0f / scaleHeight;
                rect.x = (1.0f - rect.width) / 2.0f;
                gameCamera.rect = rect;
            }
            else
            {
                // 화면이 더 좁은 경우
                float scaleWidth = 1.0f / scaleHeight;
                Rect rect = gameCamera.rect;
                rect.height = scaleWidth;
                rect.y = (1.0f - rect.height) / 2.0f;
                gameCamera.rect = rect;
            }
            
            // 초기 위치 설정 - Inspector에서 조절 가능
            Vector3 initialPosition = useAdvancedOffset ? cameraOffset : new Vector3(0, cameraHeight, -cameraDistance);
            transform.position = initialPosition;
            transform.rotation = Quaternion.Euler(cameraAngle, 0f, 0f);
            targetPosition = transform.position;
            targetFieldOfView = fieldOfView;
        }
        
        private void Update()
        {
            if (followPlayers)
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
            
            // 플레이어들 간의 거리에 따라 줌 조정
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            
            if (players.Length > 1)
            {
                float maxDistance = 0f;
                
                for (int i = 0; i < players.Length; i++)
                {
                    for (int j = i + 1; j < players.Length; j++)
                    {
                        if (players[i] != null && players[j] != null)
                        {
                            float distance = Vector3.Distance(players[i].transform.position, players[j].transform.position);
                            maxDistance = Mathf.Max(maxDistance, distance);
                        }
                    }
                }
                
                // 거리에 따른 줌 계산
                targetFieldOfView = Mathf.Clamp(maxDistance * 2f, minDistance, maxDistance);
            }
            else
            {
                targetFieldOfView = fieldOfView;
            }
            
            // 부드러운 줌 조정
            gameCamera.fieldOfView = Mathf.Lerp(gameCamera.fieldOfView, targetFieldOfView, followSpeed * Time.deltaTime);
        }
        
        // Inspector에서 실시간으로 카메라 설정 변경 가능
        public void SetCameraHeight(float height)
        {
            cameraHeight = height;
            if (!useAdvancedOffset)
            {
                Vector3 newPosition = transform.position;
                newPosition.y = height;
                transform.position = newPosition;
            }
        }
        
        public void SetCameraDistance(float distance)
        {
            cameraDistance = distance;
            if (!useAdvancedOffset)
            {
                Vector3 newPosition = transform.position;
                newPosition.z = -distance;
                transform.position = newPosition;
            }
        }
        
        public void SetCameraAngle(float angle)
        {
            cameraAngle = angle;
            transform.rotation = Quaternion.Euler(angle, 0f, 0f);
        }
        
        public void SetCameraPosition(Vector3 position)
        {
            targetPosition = position + cameraOffset;
        }
        
        public void SetCameraZoom(float zoom)
        {
            targetFieldOfView = Mathf.Clamp(zoom, minDistance, maxDistance);
        }
        
        public void ResetCamera()
        {
            targetPosition = useAdvancedOffset ? cameraOffset : new Vector3(0, cameraHeight, -cameraDistance);
            targetFieldOfView = fieldOfView;
        }
        
        // 고급 설정 토글
        public void ToggleAdvancedOffset(bool useAdvanced)
        {
            useAdvancedOffset = useAdvanced;
            SetupCamera();
        }
        
        private void OnValidate()
        {
            // 에디터에서 값이 변경될 때 카메라 설정 업데이트
            if (Application.isPlaying)
            {
                SetupCamera();
            }
        }
    }
} 