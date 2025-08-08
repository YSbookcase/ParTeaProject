using System.Collections;
using System.Collections.Generic;
using KSH;
using UnityEngine;

namespace KSH
{
    public class JumpPad : Obstacle
    { 
        [SerializeField] private float jumpForce;
        private bool isBgm = false;
        protected override void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                PlayerController player = collision.gameObject.GetComponent<PlayerController>();
                if (player != null)
                {
                    Debug.Log("JumpPad");
                    if (!isBgm)
                    {
                        Manager.Audio.SfxPlay("KSH_Jump");
                        isBgm = true;
                    }
                    isBgm = false;
                    player.Bounce(jumpForce);
                }
            }
        }
    }    
}
