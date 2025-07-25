using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
namespace KSH
{
    public class UIManager : MonoBehaviour
    {
        [Header("점수 관련 텍스트")]
        [SerializeField] private TextMeshProUGUI redText;
        [SerializeField] private TextMeshProUGUI blueText;
        [Header("시간 관련 텍스트")]
        [SerializeField] private TextMeshProUGUI timerText;

        void Start()
        {
            if (TileManager.Instance != null)
                TileManager.Instance.OnTileCount += TileUIUpdate;
            
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStart += TimerUIUpdate;
        }

        void OnDisable()
        {
            if(TileManager.Instance != null)
                TileManager.Instance.OnTileCount -= TileUIUpdate;
            
            if(GameManager.Instance != null)
                GameManager.Instance.OnGameStart -= TimerUIUpdate;
        }

        private void TileUIUpdate(int red, int blue) //팀 점수 UI 업데이트
        {
            redText.text = $"RedTeam : {red}";
            blueText.text = $"BlueTeam : {blue}";
        }
        
        private void TimerUIUpdate() //시간 업데이트
        {
            int minutes = (int)GameManager.Instance.Timer / 60;
            int seconds = (int)GameManager.Instance.Timer % 60;
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
}
