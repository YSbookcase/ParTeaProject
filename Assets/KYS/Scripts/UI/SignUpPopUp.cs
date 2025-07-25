using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;
using Firebase.Auth;
using System.Linq; // Count() 메서드를 위해 추가

namespace KYS
{
    public class SignUpPopUp : BaseUI
    {
        private TMP_InputField idInput => GetUI<TMP_InputField>("IDField");
        private TMP_InputField passInput => GetUI<TMP_InputField>("PasswordField");
        private TMP_InputField passConfirmInput => GetUI<TMP_InputField>("PasswordCheckField");

        // 이메일 유효성 상태

        private bool isEmailAvailable = false;

        private new void Awake()
        {
            base.Awake();

            GetEvent("SignUpButton").Click += SignUp;
            GetEvent("CancelButton").Click += Cancel;
            GetEvent("CheckAvailabilityButton").Click += CheckEmailAvailability;
        }

        // 이메일 사용 가능성 확인
        private void CheckEmailAvailability(PointerEventData eventData)
        {
            if (!ValidateEmailFormat(idInput.text))
            {
                ShowErrorMessage("올바른 이메일 형식을 입력해주세요.");
                return;
            }

            string email = idInput.text;
            Debug.Log($"이메일 중복 확인 시작: {email}");

            // Firebase Auth를 사용하여 이메일 중복 확인
            FirebaseManager.Auth.FetchProvidersForEmailAsync(email)
                .ContinueWithOnMainThread(task =>
                {
                    Debug.Log($"[SignUpPopUp] Task 상태 확인 - IsCompleted: {task.IsCompleted}, IsCanceled: {task.IsCanceled}, IsFaulted: {task.IsFaulted}");

                    if (task.IsCanceled)
                    {
                        Debug.LogError("이메일 중복 확인이 취소되었습니다.");
                        ShowErrorMessage("이메일 중복 확인이 취소되었습니다.");
                        return;
                    }
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"이메일 중복 확인 실패: {task.Exception}");
                        ShowErrorMessage("이메일 중복 확인에 실패했습니다.");
                        return;
                    }

                    var providers = task.Result;
                    Debug.Log($"[SignUpPopUp] Task.Result 타입: {providers?.GetType().Name ?? "null"}");
                    Debug.Log($"[SignUpPopUp] Task.Result 값: {providers}");

                    if (providers != null)
                    {
                        Debug.Log($"[SignUpPopUp] Providers 개수: {providers.Count()}");
                        Debug.Log($"[SignUpPopUp] Providers 내용: [{string.Join(", ", providers)}]");

                        // Email/Password로 가입한 경우 "password"가 포함됨
                        bool hasPasswordProvider = providers.Any(p => p == "password");
                        
                        if (hasPasswordProvider)
                        {
                            // 이미 Email/Password로 등록된 이메일
                            isEmailAvailable = false;
                            Debug.Log($"이미 사용 중인 이메일 (Email/Password): {email}");
                            ShowErrorMessage("이미 사용 중인 이메일입니다.");
                        }
                        else
                        {
                            // 다른 제공자로만 가입했거나 사용 가능한 이메일
                            isEmailAvailable = true;
                            Debug.Log($"사용 가능한 이메일: {email}");
                            ShowSuccessMessage("사용 가능한 이메일입니다.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[SignUpPopUp] Task.Result가 null입니다.");
                        // null인 경우 사용 가능한 것으로 처리
                        isEmailAvailable = true;
                        Debug.Log($"사용 가능한 이메일 (null 결과): {email}");
                        ShowSuccessMessage("사용 가능한 이메일입니다.");
                    }
                });
        }

        // 이메일 형식 검증
        private bool ValidateEmailFormat(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                Debug.Log("이메일이 비어있습니다.");
                return false;
            }

            // 기본적인 이메일 형식 검증
            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            bool isValidFormat = Regex.IsMatch(email, pattern);

            if (!isValidFormat)
            {
                Debug.Log($"이메일 형식이 올바르지 않습니다: {email}");
                return false;
            }

            // 길이 검증
            if (email.Length > 254)
            {
                Debug.Log("이메일이 너무 깁니다.");
                return false;
            }

            // 로컬 부분과 도메인 부분 분리
            string[] parts = email.Split('@');
            if (parts.Length != 2)
            {
                Debug.Log("이메일 형식이 올바르지 않습니다.");
                return false;
            }

