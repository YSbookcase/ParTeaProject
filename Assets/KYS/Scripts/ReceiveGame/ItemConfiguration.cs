using UnityEngine;
using System.Collections.Generic;

namespace KYS
{
    [System.Serializable]
    public class ItemConfig
    {
        [Header("Item Type")]
        public ItemType itemType;
        
        [Header("Visual Settings")]
        public GameObject visualPrefab; // 각 아이템 타입별 고유한 시각적 프리팹
        public Color itemColor = Color.white; // 기본 색상 (프리팹이 없을 때 사용)
        public Vector3 scale = Vector3.one; // 아이템 크기
        
        [Header("Emission Settings")]
        public bool useEmission = false; // 발광 효과 사용 여부
        public Color emissionColor = Color.white; // 발광 색상
        public float emissionIntensity = 1f; // 발광 강도
        
        [Header("Gameplay Settings")]
        public int pointValue = 1;
        public float effectDuration = 5f;
        
        [Header("Physics Settings")]
        public float bounceForce = 2f;
        public float maxFallSpeed = 12f;
        public float rotationSpeed = 60f;
        public float bobSpeed = 1.5f;
        public float bobHeight = 0.3f;
    }

    [CreateAssetMenu(fileName = "ItemConfiguration", menuName = "KYS/Item Configuration")]
    public class ItemConfiguration : ScriptableObject
    {
        [Header("Item Configurations")]
        public List<ItemConfig> itemConfigs = new List<ItemConfig>(); // 배열에서 List로 변경
        
        [Header("Default Settings")]
        [SerializeField] private GameObject defaultVisualPrefab; // 기본 시각적 프리팹
        
        /// <summary>
        /// 특정 아이템 타입에 대한 설정을 가져옵니다.
        /// </summary>
        /// <param name="itemType">아이템 타입</param>
        /// <returns>아이템 설정, 없으면 null</returns>
        public ItemConfig GetItemConfig(ItemType itemType)
        {
            foreach (var config in itemConfigs)
            {
                if (config.itemType == itemType)
                {
                    return config;
                }
            }
            
            Debug.LogWarning($"[ItemConfiguration] {itemType} 타입에 대한 설정을 찾을 수 없습니다.");
            return null;
        }
        
        /// <summary>
        /// 모든 아이템 설정을 가져옵니다.
        /// </summary>
        /// <returns>모든 아이템 설정 배열</returns>
        public ItemConfig[] GetAllItemConfigs()
        {
            return itemConfigs.ToArray();
        }
        
        /// <summary>
        /// 기본 시각적 프리팹을 가져옵니다.
        /// </summary>
        /// <returns>기본 시각적 프리팹</returns>
        public GameObject GetDefaultVisualPrefab()
        {
            return defaultVisualPrefab;
        }
        
        /// <summary>
        /// 설정이 유효한지 확인합니다.
        /// </summary>
        /// <returns>유효하면 true</returns>
        public bool IsValid()
        {
            if (itemConfigs == null || itemConfigs.Count == 0)
            {
                Debug.LogError("[ItemConfiguration] 아이템 설정이 없습니다.");
                return false;
            }
            
            // 모든 아이템 타입이 설정되어 있는지 확인
            foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType)))
            {
                bool found = false;
                foreach (var config in itemConfigs)
                {
                    if (config.itemType == itemType)
                    {
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    Debug.LogWarning($"[ItemConfiguration] {itemType} 타입에 대한 설정이 없습니다.");
                }
            }
            
            return true;
        }
    }
} 