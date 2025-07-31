using UnityEngine;
using Photon.Pun;
using System.Collections;

namespace KYS
{
    public class EnhancedItemController : MonoBehaviourPun
    {
        [Header("Configuration")]
        [SerializeField] private ItemConfiguration itemConfiguration;
        
        [Header("Item Settings")]
        [SerializeField] private ReceiveGameManager.ItemType itemType = ReceiveGameManager.ItemType.Normal;
        [SerializeField] private int pointValue = 1;
        [SerializeField] private float effectDuration = 5f;
        
        [Header("Physics Settings")]
        [SerializeField] private float groundLevel = 0.5f;
        [SerializeField] private float bounceForce = 2f;
        [SerializeField] private float maxFallSpeed = 12f;
        [SerializeField] private float rotationSpeed = 60f;
        [SerializeField] private float bobSpeed = 1.5f;
        [SerializeField] private float bobHeight = 0.3f;
        
        [Header("Visual Components")]
        [SerializeField] private Transform visualContainer; // 시각적 요소들을 담을 컨테이너
        [SerializeField] private Renderer itemRenderer; // 기본 렌더러 (색상 변경용)
        
        // Legacy color settings (fallback)
        [Header("Legacy Visual Effects")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color bonusColor = Color.yellow;
        [SerializeField] private Color speedColor = Color.blue;
        [SerializeField] private Color slowColor = Color.red;
        [SerializeField] private Color magnetColor = Color.green;
        
        private Rigidbody rb;
        private bool isCollected = false;
        private bool hasHitGround = false;
        private Vector3 startPosition;
        private Coroutine bobCoroutine;
        private GameObject currentVisualPrefab;
        
        private void Start()
        {
            // 아이템 설정 로드
            if (itemConfiguration == null)
            {
                itemConfiguration = Resources.Load<ItemConfiguration>("ItemConfiguration");
                if (itemConfiguration == null)
                {
                    Debug.LogWarning("[EnhancedItemController] ItemConfiguration을 찾을 수 없습니다. 기본 설정을 사용합니다.");
                }
            }
            
            // 컴포넌트 초기화
            if (itemRenderer == null)
            {
                itemRenderer = GetComponent<Renderer>();
            }
            
            if (visualContainer == null)
            {
                // visualContainer가 없으면 자동으로 생성
                GameObject container = new GameObject("VisualContainer");
                container.transform.SetParent(transform);
                container.transform.localPosition = Vector3.zero;
                container.transform.localRotation = Quaternion.identity;
                visualContainer = container.transform;
            }
            
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            
            SetupRigidbody();
            startPosition = transform.position;
            
            // 아이템 타입이 설정되지 않은 경우 기본값 사용
            if (itemType == ReceiveGameManager.ItemType.Normal)
            {
                UpdateVisual();
            }
        }
        
        private void SetupRigidbody()
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
                
                // Y축 회전만 허용
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
        }
        
        private void Update()
        {
            if (isCollected) return;
            
            // 회전 애니메이션
            if (rb != null && !rb.isKinematic)
            {
                rb.angularVelocity = new Vector3(0, rotationSpeed * Mathf.Deg2Rad, 0);
            }
            
            // 바닥 충돌 체크
            CheckGroundCollision();
        }
        
        private void CheckGroundCollision()
        {
            if (hasHitGround) return;
            
            if (transform.position.y <= groundLevel)
            {
                OnHitGround();
            }
        }
        
        private void OnHitGround()
        {
            hasHitGround = true;
            
            // 바운스 효과
            if (rb != null)
            {
                rb.velocity = new Vector3(rb.velocity.x, bounceForce, rb.velocity.z);
            }
            
            // 바운스 애니메이션 시작
            StartBobAnimation();
            
            // 낙하 속도 제한
            if (rb != null && rb.velocity.y < -maxFallSpeed)
            {
                rb.velocity = new Vector3(rb.velocity.x, -maxFallSpeed, rb.velocity.z);
            }
        }
        
        private void StartBobAnimation()
        {
            if (bobCoroutine == null)
            {
                bobCoroutine = StartCoroutine(BobAnimation());
            }
        }
        
        private IEnumerator BobAnimation()
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
        
        public void SetItemType(ReceiveGameManager.ItemType type)
        {
            itemType = type;
            UpdateVisual();
        }
        
