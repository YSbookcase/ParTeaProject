using System.Collections;
using UnityEngine;
using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.Playables;

namespace KSH
{
    public class GameManager : MonoBehaviourPunCallbacks
    {
        [Header("시간 설정")]
        [SerializeField] private float timer;
        public float Timer { get => timer; set => timer = value; }
        [SerializeField] private PlayableDirector timeLine;
        
        public bool isGameStart = false;
        private bool isTimeLine = false;
        public static GameManager Instance;
        public event Action OnGameStart;
        public event Action OnGameEnd;
        
        private Player player;
        public int readyCount;
        private bool isBgm = false;

        private void Awake()
        {
            if(Instance == null) // Instance가 null이면
            {
                Instance = this; // 할당
            }
            else // 이미 존재한다면
            {
                Destroy(gameObject); // 하나만 존재해야 하므로 제거
            }
        }
        

        private IEnumerator Start()
        {
            PhotonNetwork.SendRate = 30;
            PhotonNetwork.SerializationRate = 20;
            
            UIManager.Instance.OnCountDownEnd += StartGame; //카운트 다운이 끝나면 게임 시작
            
            if (!isTimeLine)
            {
                timeLine.Play();
                isTimeLine = true;
            }
            
            yield return null;

            if (PhotonNetwork.IsMasterClient)
            { 
                if(TeamManager.Instance != null)
                    TeamManager.Instance.SetTeam(); //팀 설정
                
                photonView.RPC("StartCount", RpcTarget.All);
            }
        }

        private void Update()
        {
            RunTime();
        }

        private void RunTime()
        {
            if (isGameStart) //게임이 시작하면
            {
                if (timer > 0)
                {
                    timer -= Time.deltaTime; //정해진 시간을 초마다 줄이기
                    if (!isBgm)
                    {
                        Manager.Audio.BgmPlay("KSH_BackGround");
                        isBgm = true;
                    }
                    OnGameStart?.Invoke();
                }
                else
                {
                    timer = 0;
                    EndGame();
                }
            }
        }

        private void EndGame()
        {
            foreach (PlayerController pc in FindObjectsOfType<PlayerController>())
            {
                if (pc.photonView != null && pc.photonView.ViewID != 0)
                {
                    pc.photonView.RPC("RPC_DontMove", RpcTarget.All);
                }
            }
            isGameStart = false;
            OnGameEnd?.Invoke();
            PlayerRank();
            StartCoroutine(ScoreDelay(5f));
        }

        private IEnumerator ScoreDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel("Score");
            }
        }

        private void PlayerRank()
        {
            if (!PhotonNetwork.IsMasterClient)
                return;
            
            int redTeam = TileManager.Instance.redTileCount;
            int blueTeam = TileManager.Instance.blueTileCount;

            int redRank = 0;
            int blueRank = 0;

            if (redTeam > blueTeam)
            {
                redRank = 1;
                blueRank = 2;
            }
            else if (blueTeam > redTeam)
            {
                blueRank = 1;
                redRank = 2;
            }
            else
            {
                blueRank = 3;
                redRank = 3;
            }
            
            foreach (Player player in PhotonNetwork.PlayerList)
            {
              int team = (int)player.CustomProperties["Team"];
              int totalRank = (team == 0) ? redRank : blueRank;
              int rank = player.GetRank();
              player.SetRank(totalRank);
            }
        }

        [PunRPC]
        private void StartCount()
        {
            foreach (PlayerController pc in FindObjectsOfType<PlayerController>())
            {
                pc.photonView.RPC("RPC_DontMove", RpcTarget.All);
            }
            
            UIManager.Instance.StartCountDown();
        }

        private void StartGame()
        {
            isGameStart = true;
            
            foreach (PlayerController pc in FindObjectsOfType<PlayerController>())
            {
                pc.photonView.RPC("RPC_CanMove", RpcTarget.All);
            }
        }
    }
}

