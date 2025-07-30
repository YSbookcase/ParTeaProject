using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace KYS
{
    public class ReceiveGameNicknamePanel : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI nicknameText;
        [SerializeField] Vector3 offset = new Vector3(0, 2, 0);

        private Transform player;
        private RectTransform rect;
        private bool isInit = false;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (isInit && player == null)
            {
                Destroy(gameObject);
                return;
            }
            if (player == null) return;

            // 월드 좌표를 스크린 좌표로 변환
            Vector3 position = Camera.main.WorldToScreenPoint(player.position + offset);
            
            // 화면 밖으로 나가지 않도록 제한
            position.x = Mathf.Clamp(position.x, rect.sizeDelta.x / 2, Screen.width - rect.sizeDelta.x / 2);
            position.y = Mathf.Clamp(position.y, rect.sizeDelta.y / 2, Screen.height - rect.sizeDelta.y / 2);

            rect.position = position;
        }

        public void SetInfo(string name, Transform player)
        {
            if (nicknameText != null)
            {
                nicknameText.text = name;
            }
            this.player = player;
            isInit = true;
        }

        public void SetColor(Color color)
        {
            if (nicknameText != null)
            {
                nicknameText.color = color;
            }
        }
    }
} 