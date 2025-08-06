using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

namespace PJW
{
    public class RopePassCountUIManager : MonoBehaviourPunCallbacks
    {
        [SerializeField] private Transform playerListRoot; 
        [SerializeField] private RopePassCountPlayerRow playerRowPrefab; 

        private Dictionary<int, RopePassCountPlayerRow> playerRows = new Dictionary<int, RopePassCountPlayerRow>();

        private void Start()
        {
            InitializeRows();
        }

        private void InitializeRows()
        {
            foreach (Transform child in playerListRoot)
                Destroy(child.gameObject);

            playerRows.Clear();

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                AddOrUpdateRow(player);
            }
        }

        public void AddOrUpdateRow(Player player)
        {
            RopePassCountPlayerRow row;
            if (!playerRows.TryGetValue(player.ActorNumber, out row))
            {
                row = Instantiate(playerRowPrefab, playerListRoot);
                playerRows[player.ActorNumber] = row;
            }

            int score = player.GetRopeGameScore();
            row.SetInfo(player.NickName, score);
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            AddOrUpdateRow(targetPlayer);
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            AddOrUpdateRow(newPlayer);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (playerRows.TryGetValue(otherPlayer.ActorNumber, out var row))
            {
                Destroy(row.gameObject);
                playerRows.Remove(otherPlayer.ActorNumber);
            }
        }
    }
}
