using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PJW
{
    public class RopeGameManager : MonoBehaviour
    {
        [SerializeField] private Text countdownText;

        private void Start()
        {
            StartCoroutine(StartCountdownRoutine());
        }

        private IEnumerator StartCountdownRoutine()
        {
            Time.timeScale = 0f;

            countdownText.gameObject.SetActive(true);

            countdownText.text = "3";
            yield return new WaitForSecondsRealtime(1f);

            countdownText.text = "2";
            yield return new WaitForSecondsRealtime(1f);

            countdownText.text = "1";
            yield return new WaitForSecondsRealtime(1f);

            countdownText.text = "시작!";
            yield return new WaitForSecondsRealtime(1f);

            countdownText.gameObject.SetActive(false);

            Time.timeScale = 1f;
            Debug.Log("게임 시작!");
        }
    }
}
