using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using KYS;
using UnityEngine.EventSystems;

namespace KYS
{
    public class MessagePopUp : BaseUI
    {

        private TMP_Text MessageText => GetUI<TMP_Text>("MessageText");

        private TMP_Text ConfirmText => GetUI<TMP_Text>("ConfirmText");



        private new void Awake()
        {
            base.Awake();

            GetEvent("ConfirmText").Click += CheckedMessage;

        }

        // 메시지와 버튼 텍스트를 설정하는 메서드
        public void SetMessage(string message, string buttonText = "확인")
        {
            if (MessageText != null)
            {
                MessageText.text = message;
            }

            if (ConfirmText != null)
            {
                ConfirmText.text = buttonText;
            }
        }

        private void CheckedMessage(PointerEventData eventData)
        {
            // MessagePopUp만 닫기 (RoomPopUp은 유지)
            if (UIManager.Instance.PopUp != null && UIManager.Instance.PopUp.StackCount() > 1)
            {
                UIManager.Instance.ClosePopUp();
            }
            else
            {
                // MessagePopUp만 직접 제거
                Destroy(gameObject);
            }
        }

    }
}
