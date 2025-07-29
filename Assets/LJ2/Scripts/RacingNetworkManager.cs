using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RacingNetworkManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private RacingMap racingMap;

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinOrCreateRoom("RacingTest", new RoomOptions { MaxPlayers = 4 }, new TypedLobbyInfo());
    }

    public override void OnCreatedRoom()
    {
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("입장 완료");
        PhotonNetwork.LocalPlayer.NickName = $"Player_{PhotonNetwork.LocalPlayer.ActorNumber}";
        PlayerSpawn();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (newMasterClient == PhotonNetwork.LocalPlayer)
        {

        }
    }

    private void PlayerSpawn()
    {
        racingMap.SetTrack(2);
        racingMap.SetDollyCart(racingMap.startLine);
        Transform spawnPos = racingMap.startLine.spawnPositions[3];
        PhotonNetwork.Instantiate("RacingPlayer", spawnPos.position, spawnPos.rotation);
    }
    public override void OnPlayerEnteredRoom(Player player)
    {
        Debug.Log($"{player.NickName} 입장 완료");
    }
}
