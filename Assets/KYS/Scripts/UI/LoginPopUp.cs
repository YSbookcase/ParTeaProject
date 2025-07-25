using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase.Extensions;
using Firebase.Auth;
using KYS;
using UnityEngine.EventSystems;

namespace KYS
{
    public class LoginPopUp : BaseUI
    {

        private TMP_InputField idInput => GetUI<TMP_InputField>("IDField");
        private TMP_InputField passInput => GetUI<TMP_InputField>("PasswordField");

        public bool isLoginProblem = false;



        private new void Awake()
        {

            base.Awake();

            GetEvent("SignUpButton").Click += SignUp;
            GetEvent("LoginButton").Click += Login;

            // UIManager에 자신을 등록
            UIManager.Instance.RegisterMainPanel("LoginPopUp", gameObject);
        }

        private void OnEnable()
        {
            //Debug.Log("[LoginPanel] OnEnable 호출됨 - 입력 필드 초기화 시작");
            // 패널이 활성화될 때마다 입력 필드 초기화
            ResetInputs();
        }

        private void SignUp(PointerEventData eventData)
        {
            //signUpPanel.SetActive(true);
            //gameObject.SetActive(false);
            //PopUP System으로 변경 진행.
            UIManager.Instance.ShowPopUp<SignUpPopUp>();

        }

        // LoginPanel.cs의 Login 메서드 수정
        private void Login(PointerEventData eventData)
        {
            // YSK 네임스페이스의 FirebaseManager 사용
            KYS.FirebaseManager.Auth.SignInWithEmailAndPasswordAsync(idInput.text, passInput.text)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.Log("로그인이 취소됨");
                    ShowLoginFailMessage("로그인이 취소되었습니다.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.Log($"로그인이 실패함. 이유 : {task.Exception}");
                    ShowLoginFailMessage("로그인에 실패했습니다.");
                    return;
                }

                Debug.Log("로그인 성공");

                // 1. 이미 이메일 인증이 완료된 경우
                FirebaseUser user = task.Result.User;
                if (user.IsEmailVerified == true)
                {
                    // 1-1. 닉네임이 설정되지 않은 경우
                    if (string.IsNullOrEmpty(user.DisplayName))
                    {
                        UIManager.Instance.ShowPopUp<NicknamePopUp>();
                    }
                    // 1-2. 닉네임이 설정된 경우
                    else
                    {
                        UIManager.Instance.ShowPopUp<LobbyPopUp>();
                    }
                }
                // 2. 이메일 인증이 완료되지 않은 경우
                else
                {
                    UIManager.Instance.ShowPopUp<EmailPopUp>();
                }
            });
        }

        private void ShowLoginFailMessage(string message)
        {
            MessagePopUp messagePopUp = UIManager.Instance.ShowPopUp<MessagePopUp>();
            if (messagePopUp != null)
            {
                messagePopUp.SetMessage(message, "확인");
            }
        }

        // 입력 필드 초기화
        public void ResetInputs()
        {
            //Debug.Log("[LoginPanel] ResetInputs 호출됨");
            if (idInput != null)
            {
                idInput.text = "";
                //Debug.Log("[LoginPanel] ID 필드 초기화 완료");
            }
            else
            {
                Debug.LogWarning("[LoginPanel] ID 필드를 찾을 수 없습니다.");
            }

            if (passInput != null)
            {
                passInput.text = "";
                //Debug.Log("[LoginPanel] 비밀번호 필드 초기화 완료");
            }
            else
            {
                Debug.LogWarning("[LoginPanel] 비밀번호 필드를 찾을 수 없습니다.");
            }
        }


    }
}
