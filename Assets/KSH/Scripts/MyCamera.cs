using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace KSH
{
    public class MyCamera : MonoBehaviourPun
    {
        [SerializeField] private GameObject player;
        [SerializeField] private float offsetX;
        [SerializeField] private float offsetY;
        [SerializeField] private float offsetZ;
    
        [SerializeField] private float CameraSpeed;
        private Vector3 TargetPos;

        private void FixedUpdate()
        {
            TargetPos = new Vector3(player.transform.position.x + offsetX, player.transform.position.y + offsetY, player.transform.position.z + offsetZ);
        
            transform.position = Vector3.Lerp(transform.position, TargetPos, CameraSpeed * Time.deltaTime);    
        }

        public void SetTarget(GameObject target)
        {
            player = target;       
        }

    }    
}
