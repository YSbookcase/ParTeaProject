using Firebase.Auth;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;


namespace KYS
{
    public class EditPopUp : BaseUI
    {
        private TMP_InputField nameInput => GetUI<TMP_InputField>("NicknameInputField");
        private TMP_InputField passInput => GetUI<TMP_InputField>("PasswordField");
        private TMP_InputField passConfirmInput => GetUI<TMP_InputField>("PasswordCheckField");

        private TMP_Text emailText => GetUI<TMP_Text>("EmailContent");
        //private TMP_Text userIdText => GetUI<TMP_Text>("UIDContent");

        private new void Awake()
        {
            base.Awake();

            // 안전한 이벤트 등록
            RegisterEvents();
        }

        private void Start()
        {
            // Start에서는 LoginInfo 호출하지 않음
            // OnEnable에서만 호출하도록 변경
        }

        private void OnEnable()
        {
            // 다음 프레임에 사용자 정보 로드 (UI 요소들이 완전히 초기화된 후)
            StartCoroutine(LoadUserInfoNextFrame());
        }

        private IEnumerator LoadUserInfoNextFrame()
        {
            // 다음 프레임까지 대기
            yield return null;

            // Firebase가 이미 초기화되었는지 확인
            if (FirebaseManager.Auth != null)
            {
                LoginInfo();
                yield break;
            }

            // Firebase 초기화를 기다림 (간단한 폴링 방식)
            float waitTime = 0f;
            while (FirebaseManager.Auth == null && waitTime < 10f)
            {
                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }

            // Firebase가 초기화되었는지 최종 확인
            if (FirebaseManager.Auth == null)
            {
                Debug.LogError("[EditPopUp] Firebase Auth가 초기화되지 않았습니다. 10초 대기 후에도 초기화되지 않음.");
                yield break;
            }

            // 사용자 정보 로드
            LoginInfo();
        }

        private void RegisterEvents()
        {
            // NicknameConfirmButton 이벤트 등록
            var nicknameButton = GetEvent("NicknameConfirmButton");
            if (nicknameButton != null)
            {
                nicknameButton.Click += ChangeNickname;
            }
            else
            {
                Debug.LogWarning("[EditPopUp] NicknameConfirmButton을 찾을 수 없습니다.");
            }

            // PassConfirmButton 이벤트 등록
            var passButton = GetEvent("PassConfirmButton");
            if (passButton != null)
            {
                passButton.Click += ChangePassword;
            }
            else
            {
                Debug.LogWarning("[EditPopUp] PassConfirmButton을 찾을 수 없습니다.");
            }

            // BackButton 이벤트 등록
            var backButton = GetEvent("BackButton");
            if (backButton != null)
            {
                backButton.Click += Back;
            }
            else
            {
                Debug.LogWarning("[EditPopUp] BackButton을 찾을 수 없습니다.");
            }

            var deleteUserButton = GetEvent("DeleteUserButton");
            if (deleteUserButton != null)
            {
                deleteUserButton.Click -= DeleteUser;
                deleteUserButton.Click += DeleteUser;
            }
            else
            {
                Debug.LogError("[LobbyPopUp] DeleteUserButton을 찾을 수 없습니다.");
            }

        }

        private void ChangeNickname(PointerEventData eventData)
        {
            if (nameInput == null)
            {
                Debug.LogError("[EditPopUp] nameInput이 null입니다.");
                return;
            }

            if (string.IsNullOrEmpty(nameInput.text))
            {
                ShowErrorMessage("닉네임을 입력해주세요.");
                return;
            }

            FirebaseUser user = FirebaseManager.Auth.CurrentUser;
            if (user == null)
            {
                ShowErrorMessage("사용자 정보를 찾을 수 없습니다.");
                return;
            }

            UserProfile profile = new UserProfile();
            profile.DisplayName = nameInput.text;

            user.UpdateUserProfileAsync(profile)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled)
                    {
                        ShowErrorMessage("닉네임 변경이 취소되었습니다.");
                        return;
                    }
                    if (task.IsFaulted)
                    {
                        ShowErrorMessage($"닉네임 변경 실패: {task.Exception}");
                        return;
                    }

                    ShowSuccessMessage("닉네임이 성공적으로 변경되었습니다.");
                    Debug.Log("닉네임 변경 성공");

                    // LobbyPopUp 정보 업데이트
                    UIManager.Instance.UpdatePopUp<LobbyPopUp>();
                });
        }

        private void ChangePassword(PointerEventData eventData)
        {
            if (passInput == null || passConfirmInput == null)
            {
                Debug.LogError("[EditPopUp] 비밀번호 입력 필드가 null입니다.");
                return;
            }

            if (passInput.text != passConfirmInput.text)
            {
                ShowErrorMessage("비밀번호가 일치하지 않습니다.");
                return;
            }

            FirebaseUser user = FirebaseManager.Auth.CurrentUser;
            if (user == null)
            {
                ShowErrorMessage("사용자 정보를 찾을 수 없습니다.");
                return;
            }

            user.UpdatePasswordAsync(passInput.text)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled)
                    {
                        ShowErrorMessage("비밀번호 변경이 취소되었습니다.");
                        return;
                    }
                    if (task.IsFaulted)
                    {
                        ShowErrorMessage($"비밀번호 변경 실패: {task.Exception}");
                        return;
                    }

                    ShowSuccessMessage("비밀번호가 성공적으로 변경되었습니다.");
                    Debug.Log("비밀번호 변경 성공");
                });
        }

        private void Back(PointerEventData eventData)
        {
            UIManager.Instance.ClosePopUp();
        }

        public void LoginInfo()
        {
            FirebaseUser user = FirebaseManager.Auth.CurrentUser;
            if (user == null)
            {
                Debug.LogError("[EditPopUp] 사용자 정보를 찾을 수 없습니다.");
                return;
            }

            // UI 요소들이 null인지 먼저 확인
            if (emailText == null)
            {
                Debug.LogError("[EditPopUp] emailText를 찾을 수 없습니다. UI 요소 이름을 확인해주세요.");
                return;
            }

            //if (userIdText == null)
            //{
            //    Debug.LogError("[EditPopUp] userIdText를 찾을 수 없습니다. UI 요소 이름을 확인해주세요.");
            //    return;
            //}

            if (nameInput == null)
            {
                Debug.LogError("[EditPopUp] nameInput을 찾을 수 없습니다. UI 요소 이름을 확인해주세요.");
                return;
            }

            // 안전한 UI 업데이트
            emailText.text = user.Email ?? "";
            //userIdText.text = user.UserId ?? "";
            nameInput.text = user.DisplayName ?? "";

            Debug.Log($"[EditPopUp] 사용자 정보 로드 완료 - Email: {user.Email}, Name: {user.DisplayName}, UID: {user.UserId}");
        }

        // 방 삭제 버튼 클릭 시
        private void DeleteUser(PointerEventData eventData)
        {
            // DeletePopUp 생성
            //UIManager.Instance.ShowPopUp<DeletePopUp>();
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