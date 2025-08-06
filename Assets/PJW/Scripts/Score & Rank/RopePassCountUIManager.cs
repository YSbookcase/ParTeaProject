using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

namespace PJW
{
    public class RopePassCountUIManager : MonoBehaviourPunCallbacks
    {
        [SerializeField] private Transform playerPanelRoot;  
        [SerializeField] private PlayerScorePanel playerScorePanelPrefab;

        private Dictionary<int, PlayerScorePanel> playerPanels = new Dictionary<int, PlayerScorePanel>();

        private void Start()
        {
            InitializePanels();
        }

        private void InitializePanels()
        {
            foreach (Transform child in playerPanelRoot)
                Destroy(child.gameObject);

            playerPanels.Clear();

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                AddOrUpdatePanel(player);
            }
        }

        public void AddOrUpdatePanel(Player player)
        {
            PlayerScorePanel panel;
            if (!playerPanels.TryGetValue(player.ActorNumber, out panel))
            {
                panel = Instantiate(playerScorePanelPrefab, playerPanelRoot);
                playerPanels[player.ActorNumber] = panel;
            }

            int score = player.GetRopeGameScore();
            panel.SetPanel(player.NickName, score);
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            AddOrUpdatePanel(targetPlayer);
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            AddOrUpdatePanel(newPlayer);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (playerPanels.TryGetValue(otherPlayer.ActorNumber, out var panel))
            {
                Destroy(panel.gameObject);
                playerPanels.Remove(otherPlayer.ActorNumber);
            }
        }
    }
}
