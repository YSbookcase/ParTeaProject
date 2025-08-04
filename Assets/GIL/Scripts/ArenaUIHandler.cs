using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using ExitGames.Client.Photon;
using TMPro;
using UnityEngine.UI;

namespace GIL.Scripts
{
    public class ArenaUIHandler : MonoBehaviourPunCallbacks
    {
        private List<PlayerInfo> _playerProperties = new();
        [SerializeField] private List<GameObject> playerPanels = new();
        [SerializeField] private List<TMP_Text> playerNicknameTexts = new();
        [SerializeField] private List<Image> playerColorImages = new();
        
        private Color[] colors = {
            Color.red,
            Color.blue,
            Color.green,
            Color.yellow
        };
        
        private void Start()
        {
            GetAllPlayerProperties();
            ControlPlayerPanels();
        }

        private void ControlPlayerPanels()
        {
            for (int i = 0; i < _playerProperties.Count; i++)
            {
                playerNicknameTexts[i].text = _playerProperties[i].NickName;
                playerColorImages[i].color = colors[(int)_playerProperties[i].CustomProperties["Color"]];
                playerPanels[i].SetActive(true);
            }
        }

        private void GetAllPlayerProperties()
        {
            _playerProperties.Clear();
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                var info = new PlayerInfo(player.NickName, player.ActorNumber, player.CustomProperties);
                _playerProperties.Add(info);
                Debug.Log(info.ToString());
            }
        }
    }
}
