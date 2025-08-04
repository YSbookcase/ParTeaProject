using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

namespace KYS
{
    public class CheckPopUp : BaseUI
    {
        private TMP_Text MessageText => GetUI<TMP_Text>("MessageText");
        private TMP_Text ConfirmText => GetUI<TMP_Text>("ConfirmText");
        private TMP_Text CancelText => GetUI<TMP_Text>("CancelText");

        // 콜백 함수들을 저장할 델리게이트
        private System.Action onConfirm;
        private System.Action onCancel;

        private new void Awake()
        {
            base.Awake();

            // SFX가 포함된 버튼 이벤트 등록
            GetEventWithSFX("ConfirmButton", "SFX_ButtonClick").Click += OnConfirm;
            GetBackEvent("CancelButton", "SFX_ButtonClickBack").Click += OnCancel;
        }

        // 메시지와 버튼 텍스트, 콜백 함수를 설정하는 메서드
        public void SetMessage(string message, string confirmText = "확인", string cancelText = "취소",
                             System.Action confirmCallback = null, System.Action cancelCallback = null)
        {
            if (MessageText != null)
            {
                MessageText.text = message;
            }

            if (ConfirmText != null)
            {
                ConfirmText.text = confirmText;
            }

            if (CancelText != null)
            {
                CancelText.text = cancelText;
            }

            // 콜백 함수 저장
            onConfirm = confirmCallback;
            onCancel = cancelCallback;
        }

        // 확인 버튼 클릭 시
        private void OnConfirm(PointerEventData eventData)
        {
            onConfirm?.Invoke(); // 콜백 함수 실행
            UIManager.Instance.ClosePopUp(); // 팝업 닫기
        }

        // 취소 버튼 클릭 시
        private void OnCancel(PointerEventData eventData)
        {
            onCancel?.Invoke(); // 콜백 함수 실행
            UIManager.Instance.ClosePopUp(); // 팝업 닫기
        }
    }
}
