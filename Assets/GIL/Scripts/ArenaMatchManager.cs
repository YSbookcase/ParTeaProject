using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;

public class ArenaMatchManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text roomNameText;
    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
        startButton.onClick.AddListener(GameStart);
    }

    public override void OnConnectedToMaster()
    {
        RoomOptions roomOption = new RoomOptions
        {
            MaxPlayers = 4,
            IsVisible = true,
            IsOpen = true
        };
        PhotonNetwork.JoinOrCreateRoom(roomName: "Arena", roomOptions: roomOption, typedLobby: TypedLobby.Default);
    }

    public override void OnCreatedRoom()
    {
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
    }

    public override void OnJoinedRoom()
    {
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        PhotonNetwork.LocalPlayer.NickName = $"Player_{PhotonNetwork.LocalPlayer.ActorNumber}";
        Debug.Log($"{PhotonNetwork.LocalPlayer.NickName}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"{newPlayer.NickName} 입장");
    }

    private void GameStart()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("방장이 아니므로 게임을 시작할 수 없습니다.");
            return;
        }
        
        Debug.Log("방장이 게임을 시작합니다.");
        Manager.game.GameStart("ArenaScene");
    }
}
