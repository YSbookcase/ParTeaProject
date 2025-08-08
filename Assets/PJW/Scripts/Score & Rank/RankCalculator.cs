using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace PJW
{
    public class RankCalculator : MonoBehaviourPunCallbacks
    {
        // 마스터 클라이언트가 호출 (게임 종료 시점)
        public static void CalculateRanks()
        {
            var sorted = PhotonNetwork.PlayerList
                .OrderByDescending(p => p.GetRopeGameScore())
                .ThenBy(p => p.ActorNumber)
                .ToList();

            int currentRank = 1;
            int prevScore = -1;
            int sameRankCount = 1;

            for (int i = 0; i < sorted.Count; i++)
            {
                int score = sorted[i].GetRopeGameScore();

                if (i > 0 && score == prevScore)
                {
                    sorted[i].SetRank(currentRank);
                    sameRankCount++;
                }
                else
                {
                    currentRank = i + 1;
                    sorted[i].SetRank(currentRank);
                    sameRankCount = 1;
                }
                prevScore = score;
            }
        }
    }
}
