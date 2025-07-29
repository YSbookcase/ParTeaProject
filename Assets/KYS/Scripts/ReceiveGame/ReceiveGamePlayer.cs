using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.InputSystem;

namespace KYS
{
    public class ReceiveGamePlayer : MonoBehaviourPunCallbacks, IPunObservable
    {
        [Header("Player Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float collectionRadius = 1f;
        [SerializeField] private float baseMoveSpeed = 5f;
        [SerializeField] private float speedBoostMultiplier = 1.5f;
        [SerializeField] private float slowEffectMultiplier = 0.5f;
        
        [Header("Components")]
        [SerializeField] private Renderer playerRenderer;
        [SerializeField] private Animator playerAnimator;
        
        [Header("UI Elements")]
        [SerializeField] private GameObject nameTagPrefab;
        [SerializeField] private Vector3 nameTagOffset = new Vector3(0, 2.5f, 0);
        
        [Header("Effects")]
        [SerializeField] private GameObject collectEffect;
        [SerializeField] private AudioClip collectSound;
        
        [Header("Input System")]
        [SerializeField] private UnityEngine.InputSystem.InputActionAsset inputActions;
        
        [Header("Mobile UI")]
        [SerializeField] private MobileUIManager mobileUIManager;
        
        private Vector3 targetPosition;
        private bool isMoving = false;
        private ReceiveGameManager gameManager;
        private ReceiveGameUI gameUI;
        private AudioSource audioSource;
        private GameObject nameTag;
        private TextMeshPro nameText;
        
        // Input System 변수들
        private Vector2 moveInput;
        private bool jumpPressed;
        private bool actionPressed;
        
        // 모바일 입력 변수들
        private bool isMobilePlatform;
        
        // 파워업 효과 관련 변수들
        private bool hasSpeedBoost = false;
        private bool hasSlowEffect = false;
        private bool hasMagnetEffect = false;
        private float speedBoostEndTime = 0f;
        private float slowEffectEndTime = 0f;
        private float magnetEffectEndTime = 0f;
        
        // 네트워크 동기화용 변수들
        private Vector3 networkPosition;
        private Quaternion networkRotation;
        private float lag;
        
        private void Start()
        {
            if (photonView.IsMine)
            {
                // 로컬 플레이어 설정
                targetPosition = transform.position;
                gameManager = FindObjectOfType<ReceiveGameManager>();
                gameUI = FindObjectOfType<ReceiveGameUI>();
                
                // Rigidbody 설정 (중력 비활성화, 2D 평면 이동)
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = false; // 중력 비활성화
                    rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                }
                
                // 플랫폼 감지
                DetectPlatform();
                
                // Input System 초기화
                InitializeInputSystem();
                
                // 플레이어 색상 설정 (RoomPopUp에서 설정된 색상 사용)
                SetPlayerColor();
                
                // 이름 태그 생성
                CreateNameTag();
            }
            
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        private void Update()
        {
            if (photonView.IsMine)
            {
                HandleInput();
                CheckItemCollection();
            }
            else
            {
                // 다른 플레이어의 위치 보간
                transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
                transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 10f);
            }
            
            // 이름 태그 업데이트
            UpdateNameTag();
        }
        
        private void HandleInput()
        {
            // 파워업 효과 시간 체크
            CheckPowerUpEffects();
            
            Vector3 moveDirection = Vector3.zero;
            
            // 플랫폼별 입력 처리
            if (isMobilePlatform)
            {
                HandleMobileInput(ref moveDirection);
            }
            else
            {
                HandleDesktopInput(ref moveDirection);
            }
            
            // 키보드 입력 처리 (PC 테스트용)
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                moveDirection.z += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                moveDirection.z -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                moveDirection.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                moveDirection.x += 1f;
            
            if (moveDirection.magnitude > 0.1f)
            {
                isMoving = true;
            }
            
            // 이동 처리
            if (isMoving && moveDirection.magnitude > 0.1f)
            {
                // 이동 방향으로 회전
                if (moveDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(moveDirection);
                }
                
                // 이동 적용 (Y축 제한 - 2D 평면에서만 이동)
                Vector3 newPosition = transform.position + moveDirection.normalized * moveSpeed * Time.deltaTime;
                newPosition.y = transform.position.y; // Y축 위치 고정
                transform.position = newPosition;
                
                // 애니메이션 설정
                if (playerAnimator != null)
                {
                    playerAnimator.SetBool("IsMoving", true);
                    playerAnimator.SetFloat("MoveSpeed", moveDirection.magnitude);
                }
            }
            else
            {
                isMoving = false;
                if (playerAnimator != null)
                {
                    playerAnimator.SetBool("IsMoving", false);
                }
            }
        }
        
        private void CheckItemCollection()
        {
            // 주변 아이템 검사
            Collider[] colliders = Physics.OverlapSphere(transform.position, collectionRadius);
            
            foreach (Collider collider in colliders)
            {
                CollectibleItem item = collider.GetComponent<CollectibleItem>();
                if (item != null && !item.IsCollected)
                {
                    CollectItem(item);
                }
            }
        }
        
        private void CollectItem(CollectibleItem item)
        {
            if (gameManager != null)
            {
                // 게임 매니저에 아이템 수집 알림
                gameManager.CollectItem(PhotonNetwork.LocalPlayer.ActorNumber);
                
                // 수집 효과 재생
                PlayCollectEffect();
                
                // 아이템 제거
                item.Collect();
            }
        }
        
        private void PlayCollectEffect()
        {
            // 수집 효과 파티클
            if (collectEffect != null)
            {
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }
            
            // 수집 사운드
            if (collectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(collectSound);
            }
        }
        
        private void SetPlayerColor()
        {
            if (playerRenderer != null)
            {
                // RoomPopUp에서 설정된 색상 가져오기
                int colorIndex = GetPlayerColorIndex();
                Color playerColor = GetColorByIndex(colorIndex);
                playerRenderer.material.color = playerColor;
                
                Debug.Log($"플레이어 {PhotonNetwork.LocalPlayer.NickName} 색상 설정: {colorIndex}");
            }
        }
        
        private int GetPlayerColorIndex()
        {
            // PhotonManager에서 플레이어 색상 정보 가져오기
            try
            {
                if (PhotonManager.Instance != null)
                {
                    return PhotonManager.Instance.GetPlayerColor(PhotonNetwork.LocalPlayer);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"플레이어 색상 정보를 가져올 수 없습니다: {e.Message}");
            }
            
            // 기본값: ActorNumber 기반
            return (PhotonNetwork.LocalPlayer.ActorNumber - 1) % 8;
        }
        
        private Color GetColorByIndex(int index)
        {
            Color[] playerColors = new Color[]
            {
                Color.red,
                Color.blue,
                Color.green,
                Color.yellow,
                Color.magenta,
                Color.cyan,
                Color.white,
                Color.gray
            };
            
            return playerColors[index % playerColors.Length];
        }
        
        private void CreateNameTag()
        {
            if (nameTagPrefab != null)
            {
                // World Space Canvas에서 이름 태그 생성
                Canvas worldCanvas = FindObjectOfType<Canvas>();
                if (worldCanvas != null && worldCanvas.renderMode == RenderMode.WorldSpace)
                {
                    nameTag = Instantiate(nameTagPrefab, worldCanvas.transform);
                    nameText = nameTag.GetComponentInChildren<TextMeshPro>();
                }
                else
                {
                    Debug.LogWarning("World Space Canvas를 찾을 수 없습니다!");
                    CreateDynamicNameTag();
                }
            }
            else
            {
                CreateDynamicNameTag();
            }
            
            if (nameText != null)
            {
                nameText.text = PhotonNetwork.LocalPlayer.NickName;
            }
        }
        
        private void CreateDynamicNameTag()
        {
            // World Space Canvas 찾기
            Canvas worldCanvas = FindObjectOfType<Canvas>();
            if (worldCanvas == null || worldCanvas.renderMode != RenderMode.WorldSpace)
            {
                Debug.LogError("World Space Canvas가 필요합니다!");
                return;
            }
            
            // 동적으로 이름 태그 생성
            nameTag = new GameObject("NameTag");
            nameTag.transform.SetParent(worldCanvas.transform);
            
            // Panel 컴포넌트 추가
            RectTransform rectTransform = nameTag.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 50);
            rectTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            
            // 배경 이미지 추가
            GameObject background = new GameObject("Background");
            background.transform.SetParent(nameTag.transform);
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f);
            
