using KYS;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private readonly List<string> gameList = new List<string>
    {
        "JumpGame",
        "ArenaGame",
        "RacingGame",
        "RopeGame",
        "ReceiveGame",
    };

    private readonly List<string> teamGameList = new List<string>
    {
        "TileGame",
    };

    public List<string> remainingGameList = new List<string>();
    public List<string> remainingTeamGameList = new List<string>();

    // 몇개의 게임을 연속으로 할 것인지에 대한 카운트.
    public int maxGameCount;
    public int curGameCount;

    private void OnEnable()
    {
        // 각 플레이어들이 로컬에서 이벤트 등록을 할 수 있도록 OnEnable에서 진행.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void SetGameInfo()
    {
        Hashtable roomProperty = new Hashtable();
        roomProperty["remainingGameList"] = remainingGameList.ToArray();
        roomProperty["remainingTeamGameList"] = remainingTeamGameList.ToArray();
        roomProperty["maxGameCount"] = maxGameCount;
        roomProperty["curGameCount"] = curGameCount;

        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);
    }

    public void GameStart(string sceneName = null, int maxGameCount = 1)
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        // 모든 플레이어의 점수를 0으로 초기화
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            player.SetTotalGameScore(0);
        }

        remainingGameList = gameList.ToList();
        remainingTeamGameList = teamGameList.ToList();

        this.maxGameCount = maxGameCount;
        curGameCount = 0;

        SetGameInfo();

        GoNextMiniGame(sceneName);
    }

    public void miniGameEnd()
    {
        UIManager.Instance.ShowPopUp<RoomPopUp>();

        if (!PhotonNetwork.IsMasterClient) return;

        PhotonNetwork.CurrentRoom.IsOpen = true;
        PhotonNetwork.CurrentRoom.IsVisible = true;

        PhotonNetwork.LoadLevel("NetworkScene");
    }

    public void GoScoerScene()
    {
        PhotonNetwork.LoadLevel("Score");
    }

    public void GoNextMiniGame(string sceneName = null)
    {
        // 지정된만큼 미니게임을 하였다면, 게임 종료.
        if (curGameCount == maxGameCount)
        {
            miniGameEnd();
            return;
        }

        curGameCount++;

        if (!PhotonNetwork.IsMasterClient) return;

        // 플레이어들이 미니게임 씬으로 넘어갈 때,
        // 모두가 로딩이 완료되었는지 확인하기 위해 isLoaded를 사용한다.
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            Hashtable property = new Hashtable();
            property["isLoaded"] = false;
            property["rank"] = 0;

            player.SetCustomProperties(property);
        }

        if(sceneName != null)
        {
            remainingGameList.Remove(sceneName);
            remainingTeamGameList.Remove(sceneName);
            SetGameInfo();

            PhotonNetwork.LoadLevel(sceneName);
            return;
        }

        List<string> curGameList = remainingGameList.ToList();
        if(PhotonNetwork.PlayerList.Count() % 2 == 0)
        {
            curGameList.AddRange(remainingTeamGameList);
        }

        // 더이상 진행할 수 있는 게임이 없다면 종료
        if(curGameList.Count == 0)
        {
            miniGameEnd();
            return;
        }

        int index = Random.Range(0, curGameList.Count);
        sceneName = curGameList[index];

        remainingGameList.Remove(sceneName);
        remainingTeamGameList.Remove(sceneName);
        SetGameInfo();

        PhotonNetwork.LoadLevel(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (PhotonNetwork.LocalPlayer == null) return;

        Hashtable isLoaded = new Hashtable();
        isLoaded["isLoaded"] = true;

        PhotonNetwork.LocalPlayer.SetCustomProperties(isLoaded);
    }

    public bool isAllPlayerLoaded()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.ContainsKey("isLoaded"))
            {
                if (!(bool)player.CustomProperties["isLoaded"])
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}
