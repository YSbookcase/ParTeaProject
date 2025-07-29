using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Photon.Realtime;

namespace KSH
{
    public class TeamManager : MonoBehaviourPunCallbacks
    {
        public Dictionary<int, int> teamDict = new Dictionary<int, int>(); //ActorNumber와 TeamNumber
        public static TeamManager Instance;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        public void SetTeam()
        {
            Player[] players = PhotonNetwork.PlayerList; //접속해있는 플레이어들의 배열로 저장
            List<Player> playerList = new List<Player>(players); //리스트로 변환

            for (int i = 0; i < playerList.Count; i++)
            {
                Player player = playerList[i];
                int randomPlayer = Random.Range(0, playerList.Count);
                playerList[i] = playerList[randomPlayer];
                playerList[randomPlayer] = player;
            }

            int teamSize = (playerList.Count == 2) ? 1 : 2;
            
            for (int i = 0; i < playerList.Count; i++)
            {
                int team = (i < teamSize) ? 0 : 1; //인덱스가 0과 1이면 0팀, 2이상이면 1팀
                teamDict[playerList[i].ActorNumber] = team; //딕셔너리에 저장
                
                ExitGames.Client.Photon.Hashtable playerProperty = new ExitGames.Client.Photon.Hashtable();
                playerProperty["Team"] = team; //Team 키에 팀 저장
                playerList[i].SetCustomProperties(playerProperty);
            }
        }
    }    
}
