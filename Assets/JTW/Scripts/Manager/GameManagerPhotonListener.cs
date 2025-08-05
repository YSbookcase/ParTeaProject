using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManagerPhotonListener : MonoBehaviourPunCallbacks
{
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable properties)
    {
        if (properties.ContainsKey("curGameCount"))
        {
            Manager.game.curGameCount = (int)properties["curGameCount"];
        }

        if (properties.ContainsKey("maxGameCount"))
        {
            Manager.game.maxGameCount = (int)properties["maxGameCount"];
        }

        if (properties.ContainsKey("remainingGameList"))
        {
            Manager.game.remainingGameList = ((string[])properties["remainingGameList"]).ToList();
        }

        if (properties.ContainsKey("remainingTeamGameList"))
        {
            Manager.game.remainingTeamGameList = ((string[])properties["remainingTeamGameList"]).ToList();
        }
    }
}
