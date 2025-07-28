using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace JTW_JumpGame
{
    public class NicknamePanel : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI nicknameText;

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



            Vector3 position = Camera.main.WorldToScreenPoint(player.position + (Vector3.up * 2));

            rect.position = position;
        }

        public void SetInfo(string name, Transform player)
        {
            nicknameText.text = name;
            this.player = player;
            isInit = true;
        }
    }
}

