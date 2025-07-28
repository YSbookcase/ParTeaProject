using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;   
using PJW;

namespace PJW
{
    public class RankDisplayManager : MonoBehaviourPunCallbacks
    {
        [SerializeField] private TextMeshProUGUI rankText;  

        private void Start()
        {
            UpdateRankText(PhotonNetwork.LocalPlayer);
        }

        // 플레이어 커스텀 프로퍼티가 바뀔 때 호출
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (targetPlayer == PhotonNetwork.LocalPlayer && changedProps.ContainsKey("rank"))
            {
                UpdateRankText(targetPlayer);
            }
        }

        private void UpdateRankText(Player player)
        {
            int rank = player.GetRank();
            rankText.text = $"내 랭크: {rank}";
        }
    }
}
