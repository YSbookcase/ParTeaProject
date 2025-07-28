using UnityEngine;

namespace KYS
{
    [System.Serializable]
    public class GameInfo
    {
        public string gameName;        // 게임 이름
        public string sceneName;       // 씬 이름
        public string description;     // 게임 설명

        public GameInfo(string name, string scene, string desc)
        {
            gameName = name;
            sceneName = scene;
            description = desc;
        }
    }
} 