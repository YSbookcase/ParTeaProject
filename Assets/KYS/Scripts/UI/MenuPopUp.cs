using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using KYS;
using Photon.Pun;
using Photon.Realtime;

namespace KYS
{

    public class MenuPopUp : BaseUI
    {

        private new void Awake()
        {
            base.Awake();

            GetEvent("LogOutButton").Click += LogOut;
            GetEvent("EditProfileButton").Click += EditProfile;
            GetEvent("BackButton").Click += Back;

        }



        private void Start()
        {
            // Firebase 사용자 정보 초기화
            InitializePanel();
        }

        private void OnEnable()
        {
            // Firebase 사용자 정보 업데이트
            if (gameObject.activeInHierarchy)
            {
                RegisterEvents();
            }
        }



        private void OnDisable()
        {
            // Firebase 이벤트 해제
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

        private void InitializePanel()
        {
            RegisterEvents();
            // 패널이 활성화될 때 로그인 정보 업데이트

        }
        private void RegisterEvents()
        {
            // 기존 이벤트 해제 후 다시 등록 (중복 방지)
            var logoutButton = GetEvent("LogOutButton");
            var editProfileButton = GetEvent("EditProfileButton");


            if (logoutButton != null)
            {
                logoutButton.Click -= LogOut; // 기존 이벤트 해제
                logoutButton.Click += LogOut;
            }

            if (editProfileButton != null)
            {
                editProfileButton.Click -= EditProfile; // 기존 이벤트 해제
                editProfileButton.Click += EditProfile;
            }

        }

        // Firebase 관련 메서드들 (기존 코드 유지)
        private void LogOut(PointerEventData eventData)
        {
            // Firebase 로그아웃
            FirebaseManager.Auth.SignOut();

            // Photon 연결 해제
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }

            // 모든 팝업 정리 (로비에서 나가기 전에 모든 팝업 정리)
            UIManager.Instance.CleanPopUp();

            // UIManager에서 등록된 LoginPanel을 찾아서 활성화
            GameObject loginPanel = UIManager.Instance.GetMainPanel("LoginPopUp");
            if (loginPanel != null)
            {
                // 로그인 패널을 비활성화했다가 다시 활성화해서 OnEnable 호출 보장
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

        private void Back(PointerEventData eventData)
        {
            // 팝업 닫기
            UIManager.Instance.ClosePopUp();
        }


    }
}