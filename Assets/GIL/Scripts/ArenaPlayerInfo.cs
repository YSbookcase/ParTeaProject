using System.Collections.Generic;
using ExitGames.Client.Photon;

namespace GIL.Scripts
{
    public class PlayerInfo
    {
        public string NickName;
        public int ActorNumber;
        public Dictionary<string, object> CustomProperties;

        public PlayerInfo(string nickname, int actorNumber, Hashtable customProperties)
        {
            NickName = nickname;
            ActorNumber = actorNumber;
            CustomProperties = new Dictionary<string, object>();

            foreach (var key in customProperties.Keys)
            {
                CustomProperties[key.ToString()] = customProperties[key];
            }
        }
    }
}