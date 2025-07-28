using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace KYS
{
    public class ReceiveGameManager : MonoBehaviourPunCallbacks
    {
        [Header("Game Settings")]
        [SerializeField] private float gameTime = 60f;
        [SerializeField] private int maxPlayers = 4;
        
        [Header("UI References")]
        [SerializeField] private ReceiveGameUI gameUI;
        
        [Header("Game Objects")]
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private Transform[] spawnPoints;
        
        private float currentTime;
        private bool isGameStarted = false;
        private bool isGameEnded = false;
        
        private Dictionary<int, int> playerScores = new Dictionary<int, int>();
        private List<GameObject> spawnedItems = new List<GameObject>();
        private List<int> alivePlayers = new List<int>();
        
        // Room Properties 키들
        private const string GAME_STARTED_KEY = "gameStarted";
        private const string GAME_TIME_KEY = "gameTime";
        private const string GAME_ENDED_KEY = "gameEnded";
        
        private void Start()
        {
            currentTime = gameTime;
            UpdateUI();
            
            // 디버그 로그 추가
            Debug.Log($"ReceiveGameManager 시작 - 플레이어 수: {PhotonNetwork.PlayerList.Length}");
            
            // 테스트용: 단일 플레이어에서도 게임 시작
            if (PhotonNetwork.PlayerList.Length == 1 && PhotonNetwork.IsMasterClient)
            {
                Debug.Log("단일 플레이어 모드로 게임 시작");
                StartCoroutine(StartGameDelayed());
            }
        }
        
        private IEnumerator StartGameDelayed()
        {
            yield return new WaitForSeconds(2f);
            InitializeGame();
        }
        
        private void Update()
        {
            if (isGameStarted && !isGameEnded && PhotonNetwork.IsMasterClient)
            {
                UpdateGameTime();
            }
        }
        
        private void InitializeGame()
        {
            Debug.Log("게임 초기화 시작");
            
            // 모든 플레이어의 점수 초기화
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                playerScores[player.ActorNumber] = 0;
                alivePlayers.Add(player.ActorNumber);
                
                // 플레이어 속성에 점수 설정
                Hashtable playerProps = new Hashtable();
                playerProps["score"] = 0;
                player.SetCustomProperties(playerProps);
                
                Debug.Log($"플레이어 {player.NickName} 초기화 완료");
            }
            
            // 게임 시작을 Room Properties로 설정
            StartGame();
            
            // 아이템 스폰 시작
            StartCoroutine(SpawnItemsRoutine());
        }
        
        private void StartGame()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                // Room Properties로 게임 상태 설정
                Hashtable roomProps = new Hashtable();
                roomProps[GAME_STARTED_KEY] = true;
                roomProps[GAME_TIME_KEY] = gameTime;
                roomProps[GAME_ENDED_KEY] = false;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            }
            
            isGameStarted = true;
            Debug.Log("ReceiveGame 시작!");
        }
        
        private void UpdateGameTime()
        {
            currentTime -= Time.deltaTime;
            
            if (currentTime <= 0)
            {
                currentTime = 0;
                EndGame();
            }
            
            // Room Properties로 시간 업데이트
            if (PhotonNetwork.IsMasterClient)
            {
                Hashtable roomProps = new Hashtable();
                roomProps[GAME_TIME_KEY] = currentTime;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            }
            
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            if (gameUI != null)
            {
                gameUI.UpdateTime(currentTime);
                gameUI.UpdateScore(playerScores);
            }
        }
        
        private IEnumerator SpawnItemsRoutine()
        {
            Debug.Log("아이템 스폰 시작");
            
            while (isGameStarted && !isGameEnded)
            {
                yield return new WaitForSeconds(Random.Range(1f, 3f));
                
                if (spawnPoints.Length > 0)
                {
                    Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                    SpawnItem(spawnPoint.position);
                    Debug.Log($"아이템 스폰: {spawnPoint.position}");
                }
            }
        }
        
        private void SpawnItem(Vector3 position)
        {
            if (itemPrefab != null)
            {
                GameObject item = Instantiate(itemPrefab, position, Quaternion.identity);
                spawnedItems.Add(item);
                
                // 10초 후 아이템 제거
                StartCoroutine(DestroyItemAfterTime(item, 10f));
            }
            else
            {
                Debug.LogWarning("ItemPrefab이 할당되지 않았습니다!");
            }
        }
        
        private IEnumerator DestroyItemAfterTime(GameObject item, float time)
        {
            yield return new WaitForSeconds(time);
            
            if (item != null)
            {
                spawnedItems.Remove(item);
                Destroy(item);
            }
        }
        
        public void CollectItem(int playerActorNumber)
        {
            if (PhotonNetwork.IsMasterClient && isGameStarted && !isGameEnded)
            {
                playerScores[playerActorNumber]++;
                
                // 플레이어 속성으로 점수 업데이트
                Player player = PhotonNetwork.CurrentRoom.GetPlayer(playerActorNumber);
                if (player != null)
                {
                    Hashtable playerProps = new Hashtable();
                    playerProps["score"] = playerScores[playerActorNumber];
                    player.SetCustomProperties(playerProps);
                }
                
                Debug.Log($"플레이어 {playerActorNumber} 아이템 수집! 점수: {playerScores[playerActorNumber]}");
            }
        }
        
        private void EndGame()
        {
            if (isGameEnded) return;
            
            isGameEnded = true;
            
            if (PhotonNetwork.IsMasterClient)
            {
                // Room Properties로 게임 종료 설정
                Hashtable roomProps = new Hashtable();
                roomProps[GAME_ENDED_KEY] = true;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
                
                CalculateAndSetRanks();
            }
            
            Debug.Log("ReceiveGame 종료!");
            
            // UI에 게임 종료 표시
            if (gameUI != null)
            {
                gameUI.ShowGameEnd();
            }
        }
        
        private void CalculateAndSetRanks()
        {
            // 점수별로 플레이어 정렬
            List<KeyValuePair<int, int>> sortedPlayers = new List<KeyValuePair<int, int>>(playerScores);
            sortedPlayers.Sort((a, b) => b.Value.CompareTo(a.Value));
            
            // 랭크 설정
            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                int rank = i + 1;
                int playerActorNumber = sortedPlayers[i].Key;
                
                Player player = PhotonNetwork.CurrentRoom.GetPlayer(playerActorNumber);
                if (player != null)
                {
                    Hashtable props = new Hashtable();
                    props["rank"] = rank;
                    player.SetCustomProperties(props);
                    Debug.Log($"플레이어 {player.NickName} 랭크: {rank}");
                }
            }
            
            // JTW 스코어 씬으로 이동
            StartCoroutine(LoadScoreScene());
        }
        
        private IEnumerator LoadScoreScene()
        {
            yield return new WaitForSeconds(3f); // 결과 표시 시간
            
            // JTW 스코어 씬 로드
            PhotonNetwork.LoadLevel("JTW_ScoreScene");
        }
        
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            // 플레이어가 나가면 alivePlayers에서 제거
            alivePlayers.Remove(otherPlayer.ActorNumber);
            
            // 모든 플레이어가 나가면 게임 종료
            if (PhotonNetwork.PlayerList.Length < 1)
            {
                EndGame();
            }
        }
        
        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            // Room Properties 변경 감지
            if (propertiesThatChanged.ContainsKey(GAME_STARTED_KEY))
            {
                isGameStarted = (bool)propertiesThatChanged[GAME_STARTED_KEY];
                Debug.Log($"게임 시작 상태 변경: {isGameStarted}");
            }
            
            if (propertiesThatChanged.ContainsKey(GAME_TIME_KEY))
            {
                currentTime = (float)propertiesThatChanged[GAME_TIME_KEY];
                UpdateUI();
            }
            
            if (propertiesThatChanged.ContainsKey(GAME_ENDED_KEY))
            {
                isGameEnded = (bool)propertiesThatChanged[GAME_ENDED_KEY];
                if (isGameEnded)
                {
                    Debug.Log("게임 종료됨");
                    if (gameUI != null)
                    {
                        gameUI.ShowGameEnd();
                    }
                }
            }
        }
        
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            // 플레이어 점수 업데이트 감지
            if (changedProps.ContainsKey("score"))
            {
                int newScore = (int)changedProps["score"];
                playerScores[targetPlayer.ActorNumber] = newScore;
                UpdateUI();
            }
            
            // 기존 로드 완료 체크
            if (changedProps.ContainsKey("isLoaded"))
            {
                try
                {
                    if (Manager.game.isAllPlayerLoaded())
                    {
                        Debug.Log("모든 플레이어 로드 완료 - 게임 시작");
                        InitializeGame();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Manager.game.isAllPlayerLoaded() 호출 중 오류: {e.Message}");
                    // Manager가 없는 경우 단일 플레이어 모드로 시작
                    if (PhotonNetwork.PlayerList.Length == 1)
                    {
                        Debug.Log("Manager 없음 - 단일 플레이어 모드로 시작");
                        InitializeGame();
                    }
                }
            }
        }
    }
} 