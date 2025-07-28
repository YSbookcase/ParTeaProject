using System.Collections;
using UnityEngine;

namespace KYS
{
    public class CollectibleItem : MonoBehaviour
    {
        [Header("Item Settings")]
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.5f;
        [SerializeField] private int pointValue = 1;
        
        [Header("Effects")]
        [SerializeField] private GameObject collectParticle;
        [SerializeField] private AudioClip collectSound;
        
        private Vector3 startPosition;
        private bool isCollected = false;
        private Renderer itemRenderer;
        private AudioSource audioSource;
        
        public bool IsCollected => isCollected;
        public int PointValue => pointValue;
        
        private void Start()
        {
            startPosition = transform.position;
            itemRenderer = GetComponent<Renderer>();
            audioSource = GetComponent<AudioSource>();
            
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // 아이템 애니메이션 시작
            StartCoroutine(ItemAnimation());
        }
        
        private void Update()
        {
            if (!isCollected)
            {
                // 회전 애니메이션
                transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
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
            
            // 수집 효과 재생
            PlayCollectEffect();
            
            // 아이템 비활성화
            StartCoroutine(DisableAfterEffect());
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
            transform.position = startPosition;
            transform.rotation = Quaternion.identity;
            
            if (itemRenderer != null && itemRenderer.material != null)
            {
                Color color = itemRenderer.material.color;
                itemRenderer.material.color = new Color(color.r, color.g, color.b, 1f);
            }
            
            gameObject.SetActive(true);
            StartCoroutine(ItemAnimation());
        }
    }
} 