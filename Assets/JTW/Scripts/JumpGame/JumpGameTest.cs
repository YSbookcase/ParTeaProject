using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JTW_Test
{
    public class JumpGameTest : MonoBehaviourPunCallbacks
    {
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (changedProps.ContainsKey("isLoaded"))
            {
                Debug.Log($"{targetPlayer.NickName} 준비 상태 : {changedProps["isLoaded"]}");
            }
            else
            {
            }

            if (changedProps.ContainsKey("totalGameScore"))
            {
                Debug.Log($"{targetPlayer.NickName} 점수 : {changedProps["totalGameScore"]}");
            }
        }
    }
}

