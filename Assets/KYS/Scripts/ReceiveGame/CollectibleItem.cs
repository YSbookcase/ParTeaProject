using System.Collections;
using UnityEngine;

namespace KYS
{
    public class CollectibleItem : PooledObject
    {
        [Header("Item Settings")]
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.5f;
        [SerializeField] private int pointValue = 1;
        
        [Header("Physics Settings")]
        [SerializeField] private float groundLevel = 0.5f; // 바닥 레벨 (Y축)
        [SerializeField] private float bounceForce = 3f; // 바운스 힘
        [SerializeField] private float maxFallSpeed = 15f; // 최대 낙하 속도
        [SerializeField] private float returnDelay = 3f; // 바닥 닿은 후 리턴 지연 시간
        
        [Header("Effects")]
        [SerializeField] private GameObject collectParticle;
        [SerializeField] private AudioClip collectSound;
        
        private Vector3 startPosition;
        private bool isCollected = false;
        private bool hasHitGround = false;
        private Renderer itemRenderer;
        private AudioSource audioSource;
        private Rigidbody rb;
        private Coroutine returnCoroutine;
        
        public bool IsCollected => isCollected;
        public int PointValue => pointValue;
        
        private void Start()
        {
            startPosition = transform.position;
            itemRenderer = GetComponent<Renderer>();
            audioSource = GetComponent<AudioSource>();
            rb = GetComponent<Rigidbody>();
            
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            
            // Rigidbody 설정
            SetupRigidbody();
            
            // 아이템 애니메이션 시작
            //StartCoroutine(ItemAnimation());
        }
        
        private void SetupRigidbody()
        {
            if (rb != null)
            {
                rb.useGravity = true;
                rb.drag = 0.5f; // 공기 저항
                rb.angularDrag = 0.5f; // 회전 저항
                rb.mass = 1f;
                rb.maxAngularVelocity = 10f; // 최대 각속도 제한
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
            Debug.Log($"아이템이 바닥에 닿았습니다: {transform.position}");
            
            // 바운스 효과
            if (rb != null)
            {
                // 수평 속도 감소
                Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                horizontalVelocity *= 0.3f; // 수평 속도 70% 감소
                
                // 바운스 힘 적용
                rb.velocity = horizontalVelocity + Vector3.up * bounceForce;
                
                // 회전 속도 감소
                rb.angularVelocity *= 0.5f;
            }
            
            // 일정 시간 후 오브젝트 풀로 리턴
            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
            }
            returnCoroutine = StartCoroutine(ReturnToPoolAfterDelay());
        }
        
        private IEnumerator ReturnToPoolAfterDelay()
        {
            yield return new WaitForSeconds(returnDelay);
            
            if (!isCollected)
            {
                Debug.Log("아이템이 바닥에서 시간 초과로 리턴됩니다.");
                ReturnToPool(0f);
            }
        }
        
        private IEnumerator ItemAnimation()
        {
            while (!isCollected)
            {
                // 위아래 움직임 애니메이션
                float time = 0f;
                while (time < 1f && !isCollected)
                {
                    time += Time.deltaTime * bobSpeed;
                    float yOffset = Mathf.Sin(time * Mathf.PI * 2) * bobHeight;
                    transform.position = startPosition + Vector3.up * yOffset;
                    yield return null;
                }
            }
        }
        
        public void Collect()
        {
            if (isCollected) return;
            
            isCollected = true;
            
            // 리턴 코루틴 중지
            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
                returnCoroutine = null;
            }
            
            // 수집 효과 재생
            PlayCollectEffect();
            
            // 오브젝트 풀로 반환 (0.5초 후)
            ReturnToPool(0.5f);
        }
        
        private void PlayCollectEffect()
        {
            // 수집 파티클 효과
            if (collectParticle != null)
            {
                GameObject effect = Instantiate(collectParticle, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            
            // 수집 사운드
            if (collectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(collectSound);
            }
            
            // 머티리얼 투명도 애니메이션
            StartCoroutine(FadeOutAnimation());
        }
        
        private IEnumerator FadeOutAnimation()
        {
            if (itemRenderer != null && itemRenderer.material != null)
            {
                float duration = 0.5f;
                float elapsed = 0f;
                Color originalColor = itemRenderer.material.color;
                
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                    itemRenderer.material.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                    yield return null;
                }
            }
        }
        
        private IEnumerator DisableAfterEffect()
        {
            // 효과 재생 후 비활성화
            yield return new WaitForSeconds(0.5f);
            gameObject.SetActive(false);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // 플레이어와 충돌 시 자동 수집
            if (!isCollected && other.CompareTag("Player"))
            {
                Collect();
            }
        }
        
        public void ResetItem()
        {
            isCollected = false;
            hasHitGround = false;
            
            // 리턴 코루틴 중지
            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
                returnCoroutine = null;
            }
            
            // Rigidbody 리셋
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = true;
            }
            
            // startPosition으로 되돌리지 않고 현재 위치 유지
            // transform.position = startPosition; // 이 줄 제거
            transform.rotation = Quaternion.identity;
            
            if (itemRenderer != null && itemRenderer.material != null)
            {
                Color color = itemRenderer.material.color;
                itemRenderer.material.color = new Color(color.r, color.g, color.b, 1f);
            }
            
            gameObject.SetActive(true);
            // StartCoroutine(ItemAnimation()); // 위아래 움직임 비활성화
        }
    }
} 