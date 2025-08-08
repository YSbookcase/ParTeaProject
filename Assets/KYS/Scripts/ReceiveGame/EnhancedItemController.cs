using UnityEngine;
using Photon.Pun;
using System.Collections;

namespace KYS
{
    public class EnhancedItemController : MonoBehaviourPun
    {
        [Header("Configuration")]
        [SerializeField] private ItemConfiguration itemConfiguration; // 아이템 설정
        
        [Header("Item Settings")]
        [SerializeField] private ItemType itemType = ItemType.Normal; // 아이템 타입
        [SerializeField] private int pointValue = 1; // 점수 값
        [SerializeField] private float effectDuration = 5f; // 효과 지속시간
        
        [Header("Magnetic Effect Settings")]
        [SerializeField] private float magnetRadius = 5f; // 자석 효과 범위
        [SerializeField] private float magnetForce = 10f; // 자석 효과 힘
        
        [Header("Physics Settings")]
        [SerializeField] private float groundLevel = 0.5f; // 바닥 레벨
        [SerializeField] private float bounceForce = 2f; // 바운스 힘
        [SerializeField] private float maxFallSpeed = 12f; // 최대 낙하 속도
        [SerializeField] private float rotationSpeed = 60f; // 회전 속도
        [SerializeField] private float bobSpeed = 1.5f; // 바운스 애니메이션 속도
        [SerializeField] private float bobHeight = 0.3f; // 바운스 애니메이션 높이
        
        [Header("Visual Components")]
        [SerializeField] private Transform visualContainer; // 시각적 요소들을 담을 컨테이너
        [SerializeField] private Renderer itemRenderer; // 기본 렌더러 (색상 변경용)
        
        [Header("Legacy Visual Effects")]
        [SerializeField] private Color normalColor = Color.white; // 일반 아이템 색상
        [SerializeField] private Color bonusColor = Color.yellow; // 보너스 아이템 색상
        [SerializeField] private Color speedColor = Color.blue; // 속도 아이템 색상
        [SerializeField] private Color slowColor = Color.red; // 슬로우 아이템 색상
        [SerializeField] private Color magnetColor = Color.green; // 자석 아이템 색상
        
        // Private variables
        private Rigidbody rb; // 리지드바디 컴포넌트
        private bool isCollected = false; // 수집됨 여부
        private bool hasHitGround = false; // 바닥에 착지했는지 여부
        private bool isDropAnimationComplete = false; // DropItemToGround 코루틴 완료 여부
        private Vector3 startPosition; // 시작 위치
        private Coroutine bobCoroutine; // 바운스 애니메이션 코루틴
        private GameObject currentVisualPrefab; // 현재 시각적 프리팹

        #region Unity Lifecycle

        private void Start()
        {
            LoadItemConfiguration();
            InitializeComponents();
            SetupColliderForMobile();
            
            if (itemType == ItemType.Normal)
            {
                UpdateVisual();
            }
        }
        
        private void Update()
        {
            if (isCollected) return;
            
            UpdateRotation();
            
            if (isDropAnimationComplete && !hasHitGround)
            {
                CheckGroundCollision();
            }
        }
        
        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        #endregion

        #region Initialization

        private void LoadItemConfiguration() // 아이템 설정 로드
        {
            if (itemConfiguration == null)
            {
                itemConfiguration = Resources.Load<ItemConfiguration>("ItemConfiguration");
            }
        }
        
        private void InitializeComponents() // 컴포넌트 초기화
        {
            InitializeRenderer();
            InitializeVisualContainer();
            InitializeRigidbody();
            
            startPosition = transform.position;
        }
        
        private void InitializeRenderer() // 렌더러 초기화
        {
            if (itemRenderer == null)
            {
                itemRenderer = GetComponent<Renderer>();
            }
        }
        
        private void InitializeVisualContainer() // 시각적 컨테이너 초기화
        {
            if (visualContainer == null)
            {
                GameObject container = new GameObject("VisualContainer");
                container.transform.SetParent(transform);
                container.transform.localPosition = Vector3.zero;
                container.transform.localRotation = Quaternion.identity;
                visualContainer = container.transform;
            }
        }
        
        private void InitializeRigidbody() // 리지드바디 초기화
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            
            SetupRigidbody();
        }
        
        private void SetupRigidbody() // 리지드바디 설정
        {
            if (rb != null)
            {
                rb.mass = 1f;
                rb.drag = 0.5f;
                rb.angularDrag = 0.05f;
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
        }
        
        private void SetupColliderForMobile() // 모바일용 콜라이더 설정
        {
            if (!Application.isMobilePlatform) return;
            
            SetupSphereCollider();
            SetupBoxCollider();
            SetupCapsuleCollider();
        }
        
        private void SetupSphereCollider() // 구체 콜라이더 설정
        {
            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                sphereCollider.radius *= 1.5f;
            }
        }
        
