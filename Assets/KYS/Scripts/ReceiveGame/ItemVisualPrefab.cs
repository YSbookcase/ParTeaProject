using UnityEngine;

namespace KYS
{
    /// <summary>
    /// 아이템 시각적 프리팹을 생성하기 위한 헬퍼 클래스
    /// </summary>
    public class ItemVisualPrefab : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private ReceiveGameManager.ItemType itemType;
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
                Debug.LogError($"[ItemVisualPrefab] {itemType} 아이템에 Renderer가 없습니다.");
                return;
            }
            
            // 머티리얼 인스턴스 생성
            originalMaterial = itemRenderer.material;
            instanceMaterial = new Material(originalMaterial);
            itemRenderer.material = instanceMaterial;
            
            // 색상 설정
            instanceMaterial.color = itemColor;
            
            // 발광 설정
            if (useEmission)
            {
                instanceMaterial.EnableKeyword("_EMISSION");
                instanceMaterial.SetColor("_EmissionColor", emissionColor * emissionIntensity);
            }
            
            // 스케일 설정
            transform.localScale = scale;
            
            // 파티클 효과 설정
            if (useParticles && particlePrefab != null)
            {
                GameObject particles = Instantiate(particlePrefab, transform);
                particles.transform.localPosition = Vector3.zero;
            }
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
        /// 아이템 타입에 따른 기본 설정을 적용합니다.
        /// </summary>
        public void ApplyDefaultSettings()
        {
            switch (itemType)
            {
                case ReceiveGameManager.ItemType.Normal:
                    itemColor = Color.white;
                    useEmission = false;
                    break;
                    
                case ReceiveGameManager.ItemType.Bonus:
                    itemColor = Color.yellow;
                    useEmission = true;
                    emissionColor = Color.yellow;
                    emissionIntensity = 2f;
                    break;
                    
                case ReceiveGameManager.ItemType.Speed:
                    itemColor = Color.blue;
                    useEmission = true;
                    emissionColor = Color.cyan;
                    emissionIntensity = 1.5f;
                    break;
                    
                case ReceiveGameManager.ItemType.Slow:
                    itemColor = Color.red;
                    useEmission = true;
                    emissionColor = Color.red;
                    emissionIntensity = 1.5f;
                    break;
                    
                case ReceiveGameManager.ItemType.Magnet:
                    itemColor = Color.green;
                    useEmission = true;
                    emissionColor = Color.green;
                    emissionIntensity = 1.5f;
                    break;
            }
        }
        
        /// <summary>
        /// 런타임에 색상을 변경합니다.
        /// </summary>
        /// <param name="newColor">새로운 색상</param>
        public void SetColor(Color newColor)
        {
            itemColor = newColor;
            if (instanceMaterial != null)
            {
                instanceMaterial.color = itemColor;
            }
        }
        
        /// <summary>
        /// 런타임에 발광을 설정합니다.
        /// </summary>
        /// <param name="enabled">발광 활성화 여부</param>
        /// <param name="color">발광 색상</param>
        /// <param name="intensity">발광 강도</param>
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