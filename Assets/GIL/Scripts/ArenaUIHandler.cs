using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace GIL.Scripts
{
    public class ArenaUIHandler : MonoBehaviourPunCallbacks
    {
        private Dictionary<object, object> _playerProperties = new();

        private void Start()
        {
            GetAllPlayerProperties();
        }

        private void GetAllPlayerProperties()
        {
            Debug.Log("== [Photon] 현재 플레이어들의 CustomProperties 출력 ==");

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                Debug.Log($"플레이어 이름: {player.NickName} (ActorNumber: {player.ActorNumber})");

                foreach (var key in player.CustomProperties.Keys)
                {
                    object value = player.CustomProperties[key];
                    Debug.Log($"  - {key}: {value}");
                    _playerProperties.Add(key, value);
                }
            }
        }
    }
}
