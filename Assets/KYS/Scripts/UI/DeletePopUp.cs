using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine.UI; // Added for Button

namespace KYS
{
    public class DeletePopUp : BaseUI
    {
        // 입력 필드들
        private TMP_InputField idInput => GetUI<TMP_InputField>("IDField");
        private TMP_InputField passwordInput => GetUI<TMP_InputField>("PasswordField");

        // 버튼들
        private new void Awake()
        {
            base.Awake();
            canCloseWithESC = false; // ESC로 닫을 수 없음

            // SFX가 포함된 버튼 이벤트 등록
            GetEventWithSFX("DeleteButton", "SFX_ButtonClick").Click += OnDeleteButtonClick;
            GetBackEvent("CancelButton", "SFX_ButtonClickBack").Click += OnCancelButtonClick;
            //GetEventWithSFX("MenuButton", "SFX_ButtonClick").Click += OnMenuButtonClick;
        }

        // 삭제 버튼 클릭 시
        private void OnDeleteButtonClick(PointerEventData eventData)
        {
            // 버튼 비활성화
            Button deleteButton = GetUI<Button>("DeleteButton");
            if (deleteButton != null)
            {
                deleteButton.interactable = false;
            }

            // 입력값 검증
            if (string.IsNullOrEmpty(idInput.text) || string.IsNullOrEmpty(passwordInput.text))
            {
                ShowErrorMessage("ID와 비밀번호를 모두 입력해주세요.");
                // 버튼 다시 활성화
                if (deleteButton != null)
                {
                    deleteButton.interactable = true;
                }
                return;
            }

            // 현재 로그인된 사용자 정보 확인
            FirebaseUser currentUser = FirebaseManager.Auth.CurrentUser;
            if (currentUser == null)
            {
                ShowErrorMessage("로그인 정보를 찾을 수 없습니다.");
                // 버튼 다시 활성화
                if (deleteButton != null)
                {
                    deleteButton.interactable = true;
                }
                return;
            }

            // ID가 현재 로그인된 사용자와 일치하는지 확인
            if (idInput.text != currentUser.Email)
            {
                ShowErrorMessage("입력한 ID가 현재 로그인된 사용자와 일치하지 않습니다.");
                // 버튼 다시 활성화
                if (deleteButton != null)
                {
                    deleteButton.interactable = true;
                }
                return;
            }

            // 확인 팝업 표시
            ShowConfirmDeletePopup();
        }

        // 취소 버튼 클릭 시
        private void OnCancelButtonClick(PointerEventData eventData)
        {
            ResetInputs();
            UIManager.Instance.ClosePopUp();
        }

        private void OnMenuButtonClick(PointerEventData eventData)
        {
            UIManager.Instance.ShowPopUp<MenuPopUp>();
        }


        // 삭제 확인 팝업 띄우기
        private void ShowConfirmDeletePopup()
        {
            UIManager.Instance.ShowConfirmPopUp(
                "정말 계정을 삭제하시겠습니까?\n탈퇴한 이메일 계정은 로그인 할 수 없으며, 다시 회원 가입을 진행할 수 있습니다.",
                "삭제",
                "취소",
                () => {
                    // 확인 시 계정 삭제 실행
                    DeleteAccount();
                },
                () => {
                    // 취소 시 아무것도 하지 않음
                    Debug.Log("계정 삭제가 취소되었습니다.");
                }
            );
        }

        // 계정 삭제 실행
        private void DeleteAccount()
        {
            FirebaseUser user = FirebaseManager.Auth.CurrentUser;
            if (user == null)
            {
                ShowErrorMessage("사용자 정보를 찾을 수 없습니다.");
                return;
            }

            // 비밀번호 재인증 후 삭제
            Credential credential = EmailAuthProvider.GetCredential(user.Email, passwordInput.text);
            user.ReauthenticateAsync(credential)
                .ContinueWithOnMainThread(authTask =>
                {
                    if (authTask.IsCanceled)
                    {
                        ShowErrorMessage("재인증이 취소되었습니다.");
                        return;
                    }
                    if (authTask.IsFaulted)
                    {
                        ShowErrorMessage("비밀번호가 올바르지 않습니다.");
                        return;
                    }

                    // 재인증 성공 시 계정 삭제
                    user.DeleteAsync()
                        .ContinueWithOnMainThread(deleteTask =>
                        {
                            if (deleteTask.IsCanceled)
                            {
                                ShowErrorMessage("계정 삭제가 취소되었습니다.");
                                return;
                            }
                            if (deleteTask.IsFaulted)
                            {
                                ShowErrorMessage($"계정 삭제 실패: {deleteTask.Exception}");
                                return;
                            }

                            // 삭제 성공
                            Debug.Log("계정 삭제 성공");

                            // 로그아웃 처리
                            FirebaseManager.Auth.SignOut();

                            // 모든 팝업 정리 (확인 팝업 등 모든 팝업 제거)
                            UIManager.Instance.CleanPopUp();

                            // 로그인 패널로 이동
                                UIManager.Instance.ShowPopUp<LoginPopUp>();
                             
                            
                        });
                });
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

        // 입력 필드 초기화
        private void ResetInputs()
        {
            if (idInput != null) idInput.text = "";
            if (passwordInput != null) passwordInput.text = "";
        }

        // 팝업이 활성화될 때 호출
        private void OnEnable()
        {
            ResetInputs();
        }
    }
}
