using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public void GameStart(string sceneName)
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        // 모든 플레이어의 점수를 0으로 초기화
        foreach(Player player in PhotonNetwork.PlayerList)
        {
            player.SetTotalGameScore(0);
        }

        PhotonNetwork.LoadLevel(sceneName);
    }

    public void GameEnd()
    {
        PhotonNetwork.CurrentRoom.IsOpen = true;
        PhotonNetwork.CurrentRoom.IsVisible = true;

        // TODO : Room 씬으로 돌아간다.
        PhotonNetwork.LoadLevel("Main");
    }

    public void GoScoerScene()
    {
        PhotonNetwork.LoadLevel("Score");
    }
}
