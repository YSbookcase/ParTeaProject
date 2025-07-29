using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

namespace KYS
{
    public class ReceiveGameTestUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button connectButton;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button spawnItemButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private TextMeshProUGUI roomInfoText;
        
        private ReceiveGameTester tester;
        private ReceiveGameManager gameManager;
        
        private void Start()
        {
            tester = FindObjectOfType<ReceiveGameTester>();
            gameManager = FindObjectOfType<ReceiveGameManager>();
            
            // 버튼 이벤트 연결
            if (connectButton != null)
                connectButton.onClick.AddListener(OnConnectButtonClick);
            if (createRoomButton != null)
                createRoomButton.onClick.AddListener(OnCreateRoomButtonClick);
            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGameButtonClick);
            if (spawnItemButton != null)
                spawnItemButton.onClick.AddListener(OnSpawnItemButtonClick);
        }
        
        private void Update()
        {
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            // 상태 텍스트 업데이트
            if (statusText != null)
            {
                if (PhotonNetwork.IsConnected)
                {
                    if (PhotonNetwork.InRoom)
                    {
                        statusText.text = $"연결됨 - 룸: {PhotonNetwork.CurrentRoom.Name}";
                        statusText.color = Color.green;
                    }
                    else
                    {
                        statusText.text = "연결됨 - 룸 없음";
                        statusText.color = Color.yellow;
                    }
                }
                else
                {
                    statusText.text = "연결 안됨";
                    statusText.color = Color.red;
                }
            }
            
            // 플레이어 수 텍스트 업데이트
            if (playerCountText != null)
            {
                if (PhotonNetwork.InRoom)
                {
                    playerCountText.text = $"플레이어: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
                }
                else
                {
                    playerCountText.text = "플레이어: 0/0";
                }
            }
            
            // 룸 정보 텍스트 업데이트
            if (roomInfoText != null)
            {
                if (PhotonNetwork.InRoom)
                {
                    roomInfoText.text = $"마스터: {(PhotonNetwork.IsMasterClient ? "나" : "다른사람")}";
                }
                else
                {
                    roomInfoText.text = "";
                }
            }
            
            // 버튼 활성화/비활성화
            if (connectButton != null)
                connectButton.interactable = !PhotonNetwork.IsConnected;
            if (createRoomButton != null)
                createRoomButton.interactable = PhotonNetwork.IsConnected && !PhotonNetwork.InRoom;
            if (startGameButton != null)
                startGameButton.interactable = PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom;
            if (spawnItemButton != null)
                spawnItemButton.interactable = PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom;
        }
        
        private void OnConnectButtonClick()
        {
            if (tester != null)
            {
                tester.ConnectToPhoton();
            }
            else
            {
                Debug.LogError("ReceiveGameTester를 찾을 수 없습니다!");
            }
        }
        
        private void OnCreateRoomButtonClick()
        {
            if (tester != null)
            {
                tester.CreateOrJoinRoom();
            }
            else
            {
                Debug.LogError("ReceiveGameTester를 찾을 수 없습니다!");
            }
        }
        
        private void OnStartGameButtonClick()
        {
            if (gameManager != null)
            {
                gameManager.StartGame();
            }
            else
            {
                Debug.LogError("ReceiveGameManager를 찾을 수 없습니다!");
            }
        }
        
        private void OnSpawnItemButtonClick()
        {
            if (gameManager != null)
            {
                // 수동으로 아이템 스폰
                Vector3 randomPosition = new Vector3(
                    Random.Range(-10f, 10f),
                    15f, // 하늘에서 떨어지는 높이
                    Random.Range(-10f, 10f)
                );
                
                // 리플렉션을 사용하여 private 메서드 호출
                var spawnItemMethod = typeof(ReceiveGameManager).GetMethod("SpawnItem", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (spawnItemMethod != null)
                {
                    spawnItemMethod.Invoke(gameManager, new object[] { randomPosition });
                    Debug.Log($"수동 아이템 스폰: {randomPosition}");
                }
            }
            else
            {
                Debug.LogError("ReceiveGameManager를 찾을 수 없습니다!");
            }
        }
    }
} 