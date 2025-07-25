using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace PJW
{
    public class ScoreboardManager : MonoBehaviourPunCallbacks
    {
        [SerializeField] private GameObject scoreTextPrefab;
        [SerializeField] private Transform scoreboardParent;

        private Dictionary<int, Text> playerScoreTexts = new Dictionary<int, Text>();

        private void Start()
        {
            InitializeScoreboard();
        }

        private void InitializeScoreboard()
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                CreateOrUpdateEntry(player);
            }
        }

        private void CreateOrUpdateEntry(Player player)
        {
            int actorNumber = player.ActorNumber;

            if (!playerScoreTexts.ContainsKey(actorNumber))
            {
                GameObject entry = Instantiate(scoreTextPrefab, scoreboardParent);
                Text text = entry.GetComponent<Text>();
                playerScoreTexts.Add(actorNumber, text);
            }

            UpdateScoreText(player);
        }

        private void UpdateScoreText(Player player)
        {
            int actorNumber = player.ActorNumber;
            int score = player.GetTotalGameScore();
            string nickname = player.NickName;

            if (playerScoreTexts.TryGetValue(actorNumber, out Text text))
            {
                text.text = $"{nickname} (#{actorNumber}) : {score} Á¡";
            }
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (changedProps.ContainsKey("totalGameScore"))
            {
                UpdateScoreText(targetPlayer);
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            CreateOrUpdateEntry(newPlayer);
        }
    }
}
