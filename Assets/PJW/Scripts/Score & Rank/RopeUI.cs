using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using PJW;
using TMPro;
using UnityEngine;

namespace PJW
{
    public class RopeUI : MonoBehaviourPunCallbacks
    {
        [Header("Jump Count Text Prefab")]
        [SerializeField] private GameObject jumpTextPrefab;

        [Header("생성된 텍스트들을 배치할 부모 오브젝트")]
        [SerializeField] private Transform uiContainer;

        private readonly Dictionary<int, TextMeshProUGUI> jumpTexts = new Dictionary<int, TextMeshProUGUI>();

        private const string RopeGameScoreKey = "RopeGameScore";

        private void Start()
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                var go = Instantiate(jumpTextPrefab, uiContainer);
                var txt = go.GetComponent<TextMeshProUGUI>();     
                txt.text = $"{player.NickName}: 0";               
                jumpTexts[player.ActorNumber] = txt;
            }
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);

            if (changedProps.ContainsKey(RopeGameScoreKey) &&
                jumpTexts.TryGetValue(targetPlayer.ActorNumber, out var txt))
            {
                int count = targetPlayer.GetRopeGameScore();          
                txt.text = $"{targetPlayer.NickName}: {count}";
            }
        }
    }
}
