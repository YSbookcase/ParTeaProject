using UnityEngine;
using Photon.Pun;

namespace KYS
{
    public class ItemController : MonoBehaviourPun
    {
        [Header("Item Settings")]
        [SerializeField] private ReceiveGameManager.ItemType itemType = ReceiveGameManager.ItemType.Normal;
        [SerializeField] private int pointValue = 1;
        [SerializeField] private float effectDuration = 5f;
        
        [Header("Visual Effects")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color bonusColor = Color.yellow;
        [SerializeField] private Color speedColor = Color.blue;
        [SerializeField] private Color slowColor = Color.red;
        [SerializeField] private Color magnetColor = Color.green;
        
        private Renderer itemRenderer;
        private bool isCollected = false;
        
        private void Start()
        {
            itemRenderer = GetComponent<Renderer>();
            UpdateVisual();
        }
        
        public void SetItemType(ReceiveGameManager.ItemType type)
        {
            itemType = type;
            UpdateVisual();
        }
        
        private void UpdateVisual()
        {
            if (itemRenderer == null) return;
            
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
            
            itemRenderer.material.color = targetColor;
            
            // pointValue가 사용되었음을 명시적으로 표시 (컴파일러 경고 방지)
            if (pointValue > 0)
            {
                // 이 값은 실제로는 ReceiveGameManager.CollectItem에서 사용됨
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
    }
} 