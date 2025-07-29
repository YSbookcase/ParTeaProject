using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using TMPro;

public class RacingManager : MonoBehaviourPunCallbacks
{
    public static RacingManager Instance;

    public PhotonView managerView;

    public List<Player> racingPlayers = new List<Player>();
    public List<Player> arrivePlayers = new List<Player>();

    private int currentRank;
    private int retireRank;
    private bool firstArrive;
    private bool isRacingFinished;

    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject countdownUI;
    private Coroutine racingCountDown;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        managerView = GetComponent<PhotonView>();

        racingPlayers.Clear();
        arrivePlayers.Clear();

        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            racingPlayers.Add(player);
        }

        currentRank = 1;
        retireRank = racingPlayers.Count;
        firstArrive = false;
        isRacingFinished = false;
    }

    [PunRPC]
    public void RacingStart()
    {
        if (racingCountDown != null)
            StopCoroutine(racingCountDown);

        racingCountDown = StartCoroutine(CountDown(3));
    }

    [PunRPC]
    public void RetireCount()
    {
        if(racingCountDown != null)
        {
            StopCoroutine(racingCountDown);
        }
        racingCountDown = StartCoroutine(CountDown(5));
    }

    [PunRPC]
    public void RacingFinish()
    {
        if (isRacingFinished) return;
        isRacingFinished = true;

        foreach (Player retire in racingPlayers)
        {
            retire.SetRank(retireRank);
        }
        if(PhotonNetwork.IsMasterClient)
        {
            SceneManager.LoadScene("Score");
        }
    }
    [PunRPC]
    public void PlayerArrive(int actorNumber)
    {
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (!firstArrive)
        {
            firstArrive = true;
            managerView.RPC("RetireCount", RpcTarget.All);
        }

        arrivePlayers.Add(player);
        player.SetRank(currentRank);

        currentRank++;
        racingPlayers.Remove(player);

        if (racingPlayers.Count == 0)
        {
            managerView.RPC(nameof(RacingFinish), RpcTarget.All);
        }
    }

    private IEnumerator CountDown(int seconds)
    {
        countdownUI.SetActive(true);

        while (seconds > 0)
        {
            countdownText.text = seconds.ToString();
            yield return new WaitForSecondsRealtime(1f);
            seconds--;
        }

        countdownUI.SetActive(false);
        if (firstArrive && PhotonNetwork.IsMasterClient)
        {
            managerView.RPC(nameof(RacingFinish), RpcTarget.All);
        }
        yield return null;
    }

}
