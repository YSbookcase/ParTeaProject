using PJW;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections;
using System.Linq;
using UnityEngine;
using TMPro;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

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

        private const string IsLoadedKey = "isRopeLoaded";

        private void Start()
        {
            totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

            var props = new PhotonHashtable { { IsLoadedKey, true }};
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            if (changedProps.ContainsKey(IsLoadedKey))
            {
                bool allLoaded = PhotonNetwork.PlayerList
                    .All(p => p.CustomProperties.ContainsKey(IsLoadedKey)
                           && (bool)p.CustomProperties[IsLoadedKey]);

                if (allLoaded)
                {
                    photonView.RPC("RPCRopeBeginCountdown", RpcTarget.AllViaServer);
                }
            }
        }

        // RPC로 호출되는 카운트다운 시작
        [PunRPC]
        private void RPCRopeBeginCountdown()
        {
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
