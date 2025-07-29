using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GIL.Scripts
{
    public class ArenaGameManager : MonoBehaviourPunCallbacks
    {
        public static ArenaGameManager Instance;

        private List<Player> alivePlayers = new List<Player>();
        private int currentRank;
    
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                alivePlayers.Clear();
                foreach (var kvp in PhotonNetwork.CurrentRoom.Players)
                {
                    alivePlayers.Add(kvp.Value);
                }
                currentRank = alivePlayers.Count;
            }
        }

        /// <summary>
        /// 플레이어가 죽을 때 호출되는 함수
        /// </summary>
        [PunRPC]
        public void ArenaPlayerDied(int actorNumber)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            Player deadPlayer = GetPlayerByActorNumber(actorNumber);
            if (deadPlayer == null)
            {
                Debug.LogWarning($"[ArenaPlayerDied] ActorNumber {actorNumber}에 해당하는 Player를 찾을 수 없음");
                return;
            }

            if (alivePlayers.Contains(deadPlayer))
            {
                alivePlayers.Remove(deadPlayer);

                // 랭크 설정
                deadPlayer.SetRank(currentRank);
                Debug.Log($"{deadPlayer.NickName} 탈락! {currentRank}위");

                currentRank--;

                // 게임 종료 조건 확인
                if (alivePlayers.Count <= 1)
                {
                    if (alivePlayers.Count == 1)
                    {
                        alivePlayers[0].SetRank(1);
                    }

                    photonView.RPC(nameof(ArenaEndGame), RpcTarget.All);
                }
            }
        }

        private Player GetPlayerByActorNumber(int actorNumber)
        {
            foreach (var p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber == actorNumber) return p;
            }
            return null;
        }

        [PunRPC]
        private void ArenaEndGame()
        {
            Debug.Log("게임 종료! 최종 순위:");
            foreach (var p in PhotonNetwork.PlayerList)
            {
                int rank = p.GetRank();
                Debug.Log($"{rank}위: {p.NickName}");
            }

            SceneManager.LoadScene("Score");
        }
    }
}