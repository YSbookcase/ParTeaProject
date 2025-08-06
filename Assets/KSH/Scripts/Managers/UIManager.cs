using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using DG.Tweening;
using Photon.Pun;
using Photon.Realtime;
using DG.Tweening;

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
        [Header("팀 관련 텍스트")] 
        [SerializeField] private Transform redTeamPanel;
        [SerializeField] private Transform blueTeamPanel;
        [SerializeField] private GameObject teamNickName;
        [SerializeField] private Image vsImage;
        
        [SerializeField] private RectTransform titleRect;
        [SerializeField] private RectTransform winnerRect;
        [SerializeField] private RectTransform teamRect;
        
        public event Action OnCountDownStart;
        public event Action OnCountDownEnd;
        public static UIManager Instance;
        private Vector2 startPos;
        private Vector2 targetPos;
        private bool isTitle = false;
        private bool isWin = false;
        private bool isCountDown = false;
        
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

            OnCountDownStart += TeamNicknameUpdate;
            
            winnerPanel.SetActive(false);
            redTeamPanel.gameObject.SetActive(false);
            blueTeamPanel.gameObject.SetActive(false);
            redText.gameObject.SetActive(false);
            blueText.gameObject.SetActive(false);
            vsImage.gameObject.SetActive(false);
            
            targetPos = titleRect.anchoredPosition;
            startPos = targetPos + Vector2.up * 1000f;
            titleRect.anchoredPosition = startPos;
            
            if (!isTitle)
            {
                Manager.Audio.SfxPlay("KSH_Title");
                isTitle = true;
                UIEffect(titleRect);
            }
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
            OnCountDownStart -= TeamNicknameUpdate;
        }

        private void TileUIUpdate(int red, int blue) //팀 점수 UI 업데이트
        {
            redText.text = $"{red}";
            blueText.text = $"{blue}";
        }
        
        private void TimerUIUpdate() //시간 업데이트
        {
            int minutes = (int)GameManager.Instance.Timer / 60;
            int seconds = (int)GameManager.Instance.Timer % 60;
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        private void TileCheck()
        {
            redTeamPanel.gameObject.SetActive(false);
            blueTeamPanel.gameObject.SetActive(false);
            redText.gameObject.SetActive(false);
            blueText.gameObject.SetActive(false);
            vsImage.gameObject.SetActive(false);
            winnerPanel.SetActive(true);
            
            int redTile = TileManager.Instance.redTileCount;
            int blueTile = TileManager.Instance.blueTileCount;

            if (redTile > blueTile)
                WinnerUIUpdate("레드팀 우승!", Color.red);
            else if(blueTile > redTile)
                WinnerUIUpdate("블루팀 우승!", Color.blue);
            else
                WinnerUIUpdate("무승부!", Color.green);
            
        }

        private void WinnerUIUpdate(string msg, Color winnercolor)
        {
            winnerText.text = msg;
            winnerPanelImage.color = winnercolor;
            
            UIEffect(winnerRect);
            if(!isWin)
            {
                Manager.Audio.SfxPlay("KSH_Win");
                isWin = true;
            }
        }

        public void StartCountDown()
        {
            StartCoroutine(CountDown());
        }

        private IEnumerator CountDown()
        {
            yield return new WaitForSeconds(5f);
            titleRect.gameObject.SetActive(false);
            redTeamPanel.gameObject.SetActive(true);
            blueTeamPanel.gameObject.SetActive(true);
            vsImage.gameObject.SetActive(true);
            TeamUIEffect();
            OnCountDownStart?.Invoke();
            yield return new WaitForSeconds(1f);
            countDownPanal.SetActive(true);
            
            for (int i = 3; i >= 0; i--)
            {
                countDownText.text = i.ToString();
                if (!isCountDown)
                {
                    Manager.Audio.SfxPlay("KSH_CountDown");
                    isCountDown = true;
                }
                isCountDown = false;
                yield return new WaitForSeconds(1f);
            }
            countDownText.text = "GO!";
            yield return new WaitForSeconds(1f);
            
            countDownPanal.SetActive(false);
            redText.gameObject.SetActive(true);
            blueText.gameObject.SetActive(true);
            vsImage.gameObject.SetActive(true);
            OnCountDownEnd?.Invoke();
            
        }

        private void TeamNicknameUpdate()
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                int team = player.CustomProperties.ContainsKey("Team") ? (int)player.CustomProperties["Team"] : -1;

                GameObject gameObject = Instantiate(teamNickName);
                TextMeshProUGUI textMeshProUGUI = gameObject.GetComponent<TextMeshProUGUI>();
                textMeshProUGUI.text = player.NickName;
                
                if (team == 0)
                    gameObject.transform.SetParent(redTeamPanel, false);
                else if (team == 1)
                    gameObject.transform.SetParent(blueTeamPanel, false);;
            }
        }

        private void UIEffect(RectTransform rect)
        {
            Sequence seq = DOTween.Sequence();

            seq.Append(rect.DOAnchorPosY(targetPos.y, 1.2f))
                .SetEase(Ease.OutBounce);
        }

        private void TeamUIEffect()
        {
            teamRect.anchoredPosition = new Vector2(-1920, 0);
            teamRect.DOAnchorPosX(0, 1.2f).SetEase(Ease.OutCubic);
        }
    }
}
