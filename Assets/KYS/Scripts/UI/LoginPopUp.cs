using Firebase.Auth;
using Firebase.Extensions;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KYS
{
    public class LoginPopUp : BaseUI
    {
        [Header("Audio Settings")]
        [SerializeField] private string loginBgmName = "BGM_ParTeaMain";

        private TMP_InputField idInput => GetUI<TMP_InputField>("IDField");
        private TMP_InputField passInput => GetUI<TMP_InputField>("PasswordField");

        public bool isLoginProblem = false;

        private new void Awake()
        {
            base.Awake();
            canCloseWithESC = false; // ESC로 닫을 수 없음

            // SFX가 포함된 이벤트 등록
            GetEventWithSFX("SignUpButton", "SFX_ButtonClick").Click += SignUp;
            GetEventWithSFX("LoginButton", "SFX_ButtonClick").Click += Login;
            GetEventWithSFX("FindPasswordButton", "SFX_ButtonClick").Click += FindPassword;

            // UIManager에 자신을 등록
            UIManager.Instance.RegisterMainPanel("LoginPopUp", gameObject);

            var menuButton = GetEventWithSFX("TitleMenuButton", "SFX_ButtonClick");
            if (menuButton != null)
            {
                menuButton.Click -= OnTitleMenu;
                menuButton.Click += OnTitleMenu;
            }



        }

        private void OnEnable()
        {
            //Debug.Log("[LoginPanel] OnEnable 호출됨 - 입력 필드 초기화 시작");
            // 패널이 활성화될 때마다 입력 필드 초기화
            ResetInputs();

            // 로그인 화면 진입 시 BGM 시작 (안전한 체크 포함)
            StartCoroutine(StartLoginBGMWithDelay());
        }

        // 지연된 BGM 시작 (오디오 매니저 생성 대기)
        private IEnumerator StartLoginBGMWithDelay()
        {
            // 오디오 매니저가 생성될 때까지 대기
            yield return new WaitForSeconds(0.1f);

            // 오디오 매니저 존재 확인
            if (Manager.Audio != null)
            {
                StartLoginBGM();
            }
            else
            {
                Debug.LogWarning("[LoginPopUp] AudioManager가 아직 생성되지 않았습니다. BGM 시작을 건너뜁니다.");
            }
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
                Debug.LogError("로그인 오류: " + task.Exception);
                ShowLoginFailMessage("로그인 중 오류가 발생했습니다.");
                return;
            }

            var authResult = task.Result;
            if (authResult != null && authResult.User != null)
            {
                Debug.Log("로그인 성공: " + authResult.User.UserId);

                // 로그인 성공 후 검증 진행
                CheckUserVerification(authResult.User);
            }
            else
            {
                Debug.Log("로그인 실패: 결과가 null");
                ShowLoginFailMessage("로그인에 실패했습니다.");

            }
            });
        }

        private void FindPassword(PointerEventData eventData)
        {
            UIManager.Instance.ShowPopUp<PasswordResetPopUp>();
        }


        private void OnTitleMenu(PointerEventData eventData)
        {
            UIManager.Instance.ShowPopUp<TitleMenuPopUp>();
        }


        // 사용자 검증 확인
        private void CheckUserVerification(FirebaseUser user)
        {
            Debug.Log($"[LoginPopUp] 사용자 검증 시작 - 이메일 인증: {user.IsEmailVerified}, 닉네임: {user.DisplayName}");

            // 이메일 인증 확인
            if (!user.IsEmailVerified)
            {
                Debug.Log("[LoginPopUp] 이메일 인증이 필요합니다.");
                ShowLoginSuccessMessage(); // 로그인 성공 메시지는 표시
                UIManager.Instance.ShowPopUp<EmailPopUp>();
                return;
            }

            // 닉네임 설정 확인
            if (string.IsNullOrEmpty(user.DisplayName))
            {
                Debug.Log("[LoginPopUp] 닉네임 설정이 필요합니다.");
                ShowLoginSuccessMessage(); // 로그인 성공 메시지는 표시
                UIManager.Instance.ShowPopUp<NicknamePopUp>();
                return;
            }

            // 모든 검증 통과 - 로비로 이동
            Debug.Log("[LoginPopUp] 모든 검증 통과. 로비로 이동합니다.");
            ShowLoginSuccessMessage();
            UIManager.Instance.ShowPopUp<LobbyPopUp>();
        }


        private void ShowLoginFailMessage(string message)
        {
            MessagePopUp messagePopUp = UIManager.Instance.ShowPopUp<MessagePopUp>();
            if (messagePopUp != null)
            {
                messagePopUp.SetMessage(message, "확인");
            }
        }

        private void ShowLoginSuccessMessage()
        {
            Debug.Log("로그인 성공!");
            // 로그인 성공 시 별도의 메시지는 표시하지 않음 (자동으로 로비로 이동)
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

        // 로그인 BGM 시작
        private void StartLoginBGM()
        {
            // 오디오 매니저 존재 확인
            if (Manager.Audio == null)
            {
                Debug.LogWarning("[LoginPopUp] AudioManager가 null입니다. BGM 시작을 건너뜁니다.");
                return;
            }

            // 기본 볼륨 설정 (PlayerPrefs에 저장된 값이 없으면 기본값 설정)
            InitializeDefaultAudioSettings();

            // 로그인 BGM 재생
            if (!string.IsNullOrEmpty(loginBgmName))
            {
                Manager.Audio.BgmPlay(loginBgmName, 0f); // fadeDuration을 0으로 설정하여 즉시 재생
                Debug.Log($"[LoginPopUp] 로그인 BGM 시작: {loginBgmName}");
            }
        }

        // 기본 오디오 설정 초기화
        private void InitializeDefaultAudioSettings()
        {
            // 오디오 매니저 존재 확인
            if (Manager.Audio == null)
            {
                Debug.LogWarning("[LoginPopUp] AudioManager가 null입니다. 기본 설정을 건너뜁니다.");
                return;
            }

            // PlayerPrefs에 설정이 없으면 기본값 설정
            if (!PlayerPrefs.HasKey("MasterVolume"))
            {
                // PlayerPrefs.SetFloat("MasterVolume", 0.5f); // 주석 처리
                PlayerPrefs.SetFloat("BGMVolume", 0.5f);
                PlayerPrefs.SetFloat("SFXVolume", 0.5f);
                PlayerPrefs.Save();
                Debug.Log("[LoginPopUp] 기본 오디오 설정 완료 (0.5)");
            }

            // 설정 로드 (기본값 0.5)
            // float masterVol = PlayerPrefs.GetFloat("MasterVolume", 0.5f); // 주석 처리
            float bgmVol = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
            float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.5f);

            // 오디오 매니저에 적용
            // Manager.Audio.masterVolume = masterVol; // 주석 처리
            Manager.Audio.bgmVolume = bgmVol;
            Manager.Audio.sfxVolume = sfxVol;
        }


    }
}
