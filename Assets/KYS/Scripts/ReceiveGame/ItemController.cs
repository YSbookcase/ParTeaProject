using UnityEngine;
using Photon.Pun;
using System.Collections;

namespace KYS
{
    public class ItemController : MonoBehaviourPun
    {
        [Header("Item Settings")]
        [SerializeField] private ItemType itemType = ItemType.Normal;
        [SerializeField] private int pointValue = 1;
        [SerializeField] private float effectDuration = 5f;
        
        [Header("Physics Settings")]
        [SerializeField] private float groundLevel = 0.5f; // 바닥 레벨 (Y축)
        [SerializeField] private float bounceForce = 2f; // 바운스 힘 (일반 아이템보다 약함)
        [SerializeField] private float maxFallSpeed = 12f; // 최대 낙하 속도
        [SerializeField] private float rotationSpeed = 60f; // 회전 속도
        [SerializeField] private float bobSpeed = 1.5f; // 위아래 움직임 속도
        [SerializeField] private float bobHeight = 0.3f; // 위아래 움직임 높이
        
        [Header("Visual Effects")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color bonusColor = Color.yellow;
        [SerializeField] private Color speedColor = Color.blue;
        [SerializeField] private Color slowColor = Color.red;
        [SerializeField] private Color magnetColor = Color.green;
        
        private Renderer itemRenderer;
        private Rigidbody rb;
        private bool isCollected = false;
        private bool hasHitGround = false;
        private Vector3 startPosition;
        private Coroutine bobCoroutine;
        
        private void Start()
        {
            itemRenderer = GetComponent<Renderer>();
            rb = GetComponent<Rigidbody>();
            
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            
            SetupRigidbody();
            UpdateVisual();
            
            // 시작 위치 저장
            startPosition = transform.position;
        }
        
        private void SetupRigidbody()
        {
            if (rb != null)
            {
                rb.useGravity = true;
                rb.drag = 0.3f; // 공기 저항 (일반 아이템보다 적음)
                rb.angularDrag = 0.3f; // 회전 저항
                rb.mass = 0.8f; // 질량 (일반 아이템보다 가벼움)
                rb.maxAngularVelocity = 8f; // 최대 각속도 제한
            }
        }
        
        private void Update()
        {
            if (!isCollected && !hasHitGround)
            {
                // 회전 애니메이션
                transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
                
                // 낙하 속도 제한
                if (rb != null && rb.velocity.y < -maxFallSpeed)
                {
                    rb.velocity = new Vector3(rb.velocity.x, -maxFallSpeed, rb.velocity.z);
                }
                
                // 바닥 충돌 감지
                CheckGroundCollision();
            }
        }
        
        private void CheckGroundCollision()
        {
            if (transform.position.y <= groundLevel && !hasHitGround)
            {
                OnHitGround();
            }
        }
        
        private void OnHitGround()
        {
            hasHitGround = true;
            Debug.Log($"파워업 아이템이 바닥에 닿았습니다: {transform.position}");
            
            // 바운스 효과 (일반 아이템보다 약함)
            if (rb != null)
            {
                // 수평 속도 감소
                Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                horizontalVelocity *= 0.2f; // 수평 속도 80% 감소
                
                // 바운스 힘 적용
                rb.velocity = horizontalVelocity + Vector3.up * bounceForce;
                
                // 회전 속도 감소
                rb.angularVelocity *= 0.3f;
            }
            
            // 바닥에서 위아래 움직임 애니메이션 시작
            StartBobAnimation();
        }
        
        private void StartBobAnimation()
        {
            if (bobCoroutine != null)
            {
                StopCoroutine(bobCoroutine);
            }
            bobCoroutine = StartCoroutine(BobAnimation());
        }
        
        private IEnumerator BobAnimation()
        {
            Vector3 groundPosition = new Vector3(transform.position.x, groundLevel, transform.position.z);
            
            while (!isCollected)
            {
                float time = 0f;
                while (time < 1f && !isCollected)
                {
                    time += Time.deltaTime * bobSpeed;
                    float yOffset = Mathf.Sin(time * Mathf.PI * 2) * bobHeight;
                    transform.position = groundPosition + Vector3.up * yOffset;
                    yield return null;
                }
            }
        }
        
        public void SetItemType(ItemType type)
        {
            itemType = type;
            UpdateVisual();
        }
        
        private void UpdateVisual()
        {
            if (itemRenderer == null) return;
            
            Color targetColor = normalColor;
            float scaleMultiplier = 1f;
            
            switch (itemType)
            {
                case ItemType.Normal:
                    targetColor = normalColor;
                    pointValue = 1;
                    scaleMultiplier = 1.5f;
                    break;
                case ItemType.Bonus:
                    targetColor = bonusColor;
                    pointValue = 3;
                    scaleMultiplier = 2.0f; // 보너스 아이템은 더 크게
                    break;
                case ItemType.Speed:
                    targetColor = speedColor;
                    pointValue = 1;
                    scaleMultiplier = 1.5f;
                    break;
                case ItemType.Slow:
                    targetColor = slowColor;
                    pointValue = 1;
                    scaleMultiplier = 1.2f; // 느린 아이템은 약간 작게
                    break;
                case ItemType.Magnet:
                    targetColor = magnetColor;
                    pointValue = 1;
                    scaleMultiplier = 1.5f;
                    break;
            }
            
            // 색상 적용
            itemRenderer.material.color = targetColor;
            
            // 크기 적용
            transform.localScale = Vector3.one * scaleMultiplier;
            
            // pointValue가 사용되었음을 명시적으로 표시 (컴파일러 경고 방지)
            if (pointValue > 0)
            {
                // 이 값은 실제로는 ReceiveGameManagerEnhanced.CollectItem에서 사용됨
                // 여기서는 컴파일러 경고를 방지하기 위한 명시적 사용
            }
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
                    
                    // 점수 추가 (ReceiveGamePlayer에서 처리하므로 여기서는 제거)
                    // ReceiveGamePlayer.CollectItem에서 ReceiveGameManagerEnhanced.CollectItem을 호출함
                }
                
                // 아이템 제거
                Destroy(gameObject);
            }
        }
        
        private void ApplyItemEffect(ReceiveGamePlayer player)
        {
            switch (itemType)
            {
                case ItemType.Normal:
                case ItemType.Bonus:
                    // 점수만 추가 (CollectItem에서 처리)
                    break;
                    
                case ItemType.Speed:
                    player.ApplySpeedBoost(effectDuration);
                    break;
                    
                case ItemType.Slow:
                    player.ApplySlowEffect(effectDuration);
                    break;
                    
                case ItemType.Magnet:
                    player.ApplyMagnetEffect(effectDuration);
                    break;
            }
        }
    }
} 