using System.Collections;
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

        [SerializeField] private string bgmName;
        private List<Player> _alivePlayers = new List<Player>();
        private int _currentRank;
    
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                _alivePlayers.Clear();
                foreach (var kvp in PhotonNetwork.CurrentRoom.Players)
                {
                    _alivePlayers.Add(kvp.Value);
                }
                _currentRank = _alivePlayers.Count;
            }

            StartCoroutine(StartBGM());
        }

        private IEnumerator StartBGM()
        {
            yield return new WaitForSeconds(1f);
            Manager.Audio.SfxPlay(bgmName);
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

            if (_alivePlayers.Contains(deadPlayer))
            {
                _alivePlayers.Remove(deadPlayer);

                // 랭크 설정
                deadPlayer.SetRank(_currentRank);
                Debug.Log($"{deadPlayer.NickName} 탈락! {_currentRank}위");

                _currentRank--;

                // 게임 종료 조건 확인
                if (_alivePlayers.Count <= 1)
                {
                    if (_alivePlayers.Count == 1)
                    {
                        _alivePlayers[0].SetRank(1);
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