using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;

namespace PJW
{
    public class ScoreDisplayManager : MonoBehaviourPunCallbacks
    {
        [SerializeField] private GameObject scoreTextPrefab; 
        [SerializeField] private Transform scoreboardParent;

        private Dictionary<int, TextMeshProUGUI> playerScoreTexts = new Dictionary<int, TextMeshProUGUI>();

        private void Start()
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                CreateOrUpdateEntry(player);
            }
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            CreateOrUpdateEntry(targetPlayer);
        }

        private void CreateOrUpdateEntry(Player player)
        {
            int actorNumber = player.ActorNumber;

            if (!playerScoreTexts.ContainsKey(actorNumber))
            {
                GameObject entry = Instantiate(scoreTextPrefab, scoreboardParent);
                TextMeshProUGUI text = entry.GetComponent<TextMeshProUGUI>();
                playerScoreTexts.Add(actorNumber, text);
            }

            UpdateScoreText(player);
        }

        private void UpdateScoreText(Player player)
        {
            int actorNumber = player.ActorNumber;
            int score = player.GetTotalGameScore();
            string nickname = player.NickName;

            if (playerScoreTexts.TryGetValue(actorNumber, out TextMeshProUGUI text))
            {
                text.text = $"{nickname} : {score}점";
            }
        }
    }
}
