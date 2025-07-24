using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public static class RankExtensions
{
    public static void SetRank(this Player player, int newRank)
    {
        Hashtable rank = new Hashtable();  
        rank["rank"] = newRank;

        player.SetCustomProperties(rank);
    }

    public static int GetRank(this Player player)
    {
        object rank;
        if (player.CustomProperties.TryGetValue("rank", out rank))
        {
            return (int)rank;
        }

        return 0;
    }
}
