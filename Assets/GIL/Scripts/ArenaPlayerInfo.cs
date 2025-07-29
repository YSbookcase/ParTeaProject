using System.Collections;
using System.Collections.Generic;

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

            foreach (DictionaryEntry entry in customProperties)
            {
                CustomProperties[entry.Key.ToString()] = entry.Value;
            }
        }
    }
}