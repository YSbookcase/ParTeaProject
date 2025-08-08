using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace JTW_JumpGame
{
    public class JumpScorePanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nicknameText;
        [SerializeField] private TextMeshProUGUI scoreText;

        public Player player;

        public void SetInfo(Player player)
        {
            this.player = player;

            nicknameText.text = player.NickName;
            scoreText.text = "0";
        }

        public void SetScore(int score)
        {
            scoreText.text = score.ToString();
        }
    }
}
