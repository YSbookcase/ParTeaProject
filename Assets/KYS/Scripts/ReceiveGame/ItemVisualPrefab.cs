using UnityEngine;

namespace KYS
{
    /// <summary>
    /// 아이템 시각적 프리팹을 위한 헬퍼 스크립트
    /// 각 아이템 타입별 고유한 외형을 설정할 수 있습니다.
    /// </summary>
    public class ItemVisualPrefab : MonoBehaviour
    {
        [Header("Item Type")]
        [SerializeField] private ItemType itemType;
        [SerializeField] private Color itemColor = Color.white;
        [SerializeField] private Vector3 scale = Vector3.one;
        [SerializeField] private bool useEmission = false;
        [SerializeField] private Color emissionColor = Color.white;
        [SerializeField] private float emissionIntensity = 1f;
        
        [Header("Animation Settings")]
        [SerializeField] private bool useRotation = true;
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private float rotationSpeed = 60f;
        
        [Header("Particle Effects")]
        [SerializeField] private bool useParticles = false;
        [SerializeField] private GameObject particlePrefab;
        
        private Renderer itemRenderer;
        private Material originalMaterial;
        private Material instanceMaterial;
        
        private void Start()
        {
            SetupVisual();
        }
        
        private void Update()
        {
            if (useRotation)
            {
                transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
            }
        }
        
        private void SetupVisual()
        {
            // 렌더러 가져오기
            itemRenderer = GetComponent<Renderer>();
            if (itemRenderer == null)
            {
                Debug.LogWarning($"[ItemVisualPrefab] {itemType} 프리팹에 Renderer가 없습니다.");
                return;
            }
            
            // 머티리얼 인스턴스 생성
            originalMaterial = itemRenderer.material;
            instanceMaterial = new Material(originalMaterial);
            itemRenderer.material = instanceMaterial;
            
            // 색상 적용
            instanceMaterial.color = itemColor;
            
            // Emission 설정
            if (useEmission)
            {
                instanceMaterial.EnableKeyword("_EMISSION");
                instanceMaterial.SetColor("_EmissionColor", emissionColor * emissionIntensity);
            }
            
            // 스케일 적용
            transform.localScale = scale;
            
            // 파티클 효과 추가
            if (useParticles && particlePrefab != null)
            {
                GameObject particleInstance = Instantiate(particlePrefab, transform);
                particleInstance.transform.localPosition = Vector3.zero;
            }
            
            // 기본 설정 적용
            ApplyDefaultSettings();
        }
        
        private void OnDestroy()
        {
            // 머티리얼 인스턴스 정리
            if (instanceMaterial != null)
            {
                DestroyImmediate(instanceMaterial);
            }
        }
        
        /// <summary>
        /// 아이템 타입에 따른 기본 설정 적용
        /// </summary>
        public void ApplyDefaultSettings()
        {
            switch (itemType)
            {
                case ItemType.Normal:
                    itemColor = Color.white;
                    scale = Vector3.one * 1.5f; // 기본 크기 증가
                    useEmission = false;
                    rotationSpeed = 60f;
                    break;
                    
                case ItemType.Bonus:
                    itemColor = Color.yellow;
                    scale = Vector3.one * 1.8f; // 보너스 아이템은 더 크게
                    useEmission = true;
                    emissionColor = Color.yellow;
                    emissionIntensity = 0.5f;
                    rotationSpeed = 90f;
                    break;
                    
                case ItemType.Speed:
                    itemColor = Color.blue;
                    scale = Vector3.one * 1.5f; // 기본 크기 증가
                    useEmission = true;
                    emissionColor = Color.cyan;
                    emissionIntensity = 0.8f;
                    rotationSpeed = 120f;
                    break;
                    
                case ItemType.Slow:
                    itemColor = Color.red;
                    scale = Vector3.one * 1.2f; // 느린 아이템은 약간 작게
                    useEmission = true;
                    emissionColor = Color.red;
                    emissionIntensity = 0.3f;
                    rotationSpeed = 30f;
                    break;
                    
                case ItemType.Magnet:
                    itemColor = Color.green;
                    scale = Vector3.one * 1.5f; // 기본 크기 증가
                    useEmission = true;
                    emissionColor = Color.green;
                    emissionIntensity = 0.6f;
                    rotationSpeed = 60f;
                    break;
            }
            
            // 설정 적용
            if (itemRenderer != null && instanceMaterial != null)
            {
                instanceMaterial.color = itemColor;
                
                if (useEmission)
                {
                    instanceMaterial.EnableKeyword("_EMISSION");
                    instanceMaterial.SetColor("_EmissionColor", emissionColor * emissionIntensity);
                }
                else
                {
                    instanceMaterial.DisableKeyword("_EMISSION");
                }
            }
            
            transform.localScale = scale;
        }
        
        /// <summary>
        /// 런타임에 색상 변경
        /// </summary>
        public void SetColor(Color newColor)
        {
            itemColor = newColor;
            if (instanceMaterial != null)
            {
                instanceMaterial.color = itemColor;
            }
        }
        
        /// <summary>
        /// 런타임에 Emission 설정 변경
        /// </summary>
        public void SetEmission(bool enabled, Color color, float intensity)
        {
            useEmission = enabled;
            emissionColor = color;
            emissionIntensity = intensity;
            
            if (instanceMaterial != null)
            {
                if (enabled)
                {
                    instanceMaterial.EnableKeyword("_EMISSION");
                    instanceMaterial.SetColor("_EmissionColor", color * intensity);
                }
                else
                {
                    instanceMaterial.DisableKeyword("_EMISSION");
                }
            }
        }
    }
} 