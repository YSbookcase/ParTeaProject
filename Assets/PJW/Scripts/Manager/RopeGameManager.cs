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

        [Header("JumpRopeController가 붙은 오브젝트")]
        [SerializeField] private JumpRopeController jumpRopeController;

        private int totalPlayers;
        private int deathCount = 0;

        private const string IsLoadedKey = "isRopeLoaded";

        private void Start()
        {
            if (PhotonNetwork.IsMasterClient)
                totalPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;

            // 자신의 로딩 완료 상태 설정
            var props = new PhotonHashtable { { IsLoadedKey, true } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            if (changedProps.ContainsKey(IsLoadedKey))
            {
                bool allLoaded = PhotonNetwork.PlayerList
                    .All(p => p.CustomProperties.ContainsKey(IsLoadedKey) && (bool)p.CustomProperties[IsLoadedKey]);

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
            totalPlayers = PhotonNetwork.PlayerList.Length;

            deathCount = 0;
            StopAllCoroutines();
            StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            float lag = PhotonNetwork.GetPing() / 1000f;
            float startDelay = Mathf.Max(0f, 2f - lag);

            yield return new WaitForSeconds(startDelay);

            Time.timeScale = 0f;
            countdownText.gameObject.SetActive(true);

            countdownText.text = " ";
            yield return new WaitForSecondsRealtime(3f);

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

            double startTimestamp = PhotonNetwork.Time + 0.1;
            jumpRopeController.photonView.RPC(
                "RPCStartRope",
                RpcTarget.All,
                startTimestamp
            );
        }

        public void OnPlayerDied(Player player)
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            deathCount++;

            if (deathCount >= totalPlayers)
            {
                RankCalculator.CalculateRanks();

                photonView.RPC(nameof(RPCRopeShowDeathPanel), RpcTarget.AllViaServer);
            }
        }


        [PunRPC]
        private void RPCRopeShowDeathPanel()
        {
            StartCoroutine(LoadScoreAfterDelay());
        }

        private IEnumerator LoadScoreAfterDelay()
        {
            yield return new WaitForSecondsRealtime(3f);
            PhotonNetwork.LoadLevel("Score");
        }
    }
}
