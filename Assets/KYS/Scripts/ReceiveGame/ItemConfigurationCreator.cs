using UnityEngine;
using UnityEditor;

namespace KYS
{
    /// <summary>
    /// ItemConfiguration 에셋을 생성하기 위한 에디터 도구
    /// </summary>
    public class ItemConfigurationCreator
    {
        [MenuItem("KYS/Create Item Configuration")]
        public static void CreateItemConfiguration()
        {
            // ItemConfiguration 에셋 생성
            ItemConfiguration config = ScriptableObject.CreateInstance<ItemConfiguration>();
            
            // 기본 설정으로 초기화
            InitializeDefaultConfig(config);
            
            // Resources 폴더에 저장
            string path = "Assets/KYS/Resources/ItemConfiguration.asset";
            
            // 디렉토리가 없으면 생성
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[ItemConfigurationCreator] ItemConfiguration이 생성되었습니다: {path}");
            
            // 생성된 에셋을 선택
            Selection.activeObject = config;
        }
        
        private static void InitializeDefaultConfig(ItemConfiguration config)
        {
            // 모든 아이템 타입에 대한 기본 설정 생성
            var itemTypes = System.Enum.GetValues(typeof(ReceiveGameManager.ItemType));
            var configs = new ItemConfig[itemTypes.Length];
            
            for (int i = 0; i < itemTypes.Length; i++)
            {
                ReceiveGameManager.ItemType itemType = (ReceiveGameManager.ItemType)itemTypes.GetValue(i);
                configs[i] = CreateDefaultItemConfig(itemType);
            }
            
            // 리플렉션을 사용하여 private 필드에 접근
            var field = typeof(ItemConfiguration).GetField("itemConfigs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(config, configs);
        }
        
        private static ItemConfig CreateDefaultItemConfig(ReceiveGameManager.ItemType itemType)
        {
            ItemConfig config = new ItemConfig();
            config.itemType = itemType;
            
            // 아이템 타입별 기본 설정
            switch (itemType)
            {
                case ReceiveGameManager.ItemType.Normal:
                    config.itemColor = Color.white;
                    config.pointValue = 1;
                    config.effectDuration = 0f; // 일반 아이템은 효과 없음
                    config.bounceForce = 2f;
                    config.maxFallSpeed = 12f;
                    config.rotationSpeed = 60f;
                    config.bobSpeed = 1.5f;
                    config.bobHeight = 0.3f;
                    break;
                    
                case ReceiveGameManager.ItemType.Bonus:
                    config.itemColor = Color.yellow;
                    config.pointValue = 3;
                    config.effectDuration = 0f; // 보너스 아이템은 효과 없음
                    config.bounceForce = 2.5f;
                    config.maxFallSpeed = 12f;
                    config.rotationSpeed = 90f;
                    config.bobSpeed = 2f;
                    config.bobHeight = 0.4f;
                    break;
                    
                case ReceiveGameManager.ItemType.Speed:
                    config.itemColor = Color.blue;
                    config.pointValue = 1;
                    config.effectDuration = 5f;
                    config.bounceForce = 2f;
                    config.maxFallSpeed = 12f;
                    config.rotationSpeed = 120f;
                    config.bobSpeed = 1.5f;
                    config.bobHeight = 0.3f;
                    break;
                    
                case ReceiveGameManager.ItemType.Slow:
                    config.itemColor = Color.red;
                    config.pointValue = 1;
                    config.effectDuration = 5f;
                    config.bounceForce = 1.5f;
                    config.maxFallSpeed = 10f;
                    config.rotationSpeed = 30f;
                    config.bobSpeed = 1f;
                    config.bobHeight = 0.2f;
                    break;
                    
                case ReceiveGameManager.ItemType.Magnet:
                    config.itemColor = Color.green;
                    config.pointValue = 1;
                    config.effectDuration = 5f;
                    config.bounceForce = 2f;
                    config.maxFallSpeed = 12f;
                    config.rotationSpeed = 60f;
                    config.bobSpeed = 1.5f;
                    config.bobHeight = 0.3f;
                    break;
            }
            
            return config;
        }
    }
    
