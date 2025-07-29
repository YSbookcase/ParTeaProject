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

    public List<Player> racingPlayers = new List<Player>();
    public List<Player> arrivePlayers = new List<Player>();

    private int currentRank;
    private int retireRank;
    private bool firstArrive;

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
        racingPlayers.Clear();
        arrivePlayers.Clear();

        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            racingPlayers.Add(player);
        }

        currentRank = 1;
        retireRank = racingPlayers.Count;
        firstArrive = false;
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
        foreach (Player retire in racingPlayers)
        {
            retire.SetRank(retireRank);
        }

        SceneManager.LoadScene("Score");
    }

    public void PlayerArrive(Player player)
    {
        if (!firstArrive)
        {
            firstArrive = true;
            photonView.RPC("RetireCount", RpcTarget.All);
        }

        arrivePlayers.Add(player);
        player.SetRank(currentRank);

        currentRank++;
        racingPlayers.Remove(player);

        if (racingPlayers.Count == 0)
        {
            photonView.RPC(nameof(RacingFinish), RpcTarget.All);
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
        if (firstArrive)
        {
            photonView.RPC(nameof(RacingFinish), RpcTarget.All);
        }
        yield return null;
    }

}
