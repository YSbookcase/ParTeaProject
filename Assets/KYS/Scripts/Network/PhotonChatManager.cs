using Photon.Pun;
using Photon.Chat;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExitGames.Client.Photon;

namespace KYS
{
    public class PhotonChatManager : SingtonPunCallback<PhotonChatManager>, IChatClientListener
    {
        [Header("Chat Settings")]
        [SerializeField] private string chatAppId;
        
        private ChatClient chatClient;
        private string currentChannelName;
        private bool isConnected = false;
        
        // 이벤트들
        public System.Action<string, string> OnChatMessageReceived;
        public System.Action<string> OnChatConnected;
        public System.Action<string> OnChatDisconnected;
        public System.Action<string> OnChatError;
        
        private void Awake()
        {
            InitializeChatClient();
        }
        
        private void Start()
        {
            Debug.Log("[PhotonChatManager] Start 호출");
            
            // PhotonManager가 연결되면 자동으로 채팅도 연결
            StartCoroutine(WaitForPhotonManagerAndConnect());
        }
        
        private IEnumerator WaitForPhotonManagerAndConnect()
        {
            Debug.Log("[PhotonChatManager] WaitForPhotonManagerAndConnect 시작");
            
            // PhotonManager가 준비될 때까지 대기
            while (PhotonManager.Instance == null)
            {
                Debug.Log("[PhotonChatManager] PhotonManager 대기 중...");
                yield return new WaitForSeconds(0.5f);
            }
            
            Debug.Log("[PhotonChatManager] PhotonManager 준비됨");
            
            // PhotonManager가 연결될 때까지 대기
            while (!PhotonNetwork.IsConnected)
            {
                Debug.Log("[PhotonChatManager] Photon 연결 대기 중...");
                yield return new WaitForSeconds(0.5f);
            }
            
            Debug.Log("[PhotonChatManager] Photon 연결됨, 채팅 연결 시도");
            
            // 채팅 연결 전에 잠시 대기
            yield return new WaitForSeconds(1f);
            
            // 채팅 연결
            ConnectToChat();
        }
        
        private void Update()
        {
            // ChatClient 서비스 호출
            if (chatClient != null)
            {
                chatClient.Service();
            }
        }
        
        private void InitializeChatClient()
        {
            Debug.Log("[PhotonChatManager] InitializeChatClient 시작");
            
            // Photon Chat App ID 설정
            if (string.IsNullOrEmpty(chatAppId))
            {
                // PhotonServerSettings에서 Chat App ID 가져오기
                if (PhotonNetwork.PhotonServerSettings != null && PhotonNetwork.PhotonServerSettings.AppSettings != null)
                {
                    chatAppId = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat;
                    if (string.IsNullOrEmpty(chatAppId))
                    {
                        // Chat App ID가 없으면 Realtime App ID 사용
                        chatAppId = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime;
                    }
                }
                else
                {
                    Debug.LogError("[PhotonChatManager] PhotonServerSettings를 찾을 수 없습니다!");
                    return;
                }
            }
            
            Debug.Log($"[PhotonChatManager] Chat App ID: {chatAppId}");
            
            if (string.IsNullOrEmpty(chatAppId))
            {
                Debug.LogError("[PhotonChatManager] Chat App ID를 설정할 수 없습니다!");
                return;
            }
            
            // ChatClient 초기화
            chatClient = new ChatClient(this);
            chatClient.ChatRegion = "eu"; // 유럽 서버 사용 (한국 서버 사용 불가)
            
            Debug.Log($"[PhotonChatManager] ChatClient 초기화 완료 - State: {chatClient.State}");
        }
        
        public void ConnectToChat()
        {
            Debug.Log($"[PhotonChatManager] ConnectToChat 호출 - chatClient: {chatClient != null}, isConnected: {isConnected}, chatAppId: {chatAppId}");
            
            if (chatClient == null)
            {
                Debug.Log("[PhotonChatManager] chatClient가 null입니다. 초기화합니다.");
                InitializeChatClient();
            }
            
            // chatClient가 여전히 null인지 확인
            if (chatClient == null)
            {
                Debug.LogError("[PhotonChatManager] chatClient 초기화 실패!");
                return;
            }
            
            // 연결 상태 확인
            if (chatClient.State == ChatState.Disconnected && !string.IsNullOrEmpty(chatAppId))
            {
                string nickname = PhotonNetwork.NickName;
                if (string.IsNullOrEmpty(nickname))
                {
                    nickname = $"Player_{Random.Range(1000, 9999)}";
                }
                
                Debug.Log($"[PhotonChatManager] 채팅 서버에 연결 중... (AppID: {chatAppId}, 닉네임: {nickname})");
                chatClient.Connect(chatAppId, nickname, null);
            }
            else
            {
                if (chatClient.State != ChatState.Disconnected)
                {
                    Debug.Log($"[PhotonChatManager] 이미 연결 중이거나 연결됨 (State: {chatClient.State})");
                }
                else if (string.IsNullOrEmpty(chatAppId))
                {
                    Debug.LogError("[PhotonChatManager] chatAppId가 비어있습니다!");
                }
            }
        }
        