        private void UpdateVisual()
        {
            // 기존 시각적 프리팹 제거
            ClearVisualPrefab();
            
            // 설정에서 아이템 정보 가져오기
            ItemConfig config = null;
            if (itemConfiguration != null)
            {
                config = itemConfiguration.GetItemConfig(itemType);
            }
            
            if (config != null)
            {
                // 설정된 값들 적용
                pointValue = config.pointValue;
                effectDuration = config.effectDuration;
                bounceForce = config.bounceForce;
                maxFallSpeed = config.maxFallSpeed;
                rotationSpeed = config.rotationSpeed;
                bobSpeed = config.bobSpeed;
                bobHeight = config.bobHeight;
                
                // 시각적 프리팹 적용
                if (config.visualPrefab != null)
                {
                    CreateVisualPrefab(config.visualPrefab);
                }
                else
                {
                    // 프리팹이 없으면 색상만 변경
                    ApplyColor(config.itemColor);
                }
            }
            else
            {
                // 설정이 없으면 레거시 방식 사용
                ApplyLegacyVisual();
            }
        }
        
        private void CreateVisualPrefab(GameObject prefab)
        {
            if (visualContainer == null) return;
            
            // 기존 프리팹 제거
            ClearVisualPrefab();
            
            // 새 프리팹 생성
            currentVisualPrefab = Instantiate(prefab, visualContainer);
            currentVisualPrefab.transform.localPosition = Vector3.zero;
            currentVisualPrefab.transform.localRotation = Quaternion.identity;
            currentVisualPrefab.transform.localScale = Vector3.one;
            
            Debug.Log($"[EnhancedItemController] {itemType} 타입의 시각적 프리팹 생성: {prefab.name}");
        }
        
        private void ClearVisualPrefab()
        {
            if (currentVisualPrefab != null)
            {
                DestroyImmediate(currentVisualPrefab);
                currentVisualPrefab = null;
            }
            
            // visualContainer의 모든 자식 제거
            if (visualContainer != null)
            {
                for (int i = visualContainer.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(visualContainer.GetChild(i).gameObject);
                }
            }
        }
        
        private void ApplyColor(Color color)
        {
            if (itemRenderer != null)
            {
                itemRenderer.material.color = color;
            }
        }
        
        private void ApplyLegacyVisual()
        {
            Color targetColor = normalColor;
            switch (itemType)
            {
                case ReceiveGameManager.ItemType.Normal:
                    targetColor = normalColor;
                    pointValue = 1;
                    break;
                case ReceiveGameManager.ItemType.Bonus:
                    targetColor = bonusColor;
                    pointValue = 3;
                    break;
                case ReceiveGameManager.ItemType.Speed:
                    targetColor = speedColor;
                    pointValue = 1;
                    break;
                case ReceiveGameManager.ItemType.Slow:
                    targetColor = slowColor;
                    pointValue = 1;
                    break;
                case ReceiveGameManager.ItemType.Magnet:
                    targetColor = magnetColor;
                    pointValue = 1;
                    break;
            }
            
            ApplyColor(targetColor);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;
            
            // 플레이어와 충돌했는지 확인
            if (other.CompareTag("Player"))
            {
                isCollected = true;
                
                // 바운스 애니메이션 중지
                if (bobCoroutine != null)
                {
                    StopCoroutine(bobCoroutine);
                    bobCoroutine = null;
                }
                
                // 플레이어의 ReceiveGamePlayer 컴포넌트 찾기
                ReceiveGamePlayer player = other.GetComponent<ReceiveGamePlayer>();
                if (player != null)
                {
                    // 아이템 효과 적용
                    ApplyItemEffect(player);
                    
                    // 점수 추가
                    if (PhotonNetwork.IsMasterClient)
                    {
                        ReceiveGameManager gameManager = FindObjectOfType<ReceiveGameManager>();
                        if (gameManager != null)
                        {
                            gameManager.CollectItem(player.GetPlayerActorNumber());
                        }
                    }
                }
                
                // 아이템 제거
                Destroy(gameObject);
            }
        }
        
        private void ApplyItemEffect(ReceiveGamePlayer player)
        {
            switch (itemType)
            {
                case ReceiveGameManager.ItemType.Normal:
                case ReceiveGameManager.ItemType.Bonus:
                    // 점수만 추가 (CollectItem에서 처리)
                    break;
                    
                case ReceiveGameManager.ItemType.Speed:
                    player.ApplySpeedBoost(effectDuration);
                    break;
                    
                case ReceiveGameManager.ItemType.Slow:
                    player.ApplySlowEffect(effectDuration);
                    break;
                    
                case ReceiveGameManager.ItemType.Magnet:
                    player.ApplyMagnetEffect(effectDuration);
                    break;
            }
        }
        
        public ReceiveGameManager.ItemType GetItemType()
        {
            return itemType;
        }
        
        public int GetPointValue()
        {
            return pointValue;
        }
        
        public float GetEffectDuration()
        {
            return effectDuration;
        }
    }
} 