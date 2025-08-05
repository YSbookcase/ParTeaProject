using UnityEngine;

namespace KYS
{
    /// <summary>
    /// 플레이어 주변에 표시되는 이펙트를 관리하는 스크립트입니다.
    /// MeshRenderer와 ParticleSystem을 모두 지원합니다.
    /// </summary>
    public class PlayerEffectController : MonoBehaviour
    {
        [Header("Effect Settings")]
        [SerializeField] private float rotationSpeed = 90f; // 회전 속도 (도/초)
        [SerializeField] private float pulseSpeed = 2f; // 펄스 속도
        [SerializeField] private float pulseScale = 0.2f; // 펄스 크기 변화량
        [SerializeField] private bool usePulse = true; // 펄스 효과 사용 여부
        [SerializeField] private bool useRotation = true; // 회전 효과 사용 여부
        
        [Header("Color Settings")]
        [SerializeField] private Color effectColor = Color.white; // 이펙트 색상
        [SerializeField] private bool useColorChange = false; // 색상 변화 사용 여부
        [SerializeField] private float colorChangeSpeed = 1f; // 색상 변화 속도
        
        [Header("Particle System Settings")]
        [SerializeField] private bool useParticleSystem = false; // 파티클 시스템 사용 여부
        [SerializeField] private bool autoStartParticles = true; // 자동으로 파티클 시작
        
        private Vector3 originalScale;
        private Renderer effectRenderer;
        private Material effectMaterial;
        private Color originalColor;
        private new ParticleSystem particleSystem;
        private ParticleSystem.MainModule particleMain;
        
        private void Start()
        {
            // 원본 크기 저장
            originalScale = transform.localScale;
            
            // 파티클 시스템 확인
            particleSystem = GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                useParticleSystem = true;
                particleMain = particleSystem.main;
                
                // 파티클 시스템이 있으면 자동 시작
                if (autoStartParticles)
                {
                    particleSystem.Play();
                }
                
                Debug.Log($"[PlayerEffectController] 파티클 시스템 감지됨: {gameObject.name}");
            }
            else
            {
                // 일반 렌더러와 머티리얼 가져오기
                effectRenderer = GetComponent<Renderer>();
                if (effectRenderer != null)
                {
                    effectMaterial = effectRenderer.material;
                    if (effectMaterial != null)
                    {
                        originalColor = effectMaterial.color;
                        if (useColorChange)
                        {
                            effectMaterial.color = effectColor;
                        }
                    }
                }
            }
            
            Debug.Log($"[PlayerEffectController] 이펙트 초기화 완료: {gameObject.name} (파티클: {useParticleSystem})");
        }
        
        private void Update()
        {
            // 회전 효과
            if (useRotation)
            {
                transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
            }
            
            // 펄스 효과 (파티클 시스템이 없을 때만)
            if (usePulse && !useParticleSystem)
            {
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
                transform.localScale = originalScale * pulse;
            }
            
            // 색상 변화 효과
            if (useColorChange)
            {
                if (useParticleSystem && particleSystem != null)
                {
                    // 파티클 시스템 색상 변화
                    float hue = (Time.time * colorChangeSpeed) % 1f;
                    Color newColor = Color.HSVToRGB(hue, 0.8f, 1f);
                    var main = particleSystem.main;
                    main.startColor = newColor;
                }
                else if (effectMaterial != null)
                {
                    // 일반 머티리얼 색상 변화
                    float hue = (Time.time * colorChangeSpeed) % 1f;
                    Color newColor = Color.HSVToRGB(hue, 0.8f, 1f);
                    effectMaterial.color = newColor;
                }
            }
        }
        
        /// <summary>
        /// 이펙트 색상을 설정합니다.
        /// </summary>
        /// <param name="color">설정할 색상</param>
        public void SetEffectColor(Color color)
        {
            effectColor = color;
            
            if (useParticleSystem && particleSystem != null)
            {
                var main = particleSystem.main;
                main.startColor = color;
            }
            else if (effectMaterial != null)
            {
                effectMaterial.color = color;
            }
        }
        
        /// <summary>
        /// 회전 속도를 설정합니다.
        /// </summary>
        /// <param name="speed">회전 속도 (도/초)</param>
        public void SetRotationSpeed(float speed)
        {
            rotationSpeed = speed;
        }
        
        /// <summary>
        /// 펄스 효과를 설정합니다.
        /// </summary>
        /// <param name="enabled">펄스 효과 활성화 여부</param>
        /// <param name="speed">펄스 속도</param>
        /// <param name="scale">펄스 크기 변화량</param>
        public void SetPulseEffect(bool enabled, float speed = 2f, float scale = 0.2f)
        {
            usePulse = enabled;
            pulseSpeed = speed;
            pulseScale = scale;
        }
        
        /// <summary>
        /// 색상 변화 효과를 설정합니다.
        /// </summary>
        /// <param name="enabled">색상 변화 활성화 여부</param>
        /// <param name="speed">색상 변화 속도</param>
        public void SetColorChangeEffect(bool enabled, float speed = 1f)
        {
            useColorChange = enabled;
            colorChangeSpeed = speed;
        }
        
        /// <summary>
        /// 파티클 시스템을 재시작합니다.
        /// </summary>
        public void RestartParticles()
        {
            if (particleSystem != null)
            {
                particleSystem.Stop();
                particleSystem.Play();
            }
        }
        
        /// <summary>
        /// 파티클 시스템을 중지합니다.
        /// </summary>
        public void StopParticles()
        {
            if (particleSystem != null)
            {
                particleSystem.Stop();
            }
        }
        
        /// <summary>
        /// 파티클 시스템을 일시정지합니다.
        /// </summary>
        public void PauseParticles()
        {
            if (particleSystem != null)
            {
                particleSystem.Pause();
            }
        }
        
        private void OnDestroy()
        {
            // 머티리얼 정리
            if (effectMaterial != null)
            {
                DestroyImmediate(effectMaterial);
            }
        }
    }
} 