        public void DisconnectFromChat()
        {
            if (chatClient != null && isConnected)
            {
                Debug.Log("[PhotonChatManager] 채팅 서버에서 연결 해제");
                chatClient.Disconnect();
            }
        }
        
        public void JoinChatChannel(string channelName)
        {
            Debug.Log($"[PhotonChatManager] JoinChatChannel 호출 - channelName: {channelName}, chatClient: {chatClient != null}, State: {chatClient?.State}");
            
            if (chatClient != null && chatClient.State == ChatState.ConnectedToFrontEnd)
            {
                currentChannelName = channelName;
                Debug.Log($"[PhotonChatManager] 채널 '{channelName}'에 참가");
                chatClient.Subscribe(new string[] { channelName });
            }
            else
            {
                Debug.LogWarning($"[PhotonChatManager] 채널 참가 실패 - chatClient: {chatClient != null}, State: {chatClient?.State}");
                
                // 연결되지 않은 경우 연결 시도
                if (chatClient != null && chatClient.State == ChatState.Disconnected)
                {
                    Debug.Log("[PhotonChatManager] 연결되지 않았습니다. 연결 후 채널 참가를 시도합니다.");
                    ConnectToChat();
                    // 연결 후 채널 참가를 위해 코루틴 사용
                    StartCoroutine(JoinChannelAfterConnection(channelName));
                }
                else if (chatClient == null)
                {
                    Debug.Log("[PhotonChatManager] chatClient가 null입니다. 초기화 후 연결을 시도합니다.");
                    InitializeChatClient();
                    ConnectToChat();
                    StartCoroutine(JoinChannelAfterConnection(channelName));
                }
            }
        }
        
        private IEnumerator JoinChannelAfterConnection(string channelName)
        {
            // 연결될 때까지 대기
            float timeout = 15f;
            float elapsed = 0f;
            
            while (chatClient != null && chatClient.State != ChatState.ConnectedToFrontEnd && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
                Debug.Log($"[PhotonChatManager] 연결 대기 중... State: {chatClient?.State}, 경과: {elapsed}s");
            }
            
            if (chatClient != null && chatClient.State == ChatState.ConnectedToFrontEnd)
            {
                Debug.Log($"[PhotonChatManager] 연결 완료. 채널 '{channelName}'에 참가합니다.");
                currentChannelName = channelName;
                chatClient.Subscribe(new string[] { channelName });
            }
            else
            {
                Debug.LogError($"[PhotonChatManager] 연결 시간 초과. 채널 참가 실패. 최종 State: {chatClient?.State}");
            }
        }
        
        public void LeaveChatChannel()
        {
            if (chatClient != null && isConnected && !string.IsNullOrEmpty(currentChannelName))
            {
                Debug.Log($"[PhotonChatManager] 채널 '{currentChannelName}'에서 나감");
                chatClient.Unsubscribe(new string[] { currentChannelName });
                currentChannelName = null;
            }
        }
        
        public void SendChatMessage(string message)
        {
            Debug.Log($"[PhotonChatManager] SendChatMessage 호출 - chatClient: {chatClient != null}, State: {chatClient?.State}, currentChannel: {currentChannelName}");
            
            if (chatClient != null && chatClient.State == ChatState.ConnectedToFrontEnd && !string.IsNullOrEmpty(currentChannelName))
            {
                Debug.Log($"[PhotonChatManager] 메시지 전송: {message}");
                chatClient.PublishMessage(currentChannelName, message);
            }
            else
            {
                Debug.LogWarning($"[PhotonChatManager] 채팅이 연결되지 않았거나 채널에 참가하지 않았습니다. (chatClient: {chatClient != null}, State: {chatClient?.State}, currentChannel: {currentChannelName})");
                
                // 연결 상태 확인 및 재연결 시도
                if (chatClient == null)
                {
                    Debug.Log("[PhotonChatManager] chatClient가 null입니다. 재초기화 시도...");
                    InitializeChatClient();
                }
                
                if (chatClient != null && chatClient.State == ChatState.Disconnected)
                {
                    Debug.Log("[PhotonChatManager] 연결되지 않았습니다. 재연결 시도...");
                    ConnectToChat();
                }
                
                if (string.IsNullOrEmpty(currentChannelName))
                {
                    Debug.Log("[PhotonChatManager] 채널에 참가하지 않았습니다. 채널 참가 시도...");
                    if (PhotonNetwork.InRoom)
                    {
                        JoinChatChannel(PhotonNetwork.CurrentRoom.Name);
                    }
                }
                
                // 연결이 완료될 때까지 잠시 대기 후 다시 시도
                StartCoroutine(RetrySendMessageAfterConnection(message));
            }
        }
        
