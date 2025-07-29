using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
            
            // 플레이어 색상 설정 (ActorNumber 기반)
            SetPlayerColor(actorNumber);
        }
        
        public void UpdateScore(int newScore)
        {
            currentScore = newScore;
            
            if (scoreText != null)
            {
                scoreText.text = currentScore.ToString();
                
                // 점수 업데이트 애니메이션
                StartCoroutine(ScoreUpdateAnimation());
            }
        }
        
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
        
        private void SetPlayerColor(int actorNumber)
        {
            if (playerColorImage != null)
            {
                Color[] playerColors = new Color[]
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
                
                int colorIndex = (actorNumber - 1) % playerColors.Length;
                playerColorImage.color = playerColors[colorIndex];
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
    }
} 