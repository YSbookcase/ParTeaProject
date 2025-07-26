using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum MapType
{
    City, Natural, Game
}

namespace KYS
{
    public class RoomListItem : BaseUI
    {
        // SerializeField 대신 BaseUI 방식 사용
        private TextMeshProUGUI roomNameText => GetUI<TextMeshProUGUI>("RoomNameText");
        private TextMeshProUGUI playerCountText => GetUI<TextMeshProUGUI>("PlayerCountText");
        private TextMeshProUGUI mapText => GetUI<TextMeshProUGUI>("MapText");

        private string roomName;

        private new void Awake()
        {
            base.Awake();
            
            // 이벤트 연결 (자식 버튼 또는 자기 자신 버튼)
            var joinButtonEvent = GetEvent("JoinButton");
            if (joinButtonEvent != null)
            {
                joinButtonEvent.Click += JoinRoom;
                Debug.Log("[RoomListItem] JoinButton 자식 오브젝트에 이벤트 연결 완료");
            }
            else
            {
                // 자기 자신에 Button 컴포넌트가 있는지 확인
                var selfButton = GetComponent<Button>();
                if (selfButton != null)
                {
                    // 자기 자신이 버튼인 경우
                    selfButton.onClick.AddListener(() => JoinRoom(null));
                    Debug.Log("[RoomListItem] 자기 자신의 Button 컴포넌트에 이벤트 연결 완료");
                }
                else
                {
                    Debug.LogError("[RoomListItem] JoinButton 자식 오브젝트도 없고, 자기 자신에도 Button 컴포넌트가 없습니다.");
                }
            }
        }

        public void Init(RoomInfo info)
        {
            roomName = info.Name;
            roomNameText.text = $"Room Name : {roomName}";
            playerCountText.text = $"{info.PlayerCount} / {info.MaxPlayers}";
            mapText.text = $"Map : {(MapType)info.CustomProperties["Map"]}";
        }

        private void JoinRoom(PointerEventData eventData)
        {
            Debug.Log($"[RoomListItem] 방 참가 시도: {roomName}");
            
            if (PhotonNetwork.InLobby)
            {
                // 방 참가 버튼 비활성화 (중복 클릭 방지)
                var joinButton = GetUI<Button>("JoinButton");
                if (joinButton != null)
                {
                    joinButton.interactable = false;
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