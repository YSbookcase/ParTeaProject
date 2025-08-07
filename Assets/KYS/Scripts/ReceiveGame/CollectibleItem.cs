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
        [SerializeField] private float groundReturnDelay = 3f; // 바닥 닿은 후 풀 반환 지연 시간
        [SerializeField] private float mobileGroundReturnDelay = 5f; // 모바일용 바닥 풀 반환 지연 시간
        
        [Header("Effects")]
        [SerializeField] private GameObject collectParticle;
        [SerializeField] private string collectSoundName = "SFX_NormalItem"; // AudioData 에셋 이름으로 변경
        
        [Header("Configuration")]
        [SerializeField] private ItemConfiguration itemConfiguration; // ScriptableObject 참조 추가
        
        private bool isCollected = false; // 수집됨 여부
        private bool hasLandedOnGround = false; // 바닥에 착지했는지 여부
        private Vector3 previousPosition; // 이전 프레임 위치 (떨어지는 감지용)
        
        // 이전 위치 설정을 위한 public 프로퍼티
        public Vector3 PreviousPosition
        {
            get { return previousPosition; }
            set { previousPosition = value; }
        }
        
        // 바닥 착지 상태를 위한 public 프로퍼티
        public bool HasLandedOnGround
        {
            get { return hasLandedOnGround; }
            set { hasLandedOnGround = value; }
        }
        private Renderer itemRenderer;
        private Rigidbody rb;
        private Coroutine groundReturnCoroutine; // 바닥 착지 후 풀 반환 코루틴
        
        public bool IsCollected => isCollected;
        public int PointValue => pointValue;
        
        [Header("Item Type")]
        [SerializeField] public ItemType itemType = ItemType.Normal; // 아이템 타입 설정
        
        // 모바일 플랫폼 감지
        private bool isMobilePlatform => Application.isMobilePlatform;
        
        private bool isInitialized = false; // 초기화 완료 여부
        
        private void Start()
        {
            // 이미 초기화된 경우 중복 초기화 방지
            if (isInitialized) return;
            
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
            
            // 초기 스폰 위치는 최초 생성 시에만 저장 (풀 재사용 시에는 저장하지 않음)
            // initialSpawnPosition = transform.position; // ← 제거됨
            
            // 모바일에서 콜라이더 크기 확대
            SetupColliderForMobile();
            
            // 아이템 애니메이션 시작
            StartCoroutine(ItemAnimation());
            
            // 초기화 완료 표시
            isInitialized = true;
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
                //Debug.Log($"[CollectibleItem] 모바일 콜라이더 확대: {gameObject.name}, 새로운 반지름: {sphereCollider.radius}");
            }
            
            // BoxCollider 확대
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                // 기존 크기의 1.5배로 확대
                boxCollider.size *= 1.5f;
                //Debug.Log($"[CollectibleItem] 모바일 콜라이더 확대: {gameObject.name}, 새로운 크기: {boxCollider.size}");
            }
            
            // CapsuleCollider 확대
            CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
            {
                // 기존 크기의 1.5배로 확대
                capsuleCollider.radius *= 1.5f;
                capsuleCollider.height *= 1.5f;
                //Debug.Log($"[CollectibleItem] 모바일 콜라이더 확대: {gameObject.name}, 새로운 반지름: {capsuleCollider.radius}, 높이: {capsuleCollider.height}");
            }
        }
        
        private void Update()
        {
            if (!isCollected)
            {
                // 회전 애니메이션 (바닥에 닿아도 계속 회전)
                transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
                
                // 바닥에 착지하지 않았을 때만 낙하 처리
                if (!hasLandedOnGround)
                {
                    // 낙하 속도 제한
                    if (rb != null && rb.velocity.y < -maxFallSpeed)
                    {
                        rb.velocity = new Vector3(rb.velocity.x, -maxFallSpeed, rb.velocity.z);
                    }
                    
                    // 바닥 충돌 감지 (높은 위치에서 잘못 감지되지 않도록 개선)
                    CheckGroundCollision();
                }
                
                // 이전 위치 업데이트 (다음 프레임에서 떨어지는 감지용)
                previousPosition = transform.position;
            }
        }
        
        private void CheckGroundCollision()
        {
            // 높은 위치에서 잘못된 바닥 충돌 감지를 방지
            // 아이템이 실제로 떨어지는 중일 때만 바닥 충돌을 감지
            if (transform.position.y <= groundLevel && !hasLandedOnGround)
            {
                // 더 엄격한 검증: 실제로 떨어지는 중인지 확인
                bool isActuallyFalling = false;
                
                if (rb != null && rb.velocity.y < 0)
                {
                    // Rigidbody가 있고 아래로 떨어지는 중
                    isActuallyFalling = true;
                }
                else if (rb == null && previousPosition.y > transform.position.y)
                {
                    // Rigidbody가 없지만 이전 프레임보다 낮아졌음 (DropItemToGround 코루틴으로 이동 중)
                    isActuallyFalling = true;
                }
                
                // 실제로 떨어지는 중일 때만 바닥 착지 처리
                if (isActuallyFalling)
                {
                    OnLandedOnGround();
                }
                // 그 외의 경우는 무시 (높은 위치에서 잘못된 감지 방지)
            }
        }
        
        private void OnLandedOnGround()
        {
            hasLandedOnGround = true;
            ////Debug.Log($"아이템이 바닥에 착지했습니다: {transform.position}");
            
            // 바운스 효과 (Trigger 콜라이더 대응)
            if (rb != null)
            {
                // 현재 위치를 바닥 레벨로 고정
                Vector3 currentPos = transform.position;
                currentPos.y = groundLevel;
                transform.position = currentPos;
                
                // 바운스 속도 적용
                rb.velocity = new Vector3(rb.velocity.x, bounceForce, rb.velocity.z);
            }
            else
            {
                // Rigidbody가 없는 경우 위치만 조정
                Vector3 currentPos = transform.position;
                currentPos.y = groundLevel;
                transform.position = currentPos;
            }
            
            // 일정 시간 후 오브젝트 풀로 반환
            groundReturnCoroutine = StartCoroutine(ReturnToPoolAfterGroundDelay());
        }
        
        private IEnumerator ReturnToPoolAfterGroundDelay()
        {
            // 플랫폼별 바닥 착지 후 풀 반환 지연 시간 적용
            float currentGroundReturnDelay = isMobilePlatform ? mobileGroundReturnDelay : groundReturnDelay;
            
            // 모바일 디버깅 로그
            if (isMobilePlatform)
            {
                //Debug.Log($"[CollectibleItem] 모바일에서 아이템 바닥 착지: {gameObject.name}, 풀 반환 지연: {currentGroundReturnDelay}초");
            }
            
            yield return new WaitForSeconds(currentGroundReturnDelay);
            
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
            // 애니메이션 기준 위치를 현재 위치로 설정 (풀에서 재사용될 때 대응)
            Vector3 animationBasePosition = transform.position;
            
            while (!isCollected && gameObject.activeInHierarchy)
            {
                // 바닥에 착지하지 않았을 때만 위아래 움직임
                if (!hasLandedOnGround)
                {
                    // DropItemToGround 코루틴이 실행 중일 때는 위치 변경하지 않음
                    // 대기 시간을 두어 드롭 애니메이션이 완료될 때까지 기다림
                    yield return new WaitForSeconds(4f); // DropItemToGround 완료 대기
                    
                    // 드롭 완료 후 바닥에 착지한 것으로 간주
                    hasLandedOnGround = true;
                    Debug.Log($"[CollectibleItem] ItemAnimation에서 드롭 완료 후 바닥 착지 처리 - 아이템: {gameObject.name}");
                }
                else
                {
                    // 바닥에 착지했으면 바닥 레벨에 고정
                    Vector3 currentPos = transform.position;
                    currentPos.y = groundLevel;
                    transform.position = currentPos;
                }
                
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
                //Debug.Log($"[Mobile] CollectibleItem.Collect() 호출됨 - {gameObject.name}, PhotonView: {photonView != null}, IsMine: {photonView?.IsMine}");
            }
            
            // 네트워크 동기화를 위해 RPC 호출
            if (photonView != null && photonView.IsMine)
            {
                photonView.RPC(nameof(CollectRPC), RpcTarget.All);
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
            
            // 바닥 착지 후 풀 반환 코루틴 중지
            if (groundReturnCoroutine != null)
            {
                StopCoroutine(groundReturnCoroutine);
                groundReturnCoroutine = null;
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
            hasLandedOnGround = false;
            
            // 풀에서 재사용될 때는 현재 위치를 초기 위치로 설정하지 않음
            // ReceiveGameManagerEnhanced에서 올바른 높이로 설정할 예정
            
            // 이전 위치 초기화 (위치 설정 후에 업데이트됨)
            // previousPosition = transform.position; // 위치 설정 후에 업데이트
            
            // Rigidbody 리셋 (속도만 리셋, 위치는 유지)
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false; // 드롭 코루틴 중에는 중력 비활성화
            }
            
            // 시각적 효과 리셋
            if (itemRenderer != null)
            {
                Color originalColor = itemRenderer.material.color;
                originalColor.a = 1f;
                itemRenderer.material.color = originalColor;
            }
            
            // 기존 바닥 착지 후 풀 반환 코루틴 정리
            if (groundReturnCoroutine != null)
            {
                StopCoroutine(groundReturnCoroutine);
                groundReturnCoroutine = null;
            }
            
            // ItemAnimation 코루틴만 중지 (DropItemToGround 코루틴은 유지)
            // StopAllCoroutines() 제거 - DropItemToGround 코루틴이 중단되지 않도록 함
            
            gameObject.SetActive(true);
            
            Debug.Log($"[CollectibleItem.ResetItem] 아이템 리셋 완료 - 타입: {itemType}, 현재위치: {transform.position}, 아이템: {gameObject.name}, hasLandedOnGround: {hasLandedOnGround}");
        }
        
        /// <summary>
        /// 지연 시간 후 풀로 반환하는 메서드 (ItemPoolManager에 의존하지 않음)
        /// </summary>
        public void ReturnToPoolWithDelay(float delay)
        {
            if (groundReturnCoroutine != null)
            {
                StopCoroutine(groundReturnCoroutine);
            }
            groundReturnCoroutine = StartCoroutine(ReturnToPoolDelayed(delay));
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
        /// ReceiveGameManagerEnhanced에서 groundLevel을 설정하는 메서드
        /// </summary>
        public void SetGroundLevel(float level)
        {
            groundLevel = level;
            Debug.Log($"[CollectibleItem] Ground level 설정됨: {groundLevel}");
        }
        
        /// <summary>
        /// 모든 코루틴을 중지하고 정리
        /// </summary>
        public new void StopAllCoroutines()
        {
            if (groundReturnCoroutine != null)
            {
                StopCoroutine(groundReturnCoroutine);
                groundReturnCoroutine = null;
            }
            
            // ItemAnimation 코루틴도 중지 (MonoBehaviour의 StopAllCoroutines 호출)
            base.StopAllCoroutines();
        }
        
        private void OnEnable()
        {
            // 오브젝트가 활성화될 때마다 ItemAnimation 코루틴 재시작
            // 단, 바닥에 착지한 후에만 시작 (DropItemToGround 코루틴과의 충돌 방지)
            if (isInitialized && !isCollected)
            {
                // 이전 위치 업데이트 (풀에서 재활용될 때)
                previousPosition = transform.position;
                
                // 기존 ItemAnimation 코루틴이 실행 중이면 중지
                StopAllCoroutines();
                
                // 바닥에 착지하지 않았으면 잠시 대기 후 시작
                if (!hasLandedOnGround)
                {
                    // DropItemToGround 코루틴이 완료될 때까지 대기
                    StartCoroutine(DelayedItemAnimation());
                }
                else
                {
                    // 이미 바닥에 착지한 경우 즉시 시작
                    StartCoroutine(ItemAnimation());
                }
            }
        }
        
        private IEnumerator DelayedItemAnimation()
        {
            // DropItemToGround 코루틴이 완료될 때까지 대기 (itemDropToGroundSpeed + 여유시간)
            yield return new WaitForSeconds(6f); // 5초(드롭시간) + 1초(여유시간)
            
            // 바닥에 착지했는지 확인 후 ItemAnimation 시작
            if (!isCollected && gameObject.activeInHierarchy)
            {
                // 드롭 완료 후 바닥에 착지한 것으로 간주
                hasLandedOnGround = true;
                StartCoroutine(ItemAnimation());
                Debug.Log($"[CollectibleItem] DelayedItemAnimation 완료 후 ItemAnimation 시작 - 아이템: {gameObject.name}");
            }
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
            
            groundReturnCoroutine = null;
        }
    }
} 