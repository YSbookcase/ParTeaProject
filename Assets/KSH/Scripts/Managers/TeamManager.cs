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
            teamDict.Clear(); //초기화
            
            Player[] players = PhotonNetwork.PlayerList; //접속해있는 플레이어들의 배열로 저장
            List<Player> playerList = new List<Player>(players); //리스트로 변환

            for (int i = playerList.Count - 1; i > 0; i--) //Fisher-Yates Shuffle 알고리즘
            {
                int j = Random.Range(0, i + 1); //0에서 플레이어 수까지의 랜덤 지정
                Player temp = playerList[i]; // 플레이어 기존 값을 저장
                playerList[i] = playerList[j]; //랜덤으로 지정한 인덱스를 플레이어리스트 인덱스에 덮어씀
                playerList[j] = temp; //저장해둔 기존 값을 j값에 넣기
            }
            
            for (int i = 0; i < playerList.Count; i++)
            {
                int team = i % 2;
                teamDict[playerList[i].ActorNumber] = team; //딕셔너리에 저장
                
                ExitGames.Client.Photon.Hashtable playerProperty = new ExitGames.Client.Photon.Hashtable();
                playerProperty["Team"] = team; //Team 키에 팀 저장
                playerList[i].SetCustomProperties(playerProperty);
                Debug.Log($"{playerList[i].NickName} → 팀 {team}");
            }
        }
    }    
}
