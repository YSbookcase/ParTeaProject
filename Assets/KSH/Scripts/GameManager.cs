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
            int redTeam = TileManager.Instance.redTileCount;
            int blueTeam = TileManager.Instance.blueTileCount;

            int redScore = 0;
            int blueScore = 0;

            if (redTeam > blueTeam)
            {
                redScore = 5;
                blueScore = 3;
            }
            else if (blueTeam > redTeam)
            {
                blueScore = 5;
                redScore = 3;
            }
            else
            {
                redScore = blueScore = 4;
            }
        }
    }
}

