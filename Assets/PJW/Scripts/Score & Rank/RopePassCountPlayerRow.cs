using UnityEngine;
using TMPro;

namespace PJW
{
    public class RopePassCountPlayerRow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI playerScoreText;

        public void SetInfo(string nickname, int count)
        {
            playerScoreText.text = $"{nickname} : {count}";
        }
    }
}
