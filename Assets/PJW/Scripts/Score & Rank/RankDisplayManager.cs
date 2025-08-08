using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;

namespace PJW
{
    public class RankDisplayManager : MonoBehaviourPunCallbacks
    {
        [SerializeField] private GameObject rankTextPrefab; 
        [SerializeField] private Transform rankboardParent; 

        private Dictionary<int, TextMeshProUGUI> playerRankTexts = new Dictionary<int, TextMeshProUGUI>();

        private void Start()
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                CreateOrUpdateEntry(player);
            }
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (changedProps.ContainsKey("rank"))
            {
                CreateOrUpdateEntry(targetPlayer);
            }
        }

        private void CreateOrUpdateEntry(Player player)
        {
            int actorNumber = player.ActorNumber;

            if (!playerRankTexts.ContainsKey(actorNumber))
            {
                GameObject entry = Instantiate(rankTextPrefab, rankboardParent);
                TextMeshProUGUI text = entry.GetComponent<TextMeshProUGUI>();
                playerRankTexts.Add(actorNumber, text);
            }

            UpdateRankText(player);
        }

        private void UpdateRankText(Player player)
        {
            int actorNumber = player.ActorNumber;
            int rank = player.GetRank();
            string nickname = player.NickName;

            if (playerRankTexts.TryGetValue(actorNumber, out TextMeshProUGUI text))
            {
                text.text = $"{nickname} : {rank}위";
            }
        }
    }
}