            // TextMeshPro 컴포넌트 추가
            GameObject textObj = new GameObject("NameText");
            textObj.transform.SetParent(nameTag.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 5);
            textRect.offsetMax = new Vector2(-5, -5);
            
            nameText = textObj.AddComponent<TextMeshPro>();
            nameText.fontSize = 24f;
            nameText.color = Color.white;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.text = PhotonNetwork.LocalPlayer.NickName;
        }
        
        private void UpdateNameTag()
        {
            if (nameTag != null)
            {
                // 플레이어 위치에 이름 태그 위치 설정
                Vector3 worldPosition = transform.position + nameTagOffset;
                nameTag.transform.position = worldPosition;
                
                // 카메라를 향하도록 회전
                if (Camera.main != null)
                {
                    nameTag.transform.LookAt(Camera.main.transform);
                    nameTag.transform.Rotate(0, 180, 0); // 텍스트가 올바른 방향을 향하도록
                }
            }
        }
        
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // 데이터 전송
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(isMoving);
            }
            else
            {
                // 데이터 수신
                networkPosition = (Vector3)stream.ReceiveNext();
                networkRotation = (Quaternion)stream.ReceiveNext();
                isMoving = (bool)stream.ReceiveNext();
                
                lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // 수집 반경 시각화
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collectionRadius);
        }
        
        private void OnDestroy()
        {
            // 이름 태그 정리
            if (nameTag != null)
            {
                Destroy(nameTag);
            }
            
            // Input System 정리
            if (inputActions != null)
            {
                inputActions.Disable();
                // inputActions.Dispose(); // InputActionAsset에는 Dispose 메서드가 없음
            }
        }
        
        // 파워업 효과 관련 메서드들
        private void CheckPowerUpEffects()
        {
            // 속도 부스트 효과 체크
            if (hasSpeedBoost && Time.time >= speedBoostEndTime)
            {
                hasSpeedBoost = false;
                moveSpeed = baseMoveSpeed;
                Debug.Log("속도 부스트 효과 종료");
            }
            
            // 슬로우 효과 체크
            if (hasSlowEffect && Time.time >= slowEffectEndTime)
            {
                hasSlowEffect = false;
                moveSpeed = baseMoveSpeed;
                Debug.Log("슬로우 효과 종료");
            }
            
            // 자석 효과 체크
            if (hasMagnetEffect && Time.time >= magnetEffectEndTime)
            {
                hasMagnetEffect = false;
                Debug.Log("자석 효과 종료");
            }
        }
        
        public void ApplySpeedBoost(float duration)
        {
            if (photonView.IsMine)
            {
                hasSpeedBoost = true;
                speedBoostEndTime = Time.time + duration;
                moveSpeed = baseMoveSpeed * speedBoostMultiplier;
                Debug.Log($"속도 부스트 적용! 지속시간: {duration}초");
            }
        }
        
        public void ApplySlowEffect(float duration)
        {
            if (photonView.IsMine)
            {
                hasSlowEffect = true;
                slowEffectEndTime = Time.time + duration;
                moveSpeed = baseMoveSpeed * slowEffectMultiplier;
                Debug.Log($"슬로우 효과 적용! 지속시간: {duration}초");
            }
        }
        
        public void ApplyMagnetEffect(float duration)
        {
            if (photonView.IsMine)
            {
                hasMagnetEffect = true;
                magnetEffectEndTime = Time.time + duration;
                Debug.Log($"자석 효과 적용! 지속시간: {duration}초");
            }
        }
        
        public int GetPlayerActorNumber()
        {
            return photonView.Owner.ActorNumber;
        }
        
        // 플랫폼 감지
        private void DetectPlatform()
        {
            #if UNITY_ANDROID || UNITY_IOS || UNITY_WSA
                isMobilePlatform = true;
            #else
                isMobilePlatform = false;
            #endif
            
            Debug.Log($"플랫폼 감지: {(isMobilePlatform ? "모바일" : "데스크톱")}");
        }
        
        // 모바일 입력 처리
        private void HandleMobileInput(ref Vector3 moveDirection)
        {
            if (mobileUIManager != null)
            {
                // 조이스틱 입력 처리
                Vector2 joystickInput = mobileUIManager.GetLeftJoystickInput();
                if (joystickInput.magnitude > 0.1f)
                {
                    moveDirection = new Vector3(joystickInput.x, 0, joystickInput.y);
                    isMoving = true;
                }
                else
                {
                    isMoving = false;
                }
                
                // 버튼 입력 처리
                if (mobileUIManager.IsJumpButtonPressed())
                {
                    jumpPressed = true;
                }
                
                if (mobileUIManager.IsActionButtonPressed())
                {
                    actionPressed = true;
                }
            }
        }
        
        // 데스크톱 입력 처리
        private void HandleDesktopInput(ref Vector3 moveDirection)
        {
            // Input System을 사용한 입력 처리
            if (moveInput.magnitude > 0.1f)
            {
                // 입력을 3D 이동으로 변환
                moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
                isMoving = true;
            }
            else
            {
                isMoving = false;
            }
            
            // Input System이 초기화되지 않은 경우 기본 입력 사용
            if (inputActions == null)
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                    moveDirection.z += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                    moveDirection.z -= 1f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                    moveDirection.x -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                    moveDirection.x += 1f;
                
                if (moveDirection.magnitude > 0.1f)
                {
                    isMoving = true;
                }
                
                // 점프 입력
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    jumpPressed = true;
                    Debug.Log("점프!");
                }
                else
                {
                    jumpPressed = false;
                }
                
                // 액션 입력
                if (Input.GetKeyDown(KeyCode.E))
                {
                    actionPressed = true;
                    Debug.Log("액션!");
                }
                else
                {
                    actionPressed = false;
                }
            }
        }
        
        // Input System 관련 메서드들
        private void InitializeInputSystem()
        {
            // Input Actions가 할당되지 않은 경우 자동 생성
            if (inputActions == null)
            {
                // 임시로 기본 Input.GetKey 사용
                Debug.LogWarning("Input Actions가 할당되지 않았습니다. 기본 입력을 사용합니다.");
                return;
            }
            
            // 이벤트 핸들러 등록
            var playerActionMap = inputActions.FindActionMap("Player");
            if (playerActionMap != null)
            {
                var moveAction = playerActionMap.FindAction("Move");
                var jumpAction = playerActionMap.FindAction("Jump");
                var actionAction = playerActionMap.FindAction("Action");
                
                if (moveAction != null)
                {
                    moveAction.performed += OnMove;
                    moveAction.canceled += OnMove;
                }
                if (jumpAction != null)
                {
                    jumpAction.performed += OnJump;
                }
                if (actionAction != null)
                {
                    actionAction.performed += OnAction;
                }
                
                // Input System 활성화
                inputActions.Enable();
                Debug.Log("Input System 초기화 완료");
            }
            else
            {
                Debug.LogError("Player Action Map을 찾을 수 없습니다.");
            }
        }
        
        private void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }
        
        private void OnJump(InputAction.CallbackContext context)
        {
            jumpPressed = context.performed;
            if (jumpPressed)
            {
                // 점프 로직 (필요시 구현)
                Debug.Log("점프!");
            }
        }
        
        private void OnAction(InputAction.CallbackContext context)
        {
            actionPressed = context.performed;
            if (actionPressed)
            {
                // 액션 로직 (필요시 구현)
                Debug.Log("액션!");
            }
        }
    }
} 