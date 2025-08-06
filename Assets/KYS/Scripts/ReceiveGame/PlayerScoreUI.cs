using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

namespace KYS
{
    public class PlayerScoreUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Image playerColorImage;
        
        private int playerActorNumber;
        private string playerName;
        private int currentScore = 0;
        
        public void Initialize(string name, int actorNumber)
        {
            playerName = name;
            playerActorNumber = actorNumber;
            
            if (playerNameText != null)
            {
                playerNameText.text = name;
            }
            
            if (scoreText != null)
            {
                scoreText.text = "0";
            }
            
            // 플레이어 색상 설정 (룸에서 선택한 색상 사용)
            SetPlayerColor(actorNumber);
        }
        
        public void UpdateScore(int newScore)
        {
            currentScore = newScore;
            
            if (scoreText != null)
            {
                scoreText.text = currentScore.ToString();
                
                // 글자 크기 애니메이션 제거
                // StartCoroutine(ScoreUpdateAnimation());
            }
        }
        
        // 글자 크기 애니메이션 메서드 제거
        /*
        private System.Collections.IEnumerator ScoreUpdateAnimation()
        {
            // 점수 텍스트 크기 애니메이션
            Vector3 originalScale = scoreText.transform.localScale;
            Vector3 targetScale = originalScale * 1.2f;
            
            float duration = 0.2f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                scoreText.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }
            
            // 원래 크기로 복원
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                scoreText.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }
            
            scoreText.transform.localScale = originalScale;
        }
        */
        
        private void SetPlayerColor(int actorNumber)
        {
            if (playerColorImage != null)
            {
                // 해당 플레이어의 CustomProperties에서 색상 정보 가져오기
                Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
                if (player != null && player.CustomProperties.TryGetValue("Color", out object colorValue))
                {
                    if (colorValue != null)
                    {
                        int colorIndex = (int)colorValue;
                        Color[] playerColors = new Color[]
                        {
                            Color.red,      // 0: 빨강
                            Color.blue,     // 1: 파랑
                            Color.green,    // 2: 초록
                            Color.yellow    // 3: 노랑
                        };
                        
                        if (colorIndex >= 0 && colorIndex < playerColors.Length)
                        {
                            playerColorImage.color = playerColors[colorIndex];
                            //Debug.Log($"[PlayerScoreUI] 플레이어 {player.NickName}의 색상 설정: {colorIndex} -> {playerColors[colorIndex]}");
                            return;
                        }
                    }
                }
                
                // 색상 정보가 없거나 잘못된 경우 기본 색상 사용
                Color[] defaultColors = new Color[]
                {
                    Color.red,
                    Color.blue,
                    Color.green,
                    Color.yellow,
                    Color.magenta,
                    Color.cyan,
                    Color.white,
                    Color.gray
                };
                
                int defaultColorIndex = (actorNumber - 1) % defaultColors.Length;
                playerColorImage.color = defaultColors[defaultColorIndex];
                //Debug.Log($"[PlayerScoreUI] 플레이어 {playerName}의 기본 색상 설정: {defaultColorIndex} -> {defaultColors[defaultColorIndex]}");
            }
        }
        
        public int GetPlayerActorNumber()
        {
            return playerActorNumber;
        }
        
        public string GetPlayerName()
        {
            return playerName;
        }
        
        public int GetCurrentScore()
        {
            return currentScore;
        }
        
        // UI 요소들을 설정하는 public 메서드 추가
        public void SetUIElements(TextMeshProUGUI nameText, TextMeshProUGUI scoreText, Image colorImage)
        {
            playerNameText = nameText;
            this.scoreText = scoreText;
            playerColorImage = colorImage;
        }
    }
} 