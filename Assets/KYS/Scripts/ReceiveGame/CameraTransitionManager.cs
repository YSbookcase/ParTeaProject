using UnityEngine;
using Cinemachine;
using System.Collections;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace KYS
{
    public class CameraTransitionManager : MonoBehaviour
    {
        [Header("Cinemachine Virtual Cameras")]
        [SerializeField] private CinemachineVirtualCamera introCamera; // 첫 화면용 카메라
        [SerializeField] private CinemachineVirtualCamera gameCamera; // 게임용 카메라
        
        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 3f; // 전환 시간
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 전환 커브
        
        [Header("Timeline (Optional)")]
        [SerializeField] private PlayableDirector timelineDirector; // Timeline 사용 시
        [SerializeField] private TimelineAsset cameraTimeline; // 카메라 전환용 Timeline
        
        [Header("Manual Transition")]
        [SerializeField] private bool useManualTransition = true; // 수동 전환 사용 여부
        [SerializeField] private Vector3 introPosition = new Vector3(0, 50, -30); // 첫 화면 카메라 위치
        [SerializeField] private Vector3 introRotation = new Vector3(60, 0, 0); // 첫 화면 카메라 회전
        [SerializeField] private Vector3 gamePosition = new Vector3(0, 20, -8); // 게임 카메라 위치
        [SerializeField] private Vector3 gameRotation = new Vector3(30, 0, 0); // 게임 카메라 회전
        
        private CinemachineBrain cinemachineBrain;
        private bool isTransitioning = false;
        private Coroutine transitionCoroutine;
        
        private void Start()
        {
            // Cinemachine Brain 찾기
            cinemachineBrain = FindObjectOfType<CinemachineBrain>();
            if (cinemachineBrain == null)
            {
                Debug.LogError("[CameraTransitionManager] CinemachineBrain을 찾을 수 없습니다!");
                return;
            }
            
            // 초기 설정
            SetupInitialCamera();
        }
        
        private void SetupInitialCamera()
        {
            if (introCamera != null && gameCamera != null)
            {
                // 첫 화면 카메라를 우선순위로 설정
                introCamera.Priority = 10;
                gameCamera.Priority = 5;
                
                // 첫 화면 카메라 위치 설정
                if (useManualTransition)
                {
                    introCamera.transform.position = introPosition;
                    introCamera.transform.rotation = Quaternion.Euler(introRotation);
                }
            }
        }
        
        /// <summary>
        /// 게임 시작 시 카메라 전환을 시작합니다.
        /// </summary>
        public void StartCameraTransition()
        {
            if (isTransitioning) return;
            
            if (timelineDirector != null && cameraTimeline != null)
            {
                // Timeline을 사용한 전환
                StartTimelineTransition();
            }
            else if (useManualTransition)
            {
                // 수동 전환
                StartManualTransition();
            }
            else
            {
                // 우선순위 변경으로 전환
                StartPriorityTransition();
            }
        }
        
        /// <summary>
        /// Timeline을 사용한 카메라 전환
        /// </summary>
        private void StartTimelineTransition()
        {
            if (timelineDirector != null && cameraTimeline != null)
            {
                timelineDirector.playableAsset = cameraTimeline;
                timelineDirector.Play();
                
                // Timeline 완료 후 게임 카메라로 전환
                StartCoroutine(WaitForTimelineComplete());
            }
        }
        
        private IEnumerator WaitForTimelineComplete()
        {
            isTransitioning = true;
            
            // Timeline이 완료될 때까지 대기
            while (timelineDirector.state == PlayState.Playing)
            {
                yield return null;
            }
            
            // 게임 카메라로 최종 전환
            if (gameCamera != null)
            {
                gameCamera.Priority = 10;
                introCamera.Priority = 5;
            }
            
            isTransitioning = false;
        }
        
        /// <summary>
        /// 수동 카메라 전환 (부드러운 이동)
        /// </summary>
        private void StartManualTransition()
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            
            transitionCoroutine = StartCoroutine(ManualTransitionCoroutine());
        }
        
        private IEnumerator ManualTransitionCoroutine()
        {
            isTransitioning = true;
            
            Vector3 startPos = introCamera.transform.position;
            Quaternion startRot = introCamera.transform.rotation;
            Vector3 endPos = gamePosition;
            Quaternion endRot = Quaternion.Euler(gameRotation);
            
            float elapsed = 0f;
            
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                float curveValue = transitionCurve.Evaluate(t);
                
                // 위치와 회전 보간
                introCamera.transform.position = Vector3.Lerp(startPos, endPos, curveValue);
                introCamera.transform.rotation = Quaternion.Lerp(startRot, endRot, curveValue);
                
                yield return null;
            }
            
            // 정확한 최종 위치로 설정
            introCamera.transform.position = endPos;
            introCamera.transform.rotation = endRot;
            
            // 게임 카메라로 전환
            if (gameCamera != null)
            {
                gameCamera.Priority = 10;
                introCamera.Priority = 5;
            }
            
            isTransitioning = false;
            transitionCoroutine = null;
        }
        
        /// <summary>
        /// 우선순위 변경으로 카메라 전환
        /// </summary>
        private void StartPriorityTransition()
        {
            if (gameCamera != null && introCamera != null)
            {
                gameCamera.Priority = 10;
                introCamera.Priority = 5;
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
        /// 카메라 전환을 강제로 중지
        /// </summary>
        public void StopTransition()
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }
            
            isTransitioning = false;
            
            // 즉시 게임 카메라로 전환
            if (gameCamera != null)
            {
                gameCamera.Priority = 10;
                introCamera.Priority = 5;
            }
        }
        
        /// <summary>
        /// 첫 화면 카메라로 되돌리기 (테스트용)
        /// </summary>
        [ContextMenu("Reset to Intro Camera")]
        public void ResetToIntroCamera()
        {
            if (introCamera != null)
            {
                introCamera.Priority = 10;
                gameCamera.Priority = 5;
                
                if (useManualTransition)
                {
                    introCamera.transform.position = introPosition;
                    introCamera.transform.rotation = Quaternion.Euler(introRotation);
                }
            }
        }
        
        /// <summary>
        /// 게임 카메라로 즉시 전환 (테스트용)
        /// </summary>
        [ContextMenu("Switch to Game Camera")]
        public void SwitchToGameCamera()
        {
            if (gameCamera != null)
            {
                gameCamera.Priority = 10;
                introCamera.Priority = 5;
            }
        }
    }
}


