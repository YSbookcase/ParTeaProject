using UnityEngine;

namespace KYS
{
    public class ReceiveGameCamera : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private float targetAspectRatio = 16f / 9f; // 세로 화면 16:9
        [SerializeField] private float fieldOfView = 60f;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0, 10, -10);
        
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
            
            // 초기 위치 설정
            transform.position = cameraOffset;
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
                targetPosition = averagePosition + cameraOffset;
            }
            
            // 부드러운 카메라 이동
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
            
            // 플레이어들을 바라보도록 회전
            if (playerCount > 0)
            {
                Vector3 lookDirection = averagePosition - transform.position;
                if (lookDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, followSpeed * Time.deltaTime);
                }
            }
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
            targetPosition = cameraOffset;
            targetFieldOfView = fieldOfView;
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