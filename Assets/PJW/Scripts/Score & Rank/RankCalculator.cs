using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using PJW;
using UnityEngine;
using System.Linq;

namespace PJW
{
    public class RankCalculator : MonoBehaviourPunCallbacks
    {
        public static void CalculateRanks()
        {
            var sorted = PhotonNetwork.CurrentRoom.Players
                .Values
                .OrderByDescending(p => p.GetRopeGameScore())
                .ToList();

            int rank = 1;
            int prevScore = sorted.Count > 0 ? sorted[0].GetRopeGameScore() : 0;
            int sameCount = 0;

            foreach (var player in sorted)
            {
                int score = player.GetRopeGameScore();
                if (score == prevScore)
                {
                    player.SetRank(rank);
                    sameCount++;
                }
                else
                {
                    rank += sameCount;
                    sameCount = 1;
                    prevScore = score;
                    player.SetRank(rank);
                }
            }
        }
    }
}
