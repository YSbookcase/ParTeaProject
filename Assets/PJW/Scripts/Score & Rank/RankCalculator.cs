using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using PJW;
using UnityEngine;

namespace PJW
{
    public class RankCalculator : MonoBehaviourPunCallbacks
    {
        public void CalculateRanks()
        {
            if (!PhotonNetwork.IsMasterClient)
                return; // 마스터 클라이언트만 순위 계산

            // 점수 기반으로 정렬
            List<Player> sortedPlayers = new List<Player>(PhotonNetwork.PlayerList);
            sortedPlayers.Sort((a, b) => b.GetTotalGameScore().CompareTo(a.GetTotalGameScore()));

            //  순위 반영
            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                int rank = i + 1; // 1등부터 시작
                Player p = sortedPlayers[i];

                // 커스텀 프로퍼티에 저장
                p.SetRank(rank);

                // 디버그: 플레이어 닉네임과 등수 출력
                Debug.Log($"[RankCalculator] {p.NickName} 는 {rank}등 입니다. (Score: {p.GetTotalGameScore()})");
            }
        }
    }
}
