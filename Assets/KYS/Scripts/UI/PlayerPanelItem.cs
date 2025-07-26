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

        private bool isReady;

        private new void Awake()
        {
            base.Awake();
            
            GetEvent("ReadyButton").Click += ReadyButtonClick;
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
        }

        // ?? ???? ??? (?? ??)
        private void UpdatePlayerColor(int colorIndex)
        {
            // ?? ?? ??
            Color[] colors = { Color.red, Color.blue, Color.green, Color.yellow };
            if (colorIndex >= 0 && colorIndex < colors.Length)
            {
                // ???? ?? UI ????
                Debug.Log($"???? ?? ????: {colorIndex}");
            }
        }

        // ?? ?? ???? ??? (?? ??)
        private void UpdateSelectedGame(int gameIndex)
        {
            // ?? ?? ?? ??
            string[] games = { "??1", "??2", "??3" };
            if (gameIndex >= 0 && gameIndex < games.Length)
            {
                // ?? ?? UI ????
                Debug.Log($"??? ?? ????: {games[gameIndex]}");
            }
        }
    }
}