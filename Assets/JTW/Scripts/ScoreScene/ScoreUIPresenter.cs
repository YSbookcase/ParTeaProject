using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BaseUI = JTW_JumpGame.BaseUI;

namespace JTW_JumpGame
{
    public class ScoreUIPresenter : BaseUI
    {
        [SerializeField] private GameObject playerScorePanelPrefab;

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

            foreach(Player player in PhotonNetwork.PlayerList)
            {

                GameObject obj = Instantiate(playerScorePanelPrefab, scorePanel.transform);
                obj.GetComponent<RectTransform>().anchoredPosition = startPositon;

                PlayerScorePanel panel = obj.GetComponent<PlayerScorePanel>();

                panel.InitInfo(player.NickName, player.GetTotalGameScore() + rankScore[player.GetRank()]);

                player.AddTotalGameScore(rankScore[player.GetRank()]);
                startPositon.y -= 140;
            }

            nextButton.interactable = true;
        }

        private void GoNextGame()
        {
            Manager.game.GoNextMiniGame("JumpGame");
        }
    }
}

