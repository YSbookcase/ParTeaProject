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
        
        [Header("Effects")]
        [SerializeField] private GameObject collectParticle;
        [SerializeField] private string collectSoundName = "SFX_NormalItem"; // AudioData 에셋 이름으로 변경
        
        private Vector3 startPosition;
        private bool isCollected = false;
        private bool hasHitGround = false;
        private Renderer itemRenderer;
        private Rigidbody rb;
        private Coroutine returnCoroutine;
        
        public bool IsCollected => isCollected;
        public int PointValue => pointValue;
        
        private void Start()
        {
            // PhotonView 컴포넌트 확인 및 추가
            photonView = GetComponent<PhotonView>();
            if (photonView == null)
            {
                photonView = gameObject.AddComponent<PhotonView>();
                Debug.Log("CollectibleItem에 PhotonView 컴포넌트를 추가했습니다.");
            }
            
            startPosition = transform.position;
            itemRenderer = GetComponent<Renderer>();
            rb = GetComponent<Rigidbody>();
            
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
            //Debug.Log($"아이템이 바닥에 닿았습니다: {transform.position}");
            
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
            yield return new WaitForSeconds(returnDelay);
            
            // 오브젝트 풀로 반환
            if (returnPool != null)
            {
                ReturnToPool();
            }
            else
            {
                Debug.LogWarning($"[CollectibleItem] returnPool이 null입니다. 오브젝트를 파괴합니다: {gameObject.name}");
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
            if (isCollected) return;
            
            // 즉시 수집 상태로 변경하여 중복 수집 방지
            isCollected = true;
            
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
                Debug.LogWarning($"[CollectibleItem] returnPool이 null입니다. 오브젝트를 파괴합니다: {gameObject.name}");
                // 중복 파괴 방지를 위해 즉시 비활성화
                gameObject.SetActive(false);
                if (PhotonNetwork.IsMasterClient)
                {
                    // 이미 파괴되었는지 확인 후 파괴
                    if (gameObject != null)
                    {
                        PhotonNetwork.Destroy(gameObject);
                    }
                }
                else
                {
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
            
            // 수집 사운드 - AudioManager 시스템 사용
            if (!string.IsNullOrEmpty(collectSoundName) && Manager.Audio != null)
            {
                Manager.Audio.SfxPlay(collectSoundName, transform);
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