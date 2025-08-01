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
        [SerializeField] private float collectionRadius = 2f; // 1에서 2로 변경
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
        // MobileUIManager 백업 시스템 제거 - ReceiveGameUI 조이스틱만 사용
        
        private Vector3 targetPosition;
        private bool isMoving = false;
        private ReceiveGameManagerEnhanced gameManager;
        private ReceiveGameUI gameUI;
        private AudioSource audioSource;
        private GameObject nameTag;
        private ReceiveGameNicknamePanel nicknamePanel;
        
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
        private int playerColorIndex = -1; // 플레이어 색상 인덱스
        private bool isColorSet = false; // 색상이 설정되었는지 확인
        
        private void Start()
        {
            // 모든 플레이어가 색상과 이름 태그를 설정
            targetPosition = transform.position;
            gameManager = FindObjectOfType<ReceiveGameManagerEnhanced>();
            gameUI = FindObjectOfType<ReceiveGameUI>();
            
            // Rigidbody 설정 (중력 비활성화, 2D 평면 이동)
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false; // 중력 비활성화
                rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
            
            // 플레이어 색상 설정 (모든 플레이어가 설정)
            SetPlayerColor();
            
            // 이름 태그 생성 (모든 플레이어가 생성)
            CreateNameTag();
            
            if (photonView.IsMine)
            {
                // 로컬 플레이어만 추가 설정
                Debug.Log($"로컬 플레이어 초기화: {PhotonNetwork.LocalPlayer.NickName}");
                
                // 플랫폼 감지
                DetectPlatform();
                
                            // MobileUIManager 백업 시스템 제거됨 - ReceiveGameUI 조이스틱만 사용
                
                // Input System 초기화
                InitializeInputSystem();
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
                // 로컬 플레이어만 입력 처리
                HandleInput();
                CheckItemCollection();
            }
            else
            {
                // 다른 플레이어의 위치 보간
                transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
                transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 10f);
            }
            
            // 파워업 효과 체크
            CheckPowerUpEffects();
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
            
            // 키보드 입력 처리 (PC 테스트용 - 모바일 UI가 작동하지 않을 때 대체)
            if (moveDirection.magnitude < 0.1f)
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                    moveDirection.z += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                    moveDirection.z -= 1f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                    moveDirection.x -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                    moveDirection.x += 1f;
            }
            
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
            
            if (colliders.Length > 0)
            {
                Debug.Log($"플레이어 {PhotonNetwork.LocalPlayer.ActorNumber} 주변에 {colliders.Length}개의 콜라이더 발견");
            }
            
            foreach (Collider collider in colliders)
            {
                CollectibleItem item = collider.GetComponent<CollectibleItem>();
                if (item != null && !item.IsCollected)
                {
                    Debug.Log($"아이템 발견: {item.name}, 위치: {item.transform.position}, 플레이어 위치: {transform.position}, 수집 반경: {collectionRadius}");
                    CollectItem(item);
                }
                else if (item != null && item.IsCollected)
                {
                    Debug.Log($"이미 수집된 아이템: {item.name}");
                }
                else
                {
                    Debug.Log($"CollectibleItem 컴포넌트가 없는 오브젝트: {collider.name}");
                }
            }
        }
        
        private void CollectItem(CollectibleItem item)
        {
            if (!item.IsCollected)
            {
                Debug.Log($"플레이어 {PhotonNetwork.LocalPlayer.ActorNumber}가 아이템 수집 시도");
                
                // 아이템 타입 가져오기
                ItemType itemType = GetItemType(item);
                
                // 아이템을 즉시 수집된 상태로 표시하여 중복 수집 방지
                item.Collect();
                
                // ReceiveGameManagerEnhanced 직접 찾기
                ReceiveGameManagerEnhanced enhancedManager = FindObjectOfType<ReceiveGameManagerEnhanced>();
                if (enhancedManager != null)
                {
                    // 로컬 플레이어만 점수 증가 요청 (중복 방지) - 아이템 타입 전달
                    enhancedManager.CollectItem(PhotonNetwork.LocalPlayer.ActorNumber, itemType);
                    Debug.Log($"ReceiveGameManagerEnhanced에 아이템 수집 알림 전송 - 플레이어: {PhotonNetwork.LocalPlayer.ActorNumber}, 타입: {itemType}");
                }
                else
                {
                    Debug.LogError("ReceiveGameManagerEnhanced를 찾을 수 없습니다!");
                }
                
                // 수집 효과 재생
                PlayCollectEffect();
                
                // spawnedItems 리스트에서 제거 (중복 제거 방지)
                if (enhancedManager != null && item.gameObject != null)
                {
                    enhancedManager.RemoveItemFromList(item.gameObject);
                }
            }
            else
            {
                Debug.LogWarning($"아이템이 이미 수집되었습니다: {item.name}");
            }
        }
        
        private ItemType GetItemType(CollectibleItem item)
        {
            // EnhancedItemController에서 아이템 타입 가져오기
            EnhancedItemController enhancedController = item.GetComponent<EnhancedItemController>();
            if (enhancedController != null)
            {
                return enhancedController.GetItemType();
            }
            
            // 기본값
            return ItemType.Normal;
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
            if (playerRenderer != null && !isColorSet)
            {
                // RoomPopUp에서 설정된 색상 가져오기
                int colorIndex = GetPlayerColorIndex();
                playerColorIndex = colorIndex; // 네트워크 동기화용 변수에 저장
                Color playerColor = GetColorByIndex(colorIndex);
                playerRenderer.material.color = playerColor;
                isColorSet = true;
                
                Debug.Log($"플레이어 {photonView.Owner?.NickName ?? "Unknown"} 색상 설정: {colorIndex}");
            }
        }
        
        private int GetPlayerColorIndex()
        {
            // 해당 플레이어의 색상 정보 가져오기
            Player targetPlayer = photonView.Owner;
            if (targetPlayer == null)
            {
                targetPlayer = PhotonNetwork.LocalPlayer;
            }
            
            // PhotonManager에서 플레이어 색상 정보 가져오기
            try
            {
                if (PhotonManager.Instance != null)
                {
                    return PhotonManager.Instance.GetPlayerColor(targetPlayer);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"플레이어 색상 정보를 가져올 수 없습니다: {e.Message}");
            }
            
            // 기본값: ActorNumber 기반
            return (targetPlayer.ActorNumber - 1) % 8;
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
            // Screen Space - Overlay Canvas 찾기
            Canvas overlayCanvas = FindObjectOfType<Canvas>();
            if (overlayCanvas == null || overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.Log("Screen Space - Overlay Canvas를 자동으로 생성합니다.");
                overlayCanvas = CreateOverlayCanvas();
            }
            
            if (nameTagPrefab != null)
            {
                // 프리팹에서 이름 태그 생성
                nameTag = Instantiate(nameTagPrefab, overlayCanvas.transform);
                nicknamePanel = nameTag.GetComponent<ReceiveGameNicknamePanel>();
            }
            else
            {
                // 동적으로 이름 태그 생성
                CreateDynamicNicknamePanel(overlayCanvas);
            }
            
            if (nicknamePanel != null)
            {
                // 해당 플레이어의 닉네임 표시
                if (photonView.Owner != null)
                {
                    nicknamePanel.SetInfo(photonView.Owner.NickName, transform);
                    Debug.Log($"이름 태그 생성: {photonView.Owner.NickName}");
                }
                else
                {
                    nicknamePanel.SetInfo("Unknown Player", transform);
                }
            }
        }
        
        private void CreateDynamicNicknamePanel(Canvas canvas)
        {
            // 동적으로 이름 태그 생성
            nameTag = new GameObject("NicknamePanel");
            nameTag.transform.SetParent(canvas.transform);
            
            // RectTransform 설정
            RectTransform rectTransform = nameTag.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(120, 30);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            
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
            
            // TextMeshProUGUI 컴포넌트 추가
            GameObject textObj = new GameObject("NicknameText");
            textObj.transform.SetParent(nameTag.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(3, 3);
            textRect.offsetMax = new Vector2(-3, -3);
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 16f;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.text = "Player";
            
            // NicknamePanel 컴포넌트 추가
            nicknamePanel = nameTag.AddComponent<ReceiveGameNicknamePanel>();
            nicknamePanel.nicknameText = textComponent;
        }
        
        private Canvas CreateOverlayCanvas()
        {
            // Screen Space - Overlay Canvas 생성
            GameObject canvasObj = new GameObject("OverlayCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // 다른 UI보다 위에 표시
            
            // Canvas Scaler 추가
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            // Graphic Raycaster 추가
            canvasObj.AddComponent<GraphicRaycaster>();
            
            Debug.Log("Screen Space - Overlay Canvas 생성 완료");
            return canvas;
        }
        
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // 데이터 전송
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(isMoving);
                stream.SendNext(playerColorIndex); // 색상 인덱스 전송
            }
            else
            {
                // 데이터 수신
                networkPosition = (Vector3)stream.ReceiveNext();
                networkRotation = (Quaternion)stream.ReceiveNext();
                isMoving = (bool)stream.ReceiveNext();
                int receivedColorIndex = (int)stream.ReceiveNext();
                
                // 색상 동기화
                if (receivedColorIndex != playerColorIndex && !isColorSet)
                {
                    playerColorIndex = receivedColorIndex;
                    if (playerRenderer != null)
                    {
                        Color playerColor = GetColorByIndex(playerColorIndex);
                        playerRenderer.material.color = playerColor;
                        isColorSet = true;
                        Debug.Log($"네트워크에서 받은 색상 적용: {playerColorIndex}");
                    }
                }
                
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
                // PC에서도 모바일 UI를 테스트할 수 있도록 수정
                isMobilePlatform = true; // 테스트용으로 모바일로 설정
            #endif
            
            Debug.Log($"플랫폼 감지: {(isMobilePlatform ? "모바일" : "데스크톱")} - 마우스 입력 지원");
        }
        
        // 모바일 입력 처리 (ReceiveGameUI 조이스틱만 사용)
        private void HandleMobileInput(ref Vector3 moveDirection)
        {
            if (gameUI != null)
            {
                Vector2 joystickInput = gameUI.JoystickInput;
                if (joystickInput.magnitude > 0.1f)
                {
                    moveDirection = new Vector3(joystickInput.x, 0, joystickInput.y);
                    isMoving = true;
                }
                else
                {
                    isMoving = false;
                }
            }
            else
            {
                Debug.LogWarning("ReceiveGameUI가 null입니다. 조이스틱 입력을 처리할 수 없습니다.");
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