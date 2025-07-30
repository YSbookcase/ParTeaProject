using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
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
        [SerializeField] private GameObject countDownPanal;
        [SerializeField] private TextMeshProUGUI countDownText;
        
        public event Action OnCountDownEnd;
        public static UIManager Instance;
        
        private void Awake()
        {
            if(Instance == null) // Instance가 null이면
            {
                Instance = this; // 할당
            }
            else // 이미 존재한다면
            {
                Destroy(gameObject); // 하나만 존재해야 하므로 제거
            }
        }
        

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

        public void StartCountDown()
        {
            StartCoroutine(CountDown());
        }

        private IEnumerator CountDown()
        {
            countDownPanal.SetActive(true);
            
            for (int i = 3; i >= 0; i--)
            {
                countDownText.text = i.ToString();
                yield return new WaitForSeconds(1f);
            }
            countDownText.text = "GO!";
            countDownPanal.SetActive(false);
            OnCountDownEnd?.Invoke();
        }
    }
}
