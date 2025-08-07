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
        [SerializeField] private float moveSpeed = 5f; // 플레이어 이동 속도
        [SerializeField] private float collectionRadius = 3f; // 기본 수집 반경
        [SerializeField] private float baseMoveSpeed = 5f;
        [SerializeField] private float speedBoostMultiplier = 1.5f;
        [SerializeField] private float slowEffectMultiplier = 0.5f;
        [SerializeField] private LayerMask itemLayerMask = -1; // 아이템 레이어 마스크
        
        [Header("Mobile Optimization")]
        [SerializeField] private float collectionCheckInterval = 0.01f; // 충돌 체크 간격을 더 짧게 (100fps)
        [SerializeField] private float mobileCollectionRadius = 7f; // 모바일 전용 충돌 범위를 더 크게 (5f → 7f)
        
        [Header("Magnetic Effect Settings")]
        [SerializeField] private float magnetRadius = 5f; // 자석 효과 범위
        [SerializeField] private float magnetForce = 10f; // 자석 효과 힘
        
        [Header("Components")]
        [SerializeField] private Renderer playerRenderer;
        [SerializeField] private Animator playerAnimator;
        
        [Header("UI Elements")]
        [SerializeField] private GameObject nameTagPrefab;
        [SerializeField] private Vector3 nameTagOffset = new Vector3(0, 2.5f, 0);
        
        [Header("Effects")]
        [SerializeField] private GameObject collectEffect;
        [SerializeField] private string collectSoundName = "SFX_NormalItem"; // AudioData 에셋 이름으로 변경
        
        [Header("Player Effect Management")]
        [SerializeField] private Transform effectParent; // 이펙트들이 자식으로 들어갈 부모 Transform
        [SerializeField] private ItemConfiguration itemConfiguration; // 이펙트 프리팹을 가져오기 위한 설정
        
        [Header("Input System")]
        [SerializeField] private UnityEngine.InputSystem.InputActionAsset inputActions;
        
        [Header("Mobile UI")]
        // MobileUIManager 백업 시스템 제거 - ReceiveGameUI 조이스틱만 사용
        
        private Vector3 targetPosition;
        private bool isMoving = false;
        private ReceiveGameManagerEnhanced gameManager;
        private ReceiveGameUI gameUI;
        private GameObject nameTag;
        private ReceiveGameNicknamePanel nicknamePanel;
        private float lastCollectionCheckTime = 0f; // 충돌 체크 시간 추적
        
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
        private Vector3 networkVelocity; // 속도 정보 추가
        private float lag;
        // private float interpolationBackTime = 0.1f; // 보간 시간 - 사용되지 않음
        private int playerColorIndex = -1; // 플레이어 색상 인덱스
        private bool isColorSet = false; // 색상이 설정되었는지 확인
        
        // 이펙트 관리 변수들
        private Dictionary<ItemType, GameObject> activeEffects = new Dictionary<ItemType, GameObject>();
        private Dictionary<ItemType, Coroutine> effectCoroutines = new Dictionary<ItemType, Coroutine>();
        
        // 컴포넌트 참조
        private Rigidbody rb;
        private Collider playerCollider;
        
        private void Start()
        {
            // 플랫폼 감지
            DetectPlatform();
            
            // 컴포넌트 초기화
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }
            
            if (rb != null)
            {
                // Rigidbody 설정
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            else
            {
                //Debug.LogError("[ReceiveGamePlayer] Rigidbody가 없습니다! 충돌 처리가 제대로 작동하지 않을 수 있습니다.");
            }
            
            // Collider 확인
            playerCollider = GetComponent<Collider>();
            if (playerCollider == null)
            {
                //Debug.LogError("[ReceiveGamePlayer] Collider가 없습니다! 충돌 처리가 제대로 작동하지 않을 수 있습니다.");
            }
            
            // ReceiveGameUI 찾기 및 할당
            InitializeGameUI();
            
            // 로컬 플레이어 초기화
            if (photonView.IsMine)
            {
                InitializeLocalPlayer();
            }
            
            // 이펙트 부모 초기화
            InitializeEffectParent();
        }
        
        private void Update()
        {
            if (photonView.IsMine)
            {
                // 로컬 플레이어만 입력 처리
                HandleInput();
                
                // 아이템 수집 체크 (최적화된 간격으로 실행)
                if (Time.time - lastCollectionCheckTime >= collectionCheckInterval)
                {
                    CheckItemCollection();
                    lastCollectionCheckTime = Time.time;
                }
                
                // 자석 효과가 활성화된 경우 주변 아이템을 끌어당기기
                if (hasMagnetEffect)
                {
                    CheckMagneticAttraction();
                }
            }
            else
            {
                // 다른 플레이어의 위치 보간 (고스트 무빙 방지)
                InterpolateOtherPlayerMovement();
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
                
                // Rigidbody를 사용한 이동 (물리 충돌 보장)
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    Vector3 velocity = moveDirection.normalized * moveSpeed;
                    velocity.y = 0; // Y축 속도 제한
                    rb.velocity = velocity;
                }
                else
                {
                    // Rigidbody가 없거나 Kinematic인 경우 Transform 사용 (백업)
                    Vector3 newPosition = transform.position + moveDirection.normalized * moveSpeed * Time.deltaTime;
                    newPosition.y = transform.position.y; // Y축 위치 고정
                    transform.position = newPosition;
                }
                
                // 애니메이션 설정
                if (playerAnimator != null)
                {
                    playerAnimator.SetBool("IsMoving", true);
                    
                    // 속도에 따른 애니메이션 블렌드
                    float normalizedSpeed = Mathf.Clamp01(moveDirection.magnitude);
                    
                    // 속도 부스트 시에도 MoveSpeed로만 처리 (Sprint 모션이 없으므로)
                    playerAnimator.SetFloat("MoveSpeed", normalizedSpeed);
                }
                else
                {
                    //Debug.LogWarning("playerAnimator가 null입니다. Inspector에서 Animator를 할당해주세요.");
                }
            }
            else
            {
                isMoving = false;
                
                // 정지 시 Rigidbody 속도 초기화
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    rb.velocity = Vector3.zero;
                }
                
                if (playerAnimator != null)
                {
                    playerAnimator.SetBool("IsMoving", false);
                    playerAnimator.SetFloat("MoveSpeed", 0f);
                }
            }
        }
        
        private void CheckItemCollection()
        {
            // 모바일과 데스크톱에 따른 충돌 범위 조정
            float currentCollectionRadius = isMobilePlatform ? mobileCollectionRadius : collectionRadius;
            
            // 주변 아이템 검사 (레이어 마스크 추가로 성능 최적화)
            int itemLayerMask = 1 << LayerMask.NameToLayer("Item"); // Item 레이어만 검사
            if (itemLayerMask == 0) itemLayerMask = -1; // Item 레이어가 없으면 모든 레이어 검사
            
            Collider[] colliders = Physics.OverlapSphere(transform.position, currentCollectionRadius, itemLayerMask);
            
            foreach (Collider collider in colliders)
            {
                CollectibleItem item = collider.GetComponent<CollectibleItem>();
                if (item != null && !item.IsCollected)
                {
                    // 아이템 타입 확인
                    ItemType itemType = GetItemType(item);
                    
                    // Slow 아이템은 자동 수집하지 않음 (플레이어가 피해야 하는 아이템)
                    if (itemType == ItemType.Slow)
                    {
                        if (isMobilePlatform)
                        {
                            Debug.Log($"[ReceiveGamePlayer] Slow 아이템 감지 - 수집하지 않음: {item.name}");
                        }
                        continue; // Slow 아이템은 건너뛰기
                    }
                    
                    // 모바일 디버깅을 위한 상세 정보
                    if (isMobilePlatform)
                    {
                        float distance = Vector3.Distance(transform.position, item.transform.position);
                        Debug.Log($"[ReceiveGamePlayer] 아이템 발견: {item.name}, 타입: {itemType}, 거리: {distance:F2}, 수집 가능: {!item.IsCollected}, 위치: {item.transform.position}");
                    }
                    
                    CollectItem(item);
                }
                else if (item != null && item.IsCollected && isMobilePlatform)
                {
                    // 이미 수집된 아이템인 경우 디버그 정보
                    Debug.Log($"[ReceiveGamePlayer] 이미 수집된 아이템: {item.name}");
                }
                else if (item == null && isMobilePlatform)
                {
                    // CollectibleItem이 없는 오브젝트인 경우
                    Debug.Log($"[ReceiveGamePlayer] CollectibleItem이 없는 오브젝트: {collider.name}, 태그: {collider.tag}");
                }
            }
            
            // 추가적인 충돌 감지: Raycast를 사용한 정밀 검사
            if (isMobilePlatform && colliders.Length == 0)
            {
                CheckItemCollectionWithRaycast(currentCollectionRadius);
            }
        }
        
        // Raycast를 사용한 추가 충돌 감지
        private void CheckItemCollectionWithRaycast(float radius)
        {
            // 8방향으로 Raycast 수행
            Vector3[] directions = {
                Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
                new Vector3(1, 0, 1).normalized, new Vector3(-1, 0, 1).normalized,
                new Vector3(1, 0, -1).normalized, new Vector3(-1, 0, -1).normalized
            };
            
            foreach (Vector3 direction in directions)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, direction, out hit, radius))
                {
                    CollectibleItem item = hit.collider.GetComponent<CollectibleItem>();
                    if (item != null && !item.IsCollected)
                    {
                        // 아이템 타입 확인
                        ItemType itemType = GetItemType(item);
                        
                        // Slow 아이템은 자동 수집하지 않음
                        if (itemType == ItemType.Slow)
                        {
                            if (isMobilePlatform)
                            {
                                Debug.Log($"[ReceiveGamePlayer] Raycast로 Slow 아이템 감지 - 수집하지 않음: {item.name}");
                            }
                            continue; // Slow 아이템은 건너뛰기
                        }
                        
                        Debug.Log($"[ReceiveGamePlayer] Raycast로 아이템 발견: {item.name}, 타입: {itemType}, 거리: {hit.distance:F2}, 방향: {direction}");
                        CollectItem(item);
                    }
                }
            }
        }
        
        private void CheckMagneticAttraction()
        {
            // 자석 효과 범위 내의 아이템들을 찾기
            Collider[] colliders = Physics.OverlapSphere(transform.position, magnetRadius);
            
            foreach (Collider collider in colliders)
            {
                CollectibleItem item = collider.GetComponent<CollectibleItem>();
                if (item != null && !item.IsCollected)
                {
                    // 아이템을 플레이어 방향으로 끌어당기기
                    AttractItem(item);
                }
            }
        }
        
        private void InitializeLocalPlayer()
        {
            // 플레이어 색상 설정
            SetPlayerColor();
            
            // 이름 태그 생성
            CreateNameTag();
            
            // Input System 초기화
            InitializeInputSystem();
        }
        
        private void AttractItem(CollectibleItem item)
        {
            if (item == null || item.IsCollected) return;
            
            Vector3 directionToPlayer = (transform.position - item.transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, item.transform.position);
            
            // 거리가 너무 멀면 자석 효과 적용하지 않음
            if (distanceToPlayer > magnetRadius) return;
            
            // 자석 효과 힘 계산 (거리에 반비례)
            float attractionForce = magnetForce * (1f - (distanceToPlayer / magnetRadius));
            
            // 아이템을 플레이어 쪽으로 끌어당김
            Rigidbody itemRb = item.GetComponent<Rigidbody>();
            if (itemRb != null)
            {
                itemRb.AddForce(directionToPlayer * attractionForce, ForceMode.Force);
            }
        }
        
        private void CollectItem(CollectibleItem item)
        {
            if (item == null) 
            {
                return;
            }
            
            // 이미 수집된 아이템은 무시
            if (item.IsCollected) return;
            
            // 아이템 타입 확인 (수집 전에)
            ItemType itemType = GetItemType(item);
            
            // 디버그 로그 추가
            Debug.Log($"[ReceiveGamePlayer] 아이템 수집 시도: {item.name}, 타입: {itemType}, 플레이어: {PhotonNetwork.LocalPlayer.ActorNumber}");
            
            // 아이템 수집 (네트워크 동기화 포함)
            item.Collect();
            
            // 게임 매니저 참조 확인 및 재설정
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<ReceiveGameManagerEnhanced>();
                Debug.Log($"[ReceiveGamePlayer] 게임 매니저 재찾기: {gameManager != null}");
            }
            
            // 게임 매니저에 수집 알림 (로컬 플레이어만)
            if (gameManager != null && photonView.IsMine)
            {
                Debug.Log($"[ReceiveGamePlayer] 게임 매니저에 점수 요청: 플레이어 {PhotonNetwork.LocalPlayer.ActorNumber}, 아이템 타입: {itemType}");
                gameManager.CollectItem(PhotonNetwork.LocalPlayer.ActorNumber, itemType);
            }
            else
            {
                Debug.LogWarning($"[ReceiveGamePlayer] 게임 매니저가 null이거나 로컬 플레이어가 아님. gameManager: {gameManager != null}, IsMine: {photonView.IsMine}");
            }
        }
        
        private ItemType GetItemType(CollectibleItem item)
        {
            // EnhancedItemController에서 아이템 타입 가져오기
            EnhancedItemController enhancedController = item.GetComponent<EnhancedItemController>();
            if (enhancedController != null)
            {
                ItemType itemType = enhancedController.GetItemType();
                Debug.Log($"[ReceiveGamePlayer] EnhancedItemController에서 아이템 타입 가져옴: {itemType}");
                return itemType;
            }
            
            // CollectibleItem에서 직접 아이템 타입 가져오기 (fallback)
            if (item.itemType != ItemType.Normal)
            {
                Debug.Log($"[ReceiveGamePlayer] CollectibleItem에서 직접 아이템 타입 가져옴: {item.itemType}");
                return item.itemType;
            }
            
            Debug.LogWarning($"[ReceiveGamePlayer] 아이템 타입을 찾을 수 없어 기본값 사용: {item.name}");
            return ItemType.Normal;
        }
        
        private void PlayCollectEffect()
        {
            // 수집 효과 파티클
            if (collectEffect != null)
            {
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }
            
            // 수집 사운드 - AudioManager 시스템 사용
            if (!string.IsNullOrEmpty(collectSoundName) && Manager.Audio != null)
            {
                Manager.Audio.SfxPlay(collectSoundName, transform);
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
                
                //Debug.Log($"플레이어 {photonView.Owner?.NickName ?? "Unknown"} 색상 설정: {colorIndex}");
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
                //Debug.Log("Screen Space - Overlay Canvas를 자동으로 생성합니다.");
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
                    //Debug.Log($"이름 태그 생성: {photonView.Owner.NickName}");
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
            
            //Debug.Log("Screen Space - Overlay Canvas 생성 완료");
            return canvas;
        }
        
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // 데이터 전송 (속도 정보 추가)
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(isMoving);
                stream.SendNext(playerColorIndex); // 색상 인덱스 전송
                
                // 속도 정보 전송 (고스트 무빙 방지)
                if (isMoving)
                {
                    Vector3 velocity = transform.forward * moveSpeed;
                    stream.SendNext(velocity);
                }
                else
                {
                    stream.SendNext(Vector3.zero);
                }
            }
            else
            {
                // 데이터 수신
                networkPosition = (Vector3)stream.ReceiveNext();
                networkRotation = (Quaternion)stream.ReceiveNext();
                isMoving = (bool)stream.ReceiveNext();
                int receivedColorIndex = (int)stream.ReceiveNext();
                networkVelocity = (Vector3)stream.ReceiveNext(); // 속도 정보 수신
                
                // 색상 동기화
                if (receivedColorIndex != playerColorIndex && !isColorSet)
                {
                    playerColorIndex = receivedColorIndex;
                    if (playerRenderer != null)
                    {
                        Color playerColor = GetColorByIndex(playerColorIndex);
                        playerRenderer.material.color = playerColor;
                        isColorSet = true;
                        //Debug.Log($"네트워크에서 받은 색상 적용: {playerColorIndex}");
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
            
            // 자석 효과 범위 표시 (자석 효과가 활성화된 경우에만)
            if (hasMagnetEffect)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, magnetRadius);
            }
        }
        
        private void OnDrawGizmos()
        {
            // 자석 효과가 활성화된 경우 런타임에서도 범위 표시
            if (hasMagnetEffect)
            {
                Gizmos.color = new Color(0, 0, 1, 0.3f); // 반투명 파란색
                Gizmos.DrawWireSphere(transform.position, magnetRadius);
            }
            
            // 아이템 수집 범위 시각화 (모바일 디버깅용)
            float currentCollectionRadius = isMobilePlatform ? mobileCollectionRadius : collectionRadius;
            Gizmos.color = new Color(1, 1, 0, 0.3f); // 반투명 노란색
            Gizmos.DrawWireSphere(transform.position, currentCollectionRadius);
        }
        
        // 고스트 무빙 방지를 위한 보간 메서드
        private void InterpolateOtherPlayerMovement()
        {
            // 네트워크 지연을 고려한 예측 위치 계산
            Vector3 predictedPosition = networkPosition + networkVelocity * lag;
            
            // 부드러운 보간 (고스트 무빙 방지)
            float interpolationSpeed = 15f; // 보간 속도 증가
            transform.position = Vector3.Lerp(transform.position, predictedPosition, Time.deltaTime * interpolationSpeed);
            
            // 회전도 부드럽게 보간
            transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, Time.deltaTime * interpolationSpeed);
            
            // 애니메이션 동기화
            if (playerAnimator != null)
            {
                bool isNetworkMoving = networkVelocity.magnitude > 0.1f;
                playerAnimator.SetBool("IsMoving", isNetworkMoving);
                playerAnimator.SetFloat("MoveSpeed", networkVelocity.magnitude / moveSpeed);
            }
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
            
            // 활성화된 이펙트들 정리
            ClearAllEffects(this.photonView);
        }
        
        // 파워업 효과 관련 메서드들
        private void CheckPowerUpEffects()
        {
            // 속도 부스트 효과 체크
            if (hasSpeedBoost && Time.time >= speedBoostEndTime)
            {
                hasSpeedBoost = false;
                moveSpeed = baseMoveSpeed;
                DeactivateEffect(this.photonView, ItemType.Speed);
                //Debug.Log("속도 부스트 효과 종료");
            }
            
            // 슬로우 효과 체크
            if (hasSlowEffect && Time.time >= slowEffectEndTime)
            {
                hasSlowEffect = false;
                moveSpeed = baseMoveSpeed;
                DeactivateEffect(this.photonView, ItemType.Slow);
                //Debug.Log("슬로우 효과 종료");
            }
            
            // 자석 효과 체크
            if (hasMagnetEffect && Time.time >= magnetEffectEndTime)
            {
                hasMagnetEffect = false;
                DeactivateEffect(this.photonView, ItemType.Magnet);
                //Debug.Log("자석 효과 종료");
            }
        }
        
        public void ApplySpeedBoost(float duration)
        {
            if (photonView.IsMine)
            {
                hasSpeedBoost = true;
                speedBoostEndTime = Time.time + duration;
                moveSpeed = baseMoveSpeed * speedBoostMultiplier;
                
                // 스피드 부스트 이펙트 활성화
                ActivateEffect(photonView, ItemType.Speed, duration);
                
                //Debug.Log($"속도 부스트 적용! 지속시간: {duration}초");
            }
        }
        
        public void ApplySlowEffect(float duration)
        {
            if (photonView.IsMine)
            {
                hasSlowEffect = true;
                slowEffectEndTime = Time.time + duration;
                moveSpeed = baseMoveSpeed * slowEffectMultiplier;
                
                // 슬로우 이펙트 활성화
                ActivateEffect(photonView, ItemType.Slow, duration);
                
                //Debug.Log($"슬로우 효과 적용! 지속시간: {duration}초");
            }
        }
        
        public void ApplyMagnetEffect(float duration, float customMagnetRadius = -1f, float customMagnetForce = -1f)
        {
            if (photonView.IsMine)
            {
                hasMagnetEffect = true;
                magnetEffectEndTime = Time.time + duration;
                
                // 커스텀 값이 제공된 경우 사용,否则 기본값 사용
                if (customMagnetRadius > 0f)
                {
                    magnetRadius = customMagnetRadius;
                    //Debug.Log($"[ApplyMagnetEffect] 커스텀 마그네틱 범위 적용: {customMagnetRadius}");
                }
                if (customMagnetForce > 0f)
                {
                    magnetForce = customMagnetForce;
                    //Debug.Log($"[ApplyMagnetEffect] 커스텀 마그네틱 힘 적용: {customMagnetForce}");
                }
                else
                {
                    //Debug.Log($"[ApplyMagnetEffect] 기본 마그네틱 힘 사용: {magnetForce}");
                }
                
                // 마그네틱 이펙트 활성화
                ActivateEffect(photonView, ItemType.Magnet, duration);
                
                //Debug.Log($"[ApplyMagnetEffect] 자석 효과 적용! 지속시간: {duration}초, 범위: {magnetRadius}, 힘: {magnetForce}");
            }
        }
        
        public int GetPlayerActorNumber()
        {
            return photonView.Owner.ActorNumber;
        }
        
        // 충돌 감지 디버그 (테스트용)
        private void OnCollisionEnter(Collision collision)
        {
            // 충돌 처리 로직이 필요한 경우 여기에 추가
        }
        
        private void OnCollisionStay(Collision collision)
        {
            // 지속적인 충돌 처리 로직이 필요한 경우 여기에 추가
        }
        
        // Trigger 기반 충돌 감지 추가
        private void OnTriggerEnter(Collider other)
        {
            if (!photonView.IsMine) return; // 로컬 플레이어만 처리
            
            // 아이템과의 충돌 감지
            CollectibleItem item = other.GetComponent<CollectibleItem>();
            if (item != null && !item.IsCollected)
            {
                // 아이템 타입 확인 (디버깅용)
                ItemType itemType = GetItemType(item);
                
                // 모바일 디버깅을 위한 상세 정보
                if (isMobilePlatform)
                {
                    float distance = Vector3.Distance(transform.position, item.transform.position);
                    Debug.Log($"[ReceiveGamePlayer] OnTriggerEnter로 아이템 감지: {item.name}, 타입: {itemType}, 거리: {distance:F2}, 위치: {item.transform.position}");
                }
                
                CollectItem(item);
            }
            else if (isMobilePlatform)
            {
                // 디버깅을 위한 추가 정보
                if (item == null)
                {
                    Debug.Log($"[ReceiveGamePlayer] OnTriggerEnter - CollectibleItem이 없는 오브젝트: {other.name}, 태그: {other.tag}");
                }
                else if (item.IsCollected)
                {
                    Debug.Log($"[ReceiveGamePlayer] OnTriggerEnter - 이미 수집된 아이템: {item.name}");
                }
            }
        }
        
        // Trigger 기반 충돌 감지 (지속적)
        private void OnTriggerStay(Collider other)
        {
            if (!photonView.IsMine) return; // 로컬 플레이어만 처리
            
            // 아이템과의 충돌 감지 (지속적)
            CollectibleItem item = other.GetComponent<CollectibleItem>();
            if (item != null && !item.IsCollected)
            {
                // 아이템 타입 확인 (디버깅용)
                ItemType itemType = GetItemType(item);
                
                // 모바일에서 주기적으로 체크 (0.5초마다)
                if (isMobilePlatform && Time.time % 0.5f < Time.deltaTime)
                {
                    float distance = Vector3.Distance(transform.position, item.transform.position);
                    Debug.Log($"[ReceiveGamePlayer] OnTriggerStay로 아이템 감지: {item.name}, 타입: {itemType}, 거리: {distance:F2}");
                    CollectItem(item);
                }
            }
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
            
            //Debug.Log($"플랫폼 감지: {(isMobilePlatform ? "모바일" : "데스크톱")} - 마우스 입력 지원");
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
                //Debug.LogWarning("ReceiveGameUI가 null입니다. 조이스틱 입력을 처리할 수 없습니다.");
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
                    //Debug.Log("점프!");
                }
                else
                {
                    jumpPressed = false;
                }
                
                // 액션 입력
                if (Input.GetKeyDown(KeyCode.E))
                {
                    actionPressed = true;
                    //Debug.Log("액션!");
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
                //Debug.LogWarning("Input Actions가 할당되지 않았습니다. 기본 입력을 사용합니다.");
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
                //Debug.Log("Input System 초기화 완료");
            }
            else
            {
                //Debug.LogError("Player Action Map을 찾을 수 없습니다.");
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
                //Debug.Log("점프!");
            }
        }
        
        private void OnAction(InputAction.CallbackContext context)
        {
            actionPressed = context.performed;
            if (actionPressed)
            {
                // 액션 로직 (필요시 구현)
                //Debug.Log("액션!");
            }
        }
        
        // ===== 이펙트 관리 메서드들 =====
        
        /// <summary>
        /// 이펙트 부모 Transform을 초기화합니다.
        /// </summary>
        private void InitializeEffectParent()
        {
            if (effectParent == null)
            {
                // 이펙트 부모가 설정되지 않은 경우 자동 생성
                GameObject effectParentObj = new GameObject("PlayerEffects");
                effectParent = effectParentObj.transform;
                effectParent.SetParent(transform);
                effectParent.localPosition = Vector3.zero;
                effectParent.localRotation = Quaternion.identity;
                //Debug.Log("[ReceiveGamePlayer] 이펙트 부모 자동 생성 완료");
            }
            
            // ItemConfiguration 로드
            if (itemConfiguration == null)
            {
                itemConfiguration = Resources.Load<ItemConfiguration>("ItemConfiguration");
                if (itemConfiguration == null)
                {
                    //Debug.LogWarning("[ReceiveGamePlayer] ItemConfiguration을 Resources에서 로드할 수 없습니다.");
                }
            }
        }
        
        /// <summary>
        /// 특정 아이템 타입의 이펙트를 활성화합니다.
        /// </summary>
        /// <param name="targetPlayerPhotonView">이펙트를 적용할 대상 플레이어의 PhotonView</param>
        /// <param name="itemType">아이템 타입</param>
        /// <param name="duration">지속 시간</param>
        public void ActivateEffect(PhotonView targetPlayerPhotonView, ItemType itemType, float duration)
        {
            // 대상 플레이어의 PhotonView가 로컬 소유인 경우에만 RPC 호출을 시작합니다.
            // 이렇게 하면 RPC가 한 번만 전송되고, 모든 클라이언트에서 올바른 대상에게 적용됩니다.
            if (targetPlayerPhotonView.IsMine)
            {
                // 모든 클라이언트에게 효과 활성화 RPC 전송 (대상 플레이어의 ViewID 포함)
                targetPlayerPhotonView.RPC(nameof(RPCActivateEffect), RpcTarget.All, targetPlayerPhotonView.ViewID, itemType, duration);
            }
        }
        
        /// <summary>
        /// RPC: 모든 클라이언트에서 특정 플레이어의 이펙트를 활성화합니다.
        /// </summary>
        /// <param name="targetPlayerViewID">이펙트를 적용할 대상 플레이어의 ViewID</param>
        /// <param name="itemType">아이템 타입</param>
        /// <param name="duration">지속 시간</param>
        [PunRPC]
        private void RPCActivateEffect(int targetPlayerViewID, ItemType itemType, float duration)
        {
            PhotonView targetView = PhotonView.Find(targetPlayerViewID);
            if (targetView == null)
            {
                Debug.LogWarning($"[ReceiveGamePlayer] RPCActivateEffect: Target PhotonView with ID {targetPlayerViewID} not found.");
                return;
            }
            ReceiveGamePlayer targetPlayer = targetView.GetComponent<ReceiveGamePlayer>();
            if (targetPlayer == null)
            {
                Debug.LogWarning($"[ReceiveGamePlayer] RPCActivateEffect: ReceiveGamePlayer component not found on target object with ViewID {targetPlayerViewID}.");
                return;
            }

            // 이제 올바른 'targetPlayer' 인스턴스에 이펙트를 적용합니다.
            targetPlayer.InternalActivateEffect(itemType, duration);
            Debug.Log($"[ReceiveGamePlayer] {itemType} 이펙트 활성화 완료 - 지속시간: {duration}초 (플레이어: {targetView.Owner.NickName})");
        }
        
        /// <summary>
        /// 특정 아이템 타입의 이펙트를 비활성화합니다.
        /// </summary>
        /// <param name="targetPlayerPhotonView">이펙트를 비활성화할 대상 플레이어의 PhotonView</param>
        /// <param name="itemType">아이템 타입</param>
        public void DeactivateEffect(PhotonView targetPlayerPhotonView, ItemType itemType)
        {
            if (targetPlayerPhotonView.IsMine)
            {
                // 모든 클라이언트에게 효과 비활성화 RPC 전송 (대상 플레이어의 ViewID 포함)
                targetPlayerPhotonView.RPC(nameof(RPCDeactivateEffect), RpcTarget.All, targetPlayerPhotonView.ViewID, itemType);
            }
        }
        
        /// <summary>
        /// RPC: 모든 클라이언트에서 특정 플레이어의 이펙트를 비활성화합니다.
        /// </summary>
        /// <param name="targetPlayerViewID">이펙트를 비활성화할 대상 플레이어의 ViewID</param>
        /// <param name="itemType">아이템 타입</param>
        [PunRPC]
        private void RPCDeactivateEffect(int targetPlayerViewID, ItemType itemType)
        {
            PhotonView targetView = PhotonView.Find(targetPlayerViewID);
            if (targetView == null)
            {
                Debug.LogWarning($"[ReceiveGamePlayer] RPCDeactivateEffect: Target PhotonView with ID {targetPlayerViewID} not found.");
                return;
            }
            ReceiveGamePlayer targetPlayer = targetView.GetComponent<ReceiveGamePlayer>();
            if (targetPlayer == null)
            {
                Debug.LogWarning($"[ReceiveGamePlayer] RPCDeactivateEffect: ReceiveGamePlayer component not found on target object with ViewID {targetPlayerViewID}.");
                return;
            }

            targetPlayer.InternalDeactivateEffect(itemType);
            Debug.Log($"[ReceiveGamePlayer] {itemType} 이펙트 비활성화 완료 (플레이어: {targetView.Owner.NickName})");
        }
        
        /// <summary>
        /// 모든 이펙트를 비활성화합니다.
        /// </summary>
        /// <param name="targetPlayerPhotonView">모든 이펙트를 정리할 대상 플레이어의 PhotonView</param>
        public void ClearAllEffects(PhotonView targetPlayerPhotonView)
        {
            if (targetPlayerPhotonView.IsMine)
            {
                // 모든 클라이언트에게 모든 효과 정리 RPC 전송 (대상 플레이어의 ViewID 포함)
                targetPlayerPhotonView.RPC(nameof(RPCClearAllEffects), RpcTarget.All, targetPlayerPhotonView.ViewID);
            }
        }
        
        /// <summary>
        /// RPC: 모든 클라이언트에서 특정 플레이어의 모든 이펙트를 정리합니다.
        /// </summary>
        /// <param name="targetPlayerViewID">모든 이펙트를 정리할 대상 플레이어의 ViewID</param>
        [PunRPC]
        private void RPCClearAllEffects(int targetPlayerViewID)
        {
            PhotonView targetView = PhotonView.Find(targetPlayerViewID);
            if (targetView == null)
            {
                Debug.LogWarning($"[ReceiveGamePlayer] RPCClearAllEffects: Target PhotonView with ID {targetPlayerViewID} not found.");
                return;
            }
            ReceiveGamePlayer targetPlayer = targetView.GetComponent<ReceiveGamePlayer>();
            if (targetPlayer == null)
            {
                Debug.LogWarning($"[ReceiveGamePlayer] RPCClearAllEffects: ReceiveGamePlayer component not found on target object with ViewID {targetPlayerViewID}.");
                return;
            }

            targetPlayer.InternalClearAllEffects();
            Debug.Log($"[ReceiveGamePlayer] 모든 이펙트 정리 완료 (플레이어: {targetView.Owner.NickName})");
        }
        
        /// <summary>
        /// 실제 이펙트 활성화 로직을 수행합니다. (RPC에서 호출됨)
        /// </summary>
        private void InternalActivateEffect(ItemType itemType, float duration)
        {
            // 이미 활성화된 이펙트가 있다면 제거
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

            if (itemConfiguration == null)
            {
                Debug.LogWarning($"[ReceiveGamePlayer] ItemConfiguration이 없어 {itemType} 이펙트를 활성화할 수 없습니다.");
                return;
            }

            ItemConfig config = itemConfiguration.GetItemConfig(itemType);
            if (config == null || config.playerEffectPrefab == null)
            {
                Debug.LogWarning($"[ReceiveGamePlayer] {itemType} 타입의 이펙트 프리팹이 설정되지 않았습니다.");
                return;
            }

            // 이펙트 생성 (이 ReceiveGamePlayer 인스턴스의 effectParent에 생성)
            GameObject effect = Instantiate(config.playerEffectPrefab, effectParent);
            effect.transform.localPosition = config.effectOffset;

            activeEffects[itemType] = effect;

            // 지속 시간 후 자동 제거하는 코루틴 시작
            Coroutine effectCoroutine = StartCoroutine(DeactivateEffectAfterDuration(itemType, duration));
            effectCoroutines[itemType] = effectCoroutine;
        }

        /// <summary>
        /// 실제 이펙트 비활성화 로직을 수행합니다. (RPC에서 호출됨)
        /// </summary>
        private void InternalDeactivateEffect(ItemType itemType)
        {
            if (activeEffects.TryGetValue(itemType, out GameObject effect))
            {
                if (effect != null)
                {
                    Destroy(effect);
                }
                activeEffects.Remove(itemType);
            }

            // 코루틴이 실행 중인지 확인하고 중지
            if (effectCoroutines.TryGetValue(itemType, out Coroutine coroutine))
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
                effectCoroutines.Remove(itemType);
            }
        }

        /// <summary>
        /// 실제 모든 이펙트 정리 로직을 수행합니다. (RPC에서 호출됨)
        /// </summary>
        private void InternalClearAllEffects()
        {
            // 모든 활성화된 이펙트 제거
            foreach (var kvp in activeEffects)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            activeEffects.Clear();

            // 모든 실행 중인 코루틴 중지
            foreach (var kvp in effectCoroutines)
            {
                if (kvp.Value != null)
                {
                    StopCoroutine(kvp.Value);
                }
            }
            effectCoroutines.Clear();
        }
        
        /// <summary>
        /// 지속 시간 후 이펙트를 비활성화하는 코루틴입니다.
        /// </summary>
        /// <param name="itemType">아이템 타입</param>
        /// <param name="duration">지속 시간</param>
        private IEnumerator DeactivateEffectAfterDuration(ItemType itemType, float duration)
        {
            yield return new WaitForSeconds(duration);
            DeactivateEffect(this.photonView, itemType); // 현재 플레이어의 PhotonView를 사용
        }
        
        /// <summary>
        /// 현재 활성화된 이펙트 목록을 반환합니다.
        /// </summary>
        /// <returns>활성화된 이펙트 타입들의 배열</returns>
        public ItemType[] GetActiveEffects()
        {
            return activeEffects.Keys.ToArray();
        }
        
        /// <summary>
        /// ReceiveGameUI를 찾아서 할당합니다.
        /// </summary>
        private void InitializeGameUI()
        {
            if (gameUI == null)
            {
                // 씬에서 ReceiveGameUI 찾기
                gameUI = FindObjectOfType<ReceiveGameUI>();
                
                if (gameUI != null)
                {
                    Debug.Log("[ReceiveGamePlayer] ReceiveGameUI 찾기 성공");
                }
                else
                {
                    Debug.LogWarning("[ReceiveGamePlayer] ReceiveGameUI를 찾을 수 없습니다. 조이스틱 입력이 작동하지 않을 수 있습니다.");
                }
            }
            
            // ReceiveGameManagerEnhanced도 찾아서 할당
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<ReceiveGameManagerEnhanced>();
                
                if (gameManager != null)
                {
                    Debug.Log("[ReceiveGamePlayer] ReceiveGameManagerEnhanced 찾기 성공");
                }
                else
                {
                    Debug.LogWarning("[ReceiveGamePlayer] ReceiveGameManagerEnhanced를 찾을 수 없습니다. 아이템 수집이 작동하지 않을 수 있습니다.");
                }
            }
        }
    }
} 