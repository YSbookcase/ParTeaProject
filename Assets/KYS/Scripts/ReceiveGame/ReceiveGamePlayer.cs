using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private float collectionRadius = 3f;
        [SerializeField] private float baseMoveSpeed = 5f;
        [SerializeField] private float speedBoostMultiplier = 1.5f;
        [SerializeField] private float slowEffectMultiplier = 0.5f;
        [SerializeField] private LayerMask itemLayerMask = -1;
        
        [Header("Mobile Optimization")]
        [SerializeField] private float collectionCheckInterval = 0.01f;
        [SerializeField] private float mobileCollectionRadius = 7f;
        
        [Header("Magnetic Effect Settings")]
        [SerializeField] private float magnetRadius = 5f;
        [SerializeField] private float magnetForce = 10f;
        
        [Header("Components")]
        [SerializeField] private Renderer[] playerRenderers;
        [SerializeField] private Animator playerAnimator;
        
        [Header("UI Elements")]
        [SerializeField] private GameObject nameTagPrefab;
        [SerializeField] private Vector3 nameTagOffset = new Vector3(0, 2.5f, 0);
        
        [Header("Effects")]
        [SerializeField] private GameObject collectEffect;
        [SerializeField] private string collectSoundName = "SFX_NormalItem";
        
        [Header("Player Effect Management")]
        [SerializeField] private Transform effectParent;
        [SerializeField] private ItemConfiguration itemConfiguration;
        
        [Header("Input System")]
        [SerializeField] private UnityEngine.InputSystem.InputActionAsset inputActions;
        
        // Private variables
        private Vector3 targetPosition; // 목표 이동 위치
        private bool isMoving = false; // 현재 이동 중인지 여부
        private ReceiveGameManagerEnhanced gameManager; // 게임 매니저 참조
        private ReceiveGameUI gameUI; // UI 매니저 참조
        private GameObject nameTag; // 플레이어 이름 태그 오브젝트
        private ReceiveGameNicknamePanel nicknamePanel; // 닉네임 패널 컴포넌트
        private float lastCollectionCheckTime = 0f; // 마지막 아이템 수집 체크 시간
        
        // Input System variables
        private Vector2 moveInput; // 이동 입력 값
        private bool jumpPressed; // 점프 버튼 눌림 여부
        private bool actionPressed; // 액션 버튼 눌림 여부
        
        // Platform detection
        private bool isMobilePlatform; // 모바일 플랫폼 여부
        
        // Power-up effect variables
        private bool hasSpeedBoost = false; // 속도 부스트 효과 활성화 여부
        private bool hasSlowEffect = false; // 슬로우 효과 활성화 여부
        private bool hasMagnetEffect = false; // 자석 효과 활성화 여부
        private float speedBoostEndTime = 0f; // 속도 부스트 종료 시간
        private float slowEffectEndTime = 0f; // 슬로우 효과 종료 시간
        private float magnetEffectEndTime = 0f; // 자석 효과 종료 시간
        
        // Network synchronization variables
        private Vector3 networkPosition; // 네트워크 동기화용 위치
        private Quaternion networkRotation; // 네트워크 동기화용 회전
        private Vector3 networkVelocity; // 네트워크 동기화용 속도
        private float lag; // 네트워크 지연 시간
        private int playerColorIndex = -1; // 플레이어 색상 인덱스
        private bool isColorSet = false; // 색상 설정 완료 여부
        
        // Effect management variables
        private Dictionary<ItemType, GameObject> activeEffects = new Dictionary<ItemType, GameObject>(); // 활성화된 이펙트들
        private Dictionary<ItemType, Coroutine> effectCoroutines = new Dictionary<ItemType, Coroutine>(); // 이펙트 코루틴들
        
        // Component references
        private Rigidbody rb; // 리지드바디 컴포넌트
        private Collider playerCollider; // 콜라이더 컴포넌트

        #region Unity Lifecycle

        private void Start()
        {
            DetectPlatform();
            InitializeComponents();
            InitializeGameUI();
            
            if (photonView.IsMine)
            {
                InitializeLocalPlayer();
            }
            
            InitializeEffectParent();
        }
        
        private void Update()
        {
            if (photonView.IsMine)
            {
                HandleInput();
                CheckItemCollectionOptimized();
                
                if (hasMagnetEffect)
                {
                    CheckMagneticAttraction();
                }
            }
            else
            {
                InterpolateOtherPlayerMovement();
            }
            
            CheckPowerUpEffects();
        }
        
        private void OnDestroy()
        {
            CleanupOnDestroy();
        }

        #endregion

        #region Initialization

        private void InitializeComponents() // 컴포넌트 초기화
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            
            playerCollider = GetComponent<Collider>();
        }
        
        private void InitializeLocalPlayer() // 로컬 플레이어 초기화
        {
            SetPlayerColor();
            CreateNameTag();
            InitializeInputSystem();
        }
        
        private void InitializeGameUI() // UI 시스템 초기화
        {
            gameUI = FindObjectOfType<ReceiveGameUI>();
            gameManager = FindObjectOfType<ReceiveGameManagerEnhanced>();
            
            if (gameUI == null)
            {
                Debug.LogWarning("[ReceiveGamePlayer] ReceiveGameUI를 찾을 수 없습니다.");
            }
            
            if (gameManager == null)
            {
                Debug.LogWarning("[ReceiveGamePlayer] ReceiveGameManagerEnhanced를 찾을 수 없습니다.");
            }
        }
        
        private void InitializeEffectParent() // 이펙트 부모 오브젝트 초기화
        {
            if (effectParent == null)
            {
                GameObject effectParentObj = new GameObject("PlayerEffects");
                effectParent = effectParentObj.transform;
                effectParent.SetParent(transform);
                effectParent.localPosition = Vector3.zero;
                effectParent.localRotation = Quaternion.identity;
            }
            
            if (itemConfiguration == null)
            {
                itemConfiguration = Resources.Load<ItemConfiguration>("ItemConfiguration");
            }
        }
        
        private void DetectPlatform() // 플랫폼 감지
        {
            #if UNITY_ANDROID || UNITY_IOS || UNITY_WSA
                isMobilePlatform = true;
            #else
                isMobilePlatform = true; // Test mode
            #endif
        }

        #endregion

        #region Input Handling

        private void HandleInput() // 입력 처리 메인 메서드
        {
            Vector3 moveDirection = Vector3.zero;
            
            if (isMobilePlatform)
            {
                HandleMobileInput(ref moveDirection);
            }
            else
            {
                HandleDesktopInput(ref moveDirection);
            }
            
            // Fallback keyboard input
            if (moveDirection.magnitude < 0.1f)
            {
                moveDirection = GetKeyboardInput();
            }
            
            ProcessMovement(moveDirection);
        }
        
        private void HandleMobileInput(ref Vector3 moveDirection) // 모바일 입력 처리
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
        }
        
        private void HandleDesktopInput(ref Vector3 moveDirection) // 데스크톱 입력 처리
        {
            if (moveInput.magnitude > 0.1f)
            {
                moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
                isMoving = true;
            }
            else
            {
                isMoving = false;
            }
            
            if (inputActions == null)
            {
                moveDirection = GetKeyboardInput();
                
                jumpPressed = Input.GetKeyDown(KeyCode.Space);
                actionPressed = Input.GetKeyDown(KeyCode.E);
            }
        }
        
        private Vector3 GetKeyboardInput() // 키보드 입력 처리
        {
            Vector3 input = Vector3.zero;
            
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                input.z += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                input.z -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                input.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                input.x += 1f;
                
            return input;
        }
        
        private void ProcessMovement(Vector3 moveDirection) // 이동 처리
        {
            if (moveDirection.magnitude > 0.1f)
            {
                isMoving = true;
                
                if (moveDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(moveDirection);
                }
                
                ApplyMovement(moveDirection);
                UpdateAnimation(true, moveDirection.magnitude);
            }
            else
            {
                isMoving = false;
                StopMovement();
                UpdateAnimation(false, 0f);
            }
        }
        
        private void ApplyMovement(Vector3 moveDirection) // 실제 이동 적용
        {
            Vector3 velocity = moveDirection.normalized * moveSpeed;
            velocity.y = 0;
            
            if (rb != null && !rb.isKinematic)
            {
                rb.velocity = velocity;
            }
            else
            {
                Vector3 newPosition = transform.position + velocity * Time.deltaTime;
                newPosition.y = transform.position.y;
                transform.position = newPosition;
            }
        }
        
        private void StopMovement() // 이동 정지
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
            }
        }
        
        private void UpdateAnimation(bool isMoving, float speed) // 애니메이션 업데이트
        {
            if (playerAnimator != null)
            {
                playerAnimator.SetBool("IsMoving", isMoving);
                playerAnimator.SetFloat("MoveSpeed", isMoving ? Mathf.Clamp01(speed) : 0f);
            }
        }

        #endregion

        #region Item Collection

        private void CheckItemCollectionOptimized() // 최적화된 아이템 수집 체크
        {
            if (Time.time - lastCollectionCheckTime < collectionCheckInterval)
                return;
                
            lastCollectionCheckTime = Time.time;
            CheckItemCollection();
        }
        
        private void CheckItemCollection() // 아이템 수집 체크
        {
            float currentCollectionRadius = isMobilePlatform ? mobileCollectionRadius : collectionRadius;
            int itemLayerMask = 1 << LayerMask.NameToLayer("Item");
            if (itemLayerMask == 0) itemLayerMask = -1;
            
            Collider[] colliders = Physics.OverlapSphere(transform.position, currentCollectionRadius, itemLayerMask);
            
            foreach (Collider collider in colliders)
            {
                ProcessItemCollision(collider);
            }
            
            if (isMobilePlatform && colliders.Length == 0)
            {
                CheckItemCollectionWithRaycast(currentCollectionRadius);
            }
        }
        
        private void ProcessItemCollision(Collider collider) // 아이템 충돌 처리
        {
            CollectibleItem item = collider.GetComponent<CollectibleItem>();
            if (item == null || item.IsCollected)
                return;
                
            ItemType itemType = GetItemType(item);
            
            if (itemType == ItemType.Slow)
            {
                LogDebugInfo($"Slow 아이템 감지 - 수집하지 않음: {item.name}");
                return;
            }
            
            LogDebugInfo($"아이템 발견: {item.name}, 타입: {itemType}");
            
            if (isMobilePlatform && item.gameObject.activeInHierarchy)
            {
                CollectItem(item);
                item.gameObject.SetActive(false);
                LogDebugInfo($"아이템 수집 완료 및 비활성화: {item.name}");
            }
            else
            {
                CollectItem(item);
            }
        }
        
        private void CheckItemCollectionWithRaycast(float radius) // Raycast를 이용한 아이템 수집 체크
        {
            Vector3[] directions = {
                Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
                new Vector3(1, 0, 1).normalized, new Vector3(-1, 0, 1).normalized,
                new Vector3(1, 0, -1).normalized, new Vector3(-1, 0, -1).normalized
            };
            
            foreach (Vector3 direction in directions)
            {
                if (Physics.Raycast(transform.position, direction, out RaycastHit hit, radius))
                {
                    CollectibleItem item = hit.collider.GetComponent<CollectibleItem>();
                    if (item != null && !item.IsCollected)
                    {
                        ItemType itemType = GetItemType(item);
                        if (itemType == ItemType.Slow)
                        {
                            LogDebugInfo($"Raycast로 Slow 아이템 감지 - 수집하지 않음: {item.name}");
                            continue;
                        }
                        
                        LogDebugInfo($"Raycast로 아이템 발견: {item.name}, 타입: {itemType}");
                        
                        if (isMobilePlatform && item.gameObject.activeInHierarchy)
                        {
                            CollectItem(item);
                            item.gameObject.SetActive(false);
                        }
                        else
                        {
                            CollectItem(item);
                        }
                    }
                }
            }
        }
        
        private void CheckMagneticAttraction() // 자석 효과 체크
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, magnetRadius);
            
            foreach (Collider collider in colliders)
            {
                CollectibleItem item = collider.GetComponent<CollectibleItem>();
                if (item != null && !item.IsCollected)
                {
                    AttractItem(item);
                }
            }
        }
        
        private void AttractItem(CollectibleItem item) // 아이템 끌어당기기
        {
            if (item == null || item.IsCollected) return;
            
            Vector3 directionToPlayer = (transform.position - item.transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, item.transform.position);
            
            if (distanceToPlayer > magnetRadius) return;
            
            float attractionForce = magnetForce * (1f - (distanceToPlayer / magnetRadius));
            
            Rigidbody itemRb = item.GetComponent<Rigidbody>();
            if (itemRb != null)
            {
                itemRb.AddForce(directionToPlayer * attractionForce, ForceMode.Force);
            }
        }
        
        private void CollectItem(CollectibleItem item) // 아이템 수집
        {
            if (item == null || item.IsCollected) return;
            
            ItemType itemType = GetItemType(item);
            LogDebugInfo($"아이템 수집 시도: {item.name}, 타입: {itemType}, 플레이어: {PhotonNetwork.LocalPlayer.ActorNumber}");
            
            item.Collect();
            
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<ReceiveGameManagerEnhanced>();
            }
            
            if (gameManager != null && photonView.IsMine)
            {
                gameManager.CollectItem(PhotonNetwork.LocalPlayer.ActorNumber, itemType);
            }
        }
        
        private ItemType GetItemType(CollectibleItem item) // 아이템 타입 가져오기
        {
            EnhancedItemController enhancedController = item.GetComponent<EnhancedItemController>();
            if (enhancedController != null)
            {
                return enhancedController.GetItemType();
            }
            
            if (item.itemType != ItemType.Normal)
            {
                return item.itemType;
            }
            
            return ItemType.Normal;
        }

        #endregion

        #region Power-up Effects

        private void CheckPowerUpEffects() // 파워업 효과 체크
        {
            CheckSpeedBoostEffect();
            CheckSlowEffect();
            CheckMagnetEffect();
        }
        
        private void CheckSpeedBoostEffect() // 속도 부스트 효과 체크
        {
            if (hasSpeedBoost && Time.time >= speedBoostEndTime)
            {
                hasSpeedBoost = false;
                moveSpeed = baseMoveSpeed;
                DeactivateEffect(this.photonView, ItemType.Speed);
            }
        }
        
        private void CheckSlowEffect() // 슬로우 효과 체크
        {
            if (hasSlowEffect && Time.time >= slowEffectEndTime)
            {
                hasSlowEffect = false;
                moveSpeed = baseMoveSpeed;
                DeactivateEffect(this.photonView, ItemType.Slow);
            }
        }
        
        private void CheckMagnetEffect() // 자석 효과 체크
        {
            if (hasMagnetEffect && Time.time >= magnetEffectEndTime)
            {
                hasMagnetEffect = false;
                DeactivateEffect(this.photonView, ItemType.Magnet);
            }
        }
        
        public void ApplySpeedBoost(float duration) // 속도 부스트 효과 적용
        {
            if (photonView.IsMine)
            {
                hasSpeedBoost = true;
                speedBoostEndTime = Time.time + duration;
                moveSpeed = baseMoveSpeed * speedBoostMultiplier;
                ActivateEffect(photonView, ItemType.Speed, duration);
            }
        }
        
        public void ApplySlowEffect(float duration) // 슬로우 효과 적용
        {
            if (photonView.IsMine)
            {
                hasSlowEffect = true;
                slowEffectEndTime = Time.time + duration;
                moveSpeed = baseMoveSpeed * slowEffectMultiplier;
                ActivateEffect(photonView, ItemType.Slow, duration);
            }
        }
        
        public void ApplyMagnetEffect(float duration, float customMagnetRadius = -1f, float customMagnetForce = -1f) // 자석 효과 적용
        {
            if (photonView.IsMine)
            {
                hasMagnetEffect = true;
                magnetEffectEndTime = Time.time + duration;
                
                if (customMagnetRadius > 0f)
                {
                    magnetRadius = customMagnetRadius;
                }
                if (customMagnetForce > 0f)
                {
                    magnetForce = customMagnetForce;
                }
                
                ActivateEffect(photonView, ItemType.Magnet, duration);
            }
        }

        #endregion

        #region Player Appearance

        private void SetPlayerColor() // 플레이어 색상 설정
        {
            if (playerRenderers == null || playerRenderers.Length == 0 || isColorSet)
                return;
                
            int colorIndex = GetPlayerColorIndex();
            playerColorIndex = colorIndex;
            Color playerColor = GetColorByIndex(colorIndex);
            
            foreach (Renderer renderer in playerRenderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = playerColor;
                }
            }
            
            isColorSet = true;
        }
        
        private int GetPlayerColorIndex() // 플레이어 색상 인덱스 가져오기
        {
            Player targetPlayer = photonView.Owner ?? PhotonNetwork.LocalPlayer;
            
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
            
            return (targetPlayer.ActorNumber - 1) % 8;
        }
        
        private Color GetColorByIndex(int index) // 인덱스로 색상 가져오기
        {
            Color[] playerColors = {
                Color.red, Color.blue, Color.green, Color.yellow,
                Color.magenta, Color.cyan, Color.white, Color.gray
            };
            
            return playerColors[index % playerColors.Length];
        }
        
        private void CreateNameTag() // 이름 태그 생성
        {
            Canvas overlayCanvas = FindObjectOfType<Canvas>();
            if (overlayCanvas == null || overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                overlayCanvas = CreateOverlayCanvas();
            }
            
            if (nameTagPrefab != null)
            {
                nameTag = Instantiate(nameTagPrefab, overlayCanvas.transform);
                nicknamePanel = nameTag.GetComponent<ReceiveGameNicknamePanel>();
            }
            else
            {
                CreateDynamicNicknamePanel(overlayCanvas);
            }
            
            if (nicknamePanel != null)
            {
                string playerName = photonView.Owner?.NickName ?? "Unknown Player";
                nicknamePanel.SetInfo(playerName, transform);
            }
        }
        
        private void CreateDynamicNicknamePanel(Canvas canvas) // 동적 닉네임 패널 생성
        {
            nameTag = new GameObject("NicknamePanel");
            nameTag.transform.SetParent(canvas.transform);
            
            RectTransform rectTransform = nameTag.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(120, 30);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            
            CreateBackgroundImage();
            CreateNicknameText();
            
            nicknamePanel = nameTag.AddComponent<ReceiveGameNicknamePanel>();
        }
        
        private void CreateBackgroundImage() // 배경 이미지 생성
        {
            GameObject background = new GameObject("Background");
            background.transform.SetParent(nameTag.transform);
            
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f);
        }
        
        private void CreateNicknameText() // 닉네임 텍스트 생성
        {
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
            
            nicknamePanel = nameTag.AddComponent<ReceiveGameNicknamePanel>();
            nicknamePanel.nicknameText = textComponent;
        }
        
        private Canvas CreateOverlayCanvas() // 오버레이 캔버스 생성
        {
            GameObject canvasObj = new GameObject("OverlayCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            return canvas;
        }

        #endregion

        #region Network Synchronization

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) // 네트워크 동기화
        {
            if (stream.IsWriting)
            {
                SendNetworkData(stream);
            }
            else
            {
                ReceiveNetworkData(stream, info);
            }
        }
        
        private void SendNetworkData(PhotonStream stream) // 네트워크 데이터 전송
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(isMoving);
            stream.SendNext(playerColorIndex);
            
            Vector3 velocity = isMoving ? transform.forward * moveSpeed : Vector3.zero;
            stream.SendNext(velocity);
        }
        
        private void ReceiveNetworkData(PhotonStream stream, PhotonMessageInfo info) // 네트워크 데이터 수신
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            isMoving = (bool)stream.ReceiveNext();
            int receivedColorIndex = (int)stream.ReceiveNext();
            networkVelocity = (Vector3)stream.ReceiveNext();
            
            SynchronizePlayerColor(receivedColorIndex);
            lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
        }
        
        private void SynchronizePlayerColor(int receivedColorIndex) // 플레이어 색상 동기화
        {
            if (receivedColorIndex != playerColorIndex && !isColorSet)
            {
                playerColorIndex = receivedColorIndex;
                if (playerRenderers != null && playerRenderers.Length > 0)
                {
                    Color playerColor = GetColorByIndex(playerColorIndex);
                    foreach (Renderer renderer in playerRenderers)
                    {
                        if (renderer != null && renderer.material != null)
                        {
                            renderer.material.color = playerColor;
                        }
                    }
                    isColorSet = true;
                }
            }
        }
        
        private void InterpolateOtherPlayerMovement() // 다른 플레이어 이동 보간
        {
            Vector3 predictedPosition = networkPosition + networkVelocity * lag;
            
            float interpolationSpeed = 15f;
            transform.position = Vector3.Lerp(transform.position, predictedPosition, Time.deltaTime * interpolationSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, Time.deltaTime * interpolationSpeed);
            
            if (playerAnimator != null)
            {
                bool isNetworkMoving = networkVelocity.magnitude > 0.1f;
                playerAnimator.SetBool("IsMoving", isNetworkMoving);
                playerAnimator.SetFloat("MoveSpeed", networkVelocity.magnitude / moveSpeed);
            }
        }

        #endregion

        #region Effect Management

        public void ActivateEffect(PhotonView targetPlayerPhotonView, ItemType itemType, float duration) // 이펙트 활성화
        {
            if (targetPlayerPhotonView.IsMine)
            {
                targetPlayerPhotonView.RPC(nameof(RPCActivateEffect), RpcTarget.All, targetPlayerPhotonView.ViewID, itemType, duration);
            }
        }
        
        [PunRPC]
        private void RPCActivateEffect(int targetPlayerViewID, ItemType itemType, float duration) // RPC: 이펙트 활성화
        {
            ReceiveGamePlayer targetPlayer = GetPlayerByViewID(targetPlayerViewID);
            if (targetPlayer != null)
            {
                targetPlayer.InternalActivateEffect(itemType, duration);
            }
        }
        
        public void DeactivateEffect(PhotonView targetPlayerPhotonView, ItemType itemType) // 이펙트 비활성화
        {
            if (targetPlayerPhotonView.IsMine)
            {
                targetPlayerPhotonView.RPC(nameof(RPCDeactivateEffect), RpcTarget.All, targetPlayerPhotonView.ViewID, itemType);
            }
        }
        
        [PunRPC]
        private void RPCDeactivateEffect(int targetPlayerViewID, ItemType itemType) // RPC: 이펙트 비활성화
        {
            ReceiveGamePlayer targetPlayer = GetPlayerByViewID(targetPlayerViewID);
            if (targetPlayer != null)
            {
                targetPlayer.InternalDeactivateEffect(itemType);
            }
        }
        
        public void ClearAllEffects(PhotonView targetPlayerPhotonView) // 모든 이펙트 정리
        {
            if (targetPlayerPhotonView.IsMine)
            {
                targetPlayerPhotonView.RPC(nameof(RPCClearAllEffects), RpcTarget.All, targetPlayerPhotonView.ViewID);
            }
        }
        
        [PunRPC]
        private void RPCClearAllEffects(int targetPlayerViewID) // RPC: 모든 이펙트 정리
        {
            ReceiveGamePlayer targetPlayer = GetPlayerByViewID(targetPlayerViewID);
            if (targetPlayer != null)
            {
                targetPlayer.InternalClearAllEffects();
            }
        }
        
        private ReceiveGamePlayer GetPlayerByViewID(int viewID) // ViewID로 플레이어 찾기
        {
            PhotonView targetView = PhotonView.Find(viewID);
            if (targetView == null)
            {
                Debug.LogWarning($"[ReceiveGamePlayer] PhotonView with ID {viewID} not found.");
                return null;
            }
            
            ReceiveGamePlayer targetPlayer = targetView.GetComponent<ReceiveGamePlayer>();
            if (targetPlayer == null)
            {
                Debug.LogWarning($"[ReceiveGamePlayer] ReceiveGamePlayer component not found on object with ViewID {viewID}.");
                return null;
            }
            
            return targetPlayer;
        }
        
        private void InternalActivateEffect(ItemType itemType, float duration) // 내부 이펙트 활성화
        {
            CleanupExistingEffect(itemType);
            
            if (itemConfiguration == null)
            {
                Debug.LogWarning($"[ReceiveGamePlayer] ItemConfiguration이 없어 {itemType} 이펙트를 활성화할 수 없습니다.");
                return;
            }
            
            ItemConfig config = itemConfiguration.GetItemConfig(itemType);
            if (config?.playerEffectPrefab == null)
            {
                Debug.LogWarning($"[ReceiveGamePlayer] {itemType} 타입의 이펙트 프리팹이 설정되지 않았습니다.");
                return;
            }
            
            GameObject effect = Instantiate(config.playerEffectPrefab, effectParent);
            effect.transform.localPosition = config.effectOffset;
            
            activeEffects[itemType] = effect;
            effectCoroutines[itemType] = StartCoroutine(DeactivateEffectAfterDuration(itemType, duration));
        }
        
        private void InternalDeactivateEffect(ItemType itemType) // 내부 이펙트 비활성화
        {
            CleanupExistingEffect(itemType);
        }
        
        private void InternalClearAllEffects() // 내부 모든 이펙트 정리
        {
            foreach (var kvp in activeEffects)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            activeEffects.Clear();
            
            foreach (var kvp in effectCoroutines)
            {
                if (kvp.Value != null)
                {
                    StopCoroutine(kvp.Value);
                }
            }
            effectCoroutines.Clear();
        }
        
        private void CleanupExistingEffect(ItemType itemType) // 기존 이펙트 정리
        {
            if (activeEffects.TryGetValue(itemType, out GameObject existingEffect))
            {
                if (existingEffect != null)
                {
                    Destroy(existingEffect);
                }
                activeEffects.Remove(itemType);
            }
            
            if (effectCoroutines.TryGetValue(itemType, out Coroutine existingCoroutine))
            {
                if (existingCoroutine != null)
                {
                    StopCoroutine(existingCoroutine);
                }
                effectCoroutines.Remove(itemType);
            }
        }
        
        private IEnumerator DeactivateEffectAfterDuration(ItemType itemType, float duration) // 지속시간 후 이펙트 비활성화
        {
            yield return new WaitForSeconds(duration);
            DeactivateEffect(this.photonView, itemType);
        }

        #endregion

        #region Input System

        private void InitializeInputSystem() // 입력 시스템 초기화
        {
            if (inputActions == null) return;
            
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
                
                inputActions.Enable();
            }
        }
        
        private void OnMove(InputAction.CallbackContext context) // 이동 입력 콜백
        {
            moveInput = context.ReadValue<Vector2>();
        }
        
        private void OnJump(InputAction.CallbackContext context) // 점프 입력 콜백
        {
            jumpPressed = context.performed;
        }
        
        private void OnAction(InputAction.CallbackContext context) // 액션 입력 콜백
        {
            actionPressed = context.performed;
        }

        #endregion

        #region Collision Detection

        private void OnTriggerEnter(Collider other) // 트리거 진입 감지
        {
            if (!photonView.IsMine) return;
            
            CollectibleItem item = other.GetComponent<CollectibleItem>();
            if (item != null && !item.IsCollected)
            {
                ItemType itemType = GetItemType(item);
                LogDebugInfo($"OnTriggerEnter로 아이템 감지: {item.name}, 타입: {itemType}");
                CollectItem(item);
            }
        }
        
        private void OnTriggerStay(Collider other) // 트리거 지속 감지
        {
            if (!photonView.IsMine) return;
            
            CollectibleItem item = other.GetComponent<CollectibleItem>();
            if (item != null && !item.IsCollected && isMobilePlatform && Time.time % 0.5f < Time.deltaTime)
            {
                CollectItem(item);
            }
        }

        #endregion

        #region Utility Methods

        private void CleanupOnDestroy() // 파괴 시 정리 작업
        {
            if (nameTag != null)
            {
                Destroy(nameTag);
            }
            
            if (inputActions != null)
            {
                inputActions.Disable();
            }
            
            ClearAllEffects(this.photonView);
        }
        
        private void LogDebugInfo(string message) // 디버그 정보 로그
        {
            if (isMobilePlatform)
            {
                Debug.Log($"[ReceiveGamePlayer] {message}");
            }
        }
        
        public int GetPlayerActorNumber() // 플레이어 액터 번호 반환
        {
            return photonView.Owner.ActorNumber;
        }
        
        public ItemType[] GetActiveEffects() // 활성화된 이펙트 목록 반환
        {
            return activeEffects.Keys.ToArray();
        }
        
        public void SetPlayerColor(Color color) // 플레이어 색상 설정 (외부 호출용)
        {
            if (playerRenderers != null && playerRenderers.Length > 0)
            {
                foreach (Renderer renderer in playerRenderers)
                {
                    if (renderer != null && renderer.material != null)
                    {
                        renderer.material.color = color;
                    }
                }
            }
        }
        
        public void SetPlayerColorByIndex(int colorIndex) // 색상 인덱스로 플레이어 색상 설정
        {
            if (colorIndex >= 0)
            {
                playerColorIndex = colorIndex;
                Color playerColor = GetColorByIndex(colorIndex);
                SetPlayerColor(playerColor);
                isColorSet = true;
            }
        }
        
        public int GetCurrentColorIndex() // 현재 색상 인덱스 반환
        {
            return playerColorIndex;
        }
        
        public Renderer[] GetPlayerRenderers() // 플레이어 렌더러 배열 반환
        {
            return playerRenderers;
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected() // 선택된 오브젝트 기즈모
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collectionRadius);
            
            if (hasMagnetEffect)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, magnetRadius);
            }
        }
        
        private void OnDrawGizmos() // 기즈모 그리기
        {
            if (hasMagnetEffect)
            {
                Gizmos.color = new Color(0, 0, 1, 0.3f);
                Gizmos.DrawWireSphere(transform.position, magnetRadius);
            }
            
            float currentCollectionRadius = isMobilePlatform ? mobileCollectionRadius : collectionRadius;
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, currentCollectionRadius);
        }

        #endregion
    }
} 