        private void SetupBoxCollider() // 박스 콜라이더 설정
        {
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.size *= 1.5f;
            }
        }
        
        private void SetupCapsuleCollider() // 캡슐 콜라이더 설정
        {
            CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
            {
                capsuleCollider.radius *= 1.5f;
                capsuleCollider.height *= 1.5f;
            }
        }

        #endregion

        #region Animation

        private void UpdateRotation() // 회전 애니메이션 업데이트
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.angularVelocity = new Vector3(0, rotationSpeed * Mathf.Deg2Rad, 0);
            }
        }
        
        private void CheckGroundCollision() // 바닥 충돌 체크
        {
            if (hasHitGround) return;
            
            if (transform.position.y <= groundLevel)
            {
                OnHitGround();
            }
        }
        
        private void OnHitGround() // 바닥 착지 처리
        {
            hasHitGround = true;
            
            ApplyBounceEffect();
            StartBobAnimation();
            LimitFallSpeed();
        }
        
        private void ApplyBounceEffect() // 바운스 효과 적용
        {
            if (rb != null)
            {
                rb.velocity = new Vector3(rb.velocity.x, bounceForce, rb.velocity.z);
            }
        }
        
        private void LimitFallSpeed() // 낙하 속도 제한
        {
            if (rb != null && rb.velocity.y < -maxFallSpeed)
            {
                rb.velocity = new Vector3(rb.velocity.x, -maxFallSpeed, rb.velocity.z);
            }
        }
        
        private void StartBobAnimation() // 바운스 애니메이션 시작
        {
            if (bobCoroutine == null)
            {
                bobCoroutine = StartCoroutine(BobAnimation());
            }
        }
        
        private IEnumerator BobAnimation() // 바운스 애니메이션 코루틴
        {
            Vector3 originalPosition = transform.position;
            float time = 0f;
            
            while (!isCollected)
            {
                time += Time.deltaTime * bobSpeed;
                float newY = originalPosition.y + Mathf.Sin(time) * bobHeight;
                transform.position = new Vector3(originalPosition.x, newY, originalPosition.z);
                yield return null;
            }
        }

        #endregion

        #region Visual Management

        public void SetItemType(ItemType type) // 아이템 타입 설정
        {
            itemType = type;
            UpdateVisual();
        }
        
        private void UpdateVisual() // 시각적 요소 업데이트
        {
            ClearVisualPrefab();
            
            ItemConfig config = GetItemConfig();
            
            if (config != null)
            {
                ApplyItemConfig(config);
            }
            else
            {
                ApplyLegacyVisual();
            }
        }
        
        private ItemConfig GetItemConfig() // 아이템 설정 가져오기
        {
            if (itemConfiguration != null)
            {
                return itemConfiguration.GetItemConfig(itemType);
            }
            return null;
        }
        
        private void ApplyItemConfig(ItemConfig config) // 아이템 설정 적용
        {
            pointValue = config.pointValue;
            effectDuration = config.effectDuration;
            bounceForce = config.bounceForce;
            maxFallSpeed = config.maxFallSpeed;
            rotationSpeed = config.rotationSpeed;
            bobSpeed = config.bobSpeed;
            bobHeight = config.bobHeight;
            magnetRadius = config.magnetRadius;
            magnetForce = config.magnetForce;
            
            if (config.visualPrefab != null)
            {
                CreateVisualPrefab(config.visualPrefab);
            }
            else
            {
                ApplyColor(config.itemColor);
            }
        }
        
        private void CreateVisualPrefab(GameObject prefab) // 시각적 프리팹 생성
        {
            if (visualContainer == null) return;
            
            ClearVisualPrefab();
            
            currentVisualPrefab = Instantiate(prefab, visualContainer);
            currentVisualPrefab.transform.localPosition = Vector3.zero;
            currentVisualPrefab.transform.localRotation = Quaternion.identity;
            currentVisualPrefab.transform.localScale = GetItemTypeScale();
        }
        
        private Vector3 GetItemTypeScale() // 아이템 타입별 크기 가져오기
        {
            if (itemConfiguration != null)
            {
                ItemConfig config = itemConfiguration.GetItemConfig(itemType);
                if (config != null)
                {
                    return config.scale;
                }
            }
            
            return GetDefaultScale();
        }
        
        private Vector3 GetDefaultScale() // 기본 크기 가져오기
        {
            switch (itemType)
            {
                case ItemType.Normal:
                    return Vector3.one * 1.5f;
                case ItemType.Bonus:
                    return Vector3.one * 2.0f;
                case ItemType.Speed:
                    return Vector3.one * 1.5f;
                case ItemType.Slow:
                    return Vector3.one * 1.2f;
                case ItemType.Magnet:
                    return Vector3.one * 1.5f;
                default:
                    return Vector3.one * 1.5f;
            }
        }
        
        private void ClearVisualPrefab() // 시각적 프리팹 정리
        {
            if (currentVisualPrefab != null)
            {
                DestroyImmediate(currentVisualPrefab);
                currentVisualPrefab = null;
            }
            
            if (visualContainer != null)
            {
                for (int i = visualContainer.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(visualContainer.GetChild(i).gameObject);
                }
            }
        }
        
        private void ApplyColor(Color color) // 색상 적용
        {
            if (itemRenderer != null)
            {
                itemRenderer.material.color = color;
            }
        }
        
        private void ApplyLegacyVisual() // 레거시 시각적 설정 적용
        {
            Color targetColor = normalColor;
            switch (itemType)
            {
                case ItemType.Normal:
                    targetColor = normalColor;
                    pointValue = 1;
                    break;
                case ItemType.Bonus:
                    targetColor = bonusColor;
                    pointValue = 3;
                    break;
                case ItemType.Speed:
                    targetColor = speedColor;
                    pointValue = 1;
                    break;
                case ItemType.Slow:
                    targetColor = slowColor;
                    pointValue = 1;
                    break;
                case ItemType.Magnet:
                    targetColor = magnetColor;
                    pointValue = 1;
                    break;
            }
            
            ApplyColor(targetColor);
        }

        #endregion

        #region Collision Detection

        private void OnTriggerEnter(Collider other) // 트리거 진입 감지
        {
            if (isCollected && !Application.isMobilePlatform) return;
            
            if (other.CompareTag("Player"))
            {
                HandlePlayerCollision(other);
            }
        }
        
        private void HandlePlayerCollision(Collider playerCollider) // 플레이어 충돌 처리
        {
            isCollected = true;
            
            StopBobAnimation();
            
            ReceiveGamePlayer player = playerCollider.GetComponent<ReceiveGamePlayer>();
            if (player != null)
            {
                ApplyItemEffect(player);
                PlayCollectSound();
            }
            
            StartCoroutine(DestroyAfterEffect());
        }
        
        private void StopBobAnimation() // 바운스 애니메이션 중지
        {
            if (bobCoroutine != null)
            {
                StopCoroutine(bobCoroutine);
                bobCoroutine = null;
            }
        }
        
        private IEnumerator DestroyAfterEffect() // 효과 후 파괴
        {
            yield return new WaitForSeconds(0.2f);
            
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region Item Effects

        private void ApplyItemEffect(ReceiveGamePlayer player) // 아이템 효과 적용
        {
            switch (itemType)
            {
                case ItemType.Normal:
                case ItemType.Bonus:
                    break;
                    
                case ItemType.Speed:
                    player.ApplySpeedBoost(effectDuration);
                    break;
                    
                case ItemType.Slow:
                    player.ApplySlowEffect(effectDuration);
                    break;
                    
                case ItemType.Magnet:
                    player.ApplyMagnetEffect(effectDuration, magnetRadius, magnetForce);
                    break;
            }
        }
        
        private void PlayCollectSound() // 수집 사운드 재생
        {
            if (itemConfiguration != null)
            {
                ItemConfig config = itemConfiguration.GetItemConfig(itemType);
                if (config != null && !string.IsNullOrEmpty(config.collectSoundName) && Manager.Audio != null)
                {
                    Manager.Audio.SfxPlay(config.collectSoundName, transform);
                }
                else if (Manager.Audio != null)
                {
                    Manager.Audio.SfxPlay("SFX_NormalItem", transform);
                }
            }
            else if (Manager.Audio != null)
            {
                Manager.Audio.SfxPlay("SFX_NormalItem", transform);
            }
        }

        #endregion

        #region Public Methods

        public ItemType GetItemType() // 아이템 타입 반환
        {
            return itemType;
        }
        
        public int GetPointValue() // 점수 값 반환
        {
            return pointValue;
        }
        
        public float GetEffectDuration() // 효과 지속시간 반환
        {
            return effectDuration;
        }
        
        public float GetMagnetRadius() // 자석 범위 반환
        {
            return magnetRadius;
        }
        
        public float GetMagnetForce() // 자석 힘 반환
        {
            return magnetForce;
        }
        
        public void OnDropAnimationComplete() // 드롭 애니메이션 완료 처리
        {
            isDropAnimationComplete = true;
            hasHitGround = true;
            StartBobAnimation();
        }
        
        public void SetGroundLevel(float level) // 바닥 레벨 설정
        {
            groundLevel = level;
        }
        
        public new void StopAllCoroutines() // 모든 코루틴 중지
        {
            if (bobCoroutine != null)
            {
                StopCoroutine(bobCoroutine);
                bobCoroutine = null;
            }
            
            base.StopAllCoroutines();
        }

        #endregion
    }
} 