        private IEnumerator RetrySendMessageAfterConnection(string message)
        {
            // 연결될 때까지 대기
            float timeout = 10f;
            float elapsed = 0f;
            
            while (chatClient.State != ChatState.ConnectedToFrontEnd && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
                Debug.Log($"[PhotonChatManager] 연결 대기 중... State: {chatClient.State}, 경과: {elapsed}s");
            }
            
            if (chatClient.State == ChatState.ConnectedToFrontEnd && !string.IsNullOrEmpty(currentChannelName))
            {
                Debug.Log($"[PhotonChatManager] 연결 완료. 메시지 재전송: {message}");
                chatClient.PublishMessage(currentChannelName, message);
            }
            else
            {
                Debug.LogError($"[PhotonChatManager] 연결 시간 초과. 메시지 전송 실패. 최종 State: {chatClient.State}");
            }
        }
        
        // IChatClientListener 구현
        public void DebugReturn(DebugLevel level, string message)
        {
            switch (level)
            {
                case DebugLevel.ERROR:
                    Debug.LogError($"[PhotonChatManager] {message}");
                    OnChatError?.Invoke(message);
                    break;
                case DebugLevel.WARNING:
                    Debug.LogWarning($"[PhotonChatManager] {message}");
                    break;
                case DebugLevel.INFO:
                    Debug.Log($"[PhotonChatManager] {message}");
                    break;
            }
        }
        
        public void OnDisconnected()
        {
            isConnected = false;
            Debug.Log("[PhotonChatManager] 채팅 서버에서 연결 해제됨");
            OnChatDisconnected?.Invoke("연결 해제됨");
        }
        
        public new void OnConnected()
        {
            isConnected = true;
            Debug.Log("[PhotonChatManager] 채팅 서버에 연결됨");
            OnChatConnected?.Invoke("연결됨");
        }
        
        public void OnGetMessages(string channelName, string[] senders, object[] messages)
        {
            for (int i = 0; i < senders.Length; i++)
            {
                string sender = senders[i];
                string message = messages[i] as string;
                
                Debug.Log($"[PhotonChatManager] 메시지 수신: {sender} -> {message}");
                OnChatMessageReceived?.Invoke(sender, message);
            }
        }
        
        public void OnPrivateMessage(string sender, object message, string channelName)
        {
            Debug.Log($"[PhotonChatManager] 개인 메시지 수신: {sender} -> {message}");
            OnChatMessageReceived?.Invoke(sender, message as string);
        }
        
        public void OnSubscribed(string[] channels, bool[] results)
        {
            for (int i = 0; i < channels.Length; i++)
            {
                if (results[i])
                {
                    Debug.Log($"[PhotonChatManager] 채널 '{channels[i]}' 구독 성공");
                }
                else
                {
                    Debug.LogError($"[PhotonChatManager] 채널 '{channels[i]}' 구독 실패");
                }
            }
        }
        
        public void OnUnsubscribed(string[] channels)
        {
            foreach (string channel in channels)
            {
                Debug.Log($"[PhotonChatManager] 채널 '{channel}' 구독 해제");
            }
        }
        
        public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
        {
            Debug.Log($"[PhotonChatManager] 사용자 상태 업데이트: {user} -> {status}");
        }
        
        public void OnUserSubscribed(string channel, string user)
        {
            Debug.Log($"[PhotonChatManager] 사용자 '{user}'가 채널 '{channel}'에 참가");
        }
        
        public void OnUserUnsubscribed(string channel, string user)
        {
            Debug.Log($"[PhotonChatManager] 사용자 '{user}'가 채널 '{channel}'에서 나감");
        }
        
        public void OnChatStateChange(ChatState state)
        {
            Debug.Log($"[PhotonChatManager] 채팅 상태 변경: {state}");
            
            switch (state)
            {
                case ChatState.ConnectedToNameServer:
                    Debug.Log("[PhotonChatManager] 이름 서버에 연결됨");
                    break;
                case ChatState.ConnectingToNameServer:
                    Debug.Log("[PhotonChatManager] 이름 서버에 연결 중...");
                    break;
                case ChatState.Disconnected:
                    Debug.Log("[PhotonChatManager] 채팅 연결 해제됨");
                    isConnected = false;
                    OnChatDisconnected?.Invoke("연결 해제됨");
                    break;
                case ChatState.ConnectedToFrontEnd:
                    Debug.Log("[PhotonChatManager] 프론트엔드에 연결됨");
                    isConnected = true;
                    OnChatConnected?.Invoke("연결됨");
                    break;
                case ChatState.ConnectingToFrontEnd:
                    Debug.Log("[PhotonChatManager] 프론트엔드에 연결 중...");
                    break;
                default:
                    Debug.Log($"[PhotonChatManager] 기타 상태: {state}");
                    break;
            }
        }
        
        private void OnDestroy()
        {
            DisconnectFromChat();
        }
        
        // 연결 상태 확인
        public bool IsConnected => isConnected;
        public string CurrentChannel => currentChannelName;
    }
} 