using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

namespace KYS
{
    public class ReceiveGameUI : MonoBehaviourPunCallbacks
    {
        [Header("Time UI")]
        [SerializeField] private TextMeshProUGUI timeText;
        
        [Header("Score UI")]
        [SerializeField] private Transform scorePanel;
        [SerializeField] private GameObject playerScorePrefab;
        
        [Header("Game End UI")]
        [SerializeField] private GameObject gameEndPanel;
        [SerializeField] private TextMeshProUGUI gameEndText;
        
        [Header("Countdown UI")]
        [SerializeField] private GameObject countdownPanel;
        [SerializeField] private TextMeshProUGUI countdownText;
        
        [Header("Joystick UI")]
        [SerializeField] private GameObject joystickPanel;
        [SerializeField] private Image joystickBackground;
        [SerializeField] private Image joystickHandle;
        [SerializeField] private float joystickRadius = 50f;
        
        private Dictionary<int, PlayerScoreUI> playerScoreUIs = new Dictionary<int, PlayerScoreUI>();
        private Vector2 joystickInput = Vector2.zero;
        private bool isJoystickActive = false;
        private Vector2 joystickStartPos;
        
        public Vector2 JoystickInput => joystickInput;
        public bool IsJoystickActive => isJoystickActive;
        
        private void Start()
        {
            if (gameEndPanel != null)
            {
                gameEndPanel.SetActive(false);
            }
            
            if (countdownPanel != null)
            {
                countdownPanel.SetActive(false);
            }
            
            SetupJoystick();
            SetupSafeArea();
        }
        
        private void SetupSafeArea()
        {
            // 모바일에서 Safe Area 적용
            #if UNITY_ANDROID || UNITY_IOS
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Safe Area 계산
                Rect safeArea = Screen.safeArea;
                Vector2 anchorMin = safeArea.position;
                Vector2 anchorMax = safeArea.position + safeArea.size;
                
                anchorMin.x /= Screen.width;
                anchorMin.y /= Screen.height;
                anchorMax.x /= Screen.width;
                anchorMax.y /= Screen.height;
                
                rectTransform.anchorMin = anchorMin;
                rectTransform.anchorMax = anchorMax;
            }
            #endif
        }
        
        private void SetupJoystick()
        {
            if (joystickPanel != null)
            {
                joystickPanel.SetActive(true);
                PositionJoystickAtBottomCenter();
            }
        }
        
        private void PositionJoystickAtBottomCenter()
        {
            if (joystickPanel != null)
            {
                RectTransform joystickRect = joystickPanel.GetComponent<RectTransform>();
                if (joystickRect != null)
                {
                    // 중앙 하단에 위치하도록 설정
                    joystickRect.anchorMin = new Vector2(0.5f, 0f);
                    joystickRect.anchorMax = new Vector2(0.5f, 0f);
                    joystickRect.pivot = new Vector2(0.5f, 0f);
                    
                    // 하단에서 약간 위로 올림 (100px)
                    joystickRect.anchoredPosition = new Vector2(0f, 100f);
                }
            }
        }
        
        private void Update()
        {
            HandleJoystickInput();
        }
        
