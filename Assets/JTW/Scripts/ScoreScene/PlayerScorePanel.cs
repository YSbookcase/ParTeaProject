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
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI scoreText;

        public void InitInfo(string name, int score)
        {
            nameText.text = $"{name} :";
            scoreText.text = score.ToString();
        }
    }
}

