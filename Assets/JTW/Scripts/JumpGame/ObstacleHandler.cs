using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JTW_JumpGame
{
    public class ObstacleHandler : MonoBehaviour
    {
        private Rigidbody rigid;

        private Vector3 direction = Vector3.zero;
        private float speed;

        private void Start()
        {
            rigid = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            rigid.velocity = direction * speed;
            rigid.angularVelocity = Vector3.forward * -(speed / 0.5f);
        }

        public void Init(Vector3 direction, float speed)
        {
            this.direction = direction;
            this.speed = speed;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Respawn"))
            {
                Destroy(gameObject);
            }
        }
    }
}

