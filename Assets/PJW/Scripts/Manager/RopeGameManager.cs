using PJW;
using Photon.Pun;
using Photon.Realtime;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using System.Collections;
using System.Linq;
using UnityEngine;
using TMPro;

namespace PJW
{
    [RequireComponent(typeof(PhotonView))]
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

            // 자신의 로딩 완료 상태 설정
            var props = new PhotonHashtable { { IsLoadedKey, true } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        // 모든 플레이어가 준비되면 호출
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            if (changedProps.ContainsKey(IsLoadedKey))
            {
                bool allLoaded = PhotonNetwork.PlayerList.All(p => p.CustomProperties.ContainsKey(IsLoadedKey) && (bool)p.CustomProperties[IsLoadedKey]);

                if (allLoaded)
                {
                    photonView.RPC(nameof(RPCBeginCountdown), RpcTarget.AllViaServer);
                }
            }
        }

        [PunRPC]
        private void RPCBeginCountdown()
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

            countdownText.text = " ";
            yield return new WaitForSecondsRealtime(1f);

            countdownText.text = " ";
            yield return new WaitForSecondsRealtime(1f);

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
                rankCalculator?.CalculateRanks();

                string winnerName = player.NickName;
                photonView.RPC(nameof(RPCRopeShowDeathPanel), RpcTarget.AllViaServer, winnerName);
            }
        }

        [PunRPC]
        private void RPCRopeShowDeathPanel(string winnerName)
        {
            if (RopeUIManager.Instance != null)
                RopeUIManager.Instance.ShowDeathPanel(winnerName);

            StartCoroutine(LoadScoreAfterDelay());
        }

        private IEnumerator LoadScoreAfterDelay()
        {
            yield return new WaitForSecondsRealtime(3f);
            PhotonNetwork.LoadLevel("Score");
        }
    }
}
