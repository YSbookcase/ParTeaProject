using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class ArenaPlayerPropertiesDebug : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        PrintAllPlayerProperties();
    }

    private void PrintAllPlayerProperties()
    {
        Debug.Log("== [Photon] 현재 플레이어들의 CustomProperties 출력 ==");

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            Debug.Log($"플레이어 이름: {player.NickName} (ActorNumber: {player.ActorNumber})");

            foreach (var key in player.CustomProperties.Keys)
            {
                object value = player.CustomProperties[key];
                Debug.Log($"  - {key}: {value}");
            }
        }
    }
}