    /// <summary>
    /// ItemConfiguration 에디터
    /// </summary>
    [CustomEditor(typeof(ItemConfiguration))]
    public class ItemConfigurationEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            ItemConfiguration config = (ItemConfiguration)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("도구", EditorStyles.boldLabel);
            
            if (GUILayout.Button("기본 설정으로 초기화"))
            {
                InitializeDefaultConfig(config);
                EditorUtility.SetDirty(config);
            }
            
            if (GUILayout.Button("설정 유효성 검사"))
            {
                bool isValid = config.IsValid();
                if (isValid)
                {
                    EditorUtility.DisplayDialog("유효성 검사", "모든 설정이 유효합니다!", "확인");
                }
                else
                {
                    EditorUtility.DisplayDialog("유효성 검사", "설정에 문제가 있습니다. 콘솔을 확인하세요.", "확인");
                }
            }
        }
        
        private void InitializeDefaultConfig(ItemConfiguration config)
        {
            // 모든 아이템 타입에 대한 기본 설정 생성
            var itemTypes = System.Enum.GetValues(typeof(ReceiveGameManager.ItemType));
            var configs = new ItemConfig[itemTypes.Length];
            
            for (int i = 0; i < itemTypes.Length; i++)
            {
                ReceiveGameManager.ItemType itemType = (ReceiveGameManager.ItemType)itemTypes.GetValue(i);
                configs[i] = CreateDefaultItemConfig(itemType);
            }
            
            // 리플렉션을 사용하여 private 필드에 접근
            var field = typeof(ItemConfiguration).GetField("itemConfigs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(config, configs);
        }
        
        private ItemConfig CreateDefaultItemConfig(ReceiveGameManager.ItemType itemType)
        {
            ItemConfig config = new ItemConfig();
            config.itemType = itemType;
            
            // 아이템 타입별 기본 설정
            switch (itemType)
            {
                case ReceiveGameManager.ItemType.Normal:
                    config.itemColor = Color.white;
                    config.pointValue = 1;
                    config.effectDuration = 0f;
                    config.bounceForce = 2f;
                    config.maxFallSpeed = 12f;
                    config.rotationSpeed = 60f;
                    config.bobSpeed = 1.5f;
                    config.bobHeight = 0.3f;
                    break;
                    
                case ReceiveGameManager.ItemType.Bonus:
                    config.itemColor = Color.yellow;
                    config.pointValue = 3;
                    config.effectDuration = 0f;
                    config.bounceForce = 2.5f;
                    config.maxFallSpeed = 12f;
                    config.rotationSpeed = 90f;
                    config.bobSpeed = 2f;
                    config.bobHeight = 0.4f;
                    break;
                    
                case ReceiveGameManager.ItemType.Speed:
                    config.itemColor = Color.blue;
                    config.pointValue = 1;
                    config.effectDuration = 5f;
                    config.bounceForce = 2f;
                    config.maxFallSpeed = 12f;
                    config.rotationSpeed = 120f;
                    config.bobSpeed = 1.5f;
                    config.bobHeight = 0.3f;
                    break;
                    
                case ReceiveGameManager.ItemType.Slow:
                    config.itemColor = Color.red;
                    config.pointValue = 1;
                    config.effectDuration = 5f;
                    config.bounceForce = 1.5f;
                    config.maxFallSpeed = 10f;
                    config.rotationSpeed = 30f;
                    config.bobSpeed = 1f;
                    config.bobHeight = 0.2f;
                    break;
                    
                case ReceiveGameManager.ItemType.Magnet:
                    config.itemColor = Color.green;
                    config.pointValue = 1;
                    config.effectDuration = 5f;
                    config.bounceForce = 2f;
                    config.maxFallSpeed = 12f;
                    config.rotationSpeed = 60f;
                    config.bobSpeed = 1.5f;
                    config.bobHeight = 0.3f;
                    break;
            }
            
            return config;
        }
    }
} 