        private void HandleJoystickInput()
        {
            if (joystickPanel == null) return;
            
            // 터치 입력 처리
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        // 조이스틱 영역 내에서 터치 시작
                        if (RectTransformUtility.RectangleContainsScreenPoint(joystickBackground.rectTransform, touch.position))
                        {
                            isJoystickActive = true;
                            joystickStartPos = touch.position;
                            joystickInput = Vector2.zero;
                        }
                        break;
                        
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (isJoystickActive)
                        {
                            Vector2 delta = touch.position - joystickStartPos;
                            joystickInput = Vector2.ClampMagnitude(delta / joystickRadius, 1f);
                            
                            // 조이스틱 핸들 위치 업데이트
                            if (joystickHandle != null)
                            {
                                joystickHandle.rectTransform.anchoredPosition = joystickInput * joystickRadius;
                            }
                        }
                        break;
                        
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (isJoystickActive)
                        {
                            isJoystickActive = false;
                            joystickInput = Vector2.zero;
                            
                            // 조이스틱 핸들 원위치
                            if (joystickHandle != null)
                            {
                                joystickHandle.rectTransform.anchoredPosition = Vector2.zero;
                            }
                        }
                        break;
                }
            }
            else
            {
                // 마우스 입력 처리 (PC 테스트용)
                if (Input.GetMouseButtonDown(0))
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(joystickBackground.rectTransform, Input.mousePosition))
                    {
                        isJoystickActive = true;
                        joystickStartPos = Input.mousePosition;
                        joystickInput = Vector2.zero;
                    }
                }
                else if (Input.GetMouseButton(0) && isJoystickActive)
                {
                    Vector2 delta = (Vector2)Input.mousePosition - joystickStartPos;
                    joystickInput = Vector2.ClampMagnitude(delta / joystickRadius, 1f);
                    
                    if (joystickHandle != null)
                    {
                        joystickHandle.rectTransform.anchoredPosition = joystickInput * joystickRadius;
                    }
                }
                else if (Input.GetMouseButtonUp(0) && isJoystickActive)
                {
                    isJoystickActive = false;
                    joystickInput = Vector2.zero;
                    
                    if (joystickHandle != null)
                    {
                        joystickHandle.rectTransform.anchoredPosition = Vector2.zero;
                    }
                }
            }
        }
        
        private void InitializeScoreUI()
        {
            // 기존 점수 UI 제거
            foreach (Transform child in scorePanel)
            {
                Destroy(child.gameObject);
            }
            
            playerScoreUIs.Clear();
            
            // playerScorePrefab이 할당되지 않은 경우 처리
            if (playerScorePrefab == null)
            {
                Debug.LogWarning("PlayerScorePrefab이 할당되지 않았습니다. 기본 UI를 생성합니다.");
                CreateDefaultScoreUI();
                return;
            }
            
            // 각 플레이어별 점수 UI 생성
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                GameObject scoreUI = Instantiate(playerScorePrefab, scorePanel);
                PlayerScoreUI playerScoreUI = scoreUI.GetComponent<PlayerScoreUI>();
                
                if (playerScoreUI != null)
                {
                    playerScoreUI.Initialize(player.NickName, player.ActorNumber);
                    playerScoreUIs[player.ActorNumber] = playerScoreUI;
                }
            }
        }
        
        private void CreateDefaultScoreUI()
        {
            // 기본 점수 UI 생성
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                GameObject scoreUI = new GameObject($"PlayerScore_{player.ActorNumber}");
                scoreUI.transform.SetParent(scorePanel);
                
                RectTransform rectTransform = scoreUI.AddComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(0, -50 * player.ActorNumber);
                rectTransform.sizeDelta = new Vector2(200, 40);
                
                // 배경 이미지 추가 (플레이어 색상용)
                GameObject backgroundObj = new GameObject("Background");
                backgroundObj.transform.SetParent(scoreUI.transform);
                RectTransform bgRectTransform = backgroundObj.AddComponent<RectTransform>();
                bgRectTransform.anchorMin = Vector2.zero;
                bgRectTransform.anchorMax = Vector2.one;
                bgRectTransform.offsetMin = Vector2.zero;
                bgRectTransform.offsetMax = Vector2.zero;
                
                Image backgroundImage = backgroundObj.AddComponent<Image>();
                backgroundImage.color = Color.white; // 기본 색상
                
                // 플레이어 이름 텍스트
                GameObject nameObj = new GameObject("PlayerName");
                nameObj.transform.SetParent(scoreUI.transform);
                RectTransform nameRectTransform = nameObj.AddComponent<RectTransform>();
                nameRectTransform.anchorMin = new Vector2(0, 0);
                nameRectTransform.anchorMax = new Vector2(0.6f, 1);
                nameRectTransform.offsetMin = Vector2.zero;
                nameRectTransform.offsetMax = Vector2.zero;
                
                TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
                nameText.text = player.NickName;
                nameText.fontSize = 14;
                nameText.color = Color.black;
                nameText.alignment = TextAlignmentOptions.Left;
                
                // 점수 텍스트
                GameObject scoreObj = new GameObject("Score");
                scoreObj.transform.SetParent(scoreUI.transform);
                RectTransform scoreRectTransform = scoreObj.AddComponent<RectTransform>();
                scoreRectTransform.anchorMin = new Vector2(0.6f, 0);
                scoreRectTransform.anchorMax = new Vector2(1, 1);
                scoreRectTransform.offsetMin = Vector2.zero;
                scoreRectTransform.offsetMax = Vector2.zero;
                
                TextMeshProUGUI scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
                scoreText.text = "0";
                scoreText.fontSize = 16;
                scoreText.color = Color.black;
                scoreText.alignment = TextAlignmentOptions.Right;
                
                // PlayerScoreUI 컴포넌트 추가 및 설정
                PlayerScoreUI playerScoreUI = scoreUI.AddComponent<PlayerScoreUI>();
                
                // UI 요소들 설정
                playerScoreUI.SetUIElements(nameText, scoreText, backgroundImage);
                
                playerScoreUI.Initialize(player.NickName, player.ActorNumber);
                playerScoreUIs[player.ActorNumber] = playerScoreUI;
            }
        }
        
        public void UpdateTime(float time)
        {
            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(time / 60);
                int seconds = Mathf.FloorToInt(time % 60);
                timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
                
                // 시간이 10초 이하일 때 빨간색으로 변경
                if (time <= 10f)
                {
                    timeText.color = Color.red;
                }
                else
                {
                    timeText.color = Color.white;
                }
            }
        }
        
        public void UpdateScore(Dictionary<int, int> scores)
        {
            foreach (var score in scores)
            {
                if (playerScoreUIs.ContainsKey(score.Key))
                {
                    playerScoreUIs[score.Key].UpdateScore(score.Value);
                }
            }
        }
        
        public void ShowGameEnd()
        {
            if (gameEndPanel != null)
            {
                gameEndPanel.SetActive(true);
                
                if (gameEndText != null)
                {
                    gameEndText.text = "게임 종료!\n결과를 확인하세요...";
                }
            }
        }
        
        // 카운트다운 UI 관련 메서드들
        public void ShowCountdown()
        {
            if (countdownPanel != null)
            {
                countdownPanel.SetActive(true);
            }
        }
        
        public void HideCountdown()
        {
            if (countdownPanel != null)
            {
                countdownPanel.SetActive(false);
            }
        }
        
        public void UpdateCountdownText(int count)
        {
            if (countdownText != null)
            {
                if (count > 0)
                {
                    countdownText.text = count.ToString();
                }
                else
                {
                    countdownText.text = "시작!";
                }
            }
        }
        
        public void AddPlayer(Player newPlayer)
        {
            if (!playerScoreUIs.ContainsKey(newPlayer.ActorNumber))
            {
                if (playerScorePrefab != null)
                {
                    GameObject scoreUI = Instantiate(playerScorePrefab, scorePanel);
                    PlayerScoreUI playerScoreUI = scoreUI.GetComponent<PlayerScoreUI>();
                    
                    if (playerScoreUI != null)
                    {
                        playerScoreUI.Initialize(newPlayer.NickName, newPlayer.ActorNumber);
                        playerScoreUIs[newPlayer.ActorNumber] = playerScoreUI;
                    }
                }
                else
                {
                    CreateDefaultScoreUI();
                }
            }
        }
        
        public void RemovePlayer(int actorNumber)
        {
            if (playerScoreUIs.ContainsKey(actorNumber))
            {
                if (playerScoreUIs[actorNumber] != null)
                {
                    Destroy(playerScoreUIs[actorNumber].gameObject);
                }
                playerScoreUIs.Remove(actorNumber);
            }
        }
        
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (changedProps.ContainsKey("isLoaded"))
            {
                // 모든 플레이어가 로드되었는지 확인
                bool allPlayersLoaded = true;
                foreach (Player player in PhotonNetwork.PlayerList)
                {
                    if (!player.CustomProperties.ContainsKey("isLoaded") || !(bool)player.CustomProperties["isLoaded"])
                    {
                        allPlayersLoaded = false;
                        break;
                    }
                }
                
                if (allPlayersLoaded)
                {
                    InitializeScoreUI();
                }
            }
        }
    }
} 