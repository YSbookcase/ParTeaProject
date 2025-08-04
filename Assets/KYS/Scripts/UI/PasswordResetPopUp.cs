using Firebase.Auth;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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
            string email = emailInput.text.Trim();
            if (string.IsNullOrEmpty(email))
            {
                ShowErrorMessage("이메일을 입력해주세요.");
                return;
            }

            // 이메일 형식 검증
            if (!ValidateEmailFormat(email))
            {
                ShowErrorMessage("올바른 이메일 형식을 입력해주세요.");
                return;
            }

            Debug.Log($"[PasswordResetPopUp] 비밀번호 재설정 이메일 전송 시작: {email}");
            
            FirebaseManager.Auth.SendPasswordResetEmailAsync(email)
                .ContinueWithOnMainThread(task =>
                {
                    Debug.Log($"[PasswordResetPopUp] Task 상태 확인 - IsCompleted: {task.IsCompleted}, IsCanceled: {task.IsCanceled}, IsFaulted: {task.IsFaulted}");
                    
                    if (task.IsCanceled)
                    {
                        Debug.LogWarning("[PasswordResetPopUp] 이메일 전송이 취소되었습니다.");
                        ShowErrorMessage("비밀번호 재설정 이메일 전송이 취소되었습니다.");
                        return;
                    }
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"[PasswordResetPopUp] 이메일 전송 실패: {task.Exception}");
                        ShowErrorMessage("비밀번호 재설정 이메일 전송에 실패했습니다.");
                        return;
                    }

                    Debug.Log("[PasswordResetPopUp] 비밀번호 재설정 이메일 전송 성공!");
                    ShowSuccessMessage("비밀번호 재설정 이메일이 전송되었습니다. 이메일을 확인해주세요.");
                    // 성공 메시지 팝업이 표시된 후 현재 팝업은 닫지 않음 (사용자가 확인 버튼을 누를 때까지 대기)
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