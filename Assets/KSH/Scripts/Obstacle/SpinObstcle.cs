using System.Collections;
using System.Collections.Generic;
using KSH;
using UnityEngine;

namespace KSH
{
    public class SpinObstcle : Obstacle
    {
        [SerializeField] private float spinSpeed;
        [SerializeField] private float spinForce;

        private void Update()
        {
            transform.Rotate(Vector3.up * spinSpeed * Time.deltaTime);
        }
        protected override void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
                PlayerController player = collision.gameObject.GetComponent<PlayerController>();
                if (player != null)
                {
                    Vector3 spinDir = (collision.transform.position - transform.position).normalized;
                    rb.AddForce(spinDir * spinForce, ForceMode.Impulse);
                }
            }
        }
    }
}

