using PJW;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using TMPro;

namespace PJW
{
    public class RopeGameManager : MonoBehaviourPunCallbacks
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI countdownText;

        [Header("Rank 계산기")]
        [SerializeField] private RankCalculator rankCalculator;

        private int totalPlayers;
        private int deathCount = 0;

        private void Start()
        {
            totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            BeginCountdown();
        }

        public void BeginCountdown()
        {
            deathCount = 0;

            StopAllCoroutines();
            StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
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

        public void OnPlayerDied(Player player)
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            deathCount++;
            if (deathCount >= totalPlayers)
            {
                EndGame();
            }
        }

        private void EndGame()
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            rankCalculator?.CalculateRanks();

            PhotonNetwork.LoadLevel("Score");
        }
    }
}
