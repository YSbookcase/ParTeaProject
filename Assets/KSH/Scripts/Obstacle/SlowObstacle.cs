using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KSH
{
    public class SlowObstacle : Obstacle
    {
        [SerializeField] private float slowFactor;
        protected override void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                PlayerController player = collision.gameObject.GetComponent<PlayerController>();
                if (player != null)
                {
                    Debug.Log("SlowObstacle");
                    player.Slow(slowFactor);
                }
            }
        }

        private void OnCollisionExit(Collision other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                PlayerController player = other.gameObject.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.ResetSpeed();
                }
            }
        }
    }    
}
