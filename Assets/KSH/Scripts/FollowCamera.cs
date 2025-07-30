using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

namespace KSH
{
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] private CinemachineVirtualCamera virtualCam;

        public void SetCameraTarget(Transform player)
        {
            virtualCam.Follow = player;
            virtualCam.LookAt = player;
        }
    }    
}
