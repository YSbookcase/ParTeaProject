using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace KYS.Editor
{
    public class ItemConfigurationCreator : EditorWindow
    {
        private ItemConfiguration itemConfiguration;
        private Vector2 scrollPosition;
        private bool showAdvancedSettings = false;
        
        [MenuItem("KYS/Create Item Configuration")]
        public static void ShowWindow()
        {
            GetWindow<ItemConfigurationCreator>("Item Configuration Creator");
        }
        
        private void OnEnable()
        {
            // 기존 설정 파일 로드
            itemConfiguration = Resources.Load<ItemConfiguration>("ItemConfiguration");
            
            if (itemConfiguration == null)
            {
                // 새 설정 파일 생성
                itemConfiguration = CreateInstance<ItemConfiguration>();
                
                // Resources 폴더가 없으면 생성
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                
                // 파일 저장
                AssetDatabase.CreateAsset(itemConfiguration, "Assets/Resources/ItemConfiguration.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Debug.Log("새로운 ItemConfiguration 파일이 생성되었습니다: Assets/Resources/ItemConfiguration.asset");
            }
        }
        
        private void OnGUI()
        {
            if (itemConfiguration == null)
            {
                EditorGUILayout.HelpBox("ItemConfiguration을 로드할 수 없습니다.", MessageType.Error);
                return;
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField("Item Configuration Creator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 기본 설정
            EditorGUILayout.LabelField("기본 설정", EditorStyles.boldLabel);
            
            // 모든 아이템 타입에 대한 설정 생성
            var itemTypes = System.Enum.GetValues(typeof(ItemType));
            
            for (int i = 0; i < itemTypes.Length; i++)
            {
                ItemType itemType = (ItemType)itemTypes.GetValue(i);
                CreateItemTypeSection(itemType);
            }
            
            EditorGUILayout.Space();
            
            // 고급 설정
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "고급 설정");
            if (showAdvancedSettings)
            {
                ShowAdvancedSettings();
            }
            
            EditorGUILayout.EndScrollView();
            
            // 저장 버튼
            if (GUILayout.Button("설정 저장"))
            {
                SaveConfiguration();
            }
        }
        
        private void CreateItemTypeSection(ItemType itemType)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"{itemType} 아이템 설정", EditorStyles.boldLabel);
            
            // 해당 타입의 설정 찾기 또는 생성
            ItemConfig config = GetOrCreateItemConfig(itemType);
            
            if (config != null)
            {
                EditorGUI.BeginChangeCheck();
                
                // 시각적 설정
                EditorGUILayout.LabelField("시각적 설정", EditorStyles.boldLabel);
                config.itemColor = EditorGUILayout.ColorField("아이템 색상", config.itemColor);
                config.scale = EditorGUILayout.Vector3Field("크기", config.scale);
                config.useEmission = EditorGUILayout.Toggle("발광 효과", config.useEmission);
                
                if (config.useEmission)
                {
                    config.emissionColor = EditorGUILayout.ColorField("발광 색상", config.emissionColor);
                    config.emissionIntensity = EditorGUILayout.Slider("발광 강도", config.emissionIntensity, 0f, 5f);
                }
                
                // 게임플레이 설정
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("게임플레이 설정", EditorStyles.boldLabel);
                config.pointValue = EditorGUILayout.IntField("점수", config.pointValue);
                config.effectDuration = EditorGUILayout.FloatField("효과 지속시간", config.effectDuration);
                
                // 시각적 프리팹 설정
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("시각적 프리팹", EditorStyles.boldLabel);
                config.visualPrefab = (GameObject)EditorGUILayout.ObjectField("시각적 프리팹", config.visualPrefab, typeof(GameObject), false);
                
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(itemConfiguration);
                }
            }
        }
        
        private ItemConfig GetOrCreateItemConfig(ItemType itemType)
        {
            // 기존 설정 찾기
            foreach (var config in itemConfiguration.itemConfigs)
            {
                if (config.itemType == itemType)
                {
                    return config;
                }
            }
            
            // 새 설정 생성
            ItemConfig newConfig = CreateDefaultItemConfig(itemType);
            itemConfiguration.itemConfigs.Add(newConfig);
            EditorUtility.SetDirty(itemConfiguration);
            
            return newConfig;
        }
        
        private static ItemConfig CreateDefaultItemConfig(ItemType itemType)
        {
            ItemConfig config = new ItemConfig();
            config.itemType = itemType;
            
            // 기본값 설정
            switch (itemType)
            {
                case ItemType.Normal:
                    config.itemColor = Color.white;
                    config.scale = Vector3.one * 1.5f;
                    config.pointValue = 1;
                    config.effectDuration = 0f;
                    config.useEmission = false;
                    break;
                    
                case ItemType.Bonus:
                    config.itemColor = Color.yellow;
                    config.scale = Vector3.one * 2.0f;
                    config.pointValue = 3;
                    config.effectDuration = 0f;
                    config.useEmission = true;
                    config.emissionColor = Color.yellow;
                    config.emissionIntensity = 2f;
                    break;
                    
                case ItemType.Speed:
                    config.itemColor = Color.blue;
                    config.scale = Vector3.one * 1.5f;
                    config.pointValue = 1;
                    config.effectDuration = 5f;
                    config.useEmission = true;
                    config.emissionColor = Color.cyan;
                    config.emissionIntensity = 1.5f;
                    break;
                    
                case ItemType.Slow:
                    config.itemColor = Color.red;
                    config.scale = Vector3.one * 1.2f;
                    config.pointValue = 1;
                    config.effectDuration = 5f;
                    config.useEmission = true;
                    config.emissionColor = Color.red;
                    config.emissionIntensity = 1.5f;
                    break;
                    
                case ItemType.Magnet:
                    config.itemColor = Color.green;
                    config.scale = Vector3.one * 1.5f;
                    config.pointValue = 1;
                    config.effectDuration = 5f;
                    config.useEmission = true;
                    config.emissionColor = Color.green;
                    config.emissionIntensity = 1.5f;
                    break;
            }
            
            return config;
        }
        
        private void ShowAdvancedSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("고급 설정", EditorStyles.boldLabel);
            
            // 모든 설정 초기화
            if (GUILayout.Button("모든 설정을 기본값으로 초기화"))
            {
                if (EditorUtility.DisplayDialog("초기화 확인", 
                    "모든 아이템 설정을 기본값으로 초기화하시겠습니까?\n이 작업은 되돌릴 수 없습니다.", 
                    "초기화", "취소"))
                {
                    ResetAllConfigurations();
                }
            }
            
            // 설정 검증
            if (GUILayout.Button("설정 검증"))
            {
                ValidateConfiguration();
            }
        }
        
        private void ResetAllConfigurations()
        {
            itemConfiguration.itemConfigs.Clear();
            
            // 모든 아이템 타입에 대해 기본 설정 생성
            var itemTypes = System.Enum.GetValues(typeof(ItemType));
            
            for (int i = 0; i < itemTypes.Length; i++)
            {
                ItemType itemType = (ItemType)itemTypes.GetValue(i);
                ItemConfig config = CreateDefaultItemConfig(itemType);
                itemConfiguration.itemConfigs.Add(config);
            }
            
            EditorUtility.SetDirty(itemConfiguration);
            Debug.Log("모든 아이템 설정이 기본값으로 초기화되었습니다.");
        }
        
        private void ValidateConfiguration()
        {
            bool isValid = true;
            List<string> errors = new List<string>();
            
            // 모든 아이템 타입이 설정되어 있는지 확인
            var itemTypes = System.Enum.GetValues(typeof(ItemType));
            
            for (int i = 0; i < itemTypes.Length; i++)
            {
                ItemType itemType = (ItemType)itemTypes.GetValue(i);
                bool found = false;
                
                foreach (var config in itemConfiguration.itemConfigs)
                {
                    if (config.itemType == itemType)
                    {
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    errors.Add($"{itemType} 타입의 설정이 없습니다.");
                    isValid = false;
                }
            }
            
            // 설정값 검증
            foreach (var config in itemConfiguration.itemConfigs)
            {
                if (config.pointValue < 0)
                {
                    errors.Add($"{config.itemType}의 점수가 음수입니다: {config.pointValue}");
                    isValid = false;
                }
                
                if (config.effectDuration < 0)
                {
                    errors.Add($"{config.itemType}의 효과 지속시간이 음수입니다: {config.effectDuration}");
                    isValid = false;
                }
                
                if (config.scale.x <= 0 || config.scale.y <= 0 || config.scale.z <= 0)
                {
                    errors.Add($"{config.itemType}의 크기가 잘못되었습니다: {config.scale}");
                    isValid = false;
                }
            }
            
            if (isValid)
            {
                EditorUtility.DisplayDialog("검증 완료", "모든 설정이 올바릅니다.", "확인");
            }
            else
            {
                string errorMessage = "설정에 문제가 있습니다:\n\n" + string.Join("\n", errors);
                EditorUtility.DisplayDialog("검증 실패", errorMessage, "확인");
            }
        }
        
        private void SaveConfiguration()
        {
            if (itemConfiguration != null)
            {
                EditorUtility.SetDirty(itemConfiguration);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("ItemConfiguration이 저장되었습니다.");
            }
        }
    }
} 