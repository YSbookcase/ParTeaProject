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
            
            // 이벤트 연결
            GetEvent("JoinButton").Click += JoinRoom;
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
            if (PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinRoom(roomName);
            }
        }
    }
}