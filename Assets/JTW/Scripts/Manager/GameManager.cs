using KYS;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameManager : Singleton<GameManager>
{
    // 몇개의 게임을 연속으로 할 것인지에 대한 카운트.
    private int maxGameCount;
    private int curGameCount;

    private void OnEnable()
    {
        // 각 플레이어들이 로컬에서 이벤트 등록을 할 수 있도록 OnEnable에서 진행.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void GameStart(string sceneName, int maxGameCount = 1)
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        // 모든 플레이어의 점수를 0으로 초기화
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            player.SetTotalGameScore(0);
        }

        this.maxGameCount = maxGameCount;
        curGameCount = 0;

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

    public void GoNextMiniGame(string sceneName)
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
