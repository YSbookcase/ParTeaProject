using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace JTW_JumpGame
{
    public class JumpGameHandler : MonoBehaviourPunCallbacks
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject obstaclePrefab;

        [SerializeField] private Transform obstacleSpawnPoint;

        [Header("테스트 용")]
        [SerializeField] private Button gameEndButton;

        private GameObject localPlayer;

        private List<int> alivePlayers = new List<int>();

        private Vector3 playerSpawnPoint = new Vector3(-3f, 0, 0);
        private bool isGameStarted;

        private void Awake()
        {
            gameEndButton.onClick.AddListener(GameEnd);
        }


        [PunRPC]
        private void JumpGameStart(PhotonMessageInfo info)
        { 
            if (isGameStarted) return;
            isGameStarted = true;

            int playerNum = 0;
            foreach(Player player in PhotonNetwork.PlayerList)
            {
                if (player.IsLocal)
                {
                    break;
                }
                playerNum++;
            }

            playerSpawnPoint.x += 2f * (playerNum);

            localPlayer = PhotonNetwork.Instantiate("JTW_JumpPlayer", playerSpawnPoint, Quaternion.identity);

            Debug.Log("점프 게임 시작!");

            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));

            StartCoroutine(JumpGameCoroutine(lag));
        }

        private IEnumerator JumpGameCoroutine(float lag)
        {
            // 지연 보상
            float startDelay = 2f - lag;

            yield return new WaitForSeconds(startDelay);

            float obstacleSpeed = 3f;

            float timer = 0;

            while (timer <= 60)
            {
                GameObject obstacle = Instantiate(obstaclePrefab, obstacleSpawnPoint.position, Quaternion.Euler(new Vector3(90, 0, 0)));
                obstacle.GetComponent<ObstacleHandler>().Init(Vector3.right, obstacleSpeed);
                obstacleSpeed += 0.5f;

                while (true)
                {
                    if (obstacle == null) break;

                    timer += Time.deltaTime;
                    yield return null;
                }

                if(localPlayer != null)
                {
                    PhotonNetwork.LocalPlayer.AddJumpGameScore(1);
                }
                else
                {
                    photonView.RPC("DeleteJumpGamePlayer", RpcTarget.MasterClient);
                }

                timer += 2;
                yield return new WaitForSeconds(2f);

                if(PhotonNetwork.IsMasterClient && alivePlayers.Count <= 0)
                {
                    break;
                }
            }

            if (PhotonNetwork.IsMasterClient)
            {
                GameEnd();
            }

        }

        [PunRPC]
        private void DeleteJumpGamePlayer(PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            alivePlayers.Remove(info.Sender.ActorNumber);
        }

        private void GameEnd()
        {
            List<Player> players = PhotonNetwork.PlayerList.OrderByDescending(p => p.GetJumpGameScore()).ToList();

            int rank = -1;
            int score = -1;

            for(int i = 0; i < players.Count; i++)
            {
                // 이전 플레이어와 같은 점수일 경우, 같은 등수를 부여한다.
                // 예) 1등(3점), 1등(3점), 1등(3점), 4등(2점)
                if(score == players[i].GetJumpGameScore())
                {
                    players[i].SetRank(rank);
                }
                else
                {
                    players[i].SetRank(i + 1);
                    rank = i + 1;
                    score = players[i].GetJumpGameScore();
                }
            }

            Manager.game.GoScoerScene();
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            if (changedProps.ContainsKey("isLoaded"))
            {
                if (Manager.game.isAllPlayerLoaded())
                {
                    foreach(Player player in PhotonNetwork.PlayerList)
                    {
                        alivePlayers.Add(player.ActorNumber);
                        player.SetJumpGameScore(0);
                    }

                    photonView.RPC("JumpGameStart", RpcTarget.AllViaServer);
                }
            }
        }
    }
}

