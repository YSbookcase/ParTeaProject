// GameStartManager.cs
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using System.Collections;

namespace PJW
{
    [RequireComponent(typeof(PhotonView))]
    public class GameStartManager : MonoBehaviourPunCallbacks
    {
        [Header("카운트다운 설정")]
        [SerializeField] private float countdownSeconds = 3f;
        [SerializeField] private TextMeshProUGUI countdownText;

        private int loadedPlayers = 0;

        private void Start()
        {
            Time.timeScale = 0f;

            photonView.RPC(nameof(RpcNotifyLoaded), RpcTarget.MasterClient);
        }

        [PunRPC]
        private void RpcNotifyLoaded()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            loadedPlayers++;
            Debug.Log($"[Master] 로딩 완료: {loadedPlayers}/{PhotonNetwork.CurrentRoom.PlayerCount}");

            if (loadedPlayers >= PhotonNetwork.CurrentRoom.PlayerCount)
                photonView.RPC(nameof(RpcStartGame), RpcTarget.AllBuffered);
        }

        [PunRPC]
        private void RpcStartGame()
        {
            StartCoroutine(StartCountdown());
        }

        private IEnumerator StartCountdown()
        {
            float timer = countdownSeconds;
            while (timer > 0f)
            {
                if (countdownText != null)
                    countdownText.text = Mathf.Ceil(timer).ToString();
                yield return new WaitForSecondsRealtime(1f);
                timer -= 1f;
            }

            if (countdownText != null)
                countdownText.gameObject.SetActive(false);

            Time.timeScale = 1f;
        }
    }
}
