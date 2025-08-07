using System.Collections;
using UnityEngine;
using Photon.Pun;

namespace KYS
{
    public class CollectibleItem : PooledObject
    {
        [Header("Item Settings")]
        [SerializeField] private float rotationSpeed = 90f; // 회전 속도
        [SerializeField] private int pointValue = 1; // 점수 값
        
        [Header("Physics Settings")]
        [SerializeField] private float groundLevel = 0.5f; // 바닥 레벨 (Y축)
        [SerializeField] private float bounceForce = 3f; // 바운스 힘
        [SerializeField] private float maxFallSpeed = 15f; // 최대 낙하 속도
        [SerializeField] private float groundReturnDelay = 3f; // 바닥 닿은 후 풀 반환 지연 시간
        [SerializeField] private float mobileGroundReturnDelay = 5f; // 모바일용 바닥 풀 반환 지연 시간
        
        [Header("Effects")]
        [SerializeField] private GameObject collectParticle; // 수집 파티클 효과
        [SerializeField] private string collectSoundName = "SFX_NormalItem"; // 수집 사운드 이름
        
        [Header("Configuration")]
        [SerializeField] private ItemConfiguration itemConfiguration; // ScriptableObject 참조
        
        [Header("Item Type")]
        [SerializeField] public ItemType itemType = ItemType.Normal; // 아이템 타입 설정
        
        // Private variables
        private PhotonView photonView; // PhotonView 컴포넌트
        private bool isCollected = false; // 수집됨 여부
        private bool hasLandedOnGround = false; // 바닥에 착지했는지 여부
        private Vector3 previousPosition; // 이전 프레임 위치 (떨어지는 감지용)
        private Renderer itemRenderer; // 렌더러 컴포넌트
        private Rigidbody rb; // 리지드바디 컴포넌트
        private Coroutine groundReturnCoroutine; // 바닥 착지 후 풀 반환 코루틴
        private bool isInitialized = false; // 초기화 완료 여부
        
        // 모바일 플랫폼 감지
        private bool isMobilePlatform => Application.isMobilePlatform;
        
        // Public properties
        public Vector3 PreviousPosition
        {
            get { return previousPosition; }
            set { previousPosition = value; }
        }
        
        public bool HasLandedOnGround
        {
            get { return hasLandedOnGround; }
            set { hasLandedOnGround = value; }
        }
        
        public bool IsCollected => isCollected;
        public int PointValue => pointValue;

        #region Unity Lifecycle

        private void Start()
        {
            if (isInitialized) return; // 중복 초기화 방지
            
            InitializeComponents();
            SetupColliderForMobile();
            StartCoroutine(ItemAnimation());
            
            isInitialized = true;
        }
        
        private void Update()
        {
            if (!isCollected)
            {
                UpdateItemAnimation();
                CheckGroundCollision();
                previousPosition = transform.position;
            }
        }
        
        private void OnEnable()
        {
            if (isInitialized && !isCollected)
            {
                previousPosition = transform.position;
                StopAllCoroutines();
                
                if (!hasLandedOnGround)
                {
                    StartCoroutine(DelayedItemAnimation());
                }
                else
                {
                    StartCoroutine(ItemAnimation());
                }
            }
        }
        
        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        #endregion

        #region Initialization

        private void InitializeComponents() // 컴포넌트 초기화
        {
            InitializePhotonView();
            InitializeRendererAndRigidbody();
        }
        
        private void InitializePhotonView() // PhotonView 초기화
        {
            photonView = GetComponent<PhotonView>();
            if (photonView == null)
            {
                photonView = gameObject.AddComponent<PhotonView>();
            }
        }
        
        private void InitializeRendererAndRigidbody() // 렌더러와 리지드바디 초기화
        {
            itemRenderer = GetComponent<Renderer>();
            rb = GetComponent<Rigidbody>();
            
            if (rb != null)
            {
                rb.useGravity = true;
                rb.drag = 0.5f;
                rb.angularDrag = 0.5f;
                rb.mass = 1f;
                rb.maxAngularVelocity = 10f;
            }
        }
        
