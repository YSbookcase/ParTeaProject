using UnityEngine;
using System.Collections.Generic;

namespace KYS
{
    [System.Serializable]
    public class GameInfo
    {
        public string gameName;        // 게임 이름
        public string sceneName;       // 씬 이름
        public string description;     // 게임 설명
        public List<int> requiredPlayers;    // 필요한 플레이어 수 (릴레이 조건)

        public GameInfo(string name, string scene, string desc)
        {
            gameName = name;
            sceneName = scene;
            description = desc;
            requiredPlayers = new List<int>(); // 기본값: 제한 없음
        }

        public GameInfo(string name, string scene, string desc, List<int> players)
        {
            gameName = name;
            sceneName = scene;
            description = desc;
            requiredPlayers = players;
        }
    }
} 