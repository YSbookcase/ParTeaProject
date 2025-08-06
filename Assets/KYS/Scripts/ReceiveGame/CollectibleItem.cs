using System.Collections;
using UnityEngine;
using Photon.Pun;

namespace KYS
{
    public class CollectibleItem : PooledObject
    {
        private PhotonView photonView;
    
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
        [SerializeField] private float mobileReturnDelay = 5f; // 모바일용 리턴 지연 시간 (더 길게)
        
        [Header("Effects")]
        [SerializeField] private GameObject collectParticle;
        [SerializeField] private string collectSoundName = "SFX_NormalItem"; // AudioData 에셋 이름으로 변경
        
        [Header("Configuration")]
        [SerializeField] private ItemConfiguration itemConfiguration; // ScriptableObject 참조 추가
        
        private Vector3 startPosition;
        private bool isCollected = false;
        private bool hasHitGround = false;
        private Renderer itemRenderer;
        private Rigidbody rb;
        private Coroutine returnCoroutine;
        
        public bool IsCollected => isCollected;
        public int PointValue => pointValue;
        
        [Header("Item Type")]
        [SerializeField] public ItemType itemType = ItemType.Normal; // 아이템 타입 설정
        
        // 모바일 플랫폼 감지
        private bool isMobilePlatform => Application.isMobilePlatform;
        
        private void Start()
        {
            // PhotonView 초기화
            photonView = GetComponent<PhotonView>();
            if (photonView == null)
            {
                photonView = gameObject.AddComponent<PhotonView>();
            }
            
            if (photonView == null)
            {
                //Debug.LogError($"[CollectibleItem] PhotonView 초기화 실패: {gameObject.name}");
                return;
            }
            
            // 컴포넌트 초기화
            itemRenderer = GetComponent<Renderer>();
            rb = GetComponent<Rigidbody>();
            
            // 시작 위치 저장
            startPosition = transform.position;
            
            // 모바일에서 콜라이더 크기 확대
            SetupColliderForMobile();
            
            // 아이템 애니메이션 시작
            StartCoroutine(ItemAnimation());
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
        
        /// <summary>
        /// 모바일에서 아이템 수집을 위해 콜라이더 크기를 확대합니다.
        /// </summary>
        private void SetupColliderForMobile()
        {
            if (!isMobilePlatform) return;
            
            // SphereCollider 확대
            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                // 기존 크기의 1.5배로 확대
                sphereCollider.radius *= 1.5f;
                Debug.Log($"[CollectibleItem] 모바일 콜라이더 확대: {gameObject.name}, 새로운 반지름: {sphereCollider.radius}");
            }
            
            // BoxCollider 확대
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                // 기존 크기의 1.5배로 확대
                boxCollider.size *= 1.5f;
                Debug.Log($"[CollectibleItem] 모바일 콜라이더 확대: {gameObject.name}, 새로운 크기: {boxCollider.size}");
            }
            
            // CapsuleCollider 확대
            CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
            {
                // 기존 크기의 1.5배로 확대
                capsuleCollider.radius *= 1.5f;
                capsuleCollider.height *= 1.5f;
                Debug.Log($"[CollectibleItem] 모바일 콜라이더 확대: {gameObject.name}, 새로운 반지름: {capsuleCollider.radius}, 높이: {capsuleCollider.height}");
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
            ////Debug.Log($"아이템이 바닥에 닿았습니다: {transform.position}");
            
            // 바운스 효과
            if (rb != null)
            {
                rb.velocity = new Vector3(rb.velocity.x, bounceForce, rb.velocity.z);
            }
            
            // 일정 시간 후 오브젝트 풀로 반환
            returnCoroutine = StartCoroutine(ReturnToPoolAfterDelay());
        }
        
        private IEnumerator ReturnToPoolAfterDelay()
        {
            // 플랫폼별 리턴 지연 시간 적용
            float currentReturnDelay = isMobilePlatform ? mobileReturnDelay : returnDelay;
            
            // 모바일 디버깅 로그
            if (isMobilePlatform)
            {
                Debug.Log($"[CollectibleItem] 모바일에서 아이템 바닥 도착: {gameObject.name}, 리턴 지연: {currentReturnDelay}초");
            }
            
            yield return new WaitForSeconds(currentReturnDelay);
            
            // 오브젝트 풀로 반환
            if (returnPool != null)
            {
                ReturnToPool();
            }
            else
            {
                //Debug.LogWarning($"[CollectibleItem] returnPool이 null입니다. 오브젝트를 파괴합니다: {gameObject.name}");
                Destroy(gameObject);
            }
        }
        
        private IEnumerator ItemAnimation()
        {
            Vector3 originalPosition = transform.position;
            
            while (!isCollected && !hasHitGround)
            {
                // 위아래 움직임
                float newY = originalPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
                
                yield return null;
            }
        }
        