        private void SetupColliderForMobile() // 모바일용 콜라이더 설정
        {
            if (!isMobilePlatform) return;
            
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

        #region Item Animation

        private void UpdateItemAnimation() // 아이템 애니메이션 업데이트
        {
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
            
            if (!hasLandedOnGround)
            {
                LimitFallSpeed();
            }
        }
        
        private void LimitFallSpeed() // 낙하 속도 제한
        {
            if (rb != null && rb.velocity.y < -maxFallSpeed)
            {
                rb.velocity = new Vector3(rb.velocity.x, -maxFallSpeed, rb.velocity.z);
            }
        }
        
        private IEnumerator ItemAnimation() // 아이템 애니메이션 코루틴
        {
            while (!isCollected && gameObject.activeInHierarchy)
            {
                if (!hasLandedOnGround)
                {
                    yield return new WaitForSeconds(4f);
                    hasLandedOnGround = true;
                }
                else
                {
                    FixToGroundLevel();
                }
                
                yield return null;
            }
        }
        
        private IEnumerator DelayedItemAnimation() // 지연된 아이템 애니메이션
        {
            yield return new WaitForSeconds(6f);
            
            if (!isCollected && gameObject.activeInHierarchy)
            {
                hasLandedOnGround = true;
                StartCoroutine(ItemAnimation());
            }
        }

        #endregion

        #region Ground Collision

        private void CheckGroundCollision() // 바닥 충돌 체크
        {
            if (transform.position.y <= groundLevel && !hasLandedOnGround)
            {
                if (IsActuallyFalling())
                {
                    OnLandedOnGround();
                }
            }
        }
        
        private bool IsActuallyFalling() // 실제로 떨어지는 중인지 확인
        {
            if (rb != null && rb.velocity.y < 0)
            {
                return true;
            }
            else if (rb == null && previousPosition.y > transform.position.y)
            {
                return true;
            }
            
            return false;
        }
        
        private void OnLandedOnGround() // 바닥 착지 처리
        {
            hasLandedOnGround = true;
            
            FixToGroundLevel();
            ApplyBounceEffect();
            
            groundReturnCoroutine = StartCoroutine(ReturnToPoolAfterGroundDelay());
        }
        
        private void FixToGroundLevel() // 바닥 레벨로 위치 고정
        {
            Vector3 currentPos = transform.position;
            currentPos.y = groundLevel;
            transform.position = currentPos;
        }
        
        private void ApplyBounceEffect() // 바운스 효과 적용
        {
            if (rb != null)
            {
                rb.velocity = new Vector3(rb.velocity.x, bounceForce, rb.velocity.z);
            }
        }
        
        private IEnumerator ReturnToPoolAfterGroundDelay() // 바닥 착지 후 풀 반환 지연
        {
            float currentGroundReturnDelay = isMobilePlatform ? mobileGroundReturnDelay : groundReturnDelay;
            
            yield return new WaitForSeconds(currentGroundReturnDelay);
            
            if (returnPool != null)
            {
                ReturnToPool();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region Collection

        public void Collect() // 아이템 수집
        {
            if (isCollected && !isMobilePlatform) return;
            
            isCollected = true;
            
            if (photonView != null && photonView.IsMine)
            {
                photonView.RPC(nameof(CollectRPC), RpcTarget.All);
            }
            else if (photonView == null)
            {
                CollectLocal();
            }
            else
            {
                CollectLocal();
            }
        }
        
        [PunRPC]
        private void CollectRPC() // RPC: 아이템 수집
        {
            CollectLocal();
        }
        
        private void CollectLocal() // 로컬 아이템 수집 처리
        {
            if (isCollected) return;
            
            StopGroundReturnCoroutine();
            PlayCollectEffect();
            
            float returnDelay = isMobilePlatform ? 0.1f : 0.5f;
            
            if (returnPool != null)
            {
                ReturnToPool(returnDelay);
            }
            else
            {
                DestroyItemWithDelay(returnDelay);
            }
        }
        
        private void StopGroundReturnCoroutine() // 바닥 반환 코루틴 중지
        {
            if (groundReturnCoroutine != null)
            {
                StopCoroutine(groundReturnCoroutine);
                groundReturnCoroutine = null;
            }
        }
        
        private void DestroyItemWithDelay(float delay) // 지연 시간 후 아이템 파괴
        {
            gameObject.SetActive(false);
            
            PhotonView photonView = GetComponent<PhotonView>();
            
            if (photonView != null && PhotonNetwork.IsMasterClient)
            {
                if (gameObject != null)
                {
                    PhotonNetwork.Destroy(gameObject);
                }
            }
            else
            {
                if (gameObject != null)
                {
                    Destroy(gameObject, delay);
                }
            }
        }

        #endregion

        #region Effects

        private void PlayCollectEffect() // 수집 효과 재생
        {
            PlayCollectParticle();
            PlayCollectSound();
            StartCoroutine(FadeOutAnimation());
        }
        
        private void PlayCollectParticle() // 수집 파티클 재생
        {
            if (collectParticle != null)
            {
                GameObject effect = Instantiate(collectParticle, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }
        
        private void PlayCollectSound() // 수집 사운드 재생
        {
            string soundToPlay = GetCollectSoundName();
            if (!string.IsNullOrEmpty(soundToPlay) && Manager.Audio != null)
            {
                Manager.Audio.SfxPlay(soundToPlay, transform);
            }
        }
        
        private string GetCollectSoundName() // 수집 사운드 이름 가져오기
        {
            return collectSoundName;
        }
        
        private IEnumerator FadeOutAnimation() // 페이드 아웃 애니메이션
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

        #endregion

        #region Reset and Pool Management

        public void ResetItem() // 아이템 리셋
        {
            isCollected = false;
            hasLandedOnGround = false;
            
            ResetRigidbody();
            ResetVisualEffects();
            StopGroundReturnCoroutine();
            
            gameObject.SetActive(true);
        }
        
        private void ResetRigidbody() // 리지드바디 리셋
        {
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
            }
        }
        
        private void ResetVisualEffects() // 시각적 효과 리셋
        {
            if (itemRenderer != null)
            {
                Color originalColor = itemRenderer.material.color;
                originalColor.a = 1f;
                itemRenderer.material.color = originalColor;
            }
        }
        
        public void ReturnToPoolWithDelay(float delay) // 지연 시간 후 풀로 반환
        {
            if (groundReturnCoroutine != null)
            {
                StopCoroutine(groundReturnCoroutine);
            }
            groundReturnCoroutine = StartCoroutine(ReturnToPoolDelayed(delay));
        }
        
        private IEnumerator ReturnToPoolDelayed(float delay) // 지연된 풀 반환
        {
            yield return new WaitForSeconds(delay);
            
            if (returnPool != null && returnPool.gameObject != null && returnPool.gameObject.activeInHierarchy)
            {
                returnPool.ReturnToPool(this);
            }
            
            groundReturnCoroutine = null;
        }
        
        public new void StopAllCoroutines() // 모든 코루틴 중지
        {
            if (groundReturnCoroutine != null)
            {
                StopCoroutine(groundReturnCoroutine);
                groundReturnCoroutine = null;
            }
            
            base.StopAllCoroutines();
        }

        #endregion

        #region Configuration

        public void SetItemConfiguration(ItemConfiguration config) // 아이템 설정 변경
        {
            itemConfiguration = config;
        }
        
        public void SetGroundLevel(float level) // 바닥 레벨 설정
        {
            groundLevel = level;
        }

        #endregion
    }
} 