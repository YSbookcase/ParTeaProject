using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using ExitGames.Client.Photon;

namespace PJW
{
    public class MyRankDebugger : MonoBehaviourPunCallbacks
    {
        void Start()
        {
            // 게임 시작 직후도 체크
            PrintMyRank();
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            // 내 랭크가 변경됐으면 바로 로그
            if (targetPlayer == PhotonNetwork.LocalPlayer && changedProps.ContainsKey("rank"))
            {
                PrintMyRank();
            }
        }

        private void PrintMyRank()
        {
            int rank = PhotonNetwork.LocalPlayer.GetRank();
            int score = PhotonNetwork.LocalPlayer.GetRopeGameScore();
            Debug.Log($"[내 랭크] {PhotonNetwork.LocalPlayer.NickName} : {rank}등 / 점수:{score}");
        }
    }
}
