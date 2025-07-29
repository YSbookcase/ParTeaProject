using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KYS
{
    public class NameTagPrefab : MonoBehaviour
    {
        [Header("Name Tag Settings")]
        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private Image background;
        [SerializeField] private Vector3 textOffset = new Vector3(0, 0, 0.1f);
        
        private void Awake()
        {
            // TextMeshPro 컴포넌트가 없으면 추가
            if (nameText == null)
            {
                nameText = GetComponentInChildren<TextMeshPro>();
                if (nameText == null)
                {
                    CreateTextComponent();
                }
            }
            
            // 배경이 없으면 생성
            if (background == null)
            {
                CreateBackground();
            }
        }
        
        private void CreateTextComponent()
        {
            GameObject textObj = new GameObject("NameText");
            textObj.transform.SetParent(transform);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 5);
            textRect.offsetMax = new Vector2(-5, -5);
            
            nameText = textObj.AddComponent<TextMeshPro>();
            nameText.fontSize = 24f;
            nameText.color = Color.white;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.text = "Player";
        }
        
        private void CreateBackground()
        {
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(transform);
            
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            background = bgObj.AddComponent<Image>();
            background.color = new Color(0, 0, 0, 0.7f);
        }
        
        public void SetName(string playerName)
        {
            if (nameText != null)
            {
                nameText.text = playerName;
            }
        }
        
        public void SetColor(Color color)
        {
            if (nameText != null)
            {
                nameText.color = color;
            }
        }
        
        public void SetPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }
        
        public void LookAtCamera(Camera camera)
        {
            if (camera != null)
            {
                transform.LookAt(camera.transform);
                transform.Rotate(0, 180, 0);
            }
        }
    }
} 