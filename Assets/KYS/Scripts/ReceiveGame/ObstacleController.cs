using UnityEngine;
using Photon.Pun;

namespace KYS
{
    public class ObstacleController : MonoBehaviourPun
    {
        [Header("Obstacle Settings")]
        [SerializeField] private float knockbackForce = 5f;
        [SerializeField] private float stunDuration = 2f;
        
        [Header("Visual Effects")]
        [SerializeField] private Color obstacleColor = Color.red;
        [SerializeField] private float rotationSpeed = 50f;
        
        private Renderer obstacleRenderer;
        private bool hasHitPlayer = false;
        
        private void Start()
        {
            obstacleRenderer = GetComponent<Renderer>();
            if (obstacleRenderer != null)
            {
                obstacleRenderer.material.color = obstacleColor;
            }
        }
        
        private void Update()
        {
            // 장애물 회전
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (hasHitPlayer) return;
            
            // 플레이어와 충돌했는지 확인
            if (other.CompareTag("Player"))
            {
                hasHitPlayer = true;
                
                // 플레이어의 ReceiveGamePlayer 컴포넌트 찾기
                ReceiveGamePlayer player = other.GetComponent<ReceiveGamePlayer>();
                if (player != null)
                {
                    // 장애물 효과 적용
                    ApplyObstacleEffect(player, other.transform.position);
                }
                
                // 장애물 제거
                Destroy(gameObject);
            }
        }
        
        private void ApplyObstacleEffect(ReceiveGamePlayer player, Vector3 hitPosition)
        {
            // 넉백 효과
            Vector3 knockbackDirection = (player.transform.position - hitPosition).normalized;
            player.transform.position += knockbackDirection * knockbackForce;
            
            // 슬로우 효과 적용 (ReceiveGamePlayer의 메서드 사용)
            player.ApplySlowEffect(stunDuration);
            
            Debug.Log($"플레이어가 장애물에 부딪혔습니다! 슬로우 효과 {stunDuration}초 적용");
        }
    }
} 