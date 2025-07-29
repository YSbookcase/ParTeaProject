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
        [SerializeField] private List<PlayerInfo> playerProperties = new();
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
            for (int i = 0; i < playerProperties.Count; i++)
            {
                playerNicknameTexts[i].text = playerProperties[i].NickName;
                playerColorImages[i].color = colors[(int)playerProperties[i].CustomProperties["Color"]];
                playerPanels[i].SetActive(true);
            }
        }

        private void GetAllPlayerProperties()
        {
            playerProperties.Clear();
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                var info = new PlayerInfo(player.NickName, player.ActorNumber, player.CustomProperties);
                playerProperties.Add(info);
                Debug.Log(info.ToString());
            }
        }
    }
}
