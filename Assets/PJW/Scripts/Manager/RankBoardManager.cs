using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace PJW
{
    public class RankBoardManager : MonoBehaviourPunCallbacks
    {
        [SerializeField] private GameObject rankTextPrefab;
        [SerializeField] private Transform rankBoardParent;

        private Dictionary<int, Text> playerRankTexts = new Dictionary<int, Text>();

        private void Start()
        {
            InitializeRankBoard();
        }

        private void InitializeRankBoard()
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                CreateOrUpdateEntry(player);
            }

            SortRankBoard();
        }

        private void CreateOrUpdateEntry(Player player)
        {
            int actorNumber = player.ActorNumber;

            if (!playerRankTexts.ContainsKey(actorNumber))
            {
                GameObject entry = Instantiate(rankTextPrefab, rankBoardParent);
                Text text = entry.GetComponent<Text>();
                playerRankTexts.Add(actorNumber, text);
            }

            UpdateRankText(player);
        }

        private void UpdateRankText(Player player)
        {
            int actorNumber = player.ActorNumber;
            int rank = player.GetRank();
            string nickname = player.NickName;

            if (playerRankTexts.TryGetValue(actorNumber, out Text text))
            {
                text.text = $"Rank {rank} - {nickname}";
            }
        }

        private void SortRankBoard()
        {
            
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            
        }
    }
}
