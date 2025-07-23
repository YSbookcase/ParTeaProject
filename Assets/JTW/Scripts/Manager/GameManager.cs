using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public void GameStart(string sceneName)
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

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
