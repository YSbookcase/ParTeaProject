using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace PJW
{
    public static class RopeGameScoreExtensions
    {
        private const string RopeScoreKey = "ropeGameScore";

        //  점수 설정
        public static void SetRopeGameScore(this Player player, int newScore)
        {
            var props = new Hashtable { [RopeScoreKey] = newScore };
            player.SetCustomProperties(props);
        }

        //  점수 추가
        public static void AddRopeGameScore(this Player player, int delta)
        {
            int current = player.GetRopeGameScore();
            current += delta;
            var props = new Hashtable { { "ropeGameScore", current }, { "scoreUpdated", true } };
            player.SetCustomProperties(props);

            Debug.Log($"[점수 증가] {player.NickName} => {current}점");
        }

        //  점수 조회
        public static int GetRopeGameScore(this Player player)
        {
            if (player.CustomProperties.TryGetValue(RopeScoreKey, out object val))
                return (int)val;
            return 0;
        }
    }
}