        public void Collect()
        {
            // 모바일에서는 이미 수집된 아이템도 재시도 허용 (네트워크 지연 대응)
            if (isCollected && !isMobilePlatform) return;
            
            // 즉시 수집 상태로 변경하여 중복 수집 방지
            isCollected = true;
            
            // 모바일 디버그 로그
            if (Application.isMobilePlatform)
            {
                Debug.Log($"[Mobile] CollectibleItem.Collect() 호출됨 - {gameObject.name}, PhotonView: {photonView != null}, IsMine: {photonView?.IsMine}");
            }
            
            // 네트워크 동기화를 위해 RPC 호출
            if (photonView != null && photonView.IsMine)
            {
                photonView.RPC("CollectRPC", RpcTarget.All);
            }
            else if (photonView == null)
            {
                // PhotonView가 없는 경우 로컬에서만 처리
                CollectLocal();
            }
            else
            {
                // PhotonView가 있지만 IsMine이 아닌 경우에도 로컬 처리
                // 모바일에서 네트워크 지연으로 인한 수집 실패 방지
                CollectLocal();
            }
        }
        
        [PunRPC]
        private void CollectRPC()
        {
            CollectLocal();
        }
        
        private void CollectLocal()
        {
            if (isCollected) return;
            
            // 이미 Collect()에서 isCollected = true로 설정했으므로 여기서는 중복 설정하지 않음
            
            // 리턴 코루틴 중지
            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
                returnCoroutine = null;
            }
            
            // 수집 효과 재생
            PlayCollectEffect();
            
            // 오브젝트 풀로 반환 (0.5초 후) - null 체크 추가
            if (returnPool != null)
            {
                ReturnToPool(0.5f);
            }
            else
            {
                //Debug.LogWarning($"[CollectibleItem] returnPool이 null입니다. 오브젝트를 파괴합니다: {gameObject.name}");
                // 중복 파괴 방지를 위해 즉시 비활성화
                gameObject.SetActive(false);
                
                // PhotonView가 있는지 확인 (네트워크 오브젝트인지 확인)
                PhotonView photonView = GetComponent<PhotonView>();
                
                if (photonView != null && PhotonNetwork.IsMasterClient)
                {
                    // 네트워크 오브젝트인 경우 PhotonNetwork.Destroy 사용
                    if (gameObject != null)
                    {
                        PhotonNetwork.Destroy(gameObject);
                    }
                }
                else
                {
                    // 로컬 오브젝트이거나 Master Client가 아닌 경우 일반 Destroy 사용
                    if (gameObject != null)
                    {
                        Destroy(gameObject, 0.5f);
                    }
                }
            }
        }
        
        private void PlayCollectEffect()
        {
            // 수집 파티클 효과
            if (collectParticle != null)
            {
                GameObject effect = Instantiate(collectParticle, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            
            // 수집 사운드 - ScriptableObject에서 가져온 설정 사용
            string soundToPlay = GetCollectSoundName();
            if (!string.IsNullOrEmpty(soundToPlay) && Manager.Audio != null)
            {
                Manager.Audio.SfxPlay(soundToPlay, transform);
            }
            
            // 머티리얼 투명도 애니메이션
            StartCoroutine(FadeOutAnimation());
        }
        
        /// <summary>
        /// 아이템 타입에 맞는 수집 사운드 이름을 가져옵니다.
        /// </summary>
        private string GetCollectSoundName()
        {
            return collectSoundName;
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
        
        public void ResetItem()
        {
            isCollected = false;
            hasHitGround = false;
            startPosition = transform.position;
            
            // Rigidbody 리셋
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // 시각적 효과 리셋
            if (itemRenderer != null)
            {
                Color originalColor = itemRenderer.material.color;
                originalColor.a = 1f;
                itemRenderer.material.color = originalColor;
            }
            
            // 기존 코루틴 정리
            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
                returnCoroutine = null;
            }
            
            gameObject.SetActive(true);
            
            ////Debug.Log($"[CollectibleItem] 아이템 리셋 완료 - 타입: {itemType}");
        }
        
        /// <summary>
        /// 지연 시간 후 풀로 반환하는 메서드 (ItemPoolManager에 의존하지 않음)
        /// </summary>
        public void ReturnToPoolWithDelay(float delay)
        {
            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
            }
            returnCoroutine = StartCoroutine(ReturnToPoolDelayed(delay));
        }
        
        /// <summary>
        /// ItemPoolManager에서 ItemConfiguration을 설정하는 메서드
        /// </summary>
        public void SetItemConfiguration(ItemConfiguration config)
        {
            itemConfiguration = config;
            
            // 설정이 변경되면 시각적 요소 업데이트
            if (itemRenderer != null && itemConfiguration != null)
            {
                // 색상 설정은 EnhancedItemController에서 처리됨
                // 여기서는 기본 색상만 유지
            }
        }
        
        /// <summary>
        /// 모든 코루틴을 중지하고 정리
        /// </summary>
        public new void StopAllCoroutines()
        {
            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
                returnCoroutine = null;
            }
            
            // ItemAnimation 코루틴도 중지 (MonoBehaviour의 StopAllCoroutines 호출)
            base.StopAllCoroutines();
        }
        
        private void OnDestroy()
        {
            // 오브젝트가 파괴될 때 모든 코루틴 중지
            StopAllCoroutines();
        }
        
        private IEnumerator ReturnToPoolDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // ItemPoolManager가 여전히 유효한지 확인
            if (returnPool != null && returnPool.gameObject != null && returnPool.gameObject.activeInHierarchy)
            {
                returnPool.ReturnToPool(this);
            }
            else
            {
                //Debug.LogWarning($"[CollectibleItem] 풀이 유효하지 않아 지연 반환 실패: {gameObject.name}");
            }
            
            returnCoroutine = null;
        }
    }
} 