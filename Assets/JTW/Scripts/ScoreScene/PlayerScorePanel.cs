using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace JTW_JumpGame
{
    public class PlayerScorePanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI scoreText;

        private string[] rankTexts = new string[]
        {
            "null", "1st", "2nd", "3rd", "4th"
        };

        public void InitInfo(int rank, string name, int score, int rankScore)
        {
            rankText.text = rankTexts[rank];
            nameText.text = $"{name}";
            if(Manager.game.maxGameCount > 1)
            {
                scoreText.text = $": {score} + {rankScore}";
            }
            else
            {
                scoreText.text = "";
            }
            
        }
    }
}

