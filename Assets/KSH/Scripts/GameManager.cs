using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace KSH
{
    public class GameManager : MonoBehaviour
    {
        [Header("시간 설정")]
        [SerializeField] private float timer;
        public float Timer { get => timer; set => timer = value; }
        
        private bool isGameStart = false;

        public static GameManager Instance;
        public event Action OnGameStart;
        public event Action OnGameEnd;
        
        private Player player;

        private void Awake()
        {
            if(Instance == null) // Instance가 null이면
            {
                Instance = this; // 할당
                DontDestroyOnLoad(this);
            }
            else // 이미 존재한다면
            {
                Destroy(gameObject); // 하나만 존재해야 하므로 제거
            }
        }
        

        private void Start()
        {
            isGameStart = true;
            TeamManager.Instance.SetTeam();
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
    }
}

