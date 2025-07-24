using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using UnityEngine.EventSystems;
using Firebase.Extensions;

namespace KYS
{
    public class LobbyPopUp : BaseUI
    {
        //[SerializeField] GameObject loginPanel; // UIManager를 통해 접근하므로 제거

        //[Header("표시 정보")]
        //[SerializeField] TMP_Text emailText;
        //[SerializeField] TMP_Text nameText;
        //[SerializeField] TMP_Text userIdText;

        //[Header("버튼")]
        //[SerializeField] Button logoutButton;
        //[SerializeField] Button editProfileButton;

        private TMP_Text UIemailText => GetUI<TMP_Text>("E-MailTextContent");
        private TMP_Text UInameText => GetUI<TMP_Text>("NameTextContent");
        //private TMP_Text UIuserIdText => GetUI<TMP_Text>("UserIDTextContent");





        private new void Awake()
        {
            // BaseUI의 Awake 호출
            base.Awake();

            // UIManager에 자신을 등록
            //UIManager.Instance.RegisterMainPanel("LobbyPanel", gameObject);



        }

        private void Start()
        {
            // BaseUI의 Awake가 완료된 후 초기화
            InitializePanel();
        }

        private void OnEnable()
        {
            // 이미 초기화되었다면 이벤트만 다시 등록
            if (gameObject.activeInHierarchy)
            {
                RegisterEvents();
            }



        }

        private void InitializePanel()
        {
            RegisterEvents();
            // 패널이 활성화될 때 자동으로 로그인 정보 업데이트
            LoginInfo();
        }

        private void RegisterEvents()
        {
            // 기존 이벤트 제거 후 다시 등록 (중복 방지)
            var logoutButton = GetEvent("LogOutButton");
            var editProfileButton = GetEvent("EditProfileButton");
            var deleteUserButton = GetEvent("DeleteUserButton");

            if (logoutButton != null)
            {
                logoutButton.Click -= LogOut; // 기존 이벤트 제거
                logoutButton.Click += LogOut;
            }

            if (editProfileButton != null)
            {
                editProfileButton.Click -= EditProfile; // 기존 이벤트 제거
                editProfileButton.Click += EditProfile;
            }
            if (deleteUserButton != null)
            {
                deleteUserButton.Click -= DeleteUser;
                deleteUserButton.Click += DeleteUser;
            }
        }

        private void OnDisable()
        {
            // 이벤트 정리
            var logoutButton = GetEvent("LogOutButton");
            var editProfileButton = GetEvent("EditProfileButton");

            if (logoutButton != null)
            {
                logoutButton.Click -= LogOut;
            }

            if (editProfileButton != null)
            {
                editProfileButton.Click -= EditProfile;
            }
        }

        private void LogOut(PointerEventData eventData)
        {
            // YSK 네임스페이스의 FirebaseManager 사용
            FirebaseManager.Auth.SignOut();

            // 모든 팝업 정리 (닉네임 설정 화면 등 모든 팝업 제거)
            UIManager.Instance.CleanPopUp();

            // UIManager를 통해 LoginPanel에 접근하여 활성화
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
            else
            {
                Debug.LogError("[LobbyPopUp] LoginPanel을 찾을 수 없습니다.");
            }
        }
        private void EditProfile(PointerEventData eventData)
        {
            //UIManager.Instance.ShowPopUp<EditPopUp>();
        }

        public void LoginInfo()
        {
            // FirebaseManager 사용
            FirebaseUser user = FirebaseManager.Auth.CurrentUser;

            UIemailText.text = user.Email;
            UInameText.text = user.DisplayName;
            //UIuserIdText.text = user.UserId;
        }

        // 계정 삭제 버튼 클릭 시
        private void DeleteUser(PointerEventData eventData)
        {
            // DeletePopUp 띄우기
            //UIManager.Instance.ShowPopUp<DeletePopUp>();
        }


    }


}
