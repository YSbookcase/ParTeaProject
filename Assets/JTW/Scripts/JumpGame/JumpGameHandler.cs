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
        [SerializeField] private GameObject jumpScorePanelPrefab;

        [SerializeField] private Canvas gameCanvas;
        [SerializeField] private List<Transform> obstacleLeftSpawnPoints;
        [SerializeField] private List<Transform> obstacleRightSpawnPoints;

        private GameObject localPlayer;

        private List<int> alivePlayers = new List<int>();
        private List<JumpScorePanel> jumpScorePanels = new List<JumpScorePanel>();

        private Vector3 playerSpawnPoint = new Vector3(-1f, 6f, 0);
        private bool isGameStarted;

        private bool isLeft;

        [PunRPC]
        private void JumpGameStart(PhotonMessageInfo info)
        { 
            if (isGameStarted) return;
            isGameStarted = true;
            Manager.Audio.BgmPlay("BGM_JumpGame");

            foreach(Player player in PhotonNetwork.PlayerList)
            {
                alivePlayers.Add(player.ActorNumber);
            }

            int playerNum = 0;
            foreach(Player player in PhotonNetwork.PlayerList)
            {
                if (player.IsLocal)
                {
                    break;
                }
                playerNum++;
            }

            if(playerNum > 1)
            {
                playerSpawnPoint.y = 0;
            }

            playerSpawnPoint.x += 2f * (playerNum % 2);

            localPlayer = PhotonNetwork.Instantiate("JTW_JumpPlayer", playerSpawnPoint, Quaternion.identity);

            Vector2 scorePoint = new Vector2(35, 150);

            foreach(Player player in PhotonNetwork.PlayerList)
            {
                GameObject obj = Instantiate(jumpScorePanelPrefab, gameCanvas.transform);
                obj.GetComponent<RectTransform>().anchoredPosition = scorePoint;

                JumpScorePanel scorePanel = obj.GetComponent<JumpScorePanel>();
                scorePanel.SetInfo(player);
                jumpScorePanels.Add(scorePanel);
                scorePoint.x += 260;
            }

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
                GameObject obstacle = null;

                List<Transform> obstacleSpawnPoints = isLeft ? obstacleLeftSpawnPoints : obstacleRightSpawnPoints;
                Vector3 direction = isLeft ? Vector3.right : Vector3.left;

                foreach (Transform trans in obstacleSpawnPoints)
                {
                    obstacle = Instantiate(obstaclePrefab, trans.position, Quaternion.Euler(new Vector3(90, 0, 0)));
                    obstacle.GetComponent<ObstacleHandler>().Init(direction, obstacleSpeed);
                }

                obstacleSpeed += 0.5f;

                if (PhotonNetwork.IsMasterClient)
                {
                    bool result = Random.value < 0.5f;
                    photonView.RPC(nameof(SetIsLeft), RpcTarget.All, result);
                }

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
                    photonView.RPC("DeleteJumpGamePlayer", RpcTarget.All);
                }

                timer += 0.5f;
                yield return new WaitForSeconds(0.5f);

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
        private void SetIsLeft(bool value)
        {
            isLeft = value;
        }

        [PunRPC]
        private void DeleteJumpGamePlayer(PhotonMessageInfo info)
        {
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
            if (changedProps.ContainsKey("jumpGameScore"))
            {
                foreach(JumpScorePanel panel in jumpScorePanels)
                {
                    if(panel.player == targetPlayer)
                    {
                        panel.SetScore((int)changedProps["jumpGameScore"]);
                        break;
                    }
                }
            }

            if (!PhotonNetwork.IsMasterClient) return;

            if (changedProps.ContainsKey("isLoaded"))
            {
                if (Manager.game.isAllPlayerLoaded())
                {
                    foreach(Player player in PhotonNetwork.PlayerList)
                    {
                        player.SetJumpGameScore(0);
                        bool result = Random.value < 0.5f;
                        photonView.RPC(nameof(SetIsLeft), RpcTarget.All, result);
                    }

                    photonView.RPC("JumpGameStart", RpcTarget.AllViaServer);
                }
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            alivePlayers.Remove(otherPlayer.ActorNumber);
        }
    }
}

