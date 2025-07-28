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

            if (!player.IsLocal)
            {
                return;
            }

            isReady = false;
            Debug.Log("√ ±‚»≠");
            ReadyPropertyUpdate();
        }

        private void ReadyButtonClick(PointerEventData eventData)
        {
            isReady = !isReady;

            readyText.text = isReady ? "Ready" : "Click Ready";
            readyButtonImage.color = isReady ? Color.green : Color.white;
            ReadyPropertyUpdate();
        }

        public void ReadyPropertyUpdate()
        {
            ExitGames.Client.Photon.Hashtable playerProperty = new Hashtable();
            playerProperty["Ready"] = isReady;
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperty);
        }

        public void ReadyCheck(Player player)
        {
            if (player.CustomProperties.TryGetValue("Ready", out object value))
            {
                readyText.text = (bool)value ? "Ready" : "Click Ready";
                readyButtonImage.color = (bool)value ? Color.green : Color.white;
            }
        }
    }
}