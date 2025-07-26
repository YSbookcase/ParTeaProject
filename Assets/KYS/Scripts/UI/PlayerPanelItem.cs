using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using Hashtable = ExitGames.Client.Photon.Hashtable;

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

        private new void Awake()
        {
            base.Awake();
            
            GetEvent("ReadyButton").Click += ReadyButtonClick;
            
            // 색상 선택 버튼들 초기화
            InitializeColorButtons();
        }

        public void Init(Player player)
        {
            nicknameText.text = player.NickName;
            hostImage.enabled = player.IsMasterClient;
            readyButton.interactable = player.IsLocal;

            // ?? ??? ??
            if (player.CustomProperties.TryGetValue("Ready", out object readyValue))
            {
                isReady = (bool)readyValue;
                UpdateReadyUI();
            }
            else
            {
                // ??? ??
                isReady = false;
                UpdateReadyUI();
            }

            // ?? ?? ?? (?? ??)
            if (player.CustomProperties.TryGetValue("Color", out object colorValue))
            {
                UpdatePlayerColor((int)colorValue);
            }

            // ?? ?? ?? ?? (?? ??)
            if (player.CustomProperties.TryGetValue("SelectedGame", out object gameValue))
            {
                UpdateSelectedGame((int)gameValue);
            }

            // ?? ????? ???? ?? ?? ??
            if (player.IsLocal)
            {
                ReadyPropertyUpdate();
            }
        }

        private void UpdateReadyUI()
        {
            readyText.text = isReady ? "Ready" : "Click Ready";
            readyButtonImage.color = isReady ? Color.green : Color.white;
        }

        private void ReadyButtonClick(PointerEventData eventData)
        {
            isReady = !isReady;
            UpdateReadyUI();
            
            // PhotonManager? ?? ?? ??
            PhotonManager.Instance.SetPlayerReady(isReady);
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

        // ???? ?? ???? ??? (?? ??? ??)
        public void UpdatePlayerProperties(Player player)
        {
            // ?? ?? ????
            if (player.CustomProperties.TryGetValue("Ready", out object readyValue))
            {
                bool isReadyState = (bool)readyValue;
                readyText.text = isReadyState ? "Ready" : "Click Ready";
                readyButtonImage.color = isReadyState ? Color.green : Color.white;
            }

            // ?? ???? (?? ??)
            if (player.CustomProperties.TryGetValue("Color", out object colorValue))
            {
                int colorIndex = (int)colorValue;
                UpdatePlayerColor(colorIndex);
            }

            // ?? ?? ???? (?? ??)
            if (player.CustomProperties.TryGetValue("SelectedGame", out object gameValue))
            {
                int gameIndex = (int)gameValue;
                UpdateSelectedGame(gameIndex);
            }

            // ??? ????
            nicknameText.text = player.NickName;
            
            // ??? ?? ????
            hostImage.enabled = player.IsMasterClient;
            
            // 모든 플레이어의 색상 버튼 UI 업데이트 (색상 중복 방지를 위해)
            UpdateAllColorButtonsUI();
        }
        
        // 모든 플레이어의 색상 버튼 UI 업데이트
        private void UpdateAllColorButtonsUI()
        {
            // 현재 플레이어의 색상 가져오기
            int myColorIndex = -1;
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Color", out object myColorValue))
            {
                myColorIndex = (int)myColorValue;
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
                        colorButtons[i].GetComponent<Image>().color = Color.white;
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
            // 이미 다른 플레이어가 선택한 색상인지 확인
            if (IsColorAlreadySelected(colorIndex))
            {
                Debug.LogWarning($"[PlayerPanelItem] 이미 선택된 색상입니다: {colorIndex}");
                return;
            }
            
            // PhotonManager를 통해 색상 변경
            PhotonManager.Instance.SetPlayerColor(colorIndex);
            Debug.Log($"[PlayerPanelItem] 색상 선택: {colorIndex}");
        }
        
        // 색상이 이미 다른 플레이어에 의해 선택되었는지 확인
        private bool IsColorAlreadySelected(int colorIndex)
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.TryGetValue("Color", out object value))
                {
                    int playerColor = (int)value;
                    if (playerColor == colorIndex && player != PhotonNetwork.LocalPlayer)
                    {
                        return true; // 다른 플레이어가 이미 이 색상을 선택함
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
                
                Debug.Log($"[PlayerPanelItem] 플레이어 색상 변경: {colorIndex}");
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
                        // 내가 선택한 색상 - 강조
                        colorButtons[i].GetComponent<Image>().color = Color.white;
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

        // 플레이어 게임 선택 변경 메서드 (개인 설정용)
        private void UpdateSelectedGame(int gameIndex)
        {
            // 게임 선택 변경 로직 (개인 설정)
            string[] games = { "테트리스", "스네이크", "퀴즈", "레이싱", "점프", "아레나" };
            if (gameIndex >= 0 && gameIndex < games.Length)
            {
                Debug.Log($"[PlayerPanelItem] 개인 게임 선택 변경: {games[gameIndex]}");
            }
        }
    }
}