using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace KYS
{
    public class ReceiveGameTester : MonoBehaviourPunCallbacks
    {
        [Header("Test Settings")]
        [SerializeField] private bool enableTestMode = true;
        [SerializeField] private bool autoConnect = true;
        [SerializeField] private string roomName = "ReceiveGame";
        [SerializeField] private KeyCode testStartKey = KeyCode.Space;
        [SerializeField] private KeyCode testItemSpawnKey = KeyCode.I;
        [SerializeField] private KeyCode connectKey = KeyCode.C;
        [SerializeField] private KeyCode createRoomKey = KeyCode.R;
        
        private ReceiveGameManager gameManager;
        private ReceiveGameSpawner spawner;
        private ReceiveGameUI gameUI;
        
        private void Start()
        {
            if (!enableTestMode) return;
            
            gameManager = FindObjectOfType<ReceiveGameManager>();
            spawner = FindObjectOfType<ReceiveGameSpawner>();
            gameUI = FindObjectOfType<ReceiveGameUI>();
            
            Debug.Log("=== ReceiveGame 테스트 모드 활성화 ===");
            Debug.Log($"GameManager: {(gameManager != null ? "찾음" : "없음")}");
            Debug.Log($"Spawner: {(spawner != null ? "찾음" : "없음")}");
            Debug.Log($"GameUI: {(gameUI != null ? "찾음" : "없음")}");
            Debug.Log($"Photon 연결: {PhotonNetwork.IsConnected}");
            Debug.Log($"방 입장: {PhotonNetwork.InRoom}");
            Debug.Log($"플레이어 수: {PhotonNetwork.PlayerList.Length}");
            Debug.Log($"마스터 클라이언트: {PhotonNetwork.IsMasterClient}");
            Debug.Log("=== 테스트 키 ===");
            Debug.Log($"포톤 연결: {connectKey}");
            Debug.Log($"룸 생성/입장: {createRoomKey}");
            Debug.Log($"게임 시작: {testStartKey}");
            Debug.Log($"아이템 스폰: {testItemSpawnKey}");
            
            // 자동 연결 활성화된 경우
            if (autoConnect && !PhotonNetwork.IsConnected)
            {
                ConnectToPhoton();
            }
        }
        
        private void Update()
        {
            if (!enableTestMode) return;
            
            // 포톤 연결 테스트
            if (Input.GetKeyDown(connectKey))
            {
                ConnectToPhoton();
            }
            
            // 룸 생성/입장 테스트
            if (Input.GetKeyDown(createRoomKey))
            {
                CreateOrJoinRoom();
            }
            
            // 게임 시작 테스트
            if (Input.GetKeyDown(testStartKey))
            {
                TestGameStart();
            }
            
            // 아이템 스폰 테스트
            if (Input.GetKeyDown(testItemSpawnKey))
            {
                TestItemSpawn();
            }
        }
        
        private void TestGameStart()
        {
            Debug.Log("=== 게임 시작 테스트 ===");
            
            if (gameManager == null)
            {
                Debug.LogError("GameManager를 찾을 수 없습니다!");
                return;
            }
            
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogError("Photon에 연결되지 않았습니다!");
                return;
            }
            
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogError("방에 입장하지 않았습니다!");
                return;
            }
            
            Debug.Log("게임 시작 테스트 실행...");
            
            // 단일 플레이어 모드로 게임 시작
            if (PhotonNetwork.PlayerList.Length == 1 && PhotonNetwork.IsMasterClient)
            {
                Debug.Log("단일 플레이어 모드로 게임 시작");
                // 게임 매니저의 StartGameDelayed 코루틴 호출
                gameManager.SendMessage("StartGameDelayed");
            }
            else
            {
                Debug.Log("멀티플레이어 모드 - 모든 플레이어 대기 중");
            }
        }
        
        private void TestItemSpawn()
        {
            Debug.Log("=== 아이템 스폰 테스트 ===");
            
            if (gameManager == null)
            {
                Debug.LogError("GameManager를 찾을 수 없습니다!");
                return;
            }
            
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("아이템 스폰 테스트 실행...");
                // 랜덤 위치에 아이템 스폰
                Vector3 randomPosition = new Vector3(
                    Random.Range(-5f, 5f),
                    1f,
                    Random.Range(-5f, 5f)
                );
                
                gameManager.SendMessage("SpawnItem", randomPosition);
            }
            else
            {
                Debug.Log("마스터 클라이언트가 아닙니다!");
            }
        }
        
        // 포톤 연결
        public void ConnectToPhoton()
        {
            if (PhotonNetwork.IsConnected)
            {
                Debug.Log("이미 포톤에 연결되어 있습니다.");
                return;
            }
            
            Debug.Log("포톤 서버에 연결 중...");
            PhotonNetwork.ConnectUsingSettings();
        }
        
        // 룸 생성 또는 입장
        public void CreateOrJoinRoom()
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogError("포톤에 연결되지 않았습니다. 먼저 연결해주세요.");
                return;
            }
            
            if (PhotonNetwork.InRoom)
            {
                Debug.Log("이미 방에 입장되어 있습니다.");
                return;
            }
            
            Debug.Log($"룸 '{roomName}' 생성/입장 시도...");
            
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = 4,
                IsVisible = true,
                IsOpen = true
            };
            
            PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
        }
        
        // 포톤 콜백 메서드들
        public override void OnConnectedToMaster()
        {
            Debug.Log("포톤 마스터 서버에 연결되었습니다!");
            Debug.Log($"닉네임: {PhotonNetwork.NickName}");
            
            // 자동으로 룸 생성/입장
            if (autoConnect)
            {
                CreateOrJoinRoom();
            }
        }
        
        public override void OnJoinedRoom()
        {
            Debug.Log($"룸 '{PhotonNetwork.CurrentRoom.Name}'에 입장했습니다!");
            Debug.Log($"플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
            Debug.Log($"마스터 클라이언트: {PhotonNetwork.IsMasterClient}");
            
            // 플레이어 닉네임 설정
            if (string.IsNullOrEmpty(PhotonNetwork.NickName))
            {
                PhotonNetwork.NickName = $"Player_{Random.Range(1000, 9999)}";
            }
        }
        
        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"룸 입장 실패: {message} (코드: {returnCode})");
        }
        
        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"포톤 연결 해제: {cause}");
        }
        
        [ContextMenu("게임 상태 확인")]
        private void CheckGameStatus()
        {
            Debug.Log("=== 게임 상태 확인 ===");
            Debug.Log($"Photon 연결: {PhotonNetwork.IsConnected}");
            Debug.Log($"방 입장: {PhotonNetwork.InRoom}");
            Debug.Log($"플레이어 수: {PhotonNetwork.PlayerList.Length}");
            Debug.Log($"마스터 클라이언트: {PhotonNetwork.IsMasterClient}");
            
            if (PhotonNetwork.InRoom)
            {
                Debug.Log($"현재 룸: {PhotonNetwork.CurrentRoom.Name}");
                Debug.Log($"룸 플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
            }
            
            if (gameManager != null)
            {
                Debug.Log("GameManager 상태 확인 완료");
            }
            
            if (spawner != null)
            {
                Debug.Log("Spawner 상태 확인 완료");
            }
            
            if (gameUI != null)
            {
                Debug.Log("GameUI 상태 확인 완료");
            }
        }
    }
} 