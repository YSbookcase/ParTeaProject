using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
namespace KSH
{
    public class UIManager : MonoBehaviour
    {
        [Header("점수 관련 텍스트")]
        [SerializeField] private TextMeshProUGUI redText;
        [SerializeField] private TextMeshProUGUI blueText;
        [SerializeField] private GameObject winnerPanel;
        [SerializeField] private Image winnerPanelImage;
        [SerializeField] private TextMeshProUGUI winnerText;
        [Header("시간 관련 텍스트")]
        [SerializeField] private TextMeshProUGUI timerText;
        

        void Start()
        {
            if (TileManager.Instance != null)
                TileManager.Instance.OnTileCount += TileUIUpdate;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStart += TimerUIUpdate;
                GameManager.Instance.OnGameEnd += TileCheck;
            }
            
            winnerPanel.SetActive(false);
        }

        void OnDisable()
        {
            if(TileManager.Instance != null)
                TileManager.Instance.OnTileCount -= TileUIUpdate;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStart -= TimerUIUpdate;
                GameManager.Instance.OnGameEnd -= TileCheck;
            }
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

        private void TileCheck()
        {
            winnerPanel.SetActive(true);
            
            int redTile = TileManager.Instance.redTileCount;
            int blueTile = TileManager.Instance.blueTileCount;

            if (redTile > blueTile)
                WinnerUIUpdate("Winner Team : RedTeam!", Color.red);
            else if(blueTile > redTile)
                WinnerUIUpdate("Winner Team : BlueTeam!", Color.blue);
            else
                WinnerUIUpdate("Draw", Color.green);
            
        }

        private void WinnerUIUpdate(string msg, Color winnercolor)
        {
            winnerText.text = msg;
            winnerPanelImage.color = winnercolor;
        }
    }
}
