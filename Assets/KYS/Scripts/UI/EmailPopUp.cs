using Firebase.Auth;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KYS
{
    public class EmailPopUp : BaseUI
    {
        private TMP_Text statusText => GetUI<TMP_Text>("MessageText");
        private TMP_Text emailText => GetUI<TMP_Text>("EmailText");

        private Coroutine emailVerificationRoutine;

        private new void Awake()
        {
            base.Awake();
canCloseWithESC = false; // ESC로 닫을 수 없음
            // 버튼 이벤트 등록
            GetEvent("BackButton").Click += Back;

            GetEvent("ResendButton").Click += ResendEmail;
        }

        private void OnEnable()
        {
            // 이메일 인증 시작
            StartEmailVerification();
        }

        private void OnDisable()
        {
            // 코루틴 정리
            if (emailVerificationRoutine != null)
            {
                StopCoroutine(emailVerificationRoutine);
                emailVerificationRoutine = null;
            }
        }

        private void StartEmailVerification()
        {
            // 현재 사용자 이메일 표시
            FirebaseUser user = FirebaseManager.Auth.CurrentUser;
            if (user != null && emailText != null)
            {
                emailText.text = user.Email;
            }

            // 인증 이메일 전송
            SendVerificationEmail();
        }

        private void SendVerificationEmail()
        {
            FirebaseUser user = FirebaseManager.Auth.CurrentUser;
            if (user == null)
            {
                ShowErrorMessage("사용자 정보를 찾을 수 없습니다.");
                return;
            }

            user.SendEmailVerificationAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled)
                    {
                        ShowErrorMessage("인증 이메일 전송이 취소되었습니다.");
                        return;
                    }
                    if (task.IsFaulted)
                    {
                        ShowErrorMessage($"인증 이메일 전송 실패, 인증 이메일 전송 후 얼마 되지 않았습니다.");
                        Debug.Log($"인즌 이메일 전송 실패 로그 : {task.Exception}");
                        return;
                    }

                    Debug.Log("인증 이메일 전송 성공");
                    UpdateStatusText("인증 이메일이 전송되었습니다. 이메일을 확인해주세요.");

                    // 이메일 인증 확인 루틴 시작
                    emailVerificationRoutine = StartCoroutine(EmailVerificationRoutine());
                });
        }

        private void ResendEmail(PointerEventData eventData)
        {
            // 기존 코루틴 정리
            if (emailVerificationRoutine != null)
            {
                StopCoroutine(emailVerificationRoutine);
            }

            UpdateStatusText("인증 이메일을 다시 전송합니다...");
            SendVerificationEmail();
        }

        private IEnumerator EmailVerificationRoutine()
        {
            FirebaseUser user = FirebaseManager.Auth.CurrentUser;
            WaitForSeconds delay = new WaitForSeconds(2f);

            while (true)
            {
                yield return delay;

                if (user == null)
                {
                    ShowErrorMessage("사용자 정보를 찾을 수 없습니다.");
                    break;
                }

                user.ReloadAsync();
                if (user.IsEmailVerified)
                {
                    Debug.Log("이메일 인증 완료");
                    UpdateStatusText("이메일 인증이 완료되었습니다!");

                    // 잠시 대기 후 닉네임 팝업으로 이동
                    yield return new WaitForSeconds(1f);

                    // 팝업 닫기
                    UIManager.Instance.ClosePopUp();

                    // 닉네임 팝업으로 이동
                    UIManager.Instance.ShowPopUp<NicknamePopUp>();
                    break;
                }
                else
                {
                    UpdateStatusText("이메일 인증을 기다리는 중...");
                }
            }
        }

        private void Back(PointerEventData eventData)
        {
            // 코루틴 정리
            if (emailVerificationRoutine != null)
            {
                StopCoroutine(emailVerificationRoutine);
                emailVerificationRoutine = null;
            }

            // 로그아웃 처리
            FirebaseManager.Auth.SignOut();

            // 팝업 닫기
            UIManager.Instance.ClosePopUp();

            // 로그인 패널로 이동
            GameObject loginPanel = UIManager.Instance.GetMainPanel("LoginPopUp");
            if (loginPanel != null)
            {
                // 로그인 패널을 비활성화했다가 다시 활성화하여 OnEnable 호출 보장
                loginPanel.SetActive(false);
                loginPanel.SetActive(true);

                // 추가로 LoginPanel의 ResetInputs 메서드를 직접 호출
                LoginPopUp loginPanelScript = loginPanel.GetComponent<LoginPopUp>();
                if (loginPanelScript != null)
                {
                    loginPanelScript.ResetInputs();
                }
            }
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        // 에러 메시지 표시
        private void ShowErrorMessage(string message)
        {
            MessagePopUp messagePopUp = UIManager.Instance.ShowPopUp<MessagePopUp>();
            if (messagePopUp != null)
            {
                messagePopUp.SetMessage(message, "확인");
            }
        }
    }
}