            // 로컬 부분 길이 검증 (64자 이하)
            if (parts[0].Length > 64)
            {
                Debug.Log("이메일 로컬 부분이 너무 깁니다.");
                return false;
            }

            // 도메인 부분 길이 검증 (253자 이하)
            if (parts[1].Length > 253)
            {
                Debug.Log("이메일 도메인 부분이 너무 깁니다.");
                return false;
            }

            Debug.Log($"이메일 형식 검증 성공: {email}");
            return true;
        }

        private void SignUp(PointerEventData eventData)
        {
            // 이메일 유효성 최종 검증
            if (!ValidateEmailFormat(idInput.text))
            {
                ShowErrorMessage("올바른 이메일 형식을 입력해주세요.");
                return;
            }

            // 이메일 사용 가능성 확인
            if (!isEmailAvailable)
            {
                ShowErrorMessage("이메일 중복 확인을 먼저 해주세요.");
                return;
            }

            // 비밀번호 유효성 검사
            if (string.IsNullOrEmpty(passInput.text) || passInput.text.Length < 6)
            {
                ShowErrorMessage("비밀번호는 최소 6자 이상이어야 합니다.");
                return;
            }

            if (passInput.text != passConfirmInput.text)
            {
                ShowErrorMessage("비밀번호가 일치하지 않습니다.");
                return;
            }

            // 디버깅을 위한 로그
            Debug.Log($"회원가입 시도 - 이메일: {idInput.text}, 비밀번호 길이: {passInput.text?.Length ?? 0}");
            
            // 회원가입 시도 (Firebase가 자동으로 중복 확인)
            FirebaseManager.Auth.CreateUserWithEmailAndPasswordAsync(idInput.text, passInput.text)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled)
                    {
                        ShowErrorMessage("회원가입이 취소되었습니다.");
                        return;
                    }
                    if (task.IsFaulted)
                    {
                        // Firebase에서 중복 이메일 에러를 자동으로 처리
                        string errorMessage = task.Exception.ToString();
                        if (errorMessage.Contains("already in use") || errorMessage.Contains("already exists"))
                        {
                            ShowErrorMessage("이미 사용 중인 이메일입니다.");
                        }
                        else if (errorMessage.Contains("weak password"))
                        {
                            ShowErrorMessage("비밀번호가 너무 약합니다. (최소 6자 이상)");
                        }
                        else if (errorMessage.Contains("invalid email"))
                        {
                            ShowErrorMessage("올바르지 않은 이메일 형식입니다.");
                        }
                        else if (errorMessage.Contains("internal error"))
                        {
                            ShowErrorMessage("서버 내부 오류가 발생했습니다. 잠시 후 다시 시도해주세요.");
                        }
                        else if (errorMessage.Contains("network") || errorMessage.Contains("connection"))
                        {
                            ShowErrorMessage("네트워크 연결을 확인해주세요.");
                        }
                        else if (errorMessage.Contains("too many requests"))
                        {
                            ShowErrorMessage("너무 많은 요청이 있었습니다. 잠시 후 다시 시도해주세요.");
                        }
                        else
                        {
                            ShowErrorMessage($"오류로 인한 회원가입 실패");
                            Debug.Log($"에이터 상에서 확인하는 Log {task.Exception}");
                        }
                        return;
                    }

                    Debug.Log("회원가입이 완료되었습니다!");
                    ResetInputs();

                    // 회원가입 성공 후 이메일 인증 팝업으로 이동
                    UIManager.Instance.ClosePopUp();
                    UIManager.Instance.ShowPopUp<EmailPopUp>();
                });
        }

        private void Cancel(PointerEventData eventData)
        {
            ResetInputs();
            UIManager.Instance.ClosePopUp();

            // 로그인 패널 활성화 (OnEnable이 호출되어 입력 필드가 초기화됨)
            GameObject loginPanel = UIManager.Instance.GetMainPanel("LoginPopUp");
            if (loginPanel != null)
            {
                loginPanel.SetActive(true);
            }
        }

        private void ResetInputs()
        {
            if (idInput != null) idInput.text = "";
            if (passInput != null) passInput.text = "";
            if (passConfirmInput != null) passConfirmInput.text = "";

            // 상태 초기화

            isEmailAvailable = false;
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




    }
}
