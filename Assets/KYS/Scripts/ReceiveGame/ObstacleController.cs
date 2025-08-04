using UnityEngine;
using Photon.Pun;

namespace KYS
{
    /// <summary>
    /// 방해물의 충돌 효과를 처리하는 컴포넌트
    /// </summary>
    public class ObstacleController : MonoBehaviourPun
    {
        [Header("Obstacle Settings")]
        // 사용되지 않는 변수들 - 주석 처리
        // [SerializeField] private float slowEffectDuration = 3f; // 느려지는 효과 지속 시간
        // [SerializeField] private float slowEffectMultiplier = 0.5f; // 느려지는 효과 배율
        // [SerializeField] private int damagePoints = -2; // 점수 감점
        
        [Header("Effects")]
        [SerializeField] private GameObject hitEffect;
        [SerializeField] private AudioClip hitSound;
        
        private bool isHit = false;
        private AudioSource audioSource;
        
        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            // 이미 충돌한 방해물이면 무시
            if (isHit) return;
            
            // 플레이어와 충돌했는지 확인
            ReceiveGamePlayer player = collision.gameObject.GetComponent<ReceiveGamePlayer>();
            if (player != null && player.photonView.IsMine)
            {
                // 로컬 플레이어만 처리
                HandlePlayerCollision(player);
            }
        }
        
        private void HandlePlayerCollision(ReceiveGamePlayer player)
        {
            // 이미 충돌한 방해물이면 무시
            if (isHit) return;
            
            isHit = true;
            
            Debug.Log($"[ObstacleController] 플레이어 {player.GetPlayerActorNumber()}가 방해물과 충돌!");
            
            // 부정적인 효과는 현재 제외
            // player.ApplySlowEffect(slowEffectDuration);
            // gameManager.ApplyObstaclePenalty(player.GetPlayerActorNumber(), damagePoints);
            
            // 충돌 효과만 재생
            PlayHitEffect();
            
            // 방해물은 사라지지 않고 계속 존재
            // StartCoroutine(DeactivateObstacle()); // 제거됨
        }
        
        private void PlayHitEffect()
        {
            // 충돌 파티클 효과
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            
            // 충돌 사운드
            if (hitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
        }
        
        // 방해물 비활성화 메서드 - 현재 사용하지 않음
        /*
        private System.Collections.IEnumerator DeactivateObstacle()
        {
            // 방해물을 비활성화하여 중복 충돌 방지
            gameObject.SetActive(false);
            
            // 2초 후 다시 활성화 (선택사항)
            yield return new WaitForSeconds(2f);
            
            // 방해물을 다시 활성화하거나 완전히 제거
            // gameObject.SetActive(true); // 다시 활성화하려면 주석 해제
        }
        */
        
        public void ResetObstacle()
        {
            isHit = false;
            gameObject.SetActive(true);
        }
    }
} 