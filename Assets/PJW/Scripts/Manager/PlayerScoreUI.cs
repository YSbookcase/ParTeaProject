using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace PJW
{
    public class PlayerScoreUI : MonoBehaviourPunCallbacks
    {
        [SerializeField] private Text scoreText;

        private void Start()
        {
            // 처음엔 현재 점수 표시
            UpdateScoreText();
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            // 점수가 변경된 경우 UI 업데이트
            if (changedProps.ContainsKey("totalGameScore"))
            {
                UpdateScoreText();
            }
        }

        private void UpdateScoreText()
        {
            // 로컬 플레이어 기준으로 점수 표시
            int score = PhotonNetwork.LocalPlayer.GetTotalGameScore();
            scoreText.text = $"Score: {score}";
        }
    }
}
