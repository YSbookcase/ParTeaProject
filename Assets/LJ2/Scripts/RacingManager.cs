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

    public Dictionary<int, RacingController> racingControllers = new Dictionary<int, RacingController>();

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

    private void Start()
    {
        
    }

    [PunRPC]
    public void RacingStart()
    {
        if (racingCountDown != null)
            StopCoroutine(racingCountDown);
        Manager.Audio.BgmPlay("racingBGM");
        racingCountDown = StartCoroutine(CountDown(3));
    }

    [PunRPC]
    public void RetireCount()
    {
        if (racingCountDown != null)
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
        if (!PhotonNetwork.IsMasterClient) return;

        foreach (Player retire in racingPlayers)
        {
            if(retire == null) continue; // Check if retire is not null to avoid NullReferenceException
            retire.SetRank(retireRank);
            Debug.Log($"{retire.NickName} has retired with rank {retireRank}");
        }
        managerView.RPC("StopRacingSound", RpcTarget.All);
        if (PhotonNetwork.IsMasterClient)
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
            if(PhotonNetwork.IsMasterClient)
            {
                managerView.RPC("RetireCount", RpcTarget.All);
            }
        }

        arrivePlayers.Add(player);
        player.SetRank(currentRank);

        currentRank++;
        racingPlayers.RemoveAll(p => p.ActorNumber == actorNumber);

        if (racingPlayers.Count == 0)
        {
            StopCoroutine(racingCountDown); 
            managerView.RPC(nameof(RacingFinish), RpcTarget.All);
        }
    }

    private IEnumerator CountDown(int seconds)
    {
        countdownUI.SetActive(true);

        while (seconds > 0)
        {
            Manager.Audio.SfxPlay("racingCountDown", Camera.main.transform);
            countdownText.text = seconds.ToString();
            yield return new WaitForSecondsRealtime(1f);
            seconds--;
        }

        // 시작 카운트다운이 끝나면 RacingController의 SetControllable을 호출하여 플레이어가 조종할 수 있도록 설정
        foreach (RacingController controller in racingControllers.Values)
        {
            controller.photonView.RPC("SetControllable", RpcTarget.All, true);
        }

        countdownUI.SetActive(false);

        // 도착한 플레이어가 있고 카운트다운이 끝나면 RacingFinish를 호출
        if (firstArrive && PhotonNetwork.IsMasterClient)
        {
            if (!isRacingFinished)
            {
                managerView.RPC(nameof(RacingFinish), RpcTarget.All);
            }
            foreach (RacingController controller in racingControllers.Values)
            {
                controller.photonView.RPC("SetControllable", RpcTarget.All, false);
            }
        }
        if (!isRacingFinished)
        {
            Manager.Audio.SfxPlay("racingStart", Camera.main.transform);
            Debug.Log($"IsMasterClient = {PhotonNetwork.IsMasterClient} : Racing Start!");
        }

        yield return null;
    }

}
