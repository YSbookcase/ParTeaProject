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
        [SerializeField] private TextMeshProUGUI timeText; // 시간 표시 텍스트
        
        [Header("Score UI")]
        [SerializeField] private Transform scorePanel; // 점수 패널
        [SerializeField] private GameObject playerScorePrefab; // 플레이어 점수 프리팹
        
        [Header("Game End UI")]
        [SerializeField] private GameObject gameEndPanel; // 게임 종료 패널
        [SerializeField] private TextMeshProUGUI gameEndText; // 게임 종료 텍스트
        
        [Header("Countdown UI")]
        [SerializeField] private GameObject countdownPanel; // 카운트다운 패널
        [SerializeField] private TextMeshProUGUI countdownText; // 카운트다운 텍스트
        
        [Header("Joystick UI")]
        [SerializeField] private GameObject joystickPanel; // 조이스틱 패널
        [SerializeField] private Image joystickBackground; // 조이스틱 배경
        [SerializeField] private Image joystickHandle; // 조이스틱 핸들
        [SerializeField] private float joystickRadius = 50f; // 조이스틱 반지름
        
        // Private variables
        private Dictionary<int, PlayerScoreUI> playerScoreUIs = new Dictionary<int, PlayerScoreUI>(); // 플레이어 점수 UI 딕셔너리
        private Vector2 joystickInput = Vector2.zero; // 조이스틱 입력 값
        private bool isJoystickActive = false; // 조이스틱 활성화 여부
        private Vector2 joystickStartPos; // 조이스틱 시작 위치
        
        // Screen size tracking
        private float lastScreenWidth; // 이전 화면 너비
        private float lastScreenHeight; // 이전 화면 높이
        
        // Public properties
        public Vector2 JoystickInput => joystickInput;
        public bool IsJoystickActive => isJoystickActive;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeUI();
            SetupJoystick();
            SetupSafeArea();
            InitializeScreenSizeTracking();
        }
        
        private void Update()
        {
            HandleJoystickInput();
            CheckScreenSizeChange();
        }
        
        private void InitializeScreenSizeTracking() // 화면 크기 추적 초기화
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }
        
        private void CheckScreenSizeChange() // 화면 크기 변경 체크
        {
            if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
            {
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                UpdateScoreUIGridLayout(); // ScoreUI 그리드 레이아웃 업데이트
            }
        }

        #endregion

        #region Initialization

        private void InitializeUI() // UI 초기화
        {
            SetPanelActive(gameEndPanel, false);
            SetPanelActive(countdownPanel, false);
        }
        
        private void SetPanelActive(GameObject panel, bool active) // 패널 활성화 설정
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
        
        private void SetupSafeArea() // Safe Area 설정
        {
            #if UNITY_ANDROID || UNITY_IOS
            ApplySafeArea();
            #endif
        }
        
        private void ApplySafeArea() // Safe Area 적용
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;
            
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

        #endregion

        #region Joystick Management

        private void SetupJoystick() // 조이스틱 설정
        {
            if (joystickPanel != null)
            {
                joystickPanel.SetActive(true);
                PositionJoystickAtBottomCenter();
            }
        }
        
        private void PositionJoystickAtBottomCenter() // 조이스틱 하단 중앙 위치 설정
        {
            if (joystickPanel == null) return;
            
            RectTransform joystickRect = joystickPanel.GetComponent<RectTransform>();
            if (joystickRect != null)
            {
                joystickRect.anchorMin = new Vector2(0.5f, 0f);
                joystickRect.anchorMax = new Vector2(0.5f, 0f);
                joystickRect.pivot = new Vector2(0.5f, 0f);
                joystickRect.anchoredPosition = new Vector2(0f, 100f);
            }
        }
        
        private void HandleJoystickInput() // 조이스틱 입력 처리
        {
            if (joystickPanel == null) return;
            
            if (Input.touchCount > 0)
            {
                HandleTouchInput();
            }
            else
            {
                HandleMouseInput();
            }
        }
        
        private void HandleTouchInput() // 터치 입력 처리
        {
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleTouchBegan(touch);
                    break;
                    
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    HandleTouchMoved(touch);
                    break;
                    
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    HandleTouchEnded();
                    break;
            }
        }
        
        private void HandleTouchBegan(Touch touch) // 터치 시작 처리
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(joystickBackground.rectTransform, touch.position))
            {
                isJoystickActive = true;
                joystickStartPos = touch.position;
                joystickInput = Vector2.zero;
            }
        }
        
        private void HandleTouchMoved(Touch touch) // 터치 이동 처리
        {
            if (!isJoystickActive) return;
            
            Vector2 delta = touch.position - joystickStartPos;
            joystickInput = Vector2.ClampMagnitude(delta / joystickRadius, 1f);
            UpdateJoystickHandle();
        }
        
        private void HandleTouchEnded() // 터치 종료 처리
        {
            if (!isJoystickActive) return;
            
            isJoystickActive = false;
            joystickInput = Vector2.zero;
            ResetJoystickHandle();
        }
        
        private void HandleMouseInput() // 마우스 입력 처리
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseDown();
            }
            else if (Input.GetMouseButton(0) && isJoystickActive)
            {
                HandleMouseDrag();
            }
            else if (Input.GetMouseButtonUp(0) && isJoystickActive)
            {
                HandleMouseUp();
            }
        }
        
        private void HandleMouseDown() // 마우스 다운 처리
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(joystickBackground.rectTransform, Input.mousePosition))
            {
                isJoystickActive = true;
                joystickStartPos = Input.mousePosition;
                joystickInput = Vector2.zero;
            }
        }
        
        private void HandleMouseDrag() // 마우스 드래그 처리
        {
            Vector2 delta = (Vector2)Input.mousePosition - joystickStartPos;
            joystickInput = Vector2.ClampMagnitude(delta / joystickRadius, 1f);
            UpdateJoystickHandle();
        }
        
        private void HandleMouseUp() // 마우스 업 처리
        {
            isJoystickActive = false;
            joystickInput = Vector2.zero;
            ResetJoystickHandle();
        }
        
        private void UpdateJoystickHandle() // 조이스틱 핸들 업데이트
        {
            if (joystickHandle != null)
            {
                joystickHandle.rectTransform.anchoredPosition = joystickInput * joystickRadius;
            }
        }
        
        private void ResetJoystickHandle() // 조이스틱 핸들 리셋
        {
            if (joystickHandle != null)
            {
                joystickHandle.rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        #endregion

        #region Score UI Management

        private void InitializeScoreUI() // 점수 UI 초기화
        {
            ClearScoreUI();
            
            if (playerScorePrefab == null)
            {
                CreateDefaultScoreUI();
                return;
            }
            
            CreatePlayerScoreUIs();
        }
        
        private void ClearScoreUI() // 점수 UI 정리
        {
            foreach (Transform child in scorePanel)
            {
                Destroy(child.gameObject);
            }
            
            playerScoreUIs.Clear();
        }
        
        private void CreatePlayerScoreUIs() // 플레이어 점수 UI 생성
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                CreatePlayerScoreUI(player);
            }
        }
        
        private void CreatePlayerScoreUI(Player player) // 개별 플레이어 점수 UI 생성
        {
            GameObject scoreUI = Instantiate(playerScorePrefab, scorePanel);
            PlayerScoreUI playerScoreUI = scoreUI.GetComponent<PlayerScoreUI>();
            
            if (playerScoreUI != null)
            {
                playerScoreUI.Initialize(player.NickName, player.ActorNumber);
                playerScoreUIs[player.ActorNumber] = playerScoreUI;
                
                // 2x2 반응형 배치 적용
                PositionScoreUIInGrid(scoreUI, player.ActorNumber);
            }
        }
        
        private void CreateDefaultScoreUI() // 기본 점수 UI 생성
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                CreateDefaultPlayerScoreUI(player);
            }
        }
        
        private void CreateDefaultPlayerScoreUI(Player player) // 기본 플레이어 점수 UI 생성
        {
            GameObject scoreUI = CreateScoreUIGameObject(player);
            SetupScoreUIRectTransform(scoreUI, player);
            CreateScoreUIBackground(scoreUI);
            CreateScoreUITexts(scoreUI, player);
            SetupPlayerScoreUIComponent(scoreUI, player);
        }
        
        private GameObject CreateScoreUIGameObject(Player player) // 점수 UI 게임오브젝트 생성
        {
            GameObject scoreUI = new GameObject($"PlayerScore_{player.ActorNumber}");
            scoreUI.transform.SetParent(scorePanel);
            return scoreUI;
        }
        
        private void SetupScoreUIRectTransform(GameObject scoreUI, Player player) // 점수 UI RectTransform 설정
        {
            RectTransform rectTransform = scoreUI.AddComponent<RectTransform>();
            rectTransform.sizeDelta = CalculateResponsiveScoreUISize();
        }
        
        private void CreateScoreUIBackground(GameObject scoreUI) // 점수 UI 배경 생성
        {
            GameObject backgroundObj = new GameObject("Background");
            backgroundObj.transform.SetParent(scoreUI.transform);
            
            RectTransform bgRectTransform = backgroundObj.AddComponent<RectTransform>();
            bgRectTransform.anchorMin = Vector2.zero;
            bgRectTransform.anchorMax = Vector2.one;
            bgRectTransform.offsetMin = Vector2.zero;
            bgRectTransform.offsetMax = Vector2.zero;
            
            Image backgroundImage = backgroundObj.AddComponent<Image>();
            backgroundImage.color = Color.white;
        }
        
        private void CreateScoreUITexts(GameObject scoreUI, Player player) // 점수 UI 텍스트 생성
        {
            CreatePlayerNameText(scoreUI, player);
            CreateScoreText(scoreUI);
        }
        
        private void CreatePlayerNameText(GameObject scoreUI, Player player) // 플레이어 이름 텍스트 생성
        {
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
        }
        
        private void CreateScoreText(GameObject scoreUI) // 점수 텍스트 생성
        {
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
        }
        
        private void SetupPlayerScoreUIComponent(GameObject scoreUI, Player player) // PlayerScoreUI 컴포넌트 설정
        {
            PlayerScoreUI playerScoreUI = scoreUI.AddComponent<PlayerScoreUI>();
            
            Image backgroundImage = scoreUI.transform.Find("Background").GetComponent<Image>();
            TextMeshProUGUI nameText = scoreUI.transform.Find("PlayerName").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI scoreText = scoreUI.transform.Find("Score").GetComponent<TextMeshProUGUI>();
            
            playerScoreUI.SetUIElements(nameText, scoreText, backgroundImage);
            playerScoreUI.Initialize(player.NickName, player.ActorNumber);
            playerScoreUIs[player.ActorNumber] = playerScoreUI;
            
            // 2x2 반응형 배치 적용
            PositionScoreUIInGrid(scoreUI, player.ActorNumber);
        }

        #endregion

        #region Responsive Grid Layout

        private void PositionScoreUIInGrid(GameObject scoreUI, int actorNumber) // 2x2 그리드에 ScoreUI 배치
        {
            RectTransform rectTransform = scoreUI.GetComponent<RectTransform>();
            if (rectTransform == null) return;
            
            // 2x2 그리드 위치 계산
            Vector2 gridPosition = CalculateGridPosition(actorNumber);
            Vector2 screenPosition = ConvertGridToScreenPosition(gridPosition);
            
            // 앵커와 위치 설정
            SetGridAnchor(rectTransform, gridPosition);
            rectTransform.anchoredPosition = screenPosition;
        }
        
        private Vector2 CalculateGridPosition(int actorNumber) // 그리드 위치 계산 (0,0 ~ 1,1)
        {
            // ActorNumber를 0-3 범위로 변환 (1,2,3,4 -> 0,1,2,3)
            int gridIndex = (actorNumber - 1) % 4;
            
            // 2x2 그리드 배치:
            // 0,1 (좌상단, 우상단)
            // 2,3 (좌하단, 우하단)
            int row = gridIndex / 2;    // 0 또는 1
            int col = gridIndex % 2;    // 0 또는 1
            
            return new Vector2(col, 1 - row); // Y축은 위에서 아래로 (1->0)
        }
        
        private Vector2 ConvertGridToScreenPosition(Vector2 gridPosition) // 그리드 위치를 화면 위치로 변환
        {
            // 화면 크기 가져오기
            Vector2 screenSize = GetScreenSize();
            
            // 패딩 계산 (화면 크기에 비례)
            float padding = Mathf.Min(screenSize.x, screenSize.y) * 0.05f; // 화면 크기의 5%
            
            // ScoreUI 크기 계산
            Vector2 scoreUISize = CalculateResponsiveScoreUISize();
            
            // 각 그리드 셀의 크기 계산
            Vector2 cellSize = (screenSize - new Vector2(padding * 3, padding * 3)) / 2f; // 2x2 그리드
            
            // 위치 계산 (중앙 정렬)
            Vector2 position = new Vector2(
                (gridPosition.x * cellSize.x) + (cellSize.x / 2f) + padding,
                (gridPosition.y * cellSize.y) + (cellSize.y / 2f) + padding
            );
            
            // 화면 중앙을 기준으로 오프셋 조정
            position -= screenSize / 2f;
            
            return position;
        }
        
        private void SetGridAnchor(RectTransform rectTransform, Vector2 gridPosition) // 그리드 앵커 설정
        {
            // 그리드 위치에 따른 앵커 설정
            if (gridPosition.x == 0) // 좌측
            {
                rectTransform.anchorMin = new Vector2(0, gridPosition.y);
                rectTransform.anchorMax = new Vector2(0, gridPosition.y);
                rectTransform.pivot = new Vector2(0, 0.5f);
            }
            else // 우측
            {
                rectTransform.anchorMin = new Vector2(1, gridPosition.y);
                rectTransform.anchorMax = new Vector2(1, gridPosition.y);
                rectTransform.pivot = new Vector2(1, 0.5f);
            }
        }
        
        private Vector2 CalculateResponsiveScoreUISize() // 반응형 ScoreUI 크기 계산
        {
            Vector2 screenSize = GetScreenSize();
            
            // 화면 크기에 비례한 크기 계산
            float baseWidth = Mathf.Min(screenSize.x, screenSize.y) * 0.4f; // 화면 크기의 40%
            float baseHeight = baseWidth * 0.3f; // 너비의 30%
            
            // 최소/최대 크기 제한
            float minWidth = 150f;
            float maxWidth = 300f;
            float minHeight = 50f;
            float maxHeight = 100f;
            
            float width = Mathf.Clamp(baseWidth, minWidth, maxWidth);
            float height = Mathf.Clamp(baseHeight, minHeight, maxHeight);
            
            return new Vector2(width, height);
        }
        
        private Vector2 GetScreenSize() // 화면 크기 반환
        {
            // Canvas Scaler를 고려한 실제 화면 크기 계산
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return new Vector2(Screen.width, Screen.height);
            }
            else
            {
                // Canvas Scaler가 있는 경우
                CanvasScaler scaler = GetComponentInParent<CanvasScaler>();
                if (scaler != null)
                {
                    switch (scaler.uiScaleMode)
                    {
                        case CanvasScaler.ScaleMode.ScaleWithScreenSize:
                            return scaler.referenceResolution;
                        case CanvasScaler.ScaleMode.ConstantPixelSize:
                            return new Vector2(Screen.width, Screen.height);
                        default:
                            return new Vector2(Screen.width, Screen.height);
                    }
                }
            }
            
            return new Vector2(Screen.width, Screen.height);
        }
        
        private void UpdateScoreUIGridLayout() // 모든 ScoreUI 그리드 레이아웃 업데이트
        {
            foreach (var kvp in playerScoreUIs)
            {
                if (kvp.Value != null)
                {
                    PositionScoreUIInGrid(kvp.Value.gameObject, kvp.Key);
                }
            }
        }
        
        public void RefreshGridLayout() // 그리드 레이아웃 수동 새로고침
        {
            UpdateScoreUIGridLayout();
        }

        #endregion

        #region UI Updates

        public void UpdateTime(float time) // 시간 업데이트
        {
            if (timeText == null) return;
            
            string timeString = FormatTime(time);
            timeText.text = timeString;
            UpdateTimeColor(time);
        }
        
        private string FormatTime(float time) // 시간 포맷팅
        {
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        
        private void UpdateTimeColor(float time) // 시간 색상 업데이트
        {
            timeText.color = time <= 10f ? Color.red : Color.white;
        }
        
        public void UpdateScore(Dictionary<int, int> scores) // 점수 업데이트
        {
            foreach (var score in scores)
            {
                if (playerScoreUIs.ContainsKey(score.Key))
                {
                    playerScoreUIs[score.Key].UpdateScore(score.Value);
                }
            }
        }

        #endregion

        #region Game End UI

        public void ShowGameEnd() // 게임 종료 표시
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

        #endregion

        #region Countdown UI

        public void ShowCountdown() // 카운트다운 표시
        {
            SetPanelActive(countdownPanel, true);
        }
        
        public void HideCountdown() // 카운트다운 숨김
        {
            SetPanelActive(countdownPanel, false);
        }
        
        public void UpdateCountdownText(int count) // 카운트다운 텍스트 업데이트
        {
            if (countdownText == null) return;
            
            countdownText.text = count > 0 ? count.ToString() : "시작!";
        }

        #endregion

        #region Player Management

        public void AddPlayer(Player newPlayer) // 플레이어 추가
        {
            if (playerScoreUIs.ContainsKey(newPlayer.ActorNumber)) return;
            
            if (playerScorePrefab != null)
            {
                CreatePlayerScoreUI(newPlayer);
            }
            else
            {
                CreateDefaultPlayerScoreUI(newPlayer);
            }
        }
        
        public void RemovePlayer(int actorNumber) // 플레이어 제거
        {
            if (!playerScoreUIs.ContainsKey(actorNumber)) return;
            
            if (playerScoreUIs[actorNumber] != null)
            {
                Destroy(playerScoreUIs[actorNumber].gameObject);
            }
            playerScoreUIs.Remove(actorNumber);
        }

        #endregion

        #region Photon Callbacks

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) // 플레이어 속성 업데이트
        {
            if (changedProps.ContainsKey("isLoaded"))
            {
                CheckAllPlayersLoaded();
            }
        }
        
        private void CheckAllPlayersLoaded() // 모든 플레이어 로드 확인
        {
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

        #endregion
    }
} 