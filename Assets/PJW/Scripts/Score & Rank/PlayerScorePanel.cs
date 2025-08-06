using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PJW
{
    public class PlayerScorePanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI scoreText;

        public void SetPanel(string nickname, int count)
        {
            playerNameText.text = nickname;
            scoreText.text = count.ToString();
        }
    }
}
