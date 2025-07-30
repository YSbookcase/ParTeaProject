using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using PJW;   // PlayerController 네임스페이스

namespace PJW
{
    public class NicknamePanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nicknameText;
        [SerializeField] private Vector3 worldOffset = Vector3.up * 2f;

        private Transform playerTransform;
        private RectTransform rect;
        private bool isInit = false;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (!isInit) return;

            if (playerTransform == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 screenPos = Camera.main.WorldToScreenPoint(playerTransform.position + worldOffset);
            rect.position = screenPos;
        }

        // 기존 방식: Transform 직접 전달
        public void SetInfo(string name, Transform player)
        {
            nicknameText.text = name;
            playerTransform = player;
            isInit = true;
        }

        // 새 추가: PlayerController 전달
        public void SetInfo(string name, PlayerController playerController)
        {
            SetInfo(name, playerController.transform);
        }
    }
}