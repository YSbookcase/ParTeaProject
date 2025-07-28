using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;    
using PJW;

namespace PJW
{
    public class ScoreDisplayManager : MonoBehaviourPunCallbacks
    {
        [SerializeField] private TextMeshProUGUI totalScoreText; 

        private void Start()
        {
            UpdateScoreTexts(PhotonNetwork.LocalPlayer);
        }

        // 다른 플레이어든 로컬 플레이어든 커스텀 프로퍼티가 바뀌면 호출
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (targetPlayer == PhotonNetwork.LocalPlayer)
            {
                UpdateScoreTexts(targetPlayer);
            }
        }

        private void UpdateScoreTexts(Player player)
        {
            int totalScore = player.GetTotalGameScore();
            int jumpScore  = player.GetJumpGameScore();

            totalScoreText.text = $"총 점수: {totalScore}";
        }
    }
}
