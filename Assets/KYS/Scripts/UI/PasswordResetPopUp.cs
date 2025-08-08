using Firebase.Auth;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Added for Button

namespace KYS
{
    public class PasswordResetPopUp : BaseUI
    {
        private TMP_InputField emailInput => GetUI<TMP_InputField>("EmailField");

        private new void Awake()
        {
            base.Awake();
            canCloseWithESC = false; // ESC로 닫을 수 없음

            // SFX가 포함된 버튼 이벤트 등록
            GetEventWithSFX("SendButton", "SFX_ButtonClick").Click += SendResetEmail;
            GetBackEvent("BackButton", "SFX_ButtonClickBack").Click += Back;

            var menuButton = GetEventWithSFX("TitleMenuButton", "SFX_ButtonClick");
            if (menuButton != null)
            {
                menuButton.Click -= OnTitleMenu;
                menuButton.Click += OnTitleMenu;
            }

        }

        private void SendResetEmail(PointerEventData eventData)
        {
            // 버튼 비활성화
            Button sendButton = GetUI<Button>("SendButton");
            if (sendButton != null)
            {
                sendButton.interactable = false;
            }

            // 이메일 입력값 검증
            if (string.IsNullOrEmpty(emailInput.text))
            {
                ShowErrorMessage("이메일을 입력해주세요.");
                // 버튼 다시 활성화
                if (sendButton != null)
                {
                    sendButton.interactable = true;
                }
                return;
            }

            // 이메일 형식 검증
            if (!ValidateEmailFormat(emailInput.text))
            {
                ShowErrorMessage("올바른 이메일 형식을 입력해주세요.");
                // 버튼 다시 활성화
                if (sendButton != null)
                {
                    sendButton.interactable = true;
                }
                return;
            }

            // 비밀번호 재설정 이메일 전송
            FirebaseManager.Auth.SendPasswordResetEmailAsync(emailInput.text)
                .ContinueWithOnMainThread(task =>
                {
                    // 버튼 다시 활성화
                    if (sendButton != null)
                    {
                        sendButton.interactable = true;
                    }

                    if (task.IsCanceled)
                    {
                        ShowErrorMessage("비밀번호 재설정 이메일 전송이 취소되었습니다.");
                        return;
                    }
                    if (task.IsFaulted)
                    {
                        ShowErrorMessage("비밀번호 재설정 이메일 전송에 실패했습니다.");
                        Debug.LogError($"비밀번호 재설정 이메일 전송 오류: {task.Exception}");
                        return;
                    }

                    ShowSuccessMessage("비밀번호 재설정 이메일이 전송되었습니다. 이메일을 확인해주세요.");
                    Debug.Log("비밀번호 재설정 이메일 전송 성공");
                });
        }

        private void Back(PointerEventData eventData)
        {
            UIManager.Instance.ClosePopUp();
        }

        // 이메일 형식 검증
        private bool ValidateEmailFormat(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return false;
            }

            // 기본적인 이메일 형식 검증
            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            bool isValidFormat = System.Text.RegularExpressions.Regex.IsMatch(email, pattern);

            if (!isValidFormat)
            {
                return false;
            }

            // 길이 검증
            if (email.Length > 254)
            {
                return false;
            }

            // 로컬 부분과 도메인 부분 분리
            string[] parts = email.Split('@');
            if (parts.Length != 2)
            {
                return false;
            }

            // 로컬 부분 길이 검증 (64자 이하)
            if (parts[0].Length > 64)
            {
                return false;
            }

            // 도메인 부분 길이 검증 (253자 이하)
            if (parts[1].Length > 253)
            {
                return false;
            }

            return true;
        }

        private void OnTitleMenu(PointerEventData eventData)
        {
            UIManager.Instance.ShowPopUp<TitleMenuPopUp>();
        }


        private void ShowErrorMessage(string message)
        {
            MessagePopUp messagePopUp = UIManager.Instance.ShowPopUp<MessagePopUp>();
            if (messagePopUp != null)
            {
                messagePopUp.SetMessage(message, "확인");
            }
        }

        private void ShowSuccessMessage(string message)
        {
            MessagePopUp messagePopUp = UIManager.Instance.ShowPopUp<MessagePopUp>();
            if (messagePopUp != null)
            {
                messagePopUp.SetMessage(message, "확인");
            }
        }

        // 팝업이 활성화될 때 호출
        private void OnEnable()
        {
            // 입력 필드 초기화
            if (emailInput != null)
            {
                emailInput.text = "";
            }
        }
    }
} 