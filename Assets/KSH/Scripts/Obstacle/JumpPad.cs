using System.Collections;
using System.Collections.Generic;
using KSH;
using UnityEngine;

namespace KSH
{
    public class JumpPad : Obstacle
    { 
        [SerializeField] private float jumpForce;
        protected override void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                PlayerController player = collision.gameObject.GetComponent<PlayerController>();
                if (player != null)
                {
                    Debug.Log("JumpPad");
                    player.Bounce(jumpForce);
                }
            }
        }
    }    
}
