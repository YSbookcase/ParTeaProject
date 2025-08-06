using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class BgmAudio : MonoBehaviour
    {
        [SerializeField] private string bgmClipName = "MainBGM";

        [SerializeField] private float fadeDuration = 1f;

        private void Start()
        {
            if (!string.IsNullOrEmpty(bgmClipName))
            {
                AudioManager.Instance.BgmPlay(bgmClipName, fadeDuration);
            }
        }
    }
}
