using Firebase.Auth;
using Firebase.Extensions;
using KYS;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KYS
{
    public class NicknamePopUp : BaseUI
    {
        private TMP_InputField nicknameInput => GetUI<TMP_InputField>("NicknameField");

        private new void Awake()
        {
            base.Awake();

            // 버튼 이벤트 등록
            GetEvent("ConfirmButton").Click += Confirm;
            GetEvent("BackButton").Click += Back;
        }

        private void Confirm(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(nicknameInput.text))
            {
                ShowErrorMessage("닉네임을 입력해주세요.");
                return;
            }

            UserProfile profile = new UserProfile();
            profile.DisplayName = nicknameInput.text;

            FirebaseUser user = FirebaseManager.Auth.CurrentUser;
            user.UpdateUserProfileAsync(profile)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled)
                    {
                        ShowErrorMessage("닉네임 설정이 취소되었습니다.");
                        return;
                    }
                    if (task.IsFaulted)
                    {
                        ShowErrorMessage($"닉네임 설정 실패: {task.Exception}");
                        return;
                    }

                    Debug.Log("닉네임 설정 성공");
                    ShowSuccessMessage("닉네임이 성공적으로 설정되었습니다.");

                    // 팝업 닫기
                    UIManager.Instance.ClosePopUp();

                    // 로비 팝업으로 이동
                    //UIManager.Instance.ShowPopUp<LobbyPopUp>();
                });
        }

        private void Back(PointerEventData eventData)
        {
            // 로그아웃 처리
            FirebaseManager.Auth.SignOut();

            // 팝업 닫기
            UIManager.Instance.ClosePopUp();

            // 로그인 패널로 이동
            GameObject loginPanel = UIManager.Instance.GetMainPanel("LoginPanel");
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

        // 에러 메시지 표시
        private void ShowErrorMessage(string message)
        {
            MessagePopUp messagePopUp = UIManager.Instance.ShowPopUp<MessagePopUp>();
            if (messagePopUp != null)
            {
                messagePopUp.SetMessage(message, "확인");
            }
        }

        // 성공 메시지 표시
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
            if (nicknameInput != null)
            {
                nicknameInput.text = "";
            }
        }
    }
}
