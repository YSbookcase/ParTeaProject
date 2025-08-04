using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KYS
{
    public class PlayerPanelItem : BaseUI
    {
        private TextMeshProUGUI nicknameText => GetUI<TextMeshProUGUI>("NicknameText");
        private TextMeshProUGUI readyText => GetUI<TextMeshProUGUI>("ReadyText");
        private Image hostImage => GetUI<Image>("HostImage");
        private Image readyButtonImage => GetUI<Image>("ReadyButton");
        private Button readyButton => GetUI<Button>("ReadyButton");

        // 색상 선택 관련 UI
        private Button[] colorButtons = new Button[4];
        private Image playerPanelBackground => GetUI<Image>("PlayerPanelBackground");

        private bool isReady;
        private Player currentPlayer; // 현재 플레이어 정보 저장
        private int currentPlayerActorNumber; // ActorNumber 저장

        private new void Awake()
        {
            base.Awake();
            
            // 각 PlayerPanelItem 인스턴스에 고유한 이름 부여
            gameObject.name = $"PlayerPanel_{GetInstanceID()}";
            
            // Ready 버튼에 고유한 이름 부여
            GameObject readyButtonObj = GetUI("ReadyButton");
            if (readyButtonObj != null)
            {
                readyButtonObj.name = $"ReadyButton_{GetInstanceID()}";
            }
            
            // 색상 선택 버튼들 초기화
            InitializeColorButtons();
        }

        public void Init(Player player)
        {
            currentPlayer = player; // 플레이어 정보 저장
            currentPlayerActorNumber = player.ActorNumber; // ActorNumber 저장
            nicknameText.text = player.NickName;
            UpdateMasterClientDisplay(player.IsMasterClient);
            readyButton.interactable = player.IsLocal;

            // Ready 버튼 이벤트 등록 (플레이어별 고유 설정)
            var readyButtonEvent = GetEvent($"ReadyButton_{GetInstanceID()}");
            if (readyButtonEvent != null)
            {
                readyButtonEvent.Click += ReadyButtonClick;
            }

            // 기존 Ready 상태 로드
            if (player.CustomProperties.TryGetValue("Ready", out object readyValue))
            {
                isReady = (bool)readyValue;
                UpdateReadyUI();
            }
            else
            {
                isReady = false;
                UpdateReadyUI();
            }

            // 기존 색상 로드
            if (player.CustomProperties.TryGetValue("Color", out object colorValue))
            {
                if (colorValue != null)
                {
                    int colorIndex = (int)colorValue;
                    UpdatePlayerColor(colorIndex);
                }
                else
                {
                    // 색상이 취소된 경우 (null)
                    UpdatePlayerColor(-1);
                }
                
                // 모든 플레이어의 색상 버튼 UI 업데이트 (색상 중복 방지를 위해)
                UpdateAllColorButtonsUI();
            }

            // 기존 선택된 게임 로드
            if (player.CustomProperties.TryGetValue("SelectedGame", out object gameValue))
            {
                int gameIndex = (int)gameValue;
                UpdateSelectedGame(gameIndex);
            }
        }

        private void UpdateReadyUI()
        {
            readyText.text = isReady ? "Ready" : "Click Ready";
            readyButtonImage.color = isReady ? Color.green : Color.white;

            // Ready 버튼의 interactable 상태 유지 (로컬 플레이어만 클릭 가능)
            if (currentPlayer != null)
            {
                readyButton.interactable = currentPlayer.IsLocal;
            }
        }


        // 방장 표시 업데이트
        public void UpdateMasterClientDisplay(bool isMasterClient)
        {
            bool wasMasterClient = hostImage.enabled;
            hostImage.enabled = isMasterClient;

            // 방장인 경우 닉네임에 [방장] 표시 추가
            if (isMasterClient)
            {
                if (!nicknameText.text.Contains("[방장]"))
                {
                    nicknameText.text = $"{nicknameText.text} [방장]";
                }
            }
            else
            {
                // 방장이 아닌 경우 [방장] 표시 제거
                nicknameText.text = nicknameText.text.Replace(" [방장]", "");
            }

            // 방장 상태가 변경된 경우 애니메이션 효과
            if (wasMasterClient != isMasterClient)
            {
                if (isMasterClient)
                {
                    // 방장이 된 경우 특별한 효과
                    StartCoroutine(MasterClientAnimation());
                }
            }
        }

        // 방장 변경 애니메이션
        private System.Collections.IEnumerator MasterClientAnimation()
        {
            // 방장 이미지 크기 애니메이션
            Vector3 originalScale = hostImage.transform.localScale;
            Vector3 targetScale = originalScale * 1.2f;

            // 확대
            float duration = 0.2f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                hostImage.transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
                yield return null;
            }

            // 원래 크기로 복원
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                hostImage.transform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
                yield return null;
            }

            hostImage.transform.localScale = originalScale;
        }

        private void ReadyButtonClick(PointerEventData eventData)
        {
            // 로컬 플레이어의 패널에서만 처리
            if (!currentPlayer.IsLocal)
            {
                return;
            }
            
            // Ready 상태 토글
            bool newReadyState = !isReady;
            
            // Ready 상태에 따른 사운드 재생
            if (Manager.Audio != null)
            {
                if (newReadyState)
                {
                    // Ready 상태가 될 때
                    Manager.Audio.SfxPlay("SFX_ButtonClick");
                }
                else
                {
                    // Ready 상태가 해제될 때
                    Manager.Audio.SfxPlay("SFX_ButtonClickBack");
                }
            }
            
            isReady = newReadyState;
            
            // PhotonManager를 통해 Ready 상태 업데이트
            Manager.Photon.SetPlayerReady(isReady);
            
            // UI 업데이트
            UpdateReadyUI();
        }

        public void ReadyPropertyUpdate()
        {
            // PhotonManager? ?? ?? ??
            PhotonManager.Instance.SetPlayerReady(isReady);
        }

        public void ReadyCheck(Player player)
        {
            if (player.CustomProperties.TryGetValue("Ready", out object value))
            {
                readyText.text = (bool)value ? "Ready" : "Click Ready";
                readyButtonImage.color = (bool)value ? Color.green : Color.white;
            }
        }

        // Ready 상태 초기화
        public void ResetReadyState()
        {
            isReady = false;
            UpdateReadyUI();

            // 로컬 플레이어인 경우에만 PhotonManager를 통해 속성 업데이트
            if (PhotonNetwork.LocalPlayer != null)
            {
                PhotonManager.Instance.SetPlayerReady(false);
            }
        }

        // 플레이어 속성 업데이트 (다른 플레이어의 속성 변경 시)
        public void UpdatePlayerProperties(Player player)
        {
            // 해당 플레이어의 패널에서만 업데이트
            if (player.ActorNumber != currentPlayerActorNumber)
            {
                return;
            }

            // Ready 상태 업데이트
            if (player.CustomProperties.TryGetValue("Ready", out object readyValue))
            {
                bool newReadyState = (bool)readyValue;
                if (isReady != newReadyState)
                {
                    isReady = newReadyState;
                    UpdateReadyUI();
                }
            }

            // 색상 업데이트
            if (player.CustomProperties.TryGetValue("Color", out object colorValue))
            {
                if (colorValue != null)
                {
                    int colorIndex = (int)colorValue;
                    UpdatePlayerColor(colorIndex);
                }
                else
                {
                    // 색상이 취소된 경우 (null)
                    UpdatePlayerColor(-1);
                }
                
                // 모든 플레이어의 색상 버튼 UI 업데이트 (색상 중복 방지를 위해)
                UpdateAllColorButtonsUI();
            }

            // 선택된 게임 업데이트
            if (player.CustomProperties.TryGetValue("SelectedGame", out object gameValue))
            {
                int gameIndex = (int)gameValue;
                UpdateSelectedGame(gameIndex);
            }
        }

        // 로컬 플레이어의 Ready 상태만 업데이트하는 메서드 추가
        public void UpdateLocalPlayerReadyState(bool readyState)
        {
            isReady = readyState;
            UpdateReadyUI();
        }

        // 모든 플레이어의 색상 버튼 UI 업데이트
        private void UpdateAllColorButtonsUI()
        {
            // 현재 플레이어의 색상 가져오기
            int myColorIndex = -1;
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Color", out object myColorValue))
            {
                if (myColorValue != null)
                {
                    myColorIndex = (int)myColorValue;
                }
            }

            // 모든 색상 버튼 업데이트
            for (int i = 0; i < colorButtons.Length; i++)
            {
                if (colorButtons[i] != null)
                {
                    // 현재 플레이어가 선택한 색상인지 확인
                    bool isMyColor = (i == myColorIndex);

                    // 다른 플레이어가 이미 선택한 색상인지 확인
                    bool isColorTaken = IsColorAlreadySelected(i);

                    // 버튼 상태 설정
                    if (isMyColor)
                    {
                        // 내가 선택한 색상 - 강조
                        colorButtons[i].GetComponent<Image>().color = Color.yellow; // 선택된 색상은 노란색으로 강조
                        colorButtons[i].interactable = true;
                    }
                    else if (isColorTaken)
                    {
                        // 다른 플레이어가 선택한 색상 - 비활성화
                        colorButtons[i].GetComponent<Image>().color = Color.gray;
                        colorButtons[i].interactable = false;
                    }
                    else
                    {
                        // 선택 가능한 색상 - 활성화
                        colorButtons[i].GetComponent<Image>().color = Color.white;
                        colorButtons[i].interactable = true;
                    }
                }
            }
        }

        // 색상 선택 버튼들 초기화
        private void InitializeColorButtons()
        {
            for (int i = 0; i < 4; i++)
            {
                colorButtons[i] = GetUI<Button>($"ColorButton_{i}");
                if (colorButtons[i] != null)
                {
                    int colorIndex = i;
                    colorButtons[i].onClick.AddListener(() => OnColorButtonClick(colorIndex));
                }
            }
        }

        // 색상 버튼 클릭 이벤트
        private void OnColorButtonClick(int colorIndex)
        {
            // 현재 플레이어의 색상 확인
            int currentColorIndex = -1;
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Color", out object currentColorValue))
            {
                if (currentColorValue != null)
                {
                    currentColorIndex = (int)currentColorValue;
                }
            }

            // 같은 색상을 다시 클릭한 경우 - 색상 취소
            if (currentColorIndex == colorIndex)
            {
                // 색상 취소 사운드 재생
                if (Manager.Audio != null)
                {
                    Manager.Audio.SfxPlay("SFX_ButtonClickBack");
                }
                
                // 로컬 플레이어의 패널만 흰색으로 변경
                if (currentPlayer.IsLocal && playerPanelBackground != null)
                {
                    playerPanelBackground.color = Color.white;
                }
                
                // 해당 색상 버튼만 흰색으로 변경
                if (colorButtons[colorIndex] != null)
                {
                    colorButtons[colorIndex].GetComponent<Image>().color = Color.white;
                }
                
                // PhotonManager를 통해 색상 취소
                PhotonManager.Instance.ClearPlayerColor();
                return;
            }

            // 이미 다른 플레이어가 선택한 색상인지 확인
            if (IsColorAlreadySelected(colorIndex))
            {
                return;
            }

            // 색상 선택 사운드 재생
            if (Manager.Audio != null)
            {
                Manager.Audio.SfxPlay("SFX_ButtonClick");
            }

            // PhotonManager를 통해 색상 변경
            PhotonManager.Instance.SetPlayerColor(colorIndex);
        }

        // 색상이 이미 다른 플레이어에 의해 선택되었는지 확인
        private bool IsColorAlreadySelected(int colorIndex)
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.TryGetValue("Color", out object value))
                {
                    if (value != null)
                    {
                        int playerColor = (int)value;
                        if (playerColor == colorIndex && player != PhotonNetwork.LocalPlayer)
                        {
                            return true; // 다른 플레이어가 이미 이 색상을 선택함
                        }
                    }
                }
            }
            return false; // 선택되지 않은 색상
        }

        // 플레이어 색상 변경 메서드
        private void UpdatePlayerColor(int colorIndex)
        {
            // 색상 배열 (4가지 색상)
            Color[] colors = {
                Color.red,      // 빨강
                Color.blue,     // 파랑
                Color.green,    // 초록
                Color.yellow    // 노랑
            };

            if (colorIndex >= 0 && colorIndex < colors.Length)
            {
                // 플레이어 패널 배경색 변경
                if (playerPanelBackground != null)
                {
                    playerPanelBackground.color = colors[colorIndex];
                }

                // 색상 버튼들 업데이트 (선택된 색상 강조)
                UpdateColorButtonUI(colorIndex);
            }
            else
            {
                // 색상이 취소된 경우 (colorIndex가 -1이거나 범위 밖)
                if (playerPanelBackground != null)
                {
                    playerPanelBackground.color = Color.white; // 기본 색상으로 변경
                }

                // 색상 버튼들 업데이트 (선택 해제)
                UpdateColorButtonUI(-1);
            }
        }

        // 색상 버튼 UI 업데이트
        private void UpdateColorButtonUI(int selectedColorIndex)
        {
            for (int i = 0; i < colorButtons.Length; i++)
            {
                if (colorButtons[i] != null)
                {
                    // 현재 플레이어가 선택한 색상인지 확인
                    bool isMyColor = (i == selectedColorIndex);

                    // 다른 플레이어가 이미 선택한 색상인지 확인
                    bool isColorTaken = IsColorAlreadySelected(i);

                    // 버튼 상태 설정
                    if (isMyColor)
                    {
                        // 내가 선택한 색상 - 강조 (선택된 상태)
                        colorButtons[i].GetComponent<Image>().color = Color.yellow; // 선택된 색상은 노란색으로 강조
                        colorButtons[i].interactable = true;
                    }
                    else if (isColorTaken)
                    {
                        // 다른 플레이어가 선택한 색상 - 비활성화
                        colorButtons[i].GetComponent<Image>().color = Color.gray;
                        colorButtons[i].interactable = false;
                    }
                    else
                    {
                        // 선택 가능한 색상 - 활성화 (색상 취소 시에도 이 상태로 변경됨)
                        colorButtons[i].GetComponent<Image>().color = Color.white;
                        colorButtons[i].interactable = true;
                    }
                }
            }
            
            // 색상이 취소된 경우 (selectedColorIndex가 -1) 모든 색상 버튼 UI 업데이트
            if (selectedColorIndex == -1)
            {
                UpdateAllColorButtonsUI();
            }
        }

        // 플레이어 게임 선택 변경 메서드 (개인 설정용)
        private void UpdateSelectedGame(int gameIndex)
        {
            // 게임 선택 변경 로직 (개인 설정)
            string[] games = { "테트리스", "스네이크", "퀴즈", "레이싱", "점프", "아레나" };
            if (gameIndex >= 0 && gameIndex < games.Length)
            {
                // 게임 선택 로직 구현 예정
            }
        }
    }
}