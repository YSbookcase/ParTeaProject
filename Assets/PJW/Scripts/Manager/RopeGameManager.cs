using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PJW
{
    public class RopeGameManager : MonoBehaviour
    {
        [SerializeField] private Text countdownText;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private RankCalculator rankCalculator;

        private int totalPlayers;
        private int deadPlayers = 0;
        private int deathCount = 0;

        private void Start()
        {
            totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            BeginCountdown();
        }

        public void BeginCountdown()
        {
            StopAllCoroutines();
            StartCoroutine(StartCountdownRoutine());
        }

        private IEnumerator StartCountdownRoutine()
        {
            Time.timeScale = 0f;
            countdownText.gameObject.SetActive(true);

            countdownText.text = "3";
            yield return new WaitForSecondsRealtime(1f);

            countdownText.text = "2";
            yield return new WaitForSecondsRealtime(1f);

            countdownText.text = "1";
            yield return new WaitForSecondsRealtime(1f);

            countdownText.text = "시작!";
            yield return new WaitForSecondsRealtime(1f);

            countdownText.gameObject.SetActive(false);
            Time.timeScale = 1f;
        }

        public void OnPlayerDied_RPC(Player playerWhoDied)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            deathCount++;

            int score = 0;
            switch (deathCount)
            {
                case 1: score = 2; break;
                case 2: score = 3; break;
                case 3: score = 4; break;
                case 4: score = 5; break;
                default: score = 0; break;
            }

            playerWhoDied.SetTotalGameScore(score);
            Debug.Log($"{playerWhoDied.NickName}가 {deathCount}번째로 죽었고, {score}점을 받음");

            deadPlayers++;

            if (deadPlayers >= totalPlayers)
            {
                EndGame();
            }
        }

        private void EndGame()
        {
            Debug.Log("게임 종료");

            if (PhotonNetwork.IsMasterClient && rankCalculator != null)
            {
                rankCalculator.CalculateRanks();
            }

            gameOverPanel?.SetActive(true);
        }
    }
}
