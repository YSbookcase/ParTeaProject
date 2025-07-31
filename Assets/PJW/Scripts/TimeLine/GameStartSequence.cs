using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace PJW
{
    [RequireComponent(typeof(PlayableDirector))]
    public class GameStartSequence : MonoBehaviour
    {
        [Header("Timeline Asset")]
        [SerializeField] private PlayableDirector director;
        [Header("Player Controller (비활성/활성 토글용)")]
        [SerializeField] private PlayerController playerController;

        private void Awake()
        {
            if (playerController != null)
                playerController.enabled = false;

            director.playOnAwake = false;
            director.stopped += OnTimelineEnded;
        }

        private void Start()
        {
            director.Play();
        }

        private void OnTimelineEnded(PlayableDirector pd)
        {
            if (playerController != null)
                playerController.enabled = true;
        }
    }
}
