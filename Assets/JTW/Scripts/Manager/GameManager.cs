using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameManager : Singleton<GameManager>
{
    private void OnEnable()
    {
        // 각 플레이어들이 로컬에서 이벤트 등록을 할 수 있도록 OnEnable에서 진행.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void GameStart(string sceneName)
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        // 모든 플레이어의 점수를 0으로 초기화
        foreach(Player player in PhotonNetwork.PlayerList)
        {
            player.SetTotalGameScore(0);
        }

        GoNextMiniGame(sceneName);
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

    public void GoNextMiniGame(string sceneName)
    {
        // 플레이어들이 미니게임 씬으로 넘어갈 때,
        // 모두가 로딩이 완료되었는지 확인하기 위해 isLoaded를 사용한다.
        foreach(Player player in PhotonNetwork.PlayerList)
        {
            Hashtable isLoaded = new Hashtable();
            isLoaded["isLoaded"] = false;

            player.SetCustomProperties(isLoaded);
        }

        PhotonNetwork.LoadLevel(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (PhotonNetwork.LocalPlayer == null) return;

        Hashtable isLoaded = new Hashtable();
        isLoaded["isLoaded"] = true;

        PhotonNetwork.LocalPlayer.SetCustomProperties(isLoaded);
    }
}
