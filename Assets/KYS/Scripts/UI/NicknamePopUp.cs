using Firebase.Auth;
using Firebase.Extensions;
using KYS;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;
using UnityEngine.UI;

namespace KYS
{
    public class NicknamePopUp : BaseUI
    {
        private TMP_InputField nicknameInput => GetUI<TMP_InputField>("NicknameField");

        private new void Awake()
        {
            base.Awake();
            canCloseWithESC = false; // ESC로 닫을 수 없음

            // SFX가 포함된 버튼 이벤트 등록
            GetEventWithSFX("ConfirmButton", "SFX_ButtonClick").Click += Confirm;
            
            // Back 버튼 이벤트 등록 (SFX 포함)
            GetBackEvent("BackButton", "SFX_ButtonClickBack").Click += Back;
        }

        private void Confirm(PointerEventData eventData)
        {
            // 버튼 비활성화
            Button confirmButton = GetUI<Button>("ConfirmButton");
            if (confirmButton != null)
            {
                confirmButton.interactable = false;
            }

            if (string.IsNullOrEmpty(nicknameInput.text))
            {
                ShowErrorMessage("닉네임을 입력해주세요.");
                // 버튼 다시 활성화
                if (confirmButton != null)
                {
                    confirmButton.interactable = true;
                }
                return;
            }

            UserProfile profile = new UserProfile();
            profile.DisplayName = nicknameInput.text;

            FirebaseUser user = FirebaseManager.Auth.CurrentUser;
            user.UpdateUserProfileAsync(profile)
                .ContinueWithOnMainThread(task =>
                {
                    // 버튼 다시 활성화
                    if (confirmButton != null)
                    {
                        confirmButton.interactable = true;
                    }

                    if (task.IsCanceled)
                    {
                        ShowErrorMessage("닉네임 변경이 취소되었습니다.");
                        return;
                    }
                    if (task.IsFaulted)
                    {
                        ShowErrorMessage($"닉네임 변경 실패");
                        Debug.Log($"오류 내용을 확인하세요 Log {task.Exception}");
                        return;
                    }

                    Debug.Log("닉네임 변경 성공");
                    ShowSuccessMessage("닉네임이 성공적으로 설정되었습니다.");

                    // Photon 연결 후 닉네임 동기화
                    if (PhotonManager.Instance != null)
                    {
                        // Photon에 연결되어 있지 않으면 연결 시도
                        if (!PhotonNetwork.IsConnected)
                        {
                            Debug.Log("Photon 연결을 시도합니다...");
                            PhotonManager.Instance.ConnectToPhoton();
                        }
                        
                        // 닉네임 동기화 (연결 상태와 관계없이 시도)
                        PhotonManager.Instance.SyncNicknameWithFirebase();
                    }

                    // 팝업 닫기
                    UIManager.Instance.ClosePopUp();

                    // 로비 팝업으로 이동
                    UIManager.Instance.ShowPopUp<LobbyPopUp>();
                    
                    // RoomPopUp이 활성화되어 있다면 닉네임도 업데이트
                    RoomPopUp roomPopUp = UIManager.Instance.FindActivePopUp<RoomPopUp>();
                    if (roomPopUp != null)
                    {
                        roomPopUp.RefreshPlayerNicknames();
                    }
                });
        }

        private void Back(PointerEventData eventData)
        {
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
