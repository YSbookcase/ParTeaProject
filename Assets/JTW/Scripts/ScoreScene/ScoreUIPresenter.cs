using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using BaseUI = JTW_JumpGame.BaseUI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace JTW_JumpGame
{
    public class ScoreUIPresenter : BaseUI
    {
        [SerializeField] private GameObject playerScorePanelPrefab;
        [SerializeField] private ScoreNetworkHandler network;

        private GameObject scorePanel;
        private Button nextButton;

        private Vector2 startPositon = new Vector2(0, -145);

        private int[] rankScore = new int[] { 0, 5, 3, 2, 1 };

        private void Start()
        {
            nextButton = GetUI<Button>("NextButton");
            nextButton.onClick.AddListener(GoNextGame);
            nextButton.interactable = false;
        }

        public void InitScore()
        {
            scorePanel = GetUI("ScorePanel");

            List<Player> palyers = PhotonNetwork.PlayerList.OrderBy(p => p.GetRank()).ToList();

            foreach(Player player in palyers)
            {
                GameObject obj = Instantiate(playerScorePanelPrefab, scorePanel.transform);
                obj.GetComponent<RectTransform>().anchoredPosition = startPositon;

                PlayerScorePanel panel = obj.GetComponent<PlayerScorePanel>();

                panel.InitInfo(player.GetRank(), player.NickName, player.GetTotalGameScore(), rankScore[player.GetRank()]);

                player.AddTotalGameScore(rankScore[player.GetRank()]);
                startPositon.y -= 140;

                // property가 초기화 되지 않아서 생기는 문제를 방지하기 위해 미리 초기화
                Hashtable property = new Hashtable();
                property["isLoaded"] = false;
                property["rank"] = 0;

                player.SetCustomProperties(property);
            }

            if (PhotonNetwork.IsMasterClient)
            {
                nextButton.interactable = true;
            }
        }

        private void GoNextGame()
        {
            network.GoNext();
        }
    }
}

