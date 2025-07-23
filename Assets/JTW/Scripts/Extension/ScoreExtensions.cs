using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public static class ScoreExtensions
{
    public static void SetScore(this Player player, int newScore)
    {
        Hashtable score = new Hashtable();
        score["score"] = newScore;

        player.SetCustomProperties(score);
    }

    public static void AddScore(this Player player, int scoreToAddToCurrent)
    {
        int current = player.GetScore();
        current = current + scoreToAddToCurrent;

        Hashtable score = new Hashtable();
        score["score"] = current;

        player.SetCustomProperties(score);
    }

    public static int GetScore(this Player player)
    {
        object score;
        if (player.CustomProperties.TryGetValue("score", out score))
        {
            return (int)score;
        }

        return 0;
    }
}
