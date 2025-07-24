using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JTW_JumpGame
{
    public class JumpGameHandler : MonoBehaviourPunCallbacks
    {
        [Header("테스트 용")]
        [SerializeField] private Button gameEndButton;

        private bool isGameStarted;

        private void Awake()
        {
            gameEndButton.onClick.AddListener(GameEnd);
        }

        private void GameStart()
        {
            if (isGameStarted) return;
            isGameStarted = true;

            Debug.Log("점프 게임 시작!");
            // TODO : 점프 게임 로직 구현
        }

        private void GameEnd()
        {
            // TODO : 점수에 따른 순위 확정 로직 작성

            int rank = 1;
            foreach(Player player in PhotonNetwork.PlayerList)
            {
                player.SetRank(rank);
                rank++;
            }

            Manager.game.GoScoerScene();
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (changedProps.ContainsKey("isLoaded"))
            {
                if (Manager.game.isAllPlayerLoaded())
                {
                    GameStart();
                }
            }
        }
    }
}

