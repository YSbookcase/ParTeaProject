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
                rb.isKinematic = false; // 물리 충돌을 위해 Kinematic 비활성화
                rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // 빠른 이동 시 충돌 감지 개선
                Debug.Log("[ReceiveGamePlayer] Rigidbody 설정 완료 - 물리 충돌 활성화");
            }
            else
            {
                Debug.LogError("[ReceiveGamePlayer] Rigidbody가 없습니다! 충돌 처리가 제대로 작동하지 않을 수 있습니다.");
            }
            
            // Collider 설정 확인
            Collider playerCollider = GetComponent<Collider>();
            if (playerCollider != null)
            {
                Debug.Log($"[ReceiveGamePlayer] Collider 확인됨: {playerCollider.GetType().Name}");
            }
            else
            {
                Debug.LogError("[ReceiveGamePlayer] Collider가 없습니다! 충돌 처리가 제대로 작동하지 않을 수 있습니다.");
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
            
            // AudioSource 제거 - AudioManager 시스템 사용
        }
        
        private void Update()
        {
            if (photonView.IsMine)
            {
                // 로컬 플레이어만 입력 처리
                HandleInput();
                CheckItemCollection();
                
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
                    Debug.LogWarning("playerAnimator가 null입니다. Inspector에서 Animator를 할당해주세요.");
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
        
        private void AttractItem(CollectibleItem item)
        {
            if (item == null || item.IsCollected) return;
            
            // 아이템과 플레이어 사이의 방향 계산
            Vector3 directionToPlayer = (transform.position - item.transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, item.transform.position);
            
            // 거리가 가까울수록 더 강한 힘 적용 (역제곱 법칙)
            float attractionForce = magnetForce / (distanceToPlayer * distanceToPlayer);
            
            // 아이템의 Rigidbody에 힘 적용
            Rigidbody itemRb = item.GetComponent<Rigidbody>();
            if (itemRb != null && !itemRb.isKinematic)
            {
                // Y축 속도는 제한하여 너무 빠르게 떨어지지 않도록 함
                Vector3 currentVelocity = itemRb.velocity;
                Vector3 attractionVelocity = directionToPlayer * attractionForce;
                attractionVelocity.y = Mathf.Max(currentVelocity.y, -2f); // 최대 낙하 속도 제한
                
                itemRb.velocity = attractionVelocity;
                
                // 디버그 로그 (너무 자주 출력되지 않도록 제한)
                if (Time.frameCount % 60 == 0) // 1초에 한 번씩만 출력
                {
                    Debug.Log($"[ReceiveGamePlayer] 자석 효과로 아이템 끌어당김: {item.name}, 거리: {distanceToPlayer:F2}, 힘: {attractionForce:F2}, 기본 마그네틱 힘: {magnetForce}");
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
                    Debug.Log($"[ApplyMagnetEffect] 커스텀 마그네틱 범위 적용: {customMagnetRadius}");
                }
                if (customMagnetForce > 0f)
                {
                    magnetForce = customMagnetForce;
                    Debug.Log($"[ApplyMagnetEffect] 커스텀 마그네틱 힘 적용: {customMagnetForce}");
                }
                else
                {
                    Debug.Log($"[ApplyMagnetEffect] 기본 마그네틱 힘 사용: {magnetForce}");
                }
                
                Debug.Log($"[ApplyMagnetEffect] 자석 효과 적용! 지속시간: {duration}초, 범위: {magnetRadius}, 힘: {magnetForce}");
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