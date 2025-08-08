using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public static class ScoreExtensions
{
    private const string totalScoreString = "totalGameScore";

    public static void SetTotalGameScore(this Player player, int newScore)
    {
        Hashtable score = new Hashtable();
        score[totalScoreString] = newScore;

        player.SetCustomProperties(score);
    }

    public static void AddTotalGameScore(this Player player, int scoreToAddToCurrent)
    {
        int current = player.GetTotalGameScore();
        current = current + scoreToAddToCurrent;

        Hashtable score = new Hashtable();
        score[totalScoreString] = current;

        player.SetCustomProperties(score);
    }

    public static int GetTotalGameScore(this Player player)
    {
        object score;
        if (player.CustomProperties.TryGetValue(totalScoreString, out score))
        {
            return (int)score;
        }

        return 0;
    }


    // JumpGame에 활용할 확장 메서드
    public static void SetJumpGameScore(this Player player, int newScore)
    {
        Hashtable score = new Hashtable();
        score["jumpGameScore"] = newScore;

        player.SetCustomProperties(score);
    }

    public static void AddJumpGameScore(this Player player, int scoreToAddToCurrent)
    {
        int current = player.GetJumpGameScore();
        current = current + scoreToAddToCurrent;

        Hashtable score = new Hashtable();
        score["jumpGameScore"] = current;

        player.SetCustomProperties(score);
    }
    public static int GetJumpGameScore(this Player player)
    {
        object score;
        if (player.CustomProperties.TryGetValue("jumpGameScore", out score))
        {
            return (int)score;
        }

        return 0;
    }
}
