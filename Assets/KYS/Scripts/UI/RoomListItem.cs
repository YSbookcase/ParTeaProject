using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum GameType
{
    Jump,       // 점프 게임
    Arena,      // 아레나 게임
    Tile,       // 타일 게임
    Racing,     // 레이싱 게임
    Rope,       // 로프 게임
    Receive     // 받기 게임
}

namespace KYS
{
    public class RoomListItem : BaseUI
    {
        // SerializeField 대신 BaseUI 방식 사용
        private TextMeshProUGUI roomNameText => GetUI<TextMeshProUGUI>("RoomNameText");
        private TextMeshProUGUI playerCountText => GetUI<TextMeshProUGUI>("PlayerCountText");
        private TextMeshProUGUI gameText => GetUI<TextMeshProUGUI>("GameText");

        private string roomName;

        // 게임 이름 매핑 (RoomPopUp과 동일)
        private string[] gameNames = new string[]
        {
            "점프", "아레나", "타일", "레이싱", "로프", "받기", "4G 릴레이", "6G 릴레이"// 추가 게임이 있다면 여기에 추가
        };

        private new void Awake()
        {
            base.Awake();
            
            // 자기 자신에 Button 컴포넌트가 있는지 확인 (프리팹 자체가 버튼)
            var selfButton = GetComponent<Button>();
            if (selfButton != null)
            {
                // 자기 자신이 버튼인 경우
                selfButton.onClick.AddListener(() => JoinRoom(null));
                Debug.Log("[RoomListItem] 자기 자신의 Button 컴포넌트에 이벤트 연결 완료");
            }
            else
            {
                Debug.LogError("[RoomListItem] 자기 자신에 Button 컴포넌트가 없습니다.");
            }
        }

        public void Init(RoomInfo info)
        {
            roomName = info.Name;
            roomNameText.text = $"Room Name : {roomName}";
            playerCountText.text = $"{info.PlayerCount} / {info.MaxPlayers}";
            
            // 게임 정보 표시 (RoomPopUp과 동일한 로직)
            if (info.CustomProperties.TryGetValue("SelectedGame", out object gameValue))
            {
                int gameIndex = (int)gameValue;
                
                if (gameIndex >= 0 && gameIndex < gameNames.Length)
                {
                    string gameName = gameNames[gameIndex];
                    gameText.text = $"Game : {gameName}";
                }
                else
                {
                    gameText.text = "Game : 점프"; // 기본값
                    Debug.LogWarning($"[RoomListItem] 방 '{roomName}' 게임 인덱스가 범위를 벗어남: {gameIndex}");
                }
            }
            else
            {
                gameText.text = "Game : 점프"; // 기본값
                Debug.LogWarning($"[RoomListItem] 방 '{roomName}' SelectedGame 속성을 찾을 수 없음");
            }
        }



        private void JoinRoom(PointerEventData eventData)
        {
            Debug.Log($"[RoomListItem] 방 참가 시도: {roomName}");
            
            if (PhotonNetwork.InLobby)
            {
                // 방 참가 버튼 비활성화 (중복 클릭 방지)
                var selfButton = GetComponent<Button>();
                if (selfButton != null)
                {
                    selfButton.interactable = false;
                }
                
                PhotonManager.Instance.JoinRoom(roomName);
                Debug.Log($"[RoomListItem] 방 참가 요청 완료: {roomName}");
            }
            else
            {
                Debug.LogError("[RoomListItem] 로비에 있지 않아 방에 참가할 수 없습니다.");
            }
        }
    }
}