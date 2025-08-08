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
        private GameObject obstacle;

        private List<int> alivePlayers = new List<int>();
        private List<JumpScorePanel> jumpScorePanels = new List<JumpScorePanel>();

        private Vector3 playerSpawnPoint = new Vector3(-1f, 6f, 0);
        private float obstacleSpeed;

        private double startTime;
        private bool isGameStarted;

        [PunRPC]
        private void JumpGameStart(PhotonMessageInfo info)
        { 
            if (isGameStarted) return;
            isGameStarted = true;

            obstacleSpeed = 3f;

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

            double lag = (PhotonNetwork.Time - info.SentServerTime);

            if(lag < 0)
            {
                lag *= -1;
            }

            startTime = PhotonNetwork.Time;

            if (!PhotonNetwork.IsMasterClient) return;

            StartCoroutine(JumpGameCoroutine(false));
        }

        private IEnumerator JumpGameCoroutine(bool isStarted)
        {
            float startDelay = 0f;

            if (!isStarted)
            {
                startDelay = 2f;
            }

            yield return new WaitForSeconds(startDelay);

            while (PhotonNetwork.Time - startTime <= 60)
            {
                // Master가 변했을 경우 장애물이 사라질 때까지 기다린다.
                while (true)
                {
                    if (obstacle == null) break;
                    yield return null;
                }

                bool result = Random.value < 0.5f;
                photonView.RPC(nameof(SpawnObstacle_JumpGame), RpcTarget.All, result);

                while (true)
                {
                    if (obstacle == null) break;
                    yield return null;
                }

                photonView.RPC(nameof(CheckJumpGamePlayerAlive), RpcTarget.All);

                yield return new WaitForSeconds(0.5f);

                if (alivePlayers.Count <= 0)
                {
                    break;
                }
            }

            GameEnd();
        }

        [PunRPC]
        private void SpawnObstacle_JumpGame(bool isLeft, PhotonMessageInfo info)
        {
            float delay = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));

            List<Transform> obstacleSpawnPoints = isLeft ? obstacleLeftSpawnPoints : obstacleRightSpawnPoints;
            Vector3 direction = isLeft ? Vector3.right : Vector3.left;

            foreach (Transform trans in obstacleSpawnPoints)
            {
                obstacle = Instantiate(obstaclePrefab, trans.position + direction * delay * obstacleSpeed, Quaternion.Euler(new Vector3(90, 0, 0)));
                obstacle.GetComponent<ObstacleHandler>().Init(direction, obstacleSpeed);
            }

            obstacleSpeed += 0.5f;
        }

        [PunRPC]
        private void CheckJumpGamePlayerAlive()
        {
            if (localPlayer != null)
            {
                PhotonNetwork.LocalPlayer.AddJumpGameScore(1);
            }
            else
            {
                photonView.RPC("DeleteJumpGamePlayer", RpcTarget.All);
            }
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
                if (Manager.game.isAllPlayerLoaded() && !isGameStarted)
                {
                    foreach (Player player in PhotonNetwork.PlayerList)
                    {
                        player.SetJumpGameScore(0);
                    }

                    photonView.RPC("JumpGameStart", RpcTarget.All);
                }
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            alivePlayers.Remove(otherPlayer.ActorNumber);
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            if (!newMasterClient.IsLocal) return;

            StartCoroutine(JumpGameCoroutine(true));
        }
    }
}

